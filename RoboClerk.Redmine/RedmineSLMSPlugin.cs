using Microsoft.Extensions.DependencyInjection;
using RestSharp;
using RoboClerk.Configuration;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using Tomlyn.Model;

namespace RoboClerk.Redmine
{
    public interface IRedmineClient
    {
        // Example methods; expand as needed
        RestRequest CreateRequest(string resource, Method method);
        T GetAsync<T>(RestRequest request) where T : new();
    }

    // Concrete implementation using RestSharp
    public class RestSharpRedmineClient : IRedmineClient
    {
        private readonly RestClient _client;

        public RestSharpRedmineClient(string apiEndpoint)
        {
            _client = new RestClient(apiEndpoint);
        }

        public RestRequest CreateRequest(string resource, Method method)
        {
            return new RestRequest(resource, method);
        }

        public T GetAsync<T>(RestRequest request) where T : new()
        {
            // Asynchronous execution wrapped in a synchronous call
            return _client.GetAsync<T>(request).GetAwaiter().GetResult();
        }
    }

    public class RedmineSLMSPlugin : SLMSPluginBase
    {
        private readonly IRedmineClient _client;
        private string baseURL = string.Empty;
        private string apiEndpoint = string.Empty;
        private string apiKey = string.Empty;
        private List<string> projectNames = new List<string>();
        private bool convertTextile = false;
        private TextileToAsciiDocConverter converter = null;
        private List<string> redmineVersionFields = new List<string>();
        private List<Version> versions = null;

        public RedmineSLMSPlugin(IFileSystem fileSystem, IRedmineClient client)
            : base(fileSystem)
        {
            logger.Debug("Redmine SLMS plugin created");
            SetBaseParam();
            _client = client;
        }

        public RedmineSLMSPlugin(IFileSystem fileSystem)
            : base(fileSystem)
        {
            SetBaseParam();
            //only used to configure services
        }

        private void SetBaseParam()
        {
            name = "RedmineSLMSPlugin";
            description = "A plugin that can interrogate Redmine via its REST API to retrieve information needed by RoboClerk to create documentation.";
        }

