﻿using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Tomlyn;
using Tomlyn.Model;
using RoboClerk.Configuration;

namespace RoboClerk.Redmine
{
    public class RedmineSLMSPlugin : ISLMSPlugin
    {
        private string name = string.Empty;
        private string description = string.Empty;
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private List<RequirementItem> systemRequirements = new List<RequirementItem>();
        private List<RequirementItem> softwareRequirements = new List<RequirementItem>();
        private List<TestCaseItem> testCases = new List<TestCaseItem>();
        private List<AnomalyItem> bugs = new List<AnomalyItem>();
        private string prsTrackerName = string.Empty;
        private string srsTrackerName = string.Empty;
        private string tcTrackerName = string.Empty;
        private string bugTrackerName = string.Empty;
        private string baseURL = string.Empty;
        private string apiEndpoint = string.Empty;
        private string apiKey = string.Empty;
        private string projectName = string.Empty;
        private TomlArray ignoreList = new TomlArray();
        private RestClient client = null;

        public RedmineSLMSPlugin()
        {
            logger.Debug("Redmine SLMS plugin created");
            name = "RedmineSLMSPlugin";
            description = "A plugin that can interrogate Redmine via its REST API to retrieve information needed by RoboClerk to create documentation.";
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
            logger.Info("Initializing the Redmine SLMS Plugin");
            var assembly = Assembly.GetAssembly(this.GetType());
            try
            {
                var configFileLocation = $"{Path.GetDirectoryName(assembly?.Location)}/Configuration/RedmineSLMSPlugin.toml";
                if (configuration.PluginConfigDir != string.Empty)
                {
                    configFileLocation = Path.Combine(configuration.PluginConfigDir, "RedmineSLMSPlugin.toml");
                }
                var config = Toml.Parse(File.ReadAllText(configFileLocation)).ToModel();
                apiEndpoint = configuration.CommandLineOptionOrDefault("RedmineAPIEndpoint",(string)config["RedmineAPIEndpoint"]);
                client = new RestClient(apiEndpoint);
                apiKey = configuration.CommandLineOptionOrDefault("RedmineAPIKey",(string)config["RedmineAPIKey"]);
                projectName = configuration.CommandLineOptionOrDefault("RedmineProject",(string)config["RedmineProject"]);
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
                logger.Error("Error reading configuration file for Redmine SLMS plugin.");
                logger.Error(e);
                throw new Exception("The redmine SLMS plugin could not read its configuration. Aborting...");
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

        private TestCaseItem CreateTestCase(RedmineIssue rmItem)
        {
            logger.Debug($"Creating test case item: {rmItem.Id}");
            TestCaseItem resultItem = new TestCaseItem();

            resultItem.TestCaseID = rmItem.Id.ToString();
            resultItem.TestCaseRevision = rmItem.UpdatedOn.ToString();
            resultItem.TestCaseState = rmItem.Status.Name;
            resultItem.TestCaseTitle = rmItem.Subject;
            if (baseURL != "")
            {
                resultItem.Link = new Uri($"{baseURL}{resultItem.TestCaseID}");
            }
            logger.Debug($"Getting test steps for item: {rmItem.Id}");
            resultItem.TestCaseSteps = GetTestSteps(rmItem.Description);
            resultItem.TestCaseAutomated = false;
            if (rmItem.CustomFields != null)
            {
                foreach (var field in rmItem.CustomFields)
                {
                    var value = field.Value as string;
                    if (field.Name == "Test Method" && value != null)
                    {
                        resultItem.TestCaseAutomated = (value == "Automated");
                    }
                }
            }
            if (baseURL != "")
            {
                resultItem.AddParent(rmItem.Parent.Id.ToString(), new Uri($"{baseURL}{rmItem.Parent.Id}"));
            }
            else
            {
                resultItem.AddParent(rmItem.Parent.Id.ToString(), null);
            }
            return resultItem;
        }

        private AnomalyItem CreateBug(RedmineIssue rmItem)
        {
            logger.Debug($"Creating bug item: {rmItem.Id}");
            AnomalyItem resultItem = new AnomalyItem();

            resultItem.AnomalyAssignee = rmItem.AssignedTo.Name;
            resultItem.AnomalyID = rmItem.Id.ToString();
            resultItem.AnomalyJustification = string.Empty;
            resultItem.AnomalyPriority = string.Empty;
            resultItem.AnomalyRevision = rmItem.UpdatedOn.ToString();
            resultItem.AnomalyState = rmItem.Status.Name;
            resultItem.AnomalyTitle = rmItem.Subject;
            if (baseURL != "")
            {
                resultItem.Link = new Uri($"{baseURL}{resultItem.AnomalyID}");
            }

            return resultItem;
        }

        public void RefreshItems()
        {
            if (apiEndpoint == string.Empty || apiKey == string.Empty)
            {
                throw new Exception("No API endpoint or API key provided in configuration file.");
            }

            logger.Debug($"Retrieving the issues from the redmine server...");
            var redmineIssues = PullAllIssuesFromServer(new List<string> { prsTrackerName, srsTrackerName, tcTrackerName, bugTrackerName });

            foreach (var redmineIssue in redmineIssues)
            {
                if (ignoreList.Contains(redmineIssue.Status.Name))
                {
                    logger.Debug($"Ignoring redmine issue {redmineIssue.Id}");
                    continue;
                }
                if (redmineIssue.Tracker.Name == prsTrackerName)
                {
                    logger.Debug($"System level requirement found: {redmineIssue.Id}");
                    systemRequirements.Add(CreateRequirement(redmineIssues, redmineIssue, RequirementType.SystemRequirement));
                }
                else if (redmineIssue.Tracker.Name == srsTrackerName)
                {
                    logger.Debug($"Software level requirement found: {redmineIssue.Id}");
                    softwareRequirements.Add(CreateRequirement(redmineIssues, redmineIssue, RequirementType.SoftwareRequirement));
                }
                else if (redmineIssue.Tracker.Name == tcTrackerName)
                {
                    logger.Debug($"Testcase found: {redmineIssue.Id}");
                    testCases.Add(CreateTestCase(redmineIssue));
                }
                else if (redmineIssue.Tracker.Name == bugTrackerName)
                {
                    logger.Debug($"Bug item found: {redmineIssue.Id}");
                    bugs.Add(CreateBug(redmineIssue));
                }
            }
        }

        private RequirementItem CreateRequirement(List<RedmineIssue> issues, RedmineIssue redmineItem, RequirementType requirementType)
        {
            logger.Debug($"Creating requirement item: {redmineItem.Id}");
            RequirementItem resultItem = new RequirementItem();
            resultItem.RequirementCategory = "Unknown";
            if (redmineItem.CustomFields.Count != 0)
            {
                foreach(var field in redmineItem.CustomFields)
                {
                    var value = field.Value as List<string>;
                    if(field.Name == "Functional Area" && value != null)
                    {
                        if(value.Count > 0)
                        {
                            resultItem.RequirementCategory = value[0];
                        }
                    }
                }
            }

            resultItem.RequirementDescription = redmineItem.Description;
            resultItem.RequirementID = redmineItem.Id.ToString();
            resultItem.RequirementRevision = redmineItem.UpdatedOn.ToString();
            resultItem.RequirementState = redmineItem.Status.Name;
            resultItem.RequirementTitle = redmineItem.Subject;
            resultItem.TypeOfRequirement = requirementType;
            if (baseURL != "")
            {
                resultItem.Link = new Uri($"{baseURL}{resultItem.RequirementID}");
            }

            foreach (var issue in issues)
            {
                if (issue.Parent != null && issue.Parent.Id == redmineItem.Id)
                {
                    if (baseURL != "")
                    {
                        resultItem.AddChild(issue.Id.ToString(), new Uri($"{baseURL}{issue.Id}"));
                    }
                    else
                    {
                        resultItem.AddChild(issue.Id.ToString(), null);
                    }
                }
            }

            if (redmineItem.Parent != null)
            {
                if (baseURL != "")
                {
                    resultItem.AddParent(redmineItem.Parent.Id.ToString(), new Uri($"{baseURL}{redmineItem.Parent.Id}"));
                }
                else
                {
                    resultItem.AddParent(redmineItem.Parent.Id.ToString(), null);
                }
            }
            return resultItem;
        }

        private int GetProjectID(string projectName)
        {
            var request = new RestRequest("projects.json", Method.Get)
                .AddParameter("limit", 100)
                .AddParameter("key", apiKey);
            List<RedmineProject> projects = new List<RedmineProject>();
            var response = client.GetAsync<RedmineProjects>(request).GetAwaiter().GetResult();
            projects.AddRange(response.Projects);
            while(response.Limit + response.Offset < response.TotalCount)
            {
                request.AddOrUpdateParameter("offset", response.Offset + response.Limit);
                response = client.GetAsync<RedmineProjects>(request).GetAwaiter().GetResult();
                projects.AddRange(response.Projects);
            }
            foreach(var project in projects)
            {
                if( project.Name == projectName)
                {
                    logger.Info($"Found project \"{projectName}\" in Redmine. Description: {project.Description}, ID#: {project.Id}.");
                    return project.Id;
                }
            }
            throw new Exception($"Could not find project \"{projectName}\" in Redmine. Please check plugin configuration file and Redmine server.");
        }

        private Dictionary<string,int> GetTrackers()
        {
            var request = new RestRequest("trackers.json", Method.Get)
                .AddParameter("key", apiKey);
            
            var response = client.GetAsync<RedmineTrackers>(request).GetAwaiter().GetResult();
            Dictionary<string,int> trackers = new Dictionary<string, int>();

            foreach (var tracker in response.Trackers)
            {
                trackers[tracker.Name] = tracker.Id;    
            }
            return trackers; 
        }

        private List<RedmineIssue> GetIssues(int projectID, int trackerID)
        {
            var request = new RestRequest("issues.json", Method.Get)
                .AddParameter("limit", 100)
                .AddParameter("key", apiKey)
                .AddParameter("project_id", projectID)
                .AddParameter("include", "relations")
                .AddParameter("status_id", "*")
                .AddParameter("tracker_id", trackerID);
            List<RedmineIssue> issues = new List<RedmineIssue>();
            var response = client.GetAsync<RedmineIssues>(request).GetAwaiter().GetResult();
            issues.AddRange(response.Issues);
            while (response.Limit + response.Offset < response.TotalCount)
            {
                request.AddOrUpdateParameter("offset", response.Offset + response.Limit);
                response = client.GetAsync<RedmineIssues>(request).GetAwaiter().GetResult();
                issues.AddRange(response.Issues);
            }
            return issues;
        }

        private List<RedmineIssue> PullAllIssuesFromServer(List<string> queryTrackers)
        {
            int projectID = GetProjectID(projectName);
            var trackers = GetTrackers();
            List<RedmineIssue> issueList = new List<RedmineIssue>();
            foreach(var queryTracker in queryTrackers)
            {
                if (!trackers.ContainsKey(queryTracker))
                {
                    throw new Exception($"Tracker \"{queryTracker}\" is not present on the Redmine server. Please check plugin configuration file and Redmine server.");
                }
                issueList.AddRange(GetIssues(projectID, trackers[queryTracker]));
            }
            return issueList;
        }
    }
}
