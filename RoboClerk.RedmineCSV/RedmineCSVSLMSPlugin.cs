using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Tomlyn;
using Tomlyn.Model;
using RoboClerk.Configuration;

namespace RoboClerk.RedmineCSV
{
    class RedmineCSVSLMSPlugin : ISLMSPlugin
    {
        private string name = string.Empty;
        private string description = string.Empty;
        private string csvFileName = string.Empty;
        private List<RequirementItem> systemRequirements = new List<RequirementItem>();
        private List<RequirementItem> softwareRequirements = new List<RequirementItem>();
        private List<TestCaseItem> testCases = new List<TestCaseItem>();
        private List<AnomalyItem> bugs = new List<AnomalyItem>();
        private string prsTrackerName = string.Empty;
        private string srsTrackerName = string.Empty;
        private string tcTrackerName = string.Empty;
        private string bugTrackerName = string.Empty;
        private string baseURL = string.Empty;
        private TomlArray ignoreList = null;
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public RedmineCSVSLMSPlugin()
        {
            logger.Debug("Redmine CSV SLMS plugin created");
            name = "RedmineCSVSLMSPlugin";
            description = "A plugin that can load CSV files as exported by Redmine to retrieve information needed by RoboClerk to create documentation.";
        }

        public string Name
        {
            get => name;
        }

        public string Description
        {
            get => description;
        }

        public List<AnomalyItem> GetAnomalies()
        {
            return bugs;
        }

        public List<RequirementItem> GetSystemRequirements()
        {
            return systemRequirements;
        }

        public List<RequirementItem> GetSoftwareRequirements()
        {
            return softwareRequirements;
        }

        public List<TestCaseItem> GetSoftwareSystemTests()
        {
            return testCases;
        }

        public void Initialize(IConfiguration configuration)
        {
            logger.Info("Initializing the Redmine CSV SLMS Plugin");
            var assembly = Assembly.GetAssembly(this.GetType());
            try
            {
                var configFileLocation = $"{Path.GetDirectoryName(assembly.Location)}/Configuration/RedmineCSVSLMSPlugin.toml";
                var config = Toml.Parse(File.ReadAllText(configFileLocation)).ToModel();
                csvFileName = configuration.CommandLineOptionOrDefault("ImportFilename",(string)config["ImportFilename"]);
                prsTrackerName = configuration.CommandLineOptionOrDefault("SystemRequirement",(string)config["SystemRequirement"]);
                srsTrackerName = configuration.CommandLineOptionOrDefault("SoftwareRequirement",(string)config["SoftwareRequirement"]);
                tcTrackerName = configuration.CommandLineOptionOrDefault("SoftwareSystemTest",(string)config["SoftwareSystemTest"]);
                bugTrackerName = configuration.CommandLineOptionOrDefault("Anomaly",(string)config["Anomaly"]);

                if (config.ContainsKey("RedmineBaseURL"))
                {
                    baseURL = configuration.CommandLineOptionOrDefault("RedmineBaseURL",(string)config["RedmineBaseURL"]);
                }

                ignoreList = (TomlArray)config["Ignore"];
            }
            catch (Exception e)
            {
                logger.Error("Error reading configuration file for Redmine CSV SLMS plugin.");
                logger.Error(e);
                throw new Exception("The redmine CSV SLMS plugin could not read its configuration. Aborting...");
            }
        }

        private List<string[]> GetTestSteps(string testDescription)
        {
            string[] lines = testDescription.Split('\n');
            List<string[]> output = new List<string[]>();
            bool thenFound = false;
            foreach (var line in lines)
            {
                if (!line.ToUpper().Contains("THEN:") && !thenFound)
                {
                    string[] ln = new string[2] { line, string.Empty };
                    output.Add(ln);
                }
                else
                {
                    if (!thenFound)
                    {
                        thenFound = true;
                        output[output.Count - 1][1] = line;
                    }
                    else
                    {
                        output[output.Count - 1][1] = output[output.Count - 1][1] + '\n' + line;
                    }
                }
            }
            return output;
        }

        private TestCaseItem CreateTestCase(RedmineItem rmItem)
        {
            logger.Debug($"Creating test case item: {rmItem.Id}");
            TestCaseItem resultItem = new TestCaseItem();

            resultItem.TestCaseID = rmItem.Id;
            resultItem.TestCaseRevision = rmItem.Updated;
            resultItem.TestCaseState = rmItem.Status;
            resultItem.TestCaseTitle = rmItem.Subject;
            if (baseURL != "")
            {
                resultItem.Link = new Uri($"{baseURL}{resultItem.TestCaseID}");
            }
            logger.Debug($"Getting test steps for item: {rmItem.Id}");
            resultItem.TestCaseSteps = GetTestSteps(rmItem.Description);
            resultItem.TestCaseAutomated = rmItem.TestMethod == "Automated";
            if (baseURL != "")
            {
                resultItem.AddParent(rmItem.ParentTask, new Uri($"{baseURL}{rmItem.ParentTask}"));
            }
            else
            {
                resultItem.AddParent(rmItem.ParentTask, null);
            }

            return resultItem;
        }