        public override void InitializePlugin(IConfiguration configuration)
        {
            logger.Info("Initializing the Redmine SLMS Plugin");
            base.InitializePlugin(configuration);
            try
            {
                TomlTable config = GetConfigurationTable(configuration.PluginConfigDir, $"{name}.toml");
                apiEndpoint = configuration.CommandLineOptionOrDefault("RedmineAPIEndpoint", GetObjectForKey<string>(config, "RedmineAPIEndpoint", true));
                apiKey = configuration.CommandLineOptionOrDefault("RedmineAPIKey", GetObjectForKey<string>(config, "RedmineAPIKey", true));
                var subPrj = GetObjectForKey<TomlArray>(config, "RedmineProjects", true);
                foreach (var o in subPrj)
                {
                    projectNames.Add((string)o);
                }
                baseURL = configuration.CommandLineOptionOrDefault("RedmineBaseURL", GetObjectForKey<string>(config, "RedmineBaseURL", false));
                convertTextile = configuration.CommandLineOptionOrDefault("ConvertTextile", GetObjectForKey<bool>(config, "ConvertTextile", false)?"TRUE":"FALSE").ToUpper() == "TRUE";
                if(convertTextile) 
                { 
                    converter = new TextileToAsciiDocConverter(); 
                }
                if (config.ContainsKey("VersionCustomFields"))
                {
                    //this is needed specifically for Redmine because we cannot via the API figure out if a custom field is of type "version"
                    //without having admin rights. 
                    TomlTable versionCustomFields = (TomlTable)config["VersionCustomFields"];
                    foreach (var field in versionCustomFields)
                    {
                        foreach (var fieldValue in (TomlArray)field.Value)
                        {
                            redmineVersionFields.Add((string)fieldValue);
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

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IRedmineClient>(provider => {
                var configuration = provider.GetRequiredService<IConfiguration>();
                TomlTable config = GetConfigurationTable(configuration.PluginConfigDir, $"{name}.toml");
                string apiEndpoint = configuration.CommandLineOptionOrDefault("RedmineAPIEndpoint", GetObjectForKey<string>(config, "RedmineAPIEndpoint", true)
                );
                return new RestSharpRedmineClient(apiEndpoint);
            });
            services.AddTransient(this.GetType());
        }

        private List<string> GetTrackerList()
        {
            var result = new List<string> { PrsConfig.Name, SrsConfig.Name, TcConfig.Name,
                                            BugConfig.Name, RiskConfig.Name, SoupConfig.Name,
                                            DocConfig.Name, CntConfig.Name };
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
                if (redmineIssue.Tracker.Name == PrsConfig.Name)
                {
                    logger.Debug($"System level requirement found: {redmineIssue.Id}");
                    var req = CreateRequirement(redmineIssues, redmineIssue, RequirementType.SystemRequirement);
                    string reason;
                    if (!ShouldIgnoreIssue(redmineIssue, PrsConfig, out reason))
                    {
                        systemRequirements.Add(req);
                    }
                    else
                    {                      
                        eliminatedSystemRequirements.Add(new EliminatedRequirementItem(req, reason, EliminationReason.FilteredOut));
                        retrievedIDs.Remove(redmineIssue.Id.ToString());
                    }
                }
                else if (redmineIssue.Tracker.Name == SrsConfig.Name)
                {
                    logger.Debug($"Software level requirement found: {redmineIssue.Id}");
                    var req = CreateRequirement(redmineIssues, redmineIssue, RequirementType.SoftwareRequirement);
                    string reason;
                    if (!ShouldIgnoreIssue(redmineIssue, SrsConfig, out reason))
                    {
                        softwareRequirements.Add(req);
                    }
                    else
                    {
                        eliminatedSoftwareRequirements.Add(new EliminatedRequirementItem(req, reason, EliminationReason.FilteredOut));
                        retrievedIDs.Remove(redmineIssue.Id.ToString());
                    }
                }
                else if (redmineIssue.Tracker.Name == DocConfig.Name)
                {
                    logger.Debug($"Documentation item found: {redmineIssue.Id}");
                    var req = CreateRequirement(redmineIssues, redmineIssue, RequirementType.DocumentationRequirement);
                    string reason;
                    if (!ShouldIgnoreIssue(redmineIssue, DocConfig, out reason))
                    {
                        documentationRequirements.Add(req);
                    }
                    else
                    {
                        eliminatedDocumentationRequirements.Add(new EliminatedRequirementItem(req, reason, EliminationReason.FilteredOut));
                        retrievedIDs.Remove(redmineIssue.Id.ToString());
                    }
                }
                else if (redmineIssue.Tracker.Name == TcConfig.Name)
                {
                    logger.Debug($"Testcase found: {redmineIssue.Id}");
                    var tc = CreateTestCase(redmineIssues, redmineIssue);
                    string reason;
                    if (!ShouldIgnoreIssue(redmineIssue, TcConfig, out reason))
                    {
                        testCases.Add(tc);
                    }
                    else
                    {
                        eliminatedSoftwareSystemTests.Add(new EliminatedSoftwareSystemTestItem(tc, reason, EliminationReason.FilteredOut));
                        retrievedIDs.Remove(redmineIssue.Id.ToString());
                    }
                }
                else if (redmineIssue.Tracker.Name == BugConfig.Name)
                {
                    logger.Debug($"Bug item found: {redmineIssue.Id}");
                    string reason;
                    if (!ShouldIgnoreIssue(redmineIssue, BugConfig, out reason))
                    {
                        anomalies.Add(CreateBug(redmineIssue));
                    }
                    else
                    {
                        retrievedIDs.Remove(redmineIssue.Id.ToString());
                    }
                }
                else if (redmineIssue.Tracker.Name == RiskConfig.Name)
                {
                    logger.Debug($"Risk item found: {redmineIssue.Id}");
                    var risk = CreateRisk(redmineIssues, redmineIssue);
                    string reason;
                    if (!ShouldIgnoreIssue(redmineIssue, RiskConfig, out reason))
                    {
                        risks.Add(risk);
                    }
                    else
                    {
                        eliminatedRisks.Add(new EliminatedRiskItem(risk, reason, EliminationReason.FilteredOut));
                        retrievedIDs.Remove(redmineIssue.Id.ToString());
                    }
                }
                else if (redmineIssue.Tracker.Name == SoupConfig.Name)
                {
                    logger.Debug($"SOUP item found: {redmineIssue.Id}");
                    string reason;
                    if (!ShouldIgnoreIssue(redmineIssue, SoupConfig, out reason))
                    {
                        soup.Add(CreateSOUP(redmineIssue));
                    }
                    else
                    {
                        retrievedIDs.Remove(redmineIssue.Id.ToString());
                    }
                }
                else if (redmineIssue.Tracker.Name == CntConfig.Name)
                {
                    logger.Debug($"DocContent item found: {redmineIssue.Id}");
                    string reason;
                    if (!ShouldIgnoreIssue(redmineIssue, CntConfig, out reason))
                    {
                        docContents.Add(CreateDocContent(redmineIssue));
                    }
                    else
                    {
                        retrievedIDs.Remove(redmineIssue.Id.ToString());
                    }
                }
            }
            RemoveAllItemsNotLinked(retrievedIDs);
            RemoveIgnoredLinks(retrievedIDs); //go over all items and remove any links to ignored items
            ScrubItemContents(); //go over all relevant items and escape any | characters
        }

        private (List<T>, List<T>) CheckForLinkedItem<T>(List<string> retrievedIDs, List<T> inputItems, List<ItemLinkType> lt) where T : LinkedItem
        {
            List<T> items = new List<T>();
            List<T> removedItems = new List<T>();
            string removeItem = string.Empty;
            foreach (var item in inputItems)
            {
                if(item.LinkedItems.Count() > 0) //orphan items without links are always included so they don't get lost or ingnored.
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
                    logger.Warn($"Removing {item.ItemType} {item.ItemID} because item(s) {string.Join(',',item.LinkedItems.Select(item => item.TargetID))} it was originally linked to is(are) not valid.");
                    removedItems.Add(item);
                    retrievedIDs.Remove(removeItem);
                    removeItem = string.Empty;
                }
            }
            return (items, removedItems);
        }

        private void RemoveAllItemsNotLinked(List<string> retrievedIDs)
        { 
            var(keptSoftwareReqs, eliminatedSoftwareReqs)  = CheckForLinkedItem(retrievedIDs, softwareRequirements, new List<ItemLinkType> { ItemLinkType.Parent });
            softwareRequirements = keptSoftwareReqs;
            foreach(var item  in eliminatedSoftwareReqs)
            {
                eliminatedSoftwareRequirements.Add(new EliminatedRequirementItem(item, 
                    $"Removing {item.ItemType} {item.ItemID} because item(s) {string.Join(',', item.LinkedItems.Select(item => item.TargetID))} it was originally linked to is(are) not valid.",
                    EliminationReason.IgnoredLinkTarget));
            }

            var(keptTCs, eliminatedTCs) = CheckForLinkedItem(retrievedIDs, testCases, new List<ItemLinkType> { ItemLinkType.Tests });
            testCases = keptTCs;
            foreach (var item in eliminatedTCs)
            {
                eliminatedSoftwareSystemTests.Add(new EliminatedSoftwareSystemTestItem(item,
                    $"Removing {item.ItemType} {item.ItemID} because item(s) {string.Join(',', item.LinkedItems.Select(item => item.TargetID))} it was originally linked to is(are) not valid.",
                    EliminationReason.IgnoredLinkTarget));
            }

            var (keptDCs, eliminatedDCs) = CheckForLinkedItem(retrievedIDs, docContents, new List<ItemLinkType> { ItemLinkType.Parent });
            docContents = keptDCs;

            var(keptRSKs, eliminatedRSKs) = CheckForLinkedItem(retrievedIDs, risks, new List<ItemLinkType> { ItemLinkType.RiskControl });
            risks = keptRSKs;
            foreach (var item in eliminatedRSKs)
            {
                eliminatedRisks.Add(new EliminatedRiskItem(item,
                    $"Removing {item.ItemType} {item.ItemID} because item(s) {string.Join(',', item.LinkedItems.Select(item => item.TargetID))} it was originally linked to is(are) not valid.",
                    EliminationReason.IgnoredLinkTarget));
            }

            //need to remove any bugs connected to items that were removed.
            var (keptBGs, eliminatedBGs) = CheckForLinkedItem(retrievedIDs, anomalies, new List<ItemLinkType> { ItemLinkType.Related });
            anomalies = keptBGs;
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
        protected SoftwareSystemTestItem CreateTestCase(List<RedmineIssue> issues, RedmineIssue redmineItem)
        {
            logger.Debug($"Creating test case item: {redmineItem.Id}");
            SoftwareSystemTestItem resultItem = new SoftwareSystemTestItem();

            resultItem.ItemProject = redmineItem.Project.Name ?? string.Empty;
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
            string itemDescription = redmineItem.Description ?? string.Empty;
            var testCaseSteps = SoftwareSystemTestItem.GetTestSteps(convertTextile ? converter.ConvertTextile2AsciiDoc(itemDescription) : itemDescription);
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
                        if (field.Name == "Identifier") //check if this test case is being kicked to the unit test plan
                        {
                            string values = value.GetString();
                            var unitTests = values.Split(',');
                            foreach(var unitTest in unitTests)
                            {
                                resultItem.KickToUnitTest(unitTest.Trim());
                            }
                        }
                    }
                }
            }

            AddLinksToItem(redmineItem, resultItem);
            //ensure that the relationships to requirements are of type "Tests"
            foreach (var link in resultItem.LinkedItems)
            {
                foreach (var issue in issues)
                {
                    if (issue.Id.ToString() == link.TargetID && issue.Tracker.Name == SrsConfig.Name)
                    {
                        link.LinkType = ItemLinkType.Tests;
                        break;
                    }
                }
            }

            return resultItem;
        }

