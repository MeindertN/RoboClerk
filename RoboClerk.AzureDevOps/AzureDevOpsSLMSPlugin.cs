using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using RoboClerk.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace RoboClerk.AzureDevOps
{
    class AzureDevOpsSLMSPlugin : SLMSPluginBase
    {
        private string organizationName = string.Empty;
        private string projectName = string.Empty;
        private WorkItemTrackingHttpClient witClient;

        public AzureDevOpsSLMSPlugin(IFileProviderPlugin fileSystem)
            : base(fileSystem)
        {
            logger.Debug("Azure DevOps SLMS plugin created");
            SetBaseParam();
        }

        private void SetBaseParam()
        {
            name = "AzureDevOpsSLMSPlugin";
            description = "A plugin that interfaces with azure devops to retrieve information needed by RoboClerk to create documentation.";
        }

        public override void InitializePlugin(IConfiguration configuration)
        {
            logger.Info("Initializing the Azure DevOps SLMS Plugin");
            base.InitializePlugin(configuration);
            try
            {
                var config = GetConfigurationTable(configuration.PluginConfigDir, $"{name}.toml");
                organizationName = configuration.CommandLineOptionOrDefault("OrganizationName", (string)config["OrganizationName"]);
                projectName = configuration.CommandLineOptionOrDefault("ProjectName", (string)config["ProjectName"]);
                witClient = AzureDevOpsUtilities.GetWorkItemTrackingHttpClient(organizationName,
                    configuration.CommandLineOptionOrDefault("AccessToken", (string)config["AccessToken"]));
            }
            catch (Exception e)
            {
                logger.Error("Error reading configuration file for Azure DevOps SLMS plugin.");
                logger.Error(e);
                throw new Exception("The Azure DevOps SLMS plugin could not read its configuration. Aborting...");
            }
        }
        private IEnumerable<WorkItem> GetWorkItems(string itemName)
        {
            var itemQuery = new Wiql()
            {
                Query = $"SELECT [Id] FROM WorkItems WHERE [Work Item Type] = '{itemName}' AND [System.TeamProject] = '{projectName}'",
            };
            return AzureDevOpsUtilities.PerformWorkItemQuery(witClient, itemQuery);
        }

        private bool IgnoreItem(WorkItem workitem)
        {
            string state = GetWorkItemField<string>(workitem, "System.State");
            return ignoreList.Contains(state);
        }

        public override void RefreshItems()
        {
            ClearAllItems();
            List<string> retrievedIDs = new List<string>();

            logger.Info("Retrieving and processing system level requirements.");
            foreach (var workitem in GetWorkItems(PrsConfig.Name))
            {
                if (IgnoreItem(workitem)) continue;
                retrievedIDs.Add(workitem.Id.ToString());
                var item = ConvertToRequirementItem(workitem, RequirementType.SystemRequirement);
                systemRequirements.Add(item);
            }

            logger.Info("Retrieving and processing software level requirements.");
            foreach (var workitem in GetWorkItems(SrsConfig.Name))
            {
                if (IgnoreItem(workitem)) continue;
                retrievedIDs.Add(workitem.Id.ToString());
                var item = ConvertToRequirementItem(workitem, RequirementType.SoftwareRequirement);
                softwareRequirements.Add(item);
            }

            logger.Info("Retrieving and processing documentation requirements.");
            foreach (var workitem in GetWorkItems(DocConfig.Name))
            {
                if (IgnoreItem(workitem)) continue;
                retrievedIDs.Add(workitem.Id.ToString());
                var item = ConvertToRequirementItem(workitem, RequirementType.DocumentationRequirement);
                documentationRequirements.Add(item);
            }

            logger.Info("Retrieving and processing docContent requirements.");
            foreach (var workitem in GetWorkItems(CntConfig.Name))
            {
                if (IgnoreItem(workitem)) continue;
                retrievedIDs.Add(workitem.Id.ToString());
                var item = ConvertToDocContentItem(workitem);
                docContents.Add(item);
            }

            logger.Info("Retrieving and SOUP items.");
            foreach (var workitem in GetWorkItems(SoupConfig.Name))
            {
                if (IgnoreItem(workitem)) continue;
                retrievedIDs.Add(workitem.Id.ToString());
                var item = ConvertToSOUPItem(workitem);
                soup.Add(item);
            }

            logger.Info("Retrieving test cases.");
            foreach (var workitem in GetWorkItems(TcConfig.Name))
            {
                if (IgnoreItem(workitem)) continue;
                retrievedIDs.Add(workitem.Id.ToString());
                var item = ConvertToTestCaseItem(workitem);
                testCases.Add(item);
            }

            logger.Info("Retrieving and processing bugs.");
            
            foreach (var workitem in GetWorkItems(BugConfig.Name))
            {
                if (IgnoreItem(workitem)) continue;
                retrievedIDs.Add(workitem.Id.ToString());
                var item = ConvertToBugItem(workitem);
                anomalies.Add(item);
            }

            logger.Info("Retrieving and processing risks.");
            //Note that to gather all information about the risk item, this code relies on the 
            //system level requirements having been retrieved already.
            foreach (var workitem in GetWorkItems(RiskConfig.Name))
            {
                if (IgnoreItem(workitem)) continue;
                retrievedIDs.Add(workitem.Id.ToString());
                var item = ConvertToRiskItem(workitem);
                risks.Add(item);
            }

            // go over all linked items and remove any links to items that we don't know about
            TrimLinkedItems(systemRequirements, retrievedIDs);
            TrimLinkedItems(softwareRequirements, retrievedIDs);
            TrimLinkedItems(documentationRequirements, retrievedIDs);
            TrimLinkedItems(testCases, retrievedIDs);
            TrimLinkedItems(anomalies, retrievedIDs);
            TrimLinkedItems(risks, retrievedIDs);
            TrimLinkedItems(soup, retrievedIDs);
            TrimLinkedItems(docContents, retrievedIDs);

            ScrubItemContents(); //go over all relevant items and escape any | characters

            /*var productRequirementLinksQuery = new Wiql()
            {
                Query = $"SELECT * FROM WorkItemLinks " +
                $"WHERE ([Source].[System.WorkItemType] = 'Epic' " +
                $"       AND [Source].[System.TeamProject] = '{projectName}' ) " +
                $"AND   ([System.Links.LinkType] = 'System.LinkTypes.Hierarchy-Forward') " +
                $"AND   ([Target].[System.WorkItemType] = 'User Story' " +
                $"       AND [Target].[System.State] != 'Removed' " +
                $"       AND [Target].[System.TeamProject] = '{projectName}' ) " +
                $"MODE (MustContain)",
            };*/
        }

        private void AddLinksToWorkItems(IList<WorkItemRelation> links, LinkedItem item)
        {
            logger.Debug($"Adding links to workitem {item.ItemID}");
            if (links != null) //check for links
            {
                foreach (var rel in links)
                {
                    if (rel.Rel.Contains("Hierarchy-Forward") || rel.Rel.Contains("TestedBy-Forward"))
                    {
                        //this is a child link
                        var id = AzureDevOpsUtilities.GetWorkItemIDFromURL(rel.Url);
                        logger.Debug($"Child link found: {id}");
                        item.AddLinkedItem(new ItemLink(id, ItemLinkType.Child));
                        continue;
                    }
                    if (rel.Rel.Contains("Hierarchy-Reverse") || rel.Rel.Contains("TestedBy-Reverse"))
                    {
                        //this is a parent link
                        var id = AzureDevOpsUtilities.GetWorkItemIDFromURL(rel.Url);
                        logger.Debug($"Parent link found: {id}");
                        item.AddLinkedItem(new ItemLink(id, ItemLinkType.Parent));
                        continue;
                    }
                    if (rel.Rel.Contains(".Related"))
                    {
                        //this is a related link
                        var id = AzureDevOpsUtilities.GetWorkItemIDFromURL(rel.Url);
                        logger.Debug($"Parent link found: {id}");
                        item.AddLinkedItem(new ItemLink(id, ItemLinkType.Related));
                        continue;
                    }
                    if (rel.Rel.Contains("SharedStepReferencedBy-Reverse"))
                    {
                        //we ignore these kinds of links as this type of traceability is not of interest to RoboClerk
                        continue;
                    }
                    logger.Warn($"Unknown link type encountered in workitem {item.ItemID}: {rel.Rel}");
                }
            }
        }

        private dynamic GetWorkItemField<T>(WorkItem workitem, string field)
        {
            if (workitem.Fields.ContainsKey(field))
            {
                var ident = workitem.Fields[field] as Microsoft.VisualStudio.Services.WebApi.IdentityRef;
                if (ident != null)
                {
                    return ident.DisplayName;
                }
                else
                {
                    return (T)workitem.Fields[field];
                }
            }
            else
            {
                //this can also happen if a field does exist on a workitem but it has no value (i.e. the user didn't enter anything) 
                logger.Warn($"Failed to retrieve field \"{field}\" from workitem {workitem.Id} of type \"{GetWorkItemField<string>(workitem, "System.WorkItemType")}\".");
                if (typeof(T) == typeof(string)) //ensure we return empty string instead of null
                    return string.Empty;
                return default(T);
            }
        }

        private RequirementItem ConvertToRequirementItem(WorkItem workitem, RequirementType rt)
        {
            logger.Debug($"Creating requirement item for: {workitem.Id}");
            RequirementItem item = new RequirementItem(rt);
            item.ItemID = workitem.Id.ToString();
            item.Link = new Uri($"https://dev.azure.com/{organizationName}/{projectName}/_workitems/edit/{workitem.Id}/");
            item.ItemRevision = workitem.Rev.ToString();
            item.ItemLastUpdated = GetWorkItemField<DateTime>(workitem, "System.ChangedDate");
            item.RequirementState = GetWorkItemField<string>(workitem, "System.State");
            item.ItemStatus = GetWorkItemField<string>(workitem, "System.State");
            item.RequirementDescription = HtmlToTextConverter.ToPlainText(GetWorkItemField<string>(workitem, "System.Description"));
            item.ItemTitle = GetWorkItemField<string>(workitem, "System.Title");
            item.ItemCategory = GetWorkItemField<string>(workitem, "Custom.CategoryofRequirement");
            AddLinksToWorkItems(workitem.Relations, item);
            return item;
        }

        private DocContentItem ConvertToDocContentItem(WorkItem workitem)
        {
            logger.Debug($"Creating doccontent item for: {workitem.Id}");
            DocContentItem item = new DocContentItem();
            item.ItemID = workitem.Id.ToString();
            item.Link = new Uri($"https://dev.azure.com/{organizationName}/{projectName}/_workitems/edit/{workitem.Id}/");
            item.ItemRevision = workitem.Rev.ToString();
            item.ItemLastUpdated = GetWorkItemField<DateTime>(workitem, "System.ChangedDate");
            item.ItemStatus = GetWorkItemField<string>(workitem, "System.State");
            item.DocContent = HtmlToTextConverter.ToPlainText(GetWorkItemField<string>(workitem, "System.Description"));
            item.ItemCategory = GetWorkItemField<string>(workitem, "Custom.CategoryofContent");
            AddLinksToWorkItems(workitem.Relations, item);
            return item;
        }

        private RequirementItem GetRequirementItem(string ID)
        {
            foreach (var ri in systemRequirements)
            {
                if (ri.ItemID == ID)
                {
                    return ri;
                }
            }
            foreach (var ri in softwareRequirements)
            {
                if (ri.ItemID == ID)
                {
                    return ri;
                }
            }
            foreach (var ri in documentationRequirements)
            {
                if (ri.ItemID == ID)
                {
                    return ri;
                }
            }
            return null;
        }

        //this function ensures that if a score is not set, it gets the maximum value
        private int ProcessRiskScores(Int64 rawScore)
        {
            if(rawScore <= 0)
            {
                return int.MaxValue;
            }
            else
            {
                return (int)rawScore;
            }
        }

        private RiskItem ConvertToRiskItem(WorkItem workitem)
        {
            logger.Debug($"Creating doccontent item for: {workitem.Id}");
            RiskItem item = new RiskItem();
            item.ItemID = workitem.Id.ToString();
            item.Link = new Uri($"https://dev.azure.com/{organizationName}/{projectName}/_workitems/edit/{workitem.Id}/");
            item.ItemRevision = workitem.Rev.ToString();
            item.ItemLastUpdated = GetWorkItemField<DateTime>(workitem, "System.ChangedDate");
            item.ItemStatus = GetWorkItemField<string>(workitem, "System.State");
            item.RiskCauseOfFailure = HtmlToTextConverter.ToPlainText(GetWorkItemField<string>(workitem, "System.Description"));
            item.ItemCategory = GetWorkItemField<string>(workitem, "Custom.RiskType");
            item.RiskPrimaryHazard = GetWorkItemField<string>(workitem, "Custom.Hazard");
            item.RiskSeverityScore = ProcessRiskScores(GetWorkItemField<Int64>(workitem, "Custom.HazardSeverity"));
            item.RiskOccurenceScore = ProcessRiskScores(GetWorkItemField<Int64>(workitem, "Custom.HazardProbability"));
            item.RiskModifiedOccScore = ProcessRiskScores(GetWorkItemField<Int64>(workitem, "Custom.ResidualProbability"));
            item.RiskModifiedDetScore = ProcessRiskScores(GetWorkItemField<Int64>(workitem, "Custom.ResidualDetectability"));
            item.RiskMethodOfDetection = GetWorkItemField<string>(workitem, "Custom.DetectionMethod");
            item.RiskDetectabilityScore = ProcessRiskScores(GetWorkItemField<Int64>(workitem, "Custom.Detection"));
            item.RiskControlMeasureType = GetWorkItemField<string>(workitem, "Custom.RiskControlCategory");
            item.RiskFailureMode = GetWorkItemField<string>(workitem, "System.Title");
            
            AddLinksToWorkItems(workitem.Relations, item);
            int nrOfResults = 0;
            if ((nrOfResults = item.LinkedItems.Where(x => x.LinkType == ItemLinkType.Related).Count()) > 1)
            {
                logger.Warn($"Expected 1 related link for risk item \"{item.ItemID}\". Multiple related items linked. Please check the item in AzureDevOps.");
            }
            else if (nrOfResults == 1)
            {
                ItemLink link = item.LinkedItems.Where(x => x.LinkType == ItemLinkType.Related).First();
                link.LinkType = ItemLinkType.RiskControl;
                var ri = GetRequirementItem(link.TargetID);

                if (ri != null)
                {
                    item.RiskControlMeasure = ri.ItemTitle;
                    item.RiskControlImplementation = ri.RequirementDescription;
                }
                else
                {
                    logger.Warn($"RoboClerk is unable the find the Azure Devops workitem {link.TargetID} that is linked to Risk workitem {item.ItemID}. Please check the Risk workitem in AzureDevops and its related workitems.");
                }         
            }
            return item;
        }

        private SOUPItem ConvertToSOUPItem(WorkItem workitem)
        {
            logger.Debug($"Creating SOUP item for: {workitem.Id}");
            SOUPItem item = new SOUPItem();
            item.ItemID = workitem.Id.ToString();
            item.Link = new Uri($"https://dev.azure.com/{organizationName}/{projectName}/_workitems/edit/{workitem.Id}/");
            item.ItemRevision = workitem.Rev.ToString();
            item.ItemLastUpdated = GetWorkItemField<DateTime>(workitem, "System.ChangedDate");
            item.ItemStatus = GetWorkItemField<string>(workitem, "System.State");
            item.SOUPDetailedDescription = HtmlToTextConverter.ToPlainText(GetWorkItemField<string>(workitem, "System.Description"));
            item.SOUPName = GetWorkItemField<string>(workitem, "System.Title");
            item.SOUPLicense = GetWorkItemField<string>(workitem, "Custom.License");
            item.SOUPAnomalyListDescription = GetWorkItemField<string>(workitem, "Custom.AnomalyListExamination");
            item.SOUPCybersecurityCriticalText = GetWorkItemField<string>(workitem, "Custom.CriticalforCybersecurity");
            item.SOUPCybersecurityCritical = !item.SOUPCybersecurityCriticalText.Contains(" not ");
            item.SOUPPerformanceCriticalText = GetWorkItemField<string>(workitem, "Custom.CriticalforPerformance");
            item.SOUPPerformanceCritical = !item.SOUPPerformanceCriticalText.Contains(" not ");
            item.SOUPVersion = GetWorkItemField<string>(workitem, "Custom.Version");
            item.SOUPLinkedLib = GetWorkItemField<bool>(workitem, "Custom.LinkedLibrary");
            item.SOUPEnduserTraining = HtmlToTextConverter.ToPlainText(GetWorkItemField<string>(workitem, "Custom.EndUserTraining"));
            item.SOUPInstalledByUserText = GetWorkItemField<string>(workitem, "Custom.EndUserInstallationRequired");
            item.SOUPInstalledByUser = !item.SOUPInstalledByUserText.Contains(" not ");
            item.SOUPManufacturer = GetWorkItemField<string>(workitem, "Custom.Manufacturer");
            return item;
        }

        private List<TestStep> GetTestSteps(string xml, int offset = 0)
        {
            var result = new List<TestStep>();
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreWhitespace = true;

            using (var memStream = new MemoryStream(Encoding.Unicode.GetBytes(xml)))
            using (XmlReader reader = XmlReader.Create(memStream, settings))
            {
                var stepData = new string[2];
                int index = 0;
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (reader.Name == "step")
                            {
                                index = 0;
                            }
                            if (reader.HasAttributes)
                            {
                                if (reader.Name == "compref")
                                {
                                    WorkItem workitem = witClient.GetWorkItemAsync(int.Parse(reader.GetAttribute("ref")), expand: WorkItemExpand.All).Result;
                                    var steps = GetTestSteps(GetWorkItemField<string>(workitem, "Microsoft.VSTS.TCM.Steps"), result.Count);
                                    result.AddRange(steps);
                                }
                            }
                            break;
                        case XmlNodeType.Text:
                            if (index < 2)
                            {
                                var noHTML = Regex.Replace(reader.Value, "<[a-zA-Z/].*?>", String.Empty);
                                stepData[index] = noHTML;
                                index++;
                                if (index == stepData.Length)
                                {
                                    result.Add(new TestStep((result.Count + offset + 1).ToString(), stepData[0], stepData[1]));
                                    stepData = new string[2];
                                }
                            }
                            break;
                    }
                }
            }
            return result;
        }

        private SoftwareSystemTestItem ConvertToTestCaseItem(WorkItem workitem)
        {
            logger.Debug($"Creating testcase item for: {workitem.Id}");
            SoftwareSystemTestItem item = new SoftwareSystemTestItem();
            item.ItemID = workitem.Id.ToString();
            item.Link = new Uri($"https://dev.azure.com/{organizationName}/{projectName}/_workitems/edit/{workitem.Id}/");
            item.ItemRevision = workitem.Rev.ToString();
            item.ItemLastUpdated = GetWorkItemField<DateTime>(workitem, "System.ChangedDate");
            item.TestCaseState = GetWorkItemField<string>(workitem, "System.State");
            item.ItemStatus = GetWorkItemField<string>(workitem, "System.State");
            item.ItemTitle = GetWorkItemField<string>(workitem, "System.Title");
            var testSteps = GetTestSteps(GetWorkItemField<string>(workitem, "Microsoft.VSTS.TCM.Steps"));
            foreach (var testStep in testSteps)
            {
                item.AddTestCaseStep(testStep);
            }
            item.TestCaseAutomated = GetWorkItemField<string>(workitem, "Microsoft.VSTS.TCM.AutomationStatus") != "Not Automated";
            AddLinksToWorkItems(workitem.Relations, item);
            return item;
        }

        private AnomalyItem ConvertToBugItem(WorkItem workitem)
        {
            logger.Debug($"Creating bug item for: {workitem.Id}");
            AnomalyItem item = new AnomalyItem();
            item.ItemID = workitem.Id.ToString();
            item.Link = new Uri($"https://dev.azure.com/{organizationName}/{projectName}/_workitems/edit/{workitem.Id}/");
            item.ItemRevision = workitem.Rev.ToString();
            item.ItemLastUpdated = GetWorkItemField<DateTime>(workitem, "System.ChangedDate");
            item.AnomalyState = GetWorkItemField<string>(workitem, "System.State");
            item.ItemStatus = GetWorkItemField<string>(workitem, "System.State");
            item.AnomalyJustification = HtmlToTextConverter.ToPlainText(GetWorkItemField<string>(workitem, "Microsoft.VSTS.CMMI.Justification"));
            item.AnomalyAssignee = GetWorkItemField<string>(workitem, "System.AssignedTo");
            item.AnomalySeverity = GetWorkItemField<string>(workitem, "Microsoft.VSTS.Common.Severity");
            item.AnomalyDetailedDescription = HtmlToTextConverter.ToPlainText(GetWorkItemField<string>(workitem, "Microsoft.VSTS.TCM.ReproSteps"));
            item.ItemTitle = GetWorkItemField<string>(workitem, "System.Title");
            return item;
        }
    }
}
