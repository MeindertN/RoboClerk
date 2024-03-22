using RestSharp;
using RoboClerk.Configuration;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using Tomlyn.Model;

namespace RoboClerk.Redmine
{
    public class RedmineSLMSPlugin : SLMSPluginBase
    {
        private string baseURL = string.Empty;
        private string apiEndpoint = string.Empty;
        private string apiKey = string.Empty;
        private string projectName = string.Empty;
        private Dictionary<string, List<string>> ignoreSysReqWithFieldVal = new Dictionary<string, List<string>>();
        private RestClient client = null;

        public RedmineSLMSPlugin(IFileSystem fileSystem)
            : base(fileSystem)
        {
            logger.Debug("Redmine SLMS plugin created");
            name = "RedmineSLMSPlugin";
            description = "A plugin that can interrogate Redmine via its REST API to retrieve information needed by RoboClerk to create documentation.";
        }

        public override void Initialize(IConfiguration configuration)
        {
            logger.Info("Initializing the Redmine SLMS Plugin");
            base.Initialize(configuration);
            try
            {
                TomlTable config = GetConfigurationTable(configuration.PluginConfigDir, $"{name}.toml");
                apiEndpoint = configuration.CommandLineOptionOrDefault("RedmineAPIEndpoint", GetStringForKey(config, "RedmineAPIEndpoint", true));
                client = new RestClient(apiEndpoint);
                apiKey = configuration.CommandLineOptionOrDefault("RedmineAPIKey", GetStringForKey(config, "RedmineAPIKey", true));
                projectName = configuration.CommandLineOptionOrDefault("RedmineProject", GetStringForKey(config, "RedmineProject", true));
                baseURL = configuration.CommandLineOptionOrDefault("RedmineBaseURL", GetStringForKey(config, "RedmineBaseURL", false));
                if (config.ContainsKey("IgnoreRequirements") && config["IgnoreRequirements"] is TomlTable)
                {
                    TomlTable ignoreReq = (TomlTable)config["IgnoreRequirements"];
                    foreach (var kvp in ignoreReq)
                    {
                        if (kvp.Value is TomlArray)
                        {
                            foreach (var element in (TomlArray)kvp.Value)
                            {
                                if (ignoreSysReqWithFieldVal.ContainsKey(kvp.Key))
                                {
                                    ignoreSysReqWithFieldVal[kvp.Key].Add(element.ToString());
                                }
                                else
                                {
                                    ignoreSysReqWithFieldVal.Add(kvp.Key, new List<string> { element.ToString() });
                                }
                            }
                        }
                        else
                        {
                            throw new Exception($"Error reading IgnoreRequirements table in {name}.toml. Ensure that each element in the table is an array.");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error($"Error reading configuration file for {name}.");
                logger.Error(e);
                throw new Exception($"The {name} could not read its configuration. Aborting...");
            }
        }

        private List<string> GetTrackerList()
        {
            var result = new List<string> { prsName, srsName, tcName,
                                            bugName, riskName, soupName,
                                            docName, cntName };
            result.RemoveAll(x => x == string.Empty);
            return result;
        }

        public override void RefreshItems()
        {
            if (apiEndpoint == string.Empty || apiKey == string.Empty)
            {
                throw new Exception("No API endpoint or API key provided in configuration file.");
            }
            ClearAllItems();

            logger.Debug($"Retrieving the issues from the redmine server...");
            var redmineIssues = PullAllIssuesFromServer(GetTrackerList());
            List<string> retrievedIDs = new List<string>();

            foreach (var redmineIssue in redmineIssues)
            {
                if (ignoreList.Contains(redmineIssue.Status.Name))
                {
                    logger.Debug($"Ignoring redmine issue {redmineIssue.Id}");
                    continue;
                }
                retrievedIDs.Add(redmineIssue.Id.ToString());
                if (redmineIssue.Tracker.Name == prsName)
                {
                    logger.Debug($"System level requirement found: {redmineIssue.Id}");
                    if (ignoreSysReqWithFieldVal.Count == 0 || !ShouldIgnoreSysSoftDocReq(redmineIssue))
                    {
                        systemRequirements.Add(CreateRequirement(redmineIssues, redmineIssue, RequirementType.SystemRequirement));
                    }
                    else
                    {
                        retrievedIDs.Remove(redmineIssue.Id.ToString());
                    }
                }
                else if (redmineIssue.Tracker.Name == srsName)
                {
                    logger.Debug($"Software level requirement found: {redmineIssue.Id}");
                    if (ignoreSysReqWithFieldVal.Count == 0 || !ShouldIgnoreSysSoftDocReq(redmineIssue))
                    {
                        softwareRequirements.Add(CreateRequirement(redmineIssues, redmineIssue, RequirementType.SoftwareRequirement));
                    }
                    else
                    {
                        retrievedIDs.Remove(redmineIssue.Id.ToString());
                    }
                }
                else if (redmineIssue.Tracker.Name == tcName)
                {
                    logger.Debug($"Testcase found: {redmineIssue.Id}");
                    testCases.Add(CreateTestCase(redmineIssues, redmineIssue));
                }
                else if (redmineIssue.Tracker.Name == bugName)
                {
                    logger.Debug($"Bug item found: {redmineIssue.Id}");
                    anomalies.Add(CreateBug(redmineIssue));
                }
                else if (redmineIssue.Tracker.Name == riskName)
                {
                    logger.Debug($"Risk item found: {redmineIssue.Id}");
                    risks.Add(CreateRisk(redmineIssues, redmineIssue));
                }
                else if (redmineIssue.Tracker.Name == soupName)
                {
                    logger.Debug($"SOUP item found: {redmineIssue.Id}");
                    soup.Add(CreateSOUP(redmineIssue));
                }
                else if (redmineIssue.Tracker.Name == docName)
                {
                    logger.Debug($"Documentation item found: {redmineIssue.Id}");
                    if (ignoreSysReqWithFieldVal.Count == 0 || !ShouldIgnoreSysSoftDocReq(redmineIssue))
                    {
                        documentationRequirements.Add(CreateRequirement(redmineIssues, redmineIssue, RequirementType.DocumentationRequirement));
                    }
                    else
                    {
                        retrievedIDs.Remove(redmineIssue.Id.ToString());
                    }
                }
                else if (redmineIssue.Tracker.Name == cntName)
                {
                    logger.Debug($"DocContent item found: {redmineIssue.Id}");
                    docContents.Add(CreateDocContent(redmineIssue));
                }
            }
            if (ignoreSysReqWithFieldVal.Count > 0)
            {
                RemoveAllItemsNotLinkedToSysSoftDocReq(retrievedIDs);
            }
            RemoveIgnoredLinks(retrievedIDs); //go over all items and remove any links to ignored items
            ScrubItemContents(); //go over all relevant items and escape any | characters
        }

        private List<T> CheckForLinkedItem<T>(List<string> retrievedIDs, List<T> inputItems, List<ItemLinkType> lt) where T : LinkedItem
        {
            List<T> items = new List<T>();
            string removeItem = string.Empty;
            foreach (var item in inputItems)
            {
                if(item.LinkedItems.Count() > 0) //orphan items are always included so they don't get lost or ingnored.
                    removeItem = item.ItemID;
                foreach (var link in item.LinkedItems)
                {
                    
                    if (link != null && lt.Contains(link.LinkType) &&
                        retrievedIDs.Contains(link.TargetID) && 
                        (systemRequirements.Any(obj => obj.ItemID == link.TargetID) ||
                         softwareRequirements.Any(obj => obj.ItemID == link.TargetID) ||
                         documentationRequirements.Any(obj => obj.ItemID == link.TargetID)) )
                    {
                        removeItem = string.Empty;
                        break;
                    }
                }
                if (removeItem == string.Empty)
                {
                    items.Add(item);
                }
                else
                {
                    logger.Debug($"Removing item because it is not linked to a valid item: {item.ItemID}");
                    retrievedIDs.Remove(removeItem);
                    removeItem = string.Empty;
                }
            }
            return items;
        }

        private void RemoveAllItemsNotLinkedToSysSoftDocReq(List<string> retrievedIDs)
        {
            softwareRequirements = CheckForLinkedItem(retrievedIDs, softwareRequirements, new List<ItemLinkType> { ItemLinkType.Parent });
            testCases = CheckForLinkedItem(retrievedIDs, testCases, new List<ItemLinkType> { ItemLinkType.Parent, ItemLinkType.Related });
            docContents = CheckForLinkedItem(retrievedIDs, docContents, new List<ItemLinkType> { ItemLinkType.Parent });
            risks = CheckForLinkedItem(retrievedIDs, risks, new List<ItemLinkType> { ItemLinkType.RiskControl });
            //need to remove any bugs connected to items that were removed.
            anomalies = CheckForLinkedItem(retrievedIDs, anomalies, new List<ItemLinkType> { ItemLinkType.Related } );
        }

        private void RemoveIgnoredLinks(List<string> retrievedIDs)
        {
            TrimLinkedItems(systemRequirements, retrievedIDs);
            TrimLinkedItems(softwareRequirements, retrievedIDs);
            TrimLinkedItems(documentationRequirements, retrievedIDs);
            TrimLinkedItems(testCases, retrievedIDs);
            TrimLinkedItems(anomalies, retrievedIDs);
            TrimLinkedItems(risks, retrievedIDs);
            TrimLinkedItems(soup, retrievedIDs);
            TrimLinkedItems(docContents, retrievedIDs);
        }

        private List<TestStep> GetTestSteps(string testDescription)
        {
            string[] lines = testDescription.Split('\n');
            List<TestStep> output = new List<TestStep>();
            bool thenFound = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue; //skip empty lines
                }
                if (!line.ToUpper().Contains("THEN:") && !thenFound)
                {
                    output.Add(new TestStep((output.Count + 1).ToString(), line, string.Empty));
                }
                else
                {
                    if (!thenFound)
                    {
                        thenFound = true;
                        output[output.Count - 1].ExpectedResult = line;
                    }
                    else if (line.ToUpper().Contains("WHEN:"))
                    {
                        output.Add(new TestStep((output.Count + 1).ToString(), line, string.Empty));
                        thenFound = false;
                    }
                    else if (!line.ToUpper().Contains("AND:"))
                    {
                        output[output.Count - 1].ExpectedResult = output[output.Count - 1].ExpectedResult + '\n' + line;
                    }
                    else
                    {
                        output.Add(new TestStep((output.Count + 1).ToString(), string.Empty, line));
                    }
                }
            }
            return output;
        }

        private SoftwareSystemTestItem CreateTestCase(List<RedmineIssue> issues, RedmineIssue redmineItem)
        {
            logger.Debug($"Creating test case item: {redmineItem.Id}");
            SoftwareSystemTestItem resultItem = new SoftwareSystemTestItem();

            resultItem.ItemID = redmineItem.Id.ToString();
            resultItem.ItemRevision = redmineItem.UpdatedOn.ToString();
            resultItem.ItemLastUpdated = (DateTime)redmineItem.UpdatedOn;
            resultItem.ItemStatus = redmineItem.Status.Name ?? string.Empty;
            resultItem.TestCaseState = redmineItem.Status.Name ?? string.Empty;
            resultItem.ItemTitle = redmineItem.Subject ?? string.Empty;
            if (redmineItem.FixedVersion != null)
            {
                resultItem.ItemTargetVersion = redmineItem.FixedVersion.Name ?? string.Empty;
            }
            if (baseURL != "")
            {
                resultItem.Link = new Uri($"{baseURL}{resultItem.ItemID}");
            }
            logger.Debug($"Getting test steps for item: {redmineItem.Id}");
            var testCaseSteps = GetTestSteps(redmineItem.Description ?? string.Empty);
            foreach (var testCaseStep in testCaseSteps)
            {
                resultItem.AddTestCaseStep(testCaseStep);
            }
            resultItem.TestCaseAutomated = false;
            if (redmineItem.CustomFields != null)
            {
                foreach (var field in redmineItem.CustomFields)
                {
                    if (field.Value != null)
                    {
                        var value = (System.Text.Json.JsonElement)field.Value;
                        if (field.Name == "Test Method")
                        {
                            resultItem.TestCaseAutomated = (value.GetString() == "Automated") || (value.GetString() == "Unit Tested");
                            resultItem.TestCaseToUnitTest = (value.GetString() == "Unit Tested");
                        }
                    }
                }
            }

            AddLinksToItem(redmineItem, resultItem);
            //any software requirements are treated as parents, regardless of the link type
            foreach (var link in resultItem.LinkedItems)
            {
                foreach (var issue in issues)
                {
                    if (issue.Id.ToString() == link.TargetID && issue.Tracker.Name == srsName)
                    {
                        link.LinkType = ItemLinkType.Parent;
                    }
                }
            }

            return resultItem;
        }

        private DocContentItem CreateDocContent(RedmineIssue redmineItem)
        {
            logger.Debug($"Creating DocContent item: {redmineItem.Id}");
            DocContentItem resultItem = new DocContentItem();

            resultItem.ItemID = redmineItem.Id.ToString();
            resultItem.ItemRevision = redmineItem.UpdatedOn.ToString();
            resultItem.ItemLastUpdated = (DateTime)redmineItem.UpdatedOn;
            resultItem.ItemStatus = redmineItem.Status.Name ?? string.Empty;
            resultItem.DocContent = redmineItem.Description.ToString();
            if (redmineItem.FixedVersion != null)
            {
                resultItem.ItemTargetVersion = redmineItem.FixedVersion.Name ?? string.Empty;
            }
            if (baseURL != "")
            {
                resultItem.Link = new Uri($"{baseURL}{resultItem.ItemID}");
            }

            if (redmineItem.CustomFields.Count != 0)
            {
                foreach (var field in redmineItem.CustomFields)
                {
                    if (field.Name == "Functional Area" && field.Value != null)
                    {
                        resultItem.ItemCategory = ((System.Text.Json.JsonElement)field.Value).GetString();
                    }
                }
            }
            AddLinksToItem(redmineItem, resultItem);
            return resultItem;
        }

        private SOUPItem CreateSOUP(RedmineIssue redmineItem)
        {
            logger.Debug($"Creating SOUP item: {redmineItem.Id}");
            SOUPItem resultItem = new SOUPItem();

            resultItem.ItemID = redmineItem.Id.ToString();
            resultItem.ItemRevision = redmineItem.UpdatedOn.ToString();
            resultItem.ItemLastUpdated = (DateTime)redmineItem.UpdatedOn;
            resultItem.ItemStatus = redmineItem.Status.Name ?? string.Empty;
            resultItem.SOUPName = redmineItem.Subject ?? string.Empty;
            if (redmineItem.FixedVersion != null)
            {
                resultItem.ItemTargetVersion = redmineItem.FixedVersion.Name ?? string.Empty;
            }
            if (baseURL != "")
            {
                resultItem.Link = new Uri($"{baseURL}{resultItem.ItemID}");
            }
            if (redmineItem.CustomFields != null)
            {
                foreach (var field in redmineItem.CustomFields)
                {
                    if (field.Value == null)
                    {
                        continue;
                    }
                    var value = (System.Text.Json.JsonElement)field.Value;
                    if (field.Name == "Version")
                    {
                        resultItem.SOUPVersion = value.GetString();
                    }
                    if (field.Name == "Linked Library")
                    {
                        resultItem.SOUPLinkedLib = value.GetString().Contains("1"); //this is how custom boolean fields store their value (1/0)
                    }
                    if (field.Name == "SOUP Detailed Description")
                    {
                        resultItem.SOUPDetailedDescription = value.GetString();
                    }
                    else if (field.Name == "Performance Critical?")
                    {
                        resultItem.SOUPPerformanceCritical = !value.GetString().Contains("is not");
                        resultItem.SOUPPerformanceCriticalText = value.GetString();
                    }
                    else if (field.Name == "CyberSecurity Critical?")
                    {
                        resultItem.SOUPCybersecurityCritical = !value.GetString().Contains("is not");
                        resultItem.SOUPCybersecurityCriticalText = value.GetString();
                    }
                    else if (field.Name == "Anomaly List Examination")
                    {
                        resultItem.SOUPAnomalyListDescription = value.GetString();
                    }
                    else if (field.Name == "Installed by end user?")
                    {
                        resultItem.SOUPInstalledByUser = !value.GetString().Contains("No");
                        resultItem.SOUPInstalledByUserText = value.GetString();
                    }
                    else if (field.Name == "End user training")
                    {
                        resultItem.SOUPEnduserTraining = value.GetString();
                    }
                    else if (field.Name == "SOUP License")
                    {
                        resultItem.SOUPLicense = value.GetString();
                    }
                    else if (field.Name == "Manufacturer")
                    {
                        resultItem.SOUPManufacturer = value.GetString();
                    }
                }
            }
            AddLinksToItem(redmineItem, resultItem);
            return resultItem;
        }

        private AnomalyItem CreateBug(RedmineIssue redmineItem)
        {
            logger.Debug($"Creating bug item: {redmineItem.Id}");
            AnomalyItem resultItem = new AnomalyItem();

            if (redmineItem.AssignedTo != null)
            {
                resultItem.AnomalyAssignee = redmineItem.AssignedTo.Name;
            }
            else
            {
                resultItem.AnomalyAssignee = string.Empty;
            }

            resultItem.ItemID = redmineItem.Id.ToString();
            resultItem.AnomalyJustification = string.Empty;
            resultItem.AnomalySeverity = string.Empty;
            if (redmineItem.CustomFields.Count != 0)
            {
                foreach (var field in redmineItem.CustomFields)
                {
                    if (field.Value == null)
                    {
                        continue;
                    }
                    var value = ((System.Text.Json.JsonElement)field.Value).ToString();

                    switch (field.Name)
                    {
                        case "Justification": resultItem.AnomalyJustification = value; break;
                        case "Severity": resultItem.AnomalySeverity = value; break;
                    }
                }
            }
            resultItem.ItemRevision = redmineItem.UpdatedOn.ToString();
            resultItem.ItemLastUpdated = (DateTime)redmineItem.UpdatedOn;
            resultItem.AnomalyState = redmineItem.Status.Name ?? string.Empty;
            resultItem.ItemStatus = redmineItem.Status.Name ?? string.Empty;
            resultItem.ItemTitle = redmineItem.Subject ?? string.Empty;
            resultItem.AnomalyDetailedDescription = redmineItem.Description ?? String.Empty;
            if (redmineItem.FixedVersion != null)
            {
                resultItem.ItemTargetVersion = redmineItem.FixedVersion.Name ?? string.Empty;
            }
            if (baseURL != "")
            {
                resultItem.Link = new Uri($"{baseURL}{resultItem.ItemID}");
            }
            AddLinksToItem(redmineItem, resultItem);

            return resultItem;
        }

        private RiskItem CreateRisk(List<RedmineIssue> issues, RedmineIssue redmineItem)
        {
            logger.Debug($"Creating risk item: {redmineItem.Id}");
            RiskItem resultItem = new RiskItem();
            resultItem.ItemCategory = "Unknown";
            resultItem.ItemStatus = redmineItem.Status.Name ?? string.Empty;
            resultItem.ItemID = redmineItem.Id.ToString();
            if (baseURL != "")
            {
                resultItem.Link = new Uri($"{baseURL}{resultItem.ItemID}");
            }
            if (redmineItem.CustomFields.Count != 0)
            {
                foreach (var field in redmineItem.CustomFields)
                {
                    if (field.Value == null)
                    {
                        continue;
                    }
                    var value = ((System.Text.Json.JsonElement)field.Value).ToString();

                    switch (field.Name)
                    {
                        case "Risk Type": resultItem.ItemCategory = value; break;
                        case "Risk": resultItem.RiskPrimaryHazard = value; break;
                        case "Hazard Severity": resultItem.RiskSeverityScore = (value != string.Empty ? int.Parse(value.Split('-')[0]) : int.MaxValue); break; //need to ensure default is int maxvalue for safety
                        case "Hazard Probability": resultItem.RiskOccurenceScore = (value != string.Empty ? int.Parse(value.ToString().Split('-')[0]) : int.MaxValue); break;
                        case "Hazard Detectability": resultItem.RiskDetectabilityScore = (value != string.Empty ? int.Parse(value.ToString().Split('-')[0]) : int.MaxValue); break;
                        case "Residual Probability": resultItem.RiskModifiedOccScore = (value != string.Empty ? int.Parse(value.Split('-')[0]) : int.MaxValue); break;
                        case "Residual Detectability": resultItem.RiskModifiedDetScore = (value != string.Empty ? int.Parse(value.Split('-')[0]) : int.MaxValue); break;
                        case "Risk Control Category": resultItem.RiskControlMeasureType = (value != string.Empty ? value.Split('\t')[0] : string.Empty); break;
                        case "Detection Method": resultItem.RiskMethodOfDetection = value; break;
                    }
                }
            }
            AddLinksToItem(redmineItem, resultItem);
            int nrOfResults = 0;
            if ((nrOfResults = resultItem.LinkedItems.Where(x => x.LinkType == ItemLinkType.Related).Count()) > 1)
            {
                logger.Warn($"Expected 1 related link for risk item \"{resultItem.ItemID}\". Multiple related items linked. Please check the item in Redmine.");
            }
            else if (nrOfResults == 1)
            {
                ItemLink link = resultItem.LinkedItems.Where(x => x.LinkType == ItemLinkType.Related).First();
                if (link != null)
                {
                    link.LinkType = ItemLinkType.RiskControl;
                    var issue = GetIssue(int.Parse(link.TargetID));

                    if (issue != null)
                    {
                        resultItem.RiskControlMeasure = issue.Subject;
                        resultItem.RiskControlImplementation = issue.Description;
                    }
                    else
                    {
                        logger.Warn($"RoboClerk is unable the find the Redmine ticket {link.TargetID} that is linked to Risk tracker item {resultItem.ItemID}. Please check the risk tracker item in Redmine and its related issue.");
                    }
                }
            }
            resultItem.RiskFailureMode = redmineItem.Subject ?? String.Empty;
            resultItem.RiskCauseOfFailure = redmineItem.Description ?? String.Empty;
            resultItem.ItemRevision = redmineItem.UpdatedOn.ToString() ?? String.Empty;
            resultItem.ItemLastUpdated = (DateTime)redmineItem.UpdatedOn;
            if (redmineItem.FixedVersion != null)
            {
                resultItem.ItemTargetVersion = redmineItem.FixedVersion.Name ?? string.Empty;
            }

            return resultItem;
        }

        private ItemLinkType GetLinkType(Relation rel)
        {
            switch (rel.RelationType)
            {
                case "relates": return ItemLinkType.Related;
                default: return ItemLinkType.None;
            }
        }

        private void AddLinksToItem(RedmineIssue redmineItem, LinkedItem resultItem)
        {
            if (redmineItem.Parent != null)
            {
                resultItem.AddLinkedItem(new ItemLink(redmineItem.Parent.Id.ToString(), ItemLinkType.Parent));
            }
            if (redmineItem.Relations.Count != 0)
            {
                foreach (var relation in redmineItem.Relations)
                {
                    ItemLinkType lt = GetLinkType(relation);
                    if (lt != ItemLinkType.None)
                    {
                        if (relation.IssueId != redmineItem.Id)
                        {
                            resultItem.AddLinkedItem(new ItemLink(relation.IssueId.ToString(), lt));
                        }
                        else
                        {
                            resultItem.AddLinkedItem(new ItemLink(relation.IssueToId.ToString(), lt));
                        }
                    }
                }
            }
        }

        private bool ShouldIgnoreSysSoftDocReq(RedmineIssue redmineItem)
        {
            if (redmineItem.CustomFields.Count != 0)
            {
                foreach (var field in redmineItem.CustomFields)
                {
                    if (ignoreSysReqWithFieldVal.ContainsKey(field.Name) && field.Value != null)
                    {
                        string fieldVal = ((System.Text.Json.JsonElement)field.Value).GetString();
                        if (ignoreSysReqWithFieldVal[field.Name].Contains(fieldVal))
                        {
                            logger.Debug($"Ignoring requirement item {redmineItem.Id} due to \"{field.Name}\" being equal to \"{fieldVal}\".");
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private RequirementItem CreateRequirement(List<RedmineIssue> issues, RedmineIssue redmineItem, RequirementType requirementType)
        {
            logger.Debug($"Creating requirement item: {redmineItem.Id}");
            RequirementItem resultItem = new RequirementItem(requirementType);
            resultItem.ItemCategory = "Unknown";
            if (redmineItem.CustomFields.Count != 0)
            {
                foreach (var field in redmineItem.CustomFields)
                {
                    if (field.Name == "Functional Area" && field.Value != null)
                    {
                        resultItem.ItemCategory = ((System.Text.Json.JsonElement)field.Value).GetString();
                    }                           
                }
            }

            if (redmineItem.AssignedTo != null)
            {
                resultItem.RequirementAssignee = redmineItem.AssignedTo.Name;
            }
            else
            {
                resultItem.RequirementAssignee = string.Empty;
            }

            resultItem.RequirementDescription = redmineItem.Description ?? string.Empty;
            resultItem.ItemID = redmineItem.Id.ToString();
            resultItem.ItemRevision = redmineItem.UpdatedOn.ToString();
            resultItem.ItemLastUpdated = (DateTime)redmineItem.UpdatedOn;
            if (redmineItem.FixedVersion != null)
            {
                resultItem.ItemTargetVersion = redmineItem.FixedVersion.Name ?? string.Empty;
            }
            resultItem.ItemStatus = redmineItem.Status.Name ?? string.Empty;
            resultItem.RequirementState = redmineItem.Status.Name ?? string.Empty;
            resultItem.ItemTitle = redmineItem.Subject ?? string.Empty;
            resultItem.TypeOfRequirement = requirementType;
            if (baseURL != "")
            {
                resultItem.Link = new Uri($"{baseURL}{resultItem.ItemID}");
            }

            foreach (var issue in issues)
            {
                if (issue.Parent != null && issue.Parent.Id == redmineItem.Id)
                {
                    resultItem.AddLinkedItem(new ItemLink(issue.Id.ToString(), ItemLinkType.Child));
                }
            }

            AddLinksToItem(redmineItem, resultItem);
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
            while (response.Limit + response.Offset < response.TotalCount)
            {
                request.AddOrUpdateParameter("offset", response.Offset + response.Limit);
                response = client.GetAsync<RedmineProjects>(request).GetAwaiter().GetResult();
                projects.AddRange(response.Projects);
            }
            foreach (var project in projects)
            {
                if (project.Name == projectName)
                {
                    logger.Info($"Found project \"{projectName}\" in Redmine. Description: {project.Description}, ID#: {project.Id}.");
                    return project.Id;
                }
            }
            throw new Exception($"Could not find project \"{projectName}\" in Redmine. Please check plugin configuration file and Redmine server.");
        }

        private Dictionary<string, int> GetTrackers()
        {
            var request = new RestRequest("trackers.json", Method.Get)
                .AddParameter("key", apiKey);

            var response = client.GetAsync<RedmineTrackers>(request).GetAwaiter().GetResult();
            Dictionary<string, int> trackers = new Dictionary<string, int>();

            foreach (var tracker in response.Trackers)
            {
                trackers[tracker.Name] = tracker.Id;
            }
            return trackers;
        }

        private RedmineIssue GetIssue(int issueID)
        {
            var request = new RestRequest("issues.json", Method.Get)
                .AddParameter("limit", 100)
                .AddParameter("key", apiKey)
                .AddParameter("include", "relations")
                .AddParameter("status_id", "*")
                .AddParameter("issue_id", issueID);
            var response = client.GetAsync<RedmineIssues>(request).GetAwaiter().GetResult();
            if (response.Issues.Count == 0)
            {
                return null;
            }
            else
            {
                return response.Issues[0];
            }
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
            foreach (var queryTracker in queryTrackers)
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
