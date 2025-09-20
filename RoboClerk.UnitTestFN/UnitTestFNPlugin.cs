using Microsoft.Extensions.DependencyInjection;
using RoboClerk.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using Tomlyn.Model;
using TreeSitter;

namespace RoboClerk
{
    public class UnitTestFNPlugin : SourceCodeAnalysisPluginBase
    {
        private List<string> functionMaskElements = new List<string>();
        private string sectionSeparator = string.Empty;
        private string selectedLanguage = "csharp";

        public UnitTestFNPlugin(IFileSystem fileSystem)
            : base(fileSystem)
        {
            SetBaseParam();
        }

        private void SetBaseParam()
        {
            name = "UnitTestFNPlugin";
            description = "A plugin that analyzes a project's source code to extract unit test information for RoboClerk using TreeSitter.";
        }

        public override void InitializePlugin(IConfiguration configuration)
        {
            logger.Info("Initializing the Unit Test Function Name Plugin");
            try
            {
                base.InitializePlugin(configuration);
                var config = GetConfigurationTable(configuration.PluginConfigDir, $"{name}.toml");

                // Language selection
                selectedLanguage = GetObjectForKey<string>(config, "Language", false) ?? "csharp";
                
                var functionMask = configuration.CommandLineOptionOrDefault("FunctionMask", GetObjectForKey<string>(config, "FunctionMask", true));
                functionMaskElements = ParseFunctionMask(functionMask);
                ValidateFunctionMaskElements(functionMaskElements);
                sectionSeparator = configuration.CommandLineOptionOrDefault("SectionSeparator", GetObjectForKey<string>(config, "SectionSeparator", true));
            }
            catch (Exception e)
            {
                logger.Error("Error reading configuration file for Unit Test FN plugin.");
                logger.Error(e);
                throw new Exception("The Unit Test FN plugin could not read its configuration. Aborting...");
            }
            ScanDirectoriesForSourceFiles();
        }