        private AnomalyItem CreateBug(RedmineItem rmItem)
        {
            logger.Debug($"Creating bug item: {rmItem.Id}");
            AnomalyItem resultItem = new AnomalyItem();

            resultItem.AnomalyAssignee = rmItem.Assignee;
            resultItem.AnomalyID = rmItem.Id;
            resultItem.AnomalyJustification = string.Empty;
            resultItem.AnomalyPriority = string.Empty;
            resultItem.AnomalyRevision = rmItem.Updated;
            resultItem.AnomalyState = rmItem.Status;
            resultItem.AnomalyTitle = rmItem.Subject;
            if (baseURL != "")
            {
                resultItem.Link = new Uri($"{baseURL}{resultItem.AnomalyID}");
            }

            return resultItem;
        }

        private RequirementItem CreateRequirement(List<RedmineItem> items, RedmineItem rmItem, RequirementType typeOfRequirement)
        {
            logger.Debug($"Creating requirement item: {rmItem.Id}");
            RequirementItem resultItem = new RequirementItem();

            if (rmItem.FunctionalArea != "")
            {
                resultItem.RequirementCategory = rmItem.FunctionalArea;
            }
            else
            {
                resultItem.RequirementCategory = "Unknown";
            }
            resultItem.RequirementDescription = rmItem.Description;
            resultItem.RequirementID = rmItem.Id;
            resultItem.RequirementRevision = rmItem.Updated;
            resultItem.RequirementState = rmItem.Status;
            resultItem.RequirementTitle = rmItem.Subject;
            resultItem.TypeOfRequirement = typeOfRequirement;
            if (baseURL != "")
            {
                resultItem.Link = new Uri($"{baseURL}{resultItem.RequirementID}");
            }

            foreach (var redmineItem in items)
            {
                if (redmineItem.ParentTask == rmItem.Id)
                {
                    if (baseURL != "")
                    {
                        resultItem.AddChild(redmineItem.Id, new Uri($"{baseURL}{redmineItem.Id}"));
                    }
                    else
                    {
                        resultItem.AddChild(redmineItem.Id, null);
                    }
                }
            }

            if (rmItem.ParentTask != string.Empty)
            {
                if (baseURL != "")
                {
                    resultItem.AddParent(rmItem.ParentTask, new Uri($"{baseURL}{rmItem.ParentTask}"));
                }
                else
                {
                    resultItem.AddParent(rmItem.ParentTask, null);
                }
            }
            return resultItem;
        }

        public void RefreshItems()
        {
            if (csvFileName == string.Empty)
            {
                throw new Exception("CSV filename empty. Could not read csv file.");
            }

            logger.Debug($"Parsing CSV file");
            var records = parseCSVFile();
            foreach (var redmineItem in records)
            {
                if (ignoreList.Contains(redmineItem.Status))
                {
                    logger.Debug($"Ignoring item {redmineItem.Id}");
                    continue; //ignore anything that is to be ignored
                }
                if (redmineItem.Tracker == prsTrackerName)
                {
                    logger.Debug($"System level requirement found: {redmineItem.Id}");
                    systemRequirements.Add(CreateRequirement(records, redmineItem, RequirementType.SystemRequirement));
                }
                else if (redmineItem.Tracker == srsTrackerName)
                {
                    logger.Debug($"Software level requirement found: {redmineItem.Id}");
                    softwareRequirements.Add(CreateRequirement(records, redmineItem, RequirementType.SoftwareRequirement));
                }
                else if (redmineItem.Tracker == tcTrackerName)
                {
                    logger.Debug($"Testcase found: {redmineItem.Id}");
                    testCases.Add(CreateTestCase(redmineItem));
                }
                else if (redmineItem.Tracker == bugTrackerName)
                {
                    logger.Debug($"Bug item found: {redmineItem.Id}");
                    bugs.Add(CreateBug(redmineItem));
                }
            }
        }

        private List<RedmineItem> parseCSVFile()
        {
            Dictionary<string, int> headerMapping = new Dictionary<string, int>();
            List<RedmineItem> items = new List<RedmineItem>();
            PropertyInfo[] properties = typeof(RedmineItem).GetProperties();
            string fileContents = File.ReadAllText(csvFileName);
            fileContents = Regex.Replace(fileContents, @"\r\n", "\n");
            MemoryStream stream = new MemoryStream(Encoding.ASCII.GetBytes(fileContents));

            using (TextFieldParser csvParser = new TextFieldParser(stream, Encoding.ASCII))
            {
                csvParser.SetDelimiters(new string[] { "," });
                csvParser.HasFieldsEnclosedInQuotes = true;
                //read the first line in the CSV which contains the headers
                var fields = csvParser.ReadFields();
                //create a header mapper
                for (int i = 0; i < fields.Length; ++i)
                {
                    headerMapping[fields[i]] = i;
                }

                while (!csvParser.EndOfData)
                {
                    fields = csvParser.ReadFields();
                    RedmineItem item = new RedmineItem();
                    foreach (PropertyInfo property in properties)
                    {
                        NameAttribute attr = property.GetCustomAttribute(typeof(NameAttribute)) as NameAttribute;
                        if (attr != null && headerMapping.ContainsKey(attr.Name))
                        {
                            property.SetValue(item, fields[headerMapping[attr.Name]]);
                        }
                        else
                        {
                            property.SetValue(item, "");
                        }
                    }
                    items.Add(item);
                }
            }
            return items;
        }
    }
}
