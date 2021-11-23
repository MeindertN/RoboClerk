using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Tomlyn;
using Microsoft.VisualBasic.FileIO;

namespace RoboClerk.RedmineCSV
{
    class RedmineCSVSLMSPlugin : ISLMSPlugin
    {
        private string name = string.Empty;
        private string description = string.Empty;
        private string csvFileName = string.Empty;
        List<RequirementItem> productRequirements = new List<RequirementItem>();
        List<RequirementItem> softwareRequirements = new List<RequirementItem>();
        List<TestCaseItem> testCases = new List<TestCaseItem>();

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

        public List<Item> GetBugs()
        {
            throw new NotImplementedException();
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
            csvFileName = (string)config["importFilename"];
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
                if(redmineItem.Status == "Closed")
                {
                    continue; //ignore anything that is closed
                }
                if(redmineItem.Tracker == "Requirement")
                {
                    productRequirements.Add(CreateRequirement(records, redmineItem, RequirementType.ProductRequirement));
                }
                if(redmineItem.Tracker == "Specification")
                {
                    softwareRequirements.Add(CreateRequirement(records, redmineItem, RequirementType.SoftwareRequirement));
                }
            }
        }

        private List<RedmineItem> parseCSVFile()
        {
            Dictionary<string, int> headerMapping = new Dictionary<string, int>();
            List<RedmineItem> items = new List<RedmineItem>();
            PropertyInfo[] properties = typeof(RedmineItem).GetProperties();
            string fileContents = File.ReadAllText(csvFileName);
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
                        if (attr != null)
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
