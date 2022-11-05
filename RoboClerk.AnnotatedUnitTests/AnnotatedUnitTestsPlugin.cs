﻿using System;
using RoboClerk.Configuration;
using Tomlyn.Model;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.IO.Abstractions;

namespace RoboClerk.AnnotatedUnitTests
{
    public class AnnotatedUnitTestsPlugin : SourceCodeAnalysisPluginBase
    {
        private string decorationMarker = string.Empty;
        private string parameterStartDelimiter = string.Empty;
        private string parameterEndDelimiter = string.Empty;
        private string parameterSeparator = string.Empty;

        private Dictionary<string,UTInformation> information = new Dictionary<string,UTInformation>();

        public AnnotatedUnitTestsPlugin()
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

        public override void Initialize(IConfiguration configuration)
        {
            logger.Info("Initializing the Unit Test Function Name Plugin");
            try
            {
                base.Initialize(configuration);
                var config = GetConfigurationTable(configuration.PluginConfigDir, $"{name}.toml");

                decorationMarker = configuration.CommandLineOptionOrDefault("DecorationMarker", GetStringForKey(config, "DecorationMarker", true));
                parameterStartDelimiter = configuration.CommandLineOptionOrDefault("ParameterStartDelimiter", GetStringForKey(config, "ParameterStartDelimiter", true));
                parameterEndDelimiter = configuration.CommandLineOptionOrDefault("ParameterEndDelimiter", GetStringForKey(config, "ParameterEndDelimiter", true));
                parameterSeparator = configuration.CommandLineOptionOrDefault("ParameterSeparator", GetStringForKey(config, "ParameterSeparator", true));

                PopulateUTInfo("Purpose", config);
                PopulateUTInfo("PostCondition", config);
                PopulateUTInfo("Identifier", config);
                PopulateUTInfo("TraceID", config);

            }
            catch (Exception e)
            {
                logger.Error("Error reading configuration file for Unit Test FN plugin.");
                logger.Error(e);
                throw new Exception("The Unit Test FN plugin could not read its configuration. Aborting...");
            }
            ScanDirectoriesForSourceFiles();
        }

        private int ParameterEnd(string input)
        {
            int openers = 0;
            int closers = 0;

            for (int i = 0; i < input.Length; i++)
            {
                var temp = input.Substring(i);
                if (temp.StartsWith(parameterStartDelimiter))
                {
                    openers++;
                }
                if (temp.StartsWith(parameterEndDelimiter))
                {
                    closers++;
                }
                if (openers > 0 && openers == closers)
                {
                    return i;
                }
            }
            return -1;
        }

        private Dictionary<string,string>  ParseParameterString(string pms, int startLine, string filename)
        {
            Dictionary<string, string> foundParameters = new Dictionary<string, string>();
            //replace all = , inside the strings with non-printing replacement characters unlikely 
            //to be used in practice
            StringBuilder pmsSb = new StringBuilder(pms);
            bool insideString = false;
            for(int i = 0; i < pms.Length ; i++)
            {
                if(pms[i] == '"')
                {
                    insideString = !insideString;
                }
                if (pms[i] == '=' && insideString)
                {
                    pmsSb[i] = '\a';
                }
                if (pms[i] == ',' && insideString)
                {
                    pmsSb[i] = '\f';
                }
            }
            string[] parameters = pmsSb.ToString().Split(parameterSeparator);
            foreach (var parameter in parameters)
            {
                string[] values = parameter.Split('=', StringSplitOptions.TrimEntries);
                if (values.Length != 2)
                {
                    throw new Exception($"Error parsing annotation starting on line {startLine} of \"{filename}\".");
                }
                var info = information.First(x => x.Value.KeyWord.ToUpper() == values[0].ToUpper());
                if (info.Key != string.Empty)
                {
                    StringBuilder sb = new StringBuilder(values[1]);
                    sb.Replace('\a', '=');
                    sb.Replace('\f', ',');
                    foundParameters[info.Key] = sb.ToString();
                }
            }
            return foundParameters;
        }

