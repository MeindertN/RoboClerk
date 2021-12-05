using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Tomlyn;
using Microsoft.VisualBasic.FileIO;
using Tomlyn.Model;
using System.Text.RegularExpressions;

namespace RoboClerk.RedmineCSV
{
    class RedmineCSVSLMSPlugin : ISLMSPlugin
    {
        private string name = string.Empty;
        private string description = string.Empty;
        private string csvFileName = string.Empty;
        private List<RequirementItem> productRequirements = new List<RequirementItem>();
        private List<RequirementItem> softwareRequirements = new List<RequirementItem>();
        private List<TestCaseItem> testCases = new List<TestCaseItem>();
        private List<BugItem> bugs = new List<BugItem>();
        private string prsTrackerName = string.Empty;
        private string srsTrackerName = string.Empty;
        private string tcTrackerName = string.Empty;
        private string bugTrackerName = string.Empty;
        private TomlArray ignoreList = null; 

        public RedmineCSVSLMSPlugin()
        {
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

        public List<BugItem> GetBugs()
        {
            return bugs;
        }

        public List<RequirementItem> GetProductRequirements()
        {
            return productRequirements;
        }

        public List<RequirementItem> GetSoftwareRequirements()
        {
            return softwareRequirements;
        }

        public List<TestCaseItem> GetTestCases()
        {
            return testCases;
        }

        public void Initialize()
        {
            var assembly = Assembly.GetAssembly(this.GetType());
            var configFileLocation = $"{Path.GetDirectoryName(assembly.Location)}/Configuration/RedmineCSVSLMSPlugin.toml";
            var config = Toml.Parse(File.ReadAllText(configFileLocation)).ToModel();
            csvFileName = (string)config["ImportFilename"];
            prsTrackerName = (string)config["ProductLevelRequirement"];
            srsTrackerName = (string)config["SoftwareLevelRequirement"];
            tcTrackerName = (string)config["TestCase"];
            bugTrackerName = (string)config["Bug"];

            ignoreList = (TomlArray)config["Ignore"];
        }

        private List<string[]> GetTestSteps(string testDescription)
        {
            string[] lines =  testDescription.Split('\n');
            List<string[]> output = new List<string[]>();
            bool thenFound = false;
            foreach( var line in lines)
            {
                if(!line.ToUpper().Contains("THEN:") && !thenFound)
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
            TestCaseItem item = new TestCaseItem();

            item.TestCaseID = rmItem.Id;
            item.TestCaseRevision = rmItem.Updated;
            item.TestCaseState = rmItem.Status;
            item.TestCaseTitle = rmItem.Subject;
            item.TestCaseSteps = GetTestSteps(rmItem.Description);
            item.TestCaseAutomated = false;

            item.AddParent(rmItem.ParentTask, null);

            return item;
        }

        private BugItem CreateBug(RedmineItem item)
        {
            BugItem resultItem = new BugItem();

            resultItem.BugAssignee = item.Assignee;
            resultItem.BugID = item.Id;
            resultItem.BugJustification = string.Empty;
            resultItem.BugPriority = string.Empty;
            resultItem.BugRevision = item.Updated;
            resultItem.BugState = item.Status;
            resultItem.BugTitle = item.Subject;

            return resultItem;
        }

        private RequirementItem CreateRequirement(List<RedmineItem> items, RedmineItem item, RequirementType typeOfRequirement)
        {
            RequirementItem resultItem = new RequirementItem();

            if (item.FunctionalArea != "")
            {
                resultItem.RequirementCategory = item.FunctionalArea;
            }
            else
            {
                resultItem.RequirementCategory = "Product Requirement";
            }
            resultItem.RequirementDescription = item.Description;
            resultItem.RequirementID = item.Id;
            resultItem.RequirementRevision = item.Updated;
            resultItem.RequirementState = item.Status;
            resultItem.RequirementTitle = item.Subject;
            resultItem.TypeOfRequirement = typeOfRequirement;

            foreach(var redmineItem in items)
            {
                if(redmineItem.ParentTask == item.Id)
                {
                    resultItem.AddChild(redmineItem.Id, null);
                }
            }

            if(item.ParentTask != string.Empty)
            {
                resultItem.AddParent(item.ParentTask, null);
            }
            return resultItem;
        }

        public void RefreshItems()
        {
            if(csvFileName == string.Empty)
            {
                throw new Exception("CSV filename empty. Could not read csv file.");
            }

            var records = parseCSVFile();
            foreach(var redmineItem in records)
            {
                if(ignoreList.Contains(redmineItem.Status))
                {
                    continue; //ignore anything that is to be ignored
                }
                if(redmineItem.Tracker == prsTrackerName)
                {
                    productRequirements.Add(CreateRequirement(records, redmineItem, RequirementType.ProductRequirement));
                }
                else if(redmineItem.Tracker == srsTrackerName)
                {
                    softwareRequirements.Add(CreateRequirement(records, redmineItem, RequirementType.SoftwareRequirement));
                }
                else if(redmineItem.Tracker == tcTrackerName)
                {
                    testCases.Add(CreateTestCase(redmineItem));
                }
                else if(redmineItem.Tracker == bugTrackerName)
                {
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

            using (TextFieldParser csvParser = new TextFieldParser(stream,Encoding.ASCII))
            {
                csvParser.SetDelimiters(new string[] { "," });
                csvParser.HasFieldsEnclosedInQuotes = true;
                //read the first line in the CSV which contains the headers
                var fields = csvParser.ReadFields();
                //create a header mapper
                for(int i = 0; i<fields.Length; ++i)
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
