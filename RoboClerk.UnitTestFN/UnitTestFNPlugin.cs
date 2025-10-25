using RoboClerk.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using TreeSitter;

namespace RoboClerk
{
    public class UnitTestFNPlugin : SourceCodeAnalysisPluginBase
    {
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
                // Base class handles both TestConfigurations and legacy format
                base.InitializePlugin(configuration);

                // For backward compatibility, check if we have configurations or legacy format
                if (TestConfigurations.Count == 0)
                {
                    throw new Exception("No test configurations found. At least one TestConfiguration is required for UnitTestFN plugin.");
                }

                // Validate that all configurations have required parameters
                foreach (var testConfig in TestConfigurations)
                {
                    var functionMask = configuration.CommandLineOptionOrDefault("FunctionMask", testConfig.GetValue<string>("FunctionMask"));
                    if (string.IsNullOrEmpty(functionMask))
                    {
                        throw new Exception($"FunctionMask is required for test configuration '{testConfig.Project}'. Please ensure FunctionMask is specified in the TestConfiguration.");
                    }
                    
                    var functionMaskElements = ParseFunctionMask(functionMask);
                    ValidateFunctionMaskElements(functionMaskElements);
                    
                    var sectionSeparator = configuration.CommandLineOptionOrDefault("SectionSeparator", testConfig.GetValue<string>("SectionSeparator"));
                    if (string.IsNullOrEmpty(sectionSeparator))
                    {
                        throw new Exception($"SectionSeparator is required for test configuration '{testConfig.Project}'. Please ensure SectionSeparator is specified in the TestConfiguration.");
                    }
                }

                // Get the primary configuration (first one for single-config scenarios)
                var primaryConfig = TestConfigurations[0];
                
                // Try to get plugin-specific fields from command line first, then from configuration
                selectedLanguage = configuration.CommandLineOptionOrDefault("Language", primaryConfig.GetValue<string>("Language", "csharp"));

                ScanDirectoriesForSourceFiles();

                logger.Debug($"Initialized UnitTestFN plugin with {TestConfigurations.Count} test configurations, primary language: {selectedLanguage}");
            }
            catch (Exception e)
            {
                logger.Error($"Error reading configuration file for Unit Test FN plugin: {e.Message}");
                logger.Error(e);
                throw new Exception("The Unit Test FN plugin could not read its configuration. Aborting...");
            }
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

        private string SeparateSection(string section, string sectionSeparator)
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

        private List<(string, string)> ApplyFunctionNameMask(string functionName, List<string> functionMaskElements)
        {
            List<(string, string)> resultingElements = new List<(string, string)>();
            
            // Check if the function name matches the non-element parts of the mask
            bool foundMatch = true;
            foreach (var functionMaskElement in functionMaskElements)
            {
                if (!functionMaskElement.StartsWith('<'))
                {
                    if (!functionName.Contains(functionMaskElement))
                    {
                        foundMatch = false;
                        break;
                    }
                }
            }
            
            if (foundMatch)
            {
                string remainingFunctionName = functionName;
                
                // Process pairs: identifier, separator, identifier, separator, etc.
                for (int i = 0; i < functionMaskElements.Count; i++)
                {
                    if (functionMaskElements[i].StartsWith('<'))
                    {
                        // This is an element identifier
                        if (i + 1 < functionMaskElements.Count && !functionMaskElements[i + 1].StartsWith('<'))
                        {
                            // Next element is a separator
                            var separator = functionMaskElements[i + 1];
                            var separatorIndex = remainingFunctionName.IndexOf(separator);
                            
                            if (separatorIndex >= 0)
                            {
                                var extractedContent = remainingFunctionName.Substring(0, separatorIndex);
                                resultingElements.Add((functionMaskElements[i], extractedContent));
                                remainingFunctionName = remainingFunctionName.Substring(separatorIndex + separator.Length);
                            }
                            else
                            {
                                foundMatch = false;
                                break;
                            }
                        }
                        else
                        {
                            // This is the last element - take all remaining content
                            resultingElements.Add((functionMaskElements[i], remainingFunctionName));
                            break;
                        }
                    }
                }
            }
            
            if (!foundMatch)
            {
                resultingElements.Clear();
            }
            
            return resultingElements;
        }

        private void AddUnitTest(List<(string, string)> els, string fileName, int lineNumber, string functionName, string sectionSeparator)
        {
            var unitTest = new UnitTestItem();
            bool identified = false;
            string shortFileName = Path.GetFileName(fileName);
            foreach (var el in els)
            {
                switch (el.Item1.ToUpper())
                {
                    case "<PURPOSE>": unitTest.UnitTestPurpose = SeparateSection(el.Item2, sectionSeparator); break;
                    case "<POSTCONDITION>": unitTest.UnitTestAcceptanceCriteria = SeparateSection(el.Item2, sectionSeparator); break;
                    case "<IDENTIFIER>": unitTest.ItemID = SeparateSection(el.Item2, sectionSeparator); identified = true; break;
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
            // Use the optimized approach: iterate through configurations and their associated files
            foreach (var testConfig in TestConfigurations)
            {
                if (testConfig.SourceFiles.Count == 0)
                {
                    logger.Warn($"No source files found for configuration '{testConfig.Project}' (Language: {testConfig.Language})");
                    continue;
                }
                
                logger.Debug($"Processing {testConfig.SourceFiles.Count} files for configuration '{testConfig.Project}' (Language: {testConfig.Language})");
                
                // Get configuration-specific parameters for this test configuration
                var configLanguage = testConfig.GetValue<string>("Language", selectedLanguage);
                var functionMask = testConfig.GetValue<string>("FunctionMask");
                var sectionSeparator = testConfig.GetValue<string>("SectionSeparator");
                
                // Parse function mask for this configuration
                var functionMaskElements = ParseFunctionMask(functionMask);
                
                var languageId = GetTreeSitterLanguageId(configLanguage);
                
                // Load language resources once per configuration
                using var language = new Language(languageId);
                using var parser = new Parser(language);
                
                var queryString = GetQueryForLanguage(configLanguage);
                using var query = new Query(language, queryString);
                
                // Process all files for this configuration with the same language resources
                foreach (var sourceFile in testConfig.SourceFiles)
                {
                    try
                    {
                        var text = fileSystem.File.ReadAllText(sourceFile);
                        FindAndProcessFunctions(text, sourceFile, parser, query, functionMaskElements, sectionSeparator);
                    }
                    catch (Exception e)
                    {
                        logger.Error($"Error processing file {sourceFile}: {e.Message}");
                        throw;
                    }
                }
            }
        }

        private void FindAndProcessFunctions(string sourceText, string filename, Parser parser, Query query, List<string> functionMaskElements, string sectionSeparator)
        {
            using var tree = parser.Parse(sourceText);
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
                    var els = ApplyFunctionNameMask(methodName, functionMaskElements);
                    if (els.Count > 0)
                    {
                        AddUnitTest(els, filename, methodLine, methodName, sectionSeparator);
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
; One pattern that matches class methods and standalone functions
(
  [
    (method_definition
      name: (_) @method_name      ; property_identifier, string, number, computed — all OK
    )
    (function_declaration
      name: (identifier) @method_name
    )
  ]
)
",

                _ => @"
(method_declaration
  name: (identifier) @method_name
)"
            };
        }
    }
}