        private void FindAndProcessAnnotations(string[] lines, string filename)
        {
            StringBuilder foundAnnotation = new StringBuilder();
            for(int i=0; i<lines.Length; i++)
            {
                int index = lines[i].IndexOf(decorationMarker, StringComparison.OrdinalIgnoreCase);
                int paramStartIndex = -1;
                int paramEndIndex = -1;
                int startLine = -1;
                if (index >= 0)
                {
                    startLine = i;
                    foundAnnotation.Append(lines[i].Substring(index));
                    //keep iterating until we find the beginning of the parameters of the annotation
                    for (int j = i; j < lines.Length; j++)
                    {
                        paramStartIndex = foundAnnotation.ToString().IndexOf(parameterStartDelimiter);
                        if(paramStartIndex < 0)
                        {
                            foundAnnotation.Append(lines[j]);
                        }
                        else
                        {
                            i = j+1;
                            break;
                        }
                    }
                    for (int j = i; j < lines.Length; j++)
                    {
                        paramEndIndex = ParameterEnd(foundAnnotation.ToString());
                        if(paramEndIndex >= 0)
                        {
                            break;
                        }
                        else
                        {
                            foundAnnotation.Append(lines[j]);
                        }
                    }
                    string parameterString = foundAnnotation.ToString().Substring(paramStartIndex+1, paramEndIndex - paramStartIndex - 1);
                    foundAnnotation.Clear();
                    Dictionary<string, string> foundParameters = ParseParameterString(parameterString,startLine,filename);
                    //check if any required parameters are missing
                    foreach( var info in information )
                    {
                        if(!info.Value.Optional && !foundParameters.ContainsKey(info.Key))
                        {
                            throw new Exception($"Required parameter {info.Key} missing from unit test anotation starting on {startLine} of \"{filename}\".");
                        }
                    }
                    AddUnitTest(filename,startLine,foundParameters);
                }
            }
        }

        private void AddUnitTest(string fileName, int lineNumber, Dictionary<string,string> parameterValues)
        {
            var unitTest = new UnitTestItem();
            bool identified = false;
            string shortFileName = Path.GetFileName(fileName);

            foreach (var info in information)
            {
                if (parameterValues.ContainsKey(info.Key))
                {
                    var value = parameterValues[info.Key];
                    //all strings are assumed to start and end with a string delimiter for all supported languages
                    value = value.Substring(1, value.Length - 2);
                    switch (info.Key)
                    {
                        case "Purpose": unitTest.UnitTestPurpose = value; break;
                        case "PostCondition": unitTest.UnitTestAcceptanceCriteria = value; break;
                        case "Identifier": unitTest.ItemID = value; identified = true; break;
                        case "TraceID": unitTest.AddLinkedItem(new ItemLink(value, ItemLinkType.Related)); break;
                        default: throw new Exception($"Unknown annotation identifier: {info.Key}");
                    }
                }
            }
            if (!identified)
            {
                unitTest.ItemID = $"{shortFileName}:{lineNumber}";
            }
            if (gitInfo != null && !gitInfo.GetFileLocallyUpdated(fileName))
            {
                //if gitInfo is not null, this means some item data elements should be collected through git
                unitTest.ItemLastUpdated = gitInfo.GetFileLastUpdated(fileName);
                unitTest.ItemRevision = gitInfo.GetFileVersion(fileName);
            }
            else
            {
                //the last time the local file was updated is our best guess
                unitTest.ItemLastUpdated = File.GetLastWriteTime(fileName);
                unitTest.ItemRevision = File.GetLastWriteTime(fileName).ToString("yyyy/MM/dd HH:mm:ss");
            }
            if( unitTests.FindIndex(x => x.ItemID == unitTest.ItemID) != -1 )
            {
                throw new Exception($"Duplicate unit test identifier detected in {shortFileName} in the annotation starting on line {lineNumber}. Check other unit tests to ensure all unit tests have a unique identifier.");
            }
            unitTests.Add(unitTest);
        }

        public override void RefreshItems()
        {
            foreach (var sourceFile in sourceFiles)
            {
                var lines = File.ReadAllLines(sourceFile);
                FindAndProcessAnnotations(lines, sourceFile);
            }
        }
    }
}