        private List<string> ParseFunctionMask(string functionMask)
        {
            List<string> elements = new List<string>();
            bool inElement = false;
            StringBuilder sb = new StringBuilder();
            foreach (char c in functionMask)
            {
                if (c == '<' && !inElement)
                {
                    inElement = true;
                    if (sb.Length > 0)
                    {
                        elements.Add(sb.ToString());
                        sb.Clear();
                    }
                }
                if (inElement)
                {
                    sb.Append(c);
                    if (c == '>')
                    {
                        inElement = false;
                        elements.Add(sb.ToString());
                        sb.Clear();
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            // Add any remaining content
            if (sb.Length > 0)
            {
                elements.Add(sb.ToString());
                sb.Clear();
            }
            return elements;
        }

        private void ValidateFunctionMaskElements(List<string> elements)
        {
            if (!elements.First().StartsWith('<'))
            {
                throw new Exception("Error in UnitTestFNPlugin, Function Mask must start with an element identifier (e.g. <IGNORE>)");
            }
            foreach (string element in elements)
            {
                if (element.StartsWith('<'))
                {
                    if (element.ToUpper() != "<PURPOSE>" &&
                        element.ToUpper() != "<POSTCONDITION>" &&
                        element.ToUpper() != "<IDENTIFIER>" &&
                        element.ToUpper() != "<TRACEID>" &&
                        element.ToUpper() != "<IGNORE>")
                    {
                        throw new Exception($"Error in UnitTestFNPlugin, unknown function mask element found: \"{element}\"");
                    }
                }
                else
                {
                    if (element.Any(x => Char.IsWhiteSpace(x)))
                    {
                        throw new Exception($"Error in UnitTestFNPlugin, whitespace found in an element of the function mask: \"{element}\"");
                    }
                }
            }
        }

        private string SeparateSection(string section)
        {
            if (sectionSeparator.ToUpper() == "CAMELCASE")
            {
                StringBuilder sb = new StringBuilder();
                bool first = true;
                //don't care if the first word is capitalized
                foreach (char c in section)
                {
                    if (first)
                    {
                        sb.Append(c);
                        first = false;
                    }
                    else
                    {
                        if (Char.IsUpper(c))
                        {
                            sb.Append(' ');
                            sb.Append(c);
                        }
                        else
                        {
                            sb.Append(c);
                        }
                    }
                }
                return sb.ToString();
            }
            else
            {
                return section.Replace(sectionSeparator, " ");
            }
        }

        private List<(string, string)> ApplyFunctionNameMask(string functionName)
        {
            List<(string, string)> resultingElements = new List<(string, string)>();
            bool foundMatch = true;
            
            // Check if the function name matches the non-element parts of the mask
            foreach (var functionMaskElement in functionMaskElements)
            {
                if (!functionMaskElement.StartsWith('<'))
                {
                    foundMatch = foundMatch && functionName.Contains(functionMaskElement);
                }
            }
            
            if (foundMatch)
            {
                string remainingFunctionName = functionName;
                for (int i = 1; i < functionMaskElements.Count; i += 2)
                {
                    if (remainingFunctionName == string.Empty)
                    {
                        foundMatch = false;
                        break;
                    }
                    if (functionMaskElements[i].StartsWith('<'))
                    {
                        throw new Exception("Error in UnitTestFNPlugin element identifier in unexpected position. Check FunctionMask.");
                    }
                    var items = remainingFunctionName.Split(functionMaskElements[i]);
                    resultingElements.Add((functionMaskElements[i - 1], items[0]));
                    if (items.Length - 1 != 0)
                    {
                        remainingFunctionName = String.Join(functionMaskElements[i], items, 1, items.Length - 1);
                    }
                    else
                    {
                        remainingFunctionName = string.Empty;
                    }
                }
                
                // Handle the case where there's a final element after the last separator
                if (!string.IsNullOrEmpty(remainingFunctionName) && functionMaskElements.Count >= 2)
                {
                    var lastElementIndex = functionMaskElements.Count - 1;
                    if (functionMaskElements[lastElementIndex].StartsWith('<'))
                    {
                        resultingElements.Add((functionMaskElements[lastElementIndex], remainingFunctionName));
                    }
                }
            }
            if (!foundMatch)
            {
                resultingElements.Clear();
            }
            return resultingElements;
        }

        private void AddUnitTest(List<(string, string)> els, string fileName, int lineNumber, string functionName)
        {
            var unitTest = new UnitTestItem();
            bool identified = false;
            string shortFileName = Path.GetFileName(fileName);
            foreach (var el in els)
            {
                switch (el.Item1.ToUpper())
                {
                    case "<PURPOSE>": unitTest.UnitTestPurpose = SeparateSection(el.Item2); break;
                    case "<POSTCONDITION>": unitTest.UnitTestAcceptanceCriteria = SeparateSection(el.Item2); break;
                    case "<IDENTIFIER>": unitTest.ItemID = SeparateSection(el.Item2); identified = true; break;
                    case "<TRACEID>": unitTest.AddLinkedItem(new ItemLink(el.Item2, ItemLinkType.UnitTests)); break;
                    case "<IGNORE>": break;
                    default: throw new Exception($"Unknown element identifier in FunctionMask: {el.Item1.ToUpper()}");
                }
            }
            unitTest.UnitTestFileName = shortFileName;
            unitTest.UnitTestFunctionName = functionName;
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
                FindAndProcessFunctions(text, sourceFile);
            }
        }

        private void FindAndProcessFunctions(string sourceText, string filename)
        {
            var languageId = GetTreeSitterLanguageId(selectedLanguage);
            
            using var language = new Language(languageId);
            using var parser = new Parser(language);
            using var tree = parser.Parse(sourceText);

            var queryString = GetQueryForLanguage(selectedLanguage);
            using var query = new Query(language, queryString);
            var exec = query.Execute(tree.RootNode);

            // Process all methods found by TreeSitter
            var processedMethods = new HashSet<(string methodName, int line)>();

            foreach (var match in exec.Matches)
            {
                string methodName = string.Empty;
                int methodLine = 0;

                foreach (var cap in match.Captures)
                {
                    if (cap.Name == "method_name")
                    {
                        methodName = cap.Node.Text;
                        methodLine = (int)cap.Node.StartPosition.Row + 1;
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(methodName))
                {
                    var methodKey = (methodName, methodLine);
                    if (processedMethods.Contains(methodKey))
                        continue;
                    processedMethods.Add(methodKey);
                                        
                    // Try to apply the function name mask
                    var els = ApplyFunctionNameMask(methodName);
                    if (els.Count > 0)
                    {
                        AddUnitTest(els, filename, methodLine, methodName);
                    }
                }
            }
        }

        private string GetTreeSitterLanguageId(string lang) => lang.ToLowerInvariant() switch
        {
            "c#" or "csharp" or "cs" => "C_SHARP",
            "java" => "JAVA",
            "python" or "py" => "PYTHON",
            "typescript" or "ts" => "TYPESCRIPT",
            "javascript" or "js" => "TYPESCRIPT", // use TS grammar to parse JS
            _ => "C_SHARP"
        };

        private string GetQueryForLanguage(string lang)
        {
            return lang.ToLowerInvariant() switch
            {
                "c#" or "csharp" or "cs" => @"
(method_declaration
  name: (identifier) @method_name
)",

                "java" => @"
(method_declaration
  name: (identifier) @method_name
)",

                "python" or "py" => @"
(function_definition 
  name: (identifier) @method_name
)",

                "typescript" or "ts" or "javascript" or "js" => @"
(method_definition
  name: (identifier) @method_name
)

(function_declaration
  name: (identifier) @method_name
)",

                _ => @"
(method_declaration
  name: (identifier) @method_name
)"
            };
        }
    }
}