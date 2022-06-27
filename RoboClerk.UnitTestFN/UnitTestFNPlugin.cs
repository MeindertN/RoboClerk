using RoboClerk.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Tomlyn;
using Tomlyn.Model;

namespace RoboClerk.SourceCode
{
    public class UnitTestFNPlugin : ISourceCodeAnalysisPlugin
    {
        private string name = string.Empty;
        private string description = string.Empty;
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private List<string> testDirectories = new List<string>();
        private bool subDir = false;
        private List<string> fileMasks = new List<string>();
        private string testFunctionDecoration = string.Empty;
        private List<string> functionMaskElements = new List<string>();
        private List<string> sourceFiles = new List<string>();
        private string sectionSeparator = string.Empty;        

        private List<UnitTestItem> unitTests = new List<UnitTestItem>();

        public UnitTestFNPlugin()
        {
            name = "UnitTestFNPlugin";
            description = "A plugin that analyzes a project's source code to extract unit test information for RoboClerk.";
        }

        public string Name => name;

        public string Description => description;

        public List<UnitTestItem> GetUnitTests()
        {
            return unitTests;
        }

        public void Initialize(IConfiguration configuration)
        {
            logger.Info("Initializing the Unit Test Function Name Plugin");
            var assembly = Assembly.GetAssembly(this.GetType());
            try
            {
                var configFileLocation = $"{Path.GetDirectoryName(assembly?.Location)}/Configuration/UnitTestFNPlugin.toml";
                if (configuration.PluginConfigDir != string.Empty)
                {
                    configFileLocation = Path.Combine(configuration.PluginConfigDir, "RedmineSLMSPlugin.toml");
                }
                var config = Toml.Parse(File.ReadAllText(configFileLocation)).ToModel();

                subDir = (bool)config["SubDir"]; 
                testFunctionDecoration = configuration.CommandLineOptionOrDefault("TestFunctionDecoration", (string)config["TestFunctionDecoration"]); 
                var functionMask = configuration.CommandLineOptionOrDefault("FunctionMask", (string)config["FunctionMask"]);
                functionMaskElements = ParseFunctionMask(functionMask);
                ValidateFunctionMaskElements(functionMaskElements);
                sectionSeparator = configuration.CommandLineOptionOrDefault("SectionSeparator", (string)config["SectionSeparator"]);

                foreach (var obj in (TomlArray)config["TestDirectories"])
                {
                    testDirectories.Add((string)obj);
                }

                foreach (var obj in (TomlArray)config["FileMasks"])
                {
                    fileMasks.Add((string)obj);
                }
            }
            catch (Exception e)
            {
                logger.Error("Error reading configuration file for Unit Test FN plugin.");
                logger.Error(e);
                throw new Exception("The Unit Test FN plugin could not read its configuration. Aborting...");
            }
            sourceFiles.Clear();
            ScanDirectoriesForSourceFiles();
        }

        private void ScanDirectoriesForSourceFiles()
        {
            foreach(var testDirectory in testDirectories)
            {
                DirectoryInfo dir = new DirectoryInfo(testDirectory);
                try
                {
                    foreach (var fileMask in fileMasks)
                    {
                        FileInfo[] files = dir.GetFiles(fileMask, subDir ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                        foreach(var file in files)
                        {
                            sourceFiles.Add(file.FullName);
                        }
                    }
                }
                catch
                {
                    logger.Error($"Error reading directory {testDirectory}");
                    throw;
                }
            }
        }

        private List<string> ParseFunctionMask(string functionMask)
        {
            List<string> elements = new List<string>();
            bool inElement = false;
            StringBuilder sb = new StringBuilder();
            foreach(char c in functionMask)
            {
                if(c == '<' && !inElement)
                {
                    inElement = true;
                    if(sb.Length > 0)
                    {
                        elements.Add(sb.ToString());
                        sb.Clear();
                    }
                }
                if(inElement)
                {
                    sb.Append(c);
                    if(c == '>')
                    {
                        inElement=false;
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
            if(!elements.First().StartsWith('<'))
            {
                throw new Exception("Error in UnitTestFNPlugin, Function Mask must start with an element identifier (e.g. <IGNORE>)");
            }
            foreach(string element in elements)
            {
                if(element.StartsWith('<'))
                {
                    if(element.ToUpper() != "<PURPOSE>" &&
                        element.ToUpper() != "<POSTCONDITION>" &&
                        element.ToUpper() != "<IDENTIFIER>" &&
                        element.ToUpper() != "<TRACEID>" )
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
            StringBuilder sb = new StringBuilder();
            if(sectionSeparator.ToUpper() == "CAMELCASE")
            {
                bool first = true;
                //don't case if the first word is capitalized
                foreach(char c in section)
                {
                    if(first)
                    {
                        sb.Append(c);
                        first = false;
                    }
                    else
                    {
                        if(Char.IsUpper(c))
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
            }
            else
            {
                section.Replace(sectionSeparator, " ");
            }
            return sb.ToString();
        }

        private List<(string,string)> ApplyFunctionNameMask(string line)
        {
            List<(string, string)> resultingElements = new List<(string, string)>();
            var strings = line.Trim().Split(' ');
            var longestString = strings.OrderByDescending(s => s.Length).First();
            bool foundMatch = true;
            foreach (var functionMaskElement in functionMaskElements)
            {
                if (!functionMaskElement.StartsWith('<'))
                {
                    foundMatch = foundMatch && longestString.Contains(functionMaskElement);
                }
            }
            if(foundMatch)
            {
                string remainingLine = line;
                for (int i = 1; i < functionMaskElements.Count ; i+=2)
                {
                    if(remainingLine == string.Empty)
                    {
                        foundMatch = false;
                        break;
                    }
                    if (functionMaskElements[i].StartsWith('<'))
                    {
                        throw new Exception("Error in UnitTestFNPlugin element identifier in unexpected position. Check FunctionMask.");
                    }
                    var items = remainingLine.Split(functionMaskElements[i]);
                    resultingElements.Add((functionMaskElements[i-1], items[0]));
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
            if(!foundMatch)
            {
                resultingElements.Clear();
            }
            return resultingElements;
        }

        private void AddUnitTest(List<(string,string)> els)
        {

        }

        private List<string> FindAndProcessFunctions(string[] lines)
        {
            List<string> result = new List<string>();
            bool nextLineIsFunction = testFunctionDecoration == string.Empty;
            foreach (var line in lines)
            {
                if(nextLineIsFunction)
                {
                    if(line.Trim().Length > 3)
                    {
                        var els = ApplyFunctionNameMask(line);
                        if(els.Count > 0)
                        {
                            //Create unit test
                            AddUnitTest(els);
                        }
                        nextLineIsFunction = false || testFunctionDecoration == string.Empty;
                    }
                }
                else
                {
                    if(line.Contains(testFunctionDecoration))
                    {
                        nextLineIsFunction = true;
                    }
                }
            }

        }

        public void RefreshItems()
        {
            foreach(var sourceFile in sourceFiles)
            {
                var lines = File.ReadAllLines(sourceFile);
                var functionNames = FindFunctions(lines);
            }
        }
    }
}