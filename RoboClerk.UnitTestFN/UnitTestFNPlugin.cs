using RoboClerk.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;

namespace RoboClerk
{
    public class UnitTestFNPlugin : SourceCodeAnalysisPluginBase
    {
        private string testFunctionDecoration = string.Empty;
        private List<string> functionMaskElements = new List<string>();
        private string sectionSeparator = string.Empty;

        public UnitTestFNPlugin(IFileSystem fileSystem)
            : base(fileSystem)
        {
            name = "UnitTestFNPlugin";
            description = "A plugin that analyzes a project's source code to extract unit test information for RoboClerk.";
        }

        public override void Initialize(IConfiguration configuration)
        {
            logger.Info("Initializing the Unit Test Function Name Plugin");
            try
            {
                base.Initialize(configuration);
                var config = GetConfigurationTable(configuration.PluginConfigDir, $"{name}.toml");

                testFunctionDecoration = configuration.CommandLineOptionOrDefault("TestFunctionDecoration", GetObjectForKey<string>(config, "TestFunctionDecoration", false));
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

        private List<(string, string)> ApplyFunctionNameMask(string line)
        {
            List<(string, string)> resultingElements = new List<(string, string)>();
            var strings = line.Trim().Split(' ');
            var longestString = strings.OrderByDescending(s => s.Length).First(); //we assume that the function name is the longest element
            bool foundMatch = true;
            foreach (var functionMaskElement in functionMaskElements)
            {
                if (!functionMaskElement.StartsWith('<'))
                {
                    foundMatch = foundMatch && longestString.Contains(functionMaskElement);
                }
            }
            StringBuilder functionName = new StringBuilder();
            if (foundMatch)
            {
                string remainingLine = longestString;
                for (int i = 1; i < functionMaskElements.Count; i += 2)
                {
                    if (remainingLine == string.Empty)
                    {
                        foundMatch = false;
                        break;
                    }
                    if (functionMaskElements[i].StartsWith('<'))
                    {
                        throw new Exception("Error in UnitTestFNPlugin element identifier in unexpected position. Check FunctionMask.");
                    }
                    var items = remainingLine.Split(functionMaskElements[i]);
                    resultingElements.Add((functionMaskElements[i - 1], items[0]));
                    functionName.Append(items[0]);
                    functionName.Append(functionMaskElements[i]);
                    if (items.Length - 1 != 0)
                    {
                        remainingLine = String.Join(functionMaskElements[i], items, 1, items.Length - 1);
                    }
                    else
                    {
                        remainingLine = string.Empty;
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
                    case "<TRACEID>": unitTest.AddLinkedItem(new ItemLink(el.Item2, ItemLinkType.Related)); break;
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

        private string GetFunctionName(string line)
        {
            var strings = line.Trim().Split(' ');
            var longestString = strings.OrderByDescending(s => s.Length).First(); //we assume that the function name is the longest element
            //we also assume the function parameters start with (
            int index = longestString.IndexOf('(');
            return longestString.Substring(0, index);
        }

        private void FindAndProcessFunctions(string[] lines, string fileName)
        {
            bool nextLineIsFunction = testFunctionDecoration == string.Empty;
            int currentLineNumber = 0;
            foreach (var line in lines)
            {
                currentLineNumber++;
                if (nextLineIsFunction)
                {
                    if (line.Trim().Length > 3)
                    {
                        var els = ApplyFunctionNameMask(line);
                        if (els.Count > 0)
                        {

                            //Create unit test
                            AddUnitTest(els, fileName, currentLineNumber, GetFunctionName(line));
                        }
                        nextLineIsFunction = false || testFunctionDecoration == string.Empty;
                    }
                }
                else
                {
                    if (line.Contains(testFunctionDecoration))
                    {
                        nextLineIsFunction = true;
                    }
                }
            }
        }

        public override void RefreshItems()
        {
            foreach (var sourceFile in sourceFiles)
            {
                var lines = fileSystem.File.ReadAllLines(sourceFile);
                FindAndProcessFunctions(lines, sourceFile);
            }
        }
    }
}