        protected DocContentItem CreateDocContent(RedmineIssue redmineItem)
        {
            logger.Debug($"Creating DocContent item: {redmineItem.Id}");
            DocContentItem resultItem = new DocContentItem();

            resultItem.ItemProject = redmineItem.Project.Name ?? string.Empty;
            resultItem.ItemID = redmineItem.Id.ToString();
            resultItem.ItemRevision = redmineItem.UpdatedOn.ToString();
            resultItem.ItemLastUpdated = (DateTime)redmineItem.UpdatedOn;
            resultItem.ItemStatus = redmineItem.Status.Name ?? string.Empty;
            string itemDescription = redmineItem.Description.ToString();
            resultItem.DocContent = convertTextile?converter.ConvertTextile2AsciiDoc(itemDescription):itemDescription;
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

        protected SOUPItem CreateSOUP(RedmineIssue redmineItem)
        {
            logger.Debug($"Creating SOUP item: {redmineItem.Id}");
            SOUPItem resultItem = new SOUPItem();

            resultItem.ItemProject = redmineItem.Project.Name ?? string.Empty;
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
                        string detailedDescription = value.GetString();
                        resultItem.SOUPDetailedDescription = convertTextile ? converter.ConvertTextile2AsciiDoc(detailedDescription) : detailedDescription;
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

        protected AnomalyItem CreateBug(RedmineIssue redmineItem)
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

            resultItem.ItemProject = redmineItem.Project.Name ?? string.Empty;
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

        protected RiskItem CreateRisk(List<RedmineIssue> issues, RedmineIssue redmineItem)
        {
            logger.Debug($"Creating risk item: {redmineItem.Id}");
            RiskItem resultItem = new RiskItem();

            resultItem.ItemProject = redmineItem.Project.Name ?? string.Empty;
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
                logger.Error($"Expected 1 related link for risk item \"{resultItem.ItemID}\". Multiple related items linked. Please check the item in Redmine.");
                throw new Exception($"Only a single related link to risk item \"{resultItem.ItemID}\" allowed. Cannot determine if traceability to risk item is correct.");
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

        private string ConvertValue(bool version, string value)
        {
            if (!version)
                return value;
            foreach( var rmVersion in versions )
            {
                if (rmVersion.Id.ToString() == value)
                {
                    return rmVersion.Name;
                }
            }
            return value;
        }

        protected bool ShouldIgnoreIssue(RedmineIssue redmineItem, TruthItemConfig config, out string reason)
        {
            reason = string.Empty;
            if (!config.Filtered)
                return false;

            var properties = redmineItem.GetType().GetProperties();
            foreach (var property in properties)
            {
                if (property.Name != "CustomFields" &&
                    property.Name != "EstimatedHours" &&
                    property.Name != "Relations")
                {
                    HashSet<string> values = new HashSet<string>();
                    var value = property.GetValue(redmineItem);
                    if (value != null)
                    {
                        if (value is string || value is int || value is bool)
                        {
                            values.Add(value.ToString());
                        }
                        else if (value is DateTime date)
                        {
                            values.Add(date.ToString("MM-dd-yyyy"));
                        }
                        else
                        {
                            PropertyInfo nameProperty = value.GetType().GetProperty("Name");
                            if (nameProperty != null && nameProperty.PropertyType == typeof(string))
                            {
                                values.Add(nameProperty.GetValue(value) as string);
                            }
                        }
                        if (!IncludeItem(property.Name, values))
                        {
                            reason = $"Item does not match inclusion filter for \"{property.Name}\" with value \"{String.Join(", ", values)}\"";
                            logger.Warn($"Ignoring {redmineItem.Tracker.Name} item {redmineItem.Id} due to \"{property.Name}\" not matching inclusion filter.");
                            return true;
                        }
                        if (ExcludeItem(property.Name, values))
                        {
                            reason = $"Item matches exclusion filter for \"{property.Name}\" with value \"{String.Join(", ", values)}\"";
                            logger.Warn($"Ignoring {redmineItem.Tracker.Name} item {redmineItem.Id} due to \"{property.Name}\" matching exclusion filter.");
                            return true;
                        }
                    }
                }
            }
            if (redmineItem.CustomFields.Count != 0)
            {
                foreach (var field in redmineItem.CustomFields)
                {
                    if (field.Value != null)
                    {
                        bool versionField = redmineVersionFields.Contains(field.Name);
                        HashSet<string> values = new HashSet<string>();
                        
                        if (field.Value is System.Text.Json.JsonElement jsonElement)
                        {
                            if (field.Multiple && jsonElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                            {
                                foreach (var value in jsonElement.EnumerateArray())
                                {
                                    values.Add(ConvertValue(versionField, value.ToString()));
                                }
                            }
                            else if (jsonElement.ValueKind != System.Text.Json.JsonValueKind.Null &&
                                jsonElement.ValueKind != System.Text.Json.JsonValueKind.Undefined)
                            {
                                values.Add(ConvertValue(versionField, jsonElement.ToString()));
                            }
                        }
                        else
                        {
                            if (field.Multiple && field.Value is System.Collections.IEnumerable enumerable && !(field.Value is string))
                            {
                                foreach (var value in enumerable)
                                {
                                    values.Add(ConvertValue(versionField, value?.ToString() ?? string.Empty));
                                }
                            }
                            else
                            {
                                values.Add(ConvertValue(versionField, field.Value?.ToString() ?? string.Empty));
                            }
                        }
                        
                        if (!IncludeItem(field.Name, values))
                        {
                            reason = $"Item does not match inclusion filter for custom field \"{field.Name}\" with value \"{String.Join(", ", values)}\"";
                            logger.Warn($"Ignoring {redmineItem.Tracker.Name} item {redmineItem.Id} due to \"{field.Name}\" not matching inclusion filter.");
                            return true;
                        }
                        if (ExcludeItem(field.Name, values))
                        {
                            reason = $"Item matches exclusion filter for custom field \"{field.Name}\" with value \"{String.Join(", ", values)}\"";
                            logger.Warn($"Ignoring {redmineItem.Tracker.Name} item {redmineItem.Id} due to \"{field.Name}\" matching exclusion filter.");
                            return true;
                        }
                    }
                }
            }
            return false;
        }
       
        protected RequirementItem CreateRequirement(List<RedmineIssue> issues, RedmineIssue redmineItem, RequirementType requirementType)
        {
            logger.Debug($"Creating requirement item: {redmineItem.Id}");
            RequirementItem resultItem = new RequirementItem(requirementType);

            resultItem.ItemProject = redmineItem.Project.Name ?? string.Empty;
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
            string itemDescription = redmineItem.Description ?? string.Empty;
            resultItem.RequirementDescription = convertTextile ? converter.ConvertTextile2AsciiDoc(itemDescription) : itemDescription;
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

            //ensure that the relationships to SoftwareSystemTests are of type "TestedBy"
            foreach (var link in resultItem.LinkedItems)
            {
                foreach (var issue in issues)
                {
                    if (issue.Id.ToString() == link.TargetID && issue.Tracker.Name == TcConfig.Name)
                    {
                        link.LinkType = ItemLinkType.TestedBy;
                    }
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
            var response = _client.GetAsync<RedmineProjects>(request);
            projects.AddRange(response.Projects);
            while (response.Limit + response.Offset < response.TotalCount)
            {
                request.AddOrUpdateParameter("offset", response.Offset + response.Limit);
                response = _client.GetAsync<RedmineProjects>(request);
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

            var response = _client.GetAsync<RedmineTrackers>(request);
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
            var response = _client.GetAsync<RedmineIssues>(request);
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
            var response = _client.GetAsync<RedmineIssues>(request);
            issues.AddRange(response.Issues);
            while (response.Limit + response.Offset < response.TotalCount)
            {
                request.AddOrUpdateParameter("offset", response.Offset + response.Limit);
                response = _client.GetAsync<RedmineIssues>(request);
                issues.AddRange(response.Issues);
            }
            return issues;
        }

        private List<RedmineIssue> PullAllIssuesFromServer(List<string> queryTrackers)
        {
            List<RedmineIssue> issueList = new List<RedmineIssue>();
            foreach (var pName in projectNames)
            {
                int projectID = GetProjectID(pName);
                versions = PullAllVersionsFromServer(projectID);
                var trackers = GetTrackers();
                foreach (var queryTracker in queryTrackers)
                {
                    if (!trackers.ContainsKey(queryTracker))
                    {
                        throw new Exception($"Tracker \"{queryTracker}\" is not present on the Redmine server. Please check plugin configuration file and Redmine server.");
                    }
                    issueList.AddRange(GetIssues(projectID, trackers[queryTracker]));
                }
            }
            return issueList;
        }

        private List<Version> PullAllVersionsFromServer(int projectID)
        {
            var request = new RestRequest($"projects/{projectID}/versions.json", Method.Get)
                .AddParameter("key", apiKey);
            var response = _client.GetAsync<VersionList>(request);
            if (response.TotalCount == 0)
            {
                return null;
            }
            else
            {
                return response.Versions;
            }
        }
    }
}
