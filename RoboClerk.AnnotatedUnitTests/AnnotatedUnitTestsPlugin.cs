using RoboClerk.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using Tomlyn.Model;
using TreeSitter;

namespace RoboClerk.AnnotatedUnitTests
{
    public class AnnotatedUnitTestPlugin : SourceCodeAnalysisPluginBase
    {
        private string decorationMarker = string.Empty;

        private Dictionary<string, UTInformation> information = new Dictionary<string, UTInformation>();

        public AnnotatedUnitTestPlugin(IFileSystem fileSystem)
            : base(fileSystem)
        {
            SetBaseParam();
        }

        private void SetBaseParam()
        {
            name = "AnnotatedUnitTestPlugin";
            description = "A plugin that analyzes a project's source code to extract unit test information for RoboClerk.";
        }

        private void PopulateUTInfo(string tableName, TomlTable config)
        {
            if (!config.ContainsKey(tableName))
            {
                throw new Exception($"A required table \"{tableName}\" is missing from the {tableName}.toml configuration file. Cannot continue.");
            }
            var tomlTable = (TomlTable)config[tableName];
            var info = new UTInformation();
            try
            {
                info.FromToml(tomlTable);
            }
            catch (Exception e)
            {
                throw new Exception($"{e.Message}\"{tableName}\"");
            }
            information[tableName] = info;
        }

        public override void InitializePlugin(IConfiguration configuration)
        {
            logger.Info("Initializing the Annotated Unit Tests Plugin");
            try
            {
                base.InitializePlugin(configuration);
                var config = GetConfigurationTable(configuration.PluginConfigDir, $"{name}.toml");

                decorationMarker = configuration.CommandLineOptionOrDefault("DecorationMarker", GetObjectForKey<string>(config, "DecorationMarker", true));

                PopulateUTInfo("Purpose", config);
                PopulateUTInfo("PostCondition", config);
                PopulateUTInfo("Identifier", config);
                PopulateUTInfo("TraceID", config);
            }
            catch (Exception e)
            {
                logger.Error("Error reading configuration file for Annotated Unit Test plugin.");
                logger.Error(e);
                throw new Exception("The Annotated Unit Test plugin could not read its configuration. Aborting...");
            }
            ScanDirectoriesForSourceFiles();
        }

        private void FindAndProcessAnnotations(string text, string filename)
        {
            // Use base class helper methods for TreeSitter operations
            var language = CreateLanguage("CSharp");
            using var tree = ParseSourceCode("CSharp", text);

            var queryText = @"
                (method_declaration
                  (attribute_list
                    (attribute
                      [(identifier) (qualified_name)] @attr.name
                      (#eq? @attr.name ""UnitTestAttribute"")
                    )
                  )
                  name: (identifier) @method
                ) @method_with_attr
                ";

            // Use base class helper method to execute query
            foreach (var match in ExecuteQuery(language, tree, queryText))
            {
                var methodNode = match.Captures.First(c => c.Name == "method").Node;
                var attrNameNode = match.Captures.First(c => c.Name == "attr.name").Node;

                // Get the entire attribute node to show its arguments too:
                // Walk up from attr.name to its parent 'attribute' to slice full text.
                var attributeNode = attrNameNode.Parent; // parent is 'attribute'
                
                // Use base class helper methods - provide text for compatibility
                var attrText = GetNodeText(attributeNode, text);
                var methodName = GetNodeText(methodNode, text);
                var lineNumber = GetNodeLineNumber(methodNode, text);

                Console.WriteLine($"Method: {methodName} (Line {lineNumber})");
                Console.WriteLine($"  Attribute: {attrText}");
                
                // TODO: Extract parameters from attribute and call AddUnitTest
                // var parameterValues = ExtractAttributeParameters(attributeNode, text);
                // AddUnitTest(filename, lineNumber, parameterValues, methodName);
            }
        }

        private void AddUnitTest(string fileName, int lineNumber, Dictionary<string, string> parameterValues, string functionName)
        {
            var unitTest = new UnitTestItem();
            unitTest.UnitTestFunctionName = functionName;
            bool identified = false;
            string shortFileName = Path.GetFileName(fileName);
            unitTest.UnitTestFileName = shortFileName;

            foreach (var info in information)
            {
                if (parameterValues.ContainsKey(info.Key))
                {
                    var value = parameterValues[info.Key];
                    //all strings are assumed to start and end with a string delimiter for all supported languages,
                    //note that for some languages the string delimiter can be """
                    if (value.StartsWith("\"\"\""))
                        value = value.Substring(3, value.Length - 6).Replace("\\\"", "\"");
                    else
                        value = value.Substring(1, value.Length - 2).Replace("\\\"", "\"");
                    switch (info.Key)
                    {
                        case "Purpose": unitTest.UnitTestPurpose = value; break;
                        case "PostCondition": unitTest.UnitTestAcceptanceCriteria = value; break;
                        case "Identifier": unitTest.ItemID = value; identified = true; break;
                        case "TraceID": unitTest.AddLinkedItem(new ItemLink(value, ItemLinkType.UnitTests)); break;
                        default: throw new Exception($"Unknown annotation identifier: {info.Key}");
                    }
                }
            }
            if (!identified)
            {
                unitTest.ItemID = $"{shortFileName}:{lineNumber}";
            }
            if (gitRepo != null && !gitRepo.GetFileLocallyUpdated(fileName))
            {
                //if gitInfo is not null, this means some item data elements should be collected through git
                unitTest.ItemLastUpdated = gitRepo.GetFileLastUpdated(fileName);
                unitTest.ItemRevision = gitRepo.GetFileVersion(fileName);
            }
            else
            {
                //the last time the local file was updated is our best guess
                unitTest.ItemLastUpdated = File.GetLastWriteTime(fileName);
                unitTest.ItemRevision = File.GetLastWriteTime(fileName).ToString("yyyy/MM/dd HH:mm:ss");
            }
            if (unitTests.FindIndex(x => x.ItemID == unitTest.ItemID) != -1)
            {
                throw new Exception($"Duplicate unit test identifier detected in {shortFileName} in the annotation starting on line {lineNumber}. Check other unit tests to ensure all unit tests have a unique identifier.");
            }
            unitTests.Add(unitTest);
        }

        public override void RefreshItems()
        {
            foreach (var sourceFile in sourceFiles)
            {
                var text = fileSystem.File.ReadAllText(sourceFile);
                FindAndProcessAnnotations(text, sourceFile);
            }
        }
    }
}