using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Tomlyn;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;

namespace RoboClerk.AzureDevOps
{
    class AzureDevOpsSLMSPlugin : ISLMSPlugin
    {
        private string name;
        private string description;
        private string organizationName;
        private string projectName;
        private bool ignoreNewProductReqs = false;
        private List<RequirementItem> systemRequirements = new List<RequirementItem>();
        private List<RequirementItem> softwareRequirements = new List<RequirementItem>();
        private List<TestCaseItem> testCases = new List<TestCaseItem>();
        private List<BugItem> bugs = new List<BugItem>();
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private WorkItemTrackingHttpClient witClient;

        public AzureDevOpsSLMSPlugin()
        {
            logger.Debug("Azure DevOps SLMS plugin created");
            name = "AzureDevOpsSLMSPlugin";
            description = "A plugin that interfaces with azure devops to retrieve information needed by RoboClerk to create documentation.";
        }

        public string Name => name;

        public string Description => description;

        public List<BugItem> GetBugs()
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

        public void Initialize()
        {
            logger.Info("Initializing the Azure DevOps SLMS Plugin");
            var assembly = Assembly.GetAssembly(this.GetType());
            try
            {
                var configFileLocation = $"{Path.GetDirectoryName(assembly.Location)}/Configuration/AzureDevOpsPlugin.toml";
                var config = Toml.Parse(File.ReadAllText(configFileLocation)).ToModel();
                organizationName = (string)config["OrganizationName"];
                projectName = (string)config["ProjectName"];
                witClient = AzureDevOpsUtilities.GetWorkItemTrackingHttpClient(organizationName, (string)config["AccessToken"]);
                ignoreNewProductReqs = (bool)config["IgnoreNewSystemRequirements"];
            }
            catch(Exception e)
            {
                logger.Error("Error reading configuration file for Azure DevOps SLMS plugin.");
                logger.Error(e);
                throw new Exception("The Azure DevOps SLMS plugin could not read its configuration. Aborting...");
            }
        }

        private void AddLinksToWorkItems(IList<WorkItemRelation> links, LinkedItem item)
        {
            logger.Debug($"Adding links to workitem {item.ItemID}");
            if (links != null) //check for links
            {
                foreach (var rel in links)
                {
                    if (rel.Rel.Contains("Hierarchy-Forward"))
                    {
                        //this is a child link
                        var id = AzureDevOpsUtilities.GetWorkItemIDFromURL(rel.Url);
                        logger.Debug($"Child link found: {id}");
                        item.AddChild(id, new Uri($"https://dev.azure.com/{organizationName}/{projectName}/_workitems/edit/{id}/"));
                        continue;
                    }
                    if (rel.Rel.Contains("Hierarchy-Reverse") || rel.Rel.Contains("TestedBy-Reverse"))
                    {
                        //this is a parent link
                        var id = AzureDevOpsUtilities.GetWorkItemIDFromURL(rel.Url);
                        logger.Debug($"Parent link found: {id}");
                        item.AddParent(id, new Uri($"https://dev.azure.com/{organizationName}/{projectName}/_workitems/edit/{id}/"));
                        continue;
                    }
                }
            }
        }

        private string GetWorkItemField(WorkItem workitem, string field)
        {
            if(workitem.Fields.ContainsKey(field))
            {
                var ident = workitem.Fields[field] as Microsoft.VisualStudio.Services.WebApi.IdentityRef;
                if (ident != null)
                {
                    return ident.DisplayName;
                }
                else
                {
                    return workitem.Fields[field].ToString();
                }
            }
            else
            {
                return String.Empty;
            }
        }

        private RequirementItem ConvertToRequirementItem(WorkItem workitem)
        {
            logger.Debug($"Creating requirement item for: {workitem.Id.ToString()}");
            RequirementItem item = new RequirementItem();
            item.RequirementID = workitem.Id.ToString();
            item.Link = new Uri($"https://dev.azure.com/{organizationName}/{projectName}/_workitems/edit/{workitem.Id}/");
            item.RequirementRevision = workitem.Rev.ToString();
            item.RequirementState = GetWorkItemField(workitem,"System.State");
            item.RequirementDescription = GetWorkItemField(workitem, "System.Description");
            item.RequirementTitle = GetWorkItemField(workitem, "System.Title");
            AddLinksToWorkItems(workitem.Relations, item);
            return item;
        }

        private List<string[]> GetTestSteps(string xml)
        {
            var result = new List<string[]>();
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
                                    var steps = GetTestSteps(GetWorkItemField(workitem, "Microsoft.VSTS.TCM.Steps"));
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
                                    result.Add(stepData);
                                    stepData = new string[2];
                                }
                            }
                            break;
                    }
                }
            }
            return result;
        }

        private TestCaseItem ConvertToTestCaseItem(WorkItem workitem)
        {
            logger.Debug($"Creating testcase item for: {workitem.Id.ToString()}");
            TestCaseItem item = new TestCaseItem();
            item.TestCaseID = workitem.Id.ToString();
            item.Link = new Uri($"https://dev.azure.com/{organizationName}/{projectName}/_workitems/edit/{workitem.Id}/");
            item.TestCaseRevision = workitem.Rev.ToString();
            item.TestCaseState = GetWorkItemField(workitem, "System.State");
            item.TestCaseTitle = GetWorkItemField(workitem, "System.Title");
            item.TestCaseDescription = GetWorkItemField(workitem, "System.Description");
            item.TestCaseSteps = GetTestSteps(GetWorkItemField(workitem, "Microsoft.VSTS.TCM.Steps"));
            item.TestCaseAutomated = GetWorkItemField(workitem, "Microsoft.VSTS.TCM.AutomationStatus") != "Not Automated";
            AddLinksToWorkItems(workitem.Relations, item);
            return item;
        }

        private BugItem ConvertToBugItem(WorkItem workitem)
        {
            logger.Debug($"Creating bug item for: {workitem.Id.ToString()}");
            BugItem item = new BugItem();
            item.BugID = workitem.Id.ToString();
            item.Link = new Uri($"https://dev.azure.com/{organizationName}/{projectName}/_workitems/edit/{workitem.Id}/");
            item.BugRevision = workitem.Rev.ToString();
            item.BugState = GetWorkItemField(workitem, "System.State");
            item.BugJustification = GetWorkItemField(workitem, "Microsoft.VSTS.CMMI.Justification");
            item.BugAssignee = GetWorkItemField(workitem, "System.AssignedTo");
            item.BugPriority = GetWorkItemField(workitem, "Microsoft.VSTS.Common.Priority");
            item.BugTitle = GetWorkItemField(workitem, "System.Title");
            return item;
        }

        public void RefreshItems()
        {
            //re-initialize 
            systemRequirements.Clear();
            softwareRequirements.Clear();
            testCases.Clear();

            logger.Info("Retrieving and processing product level requirements.");
            var systemRequirementQuery = new Wiql()
            {
                Query = $"SELECT [Id] FROM WorkItems WHERE [Work Item Type] = 'Epic' AND [System.TeamProject] = '{projectName}'",
            };

            foreach (var workitem in AzureDevOpsUtilities.PerformWorkItemQuery(witClient, systemRequirementQuery))
            {
                string state = GetWorkItemField(workitem, "System.State").ToUpper();
                if ( (ignoreNewProductReqs &&  state == "NEW") || state == "REMOVED" )
                {
                    continue;
                }
                var item = ConvertToRequirementItem(workitem);
                item.TypeOfRequirement = RequirementType.SystemRequirement;
                item.RequirementCategory = GetWorkItemField(workitem, "Custom.TypeofSystemRequirement");
                if(item.RequirementCategory == String.Empty) //default sometimes comes back as empty
                {
                    item.RequirementCategory = "Product Requirement";
                }
                systemRequirements.Add(item);
            }

            logger.Info("Retrieving and processing software level requirements.");
            var softwareRequirementQuery = new Wiql()
            {
                Query = $"Select [Id] From WorkItems Where [Work Item Type] = 'User Story' And [System.TeamProject] = '{projectName}'",
            };

            foreach (var workitem in AzureDevOpsUtilities.PerformWorkItemQuery(witClient, softwareRequirementQuery))
            {
                var item = ConvertToRequirementItem(workitem);
                item.TypeOfRequirement = RequirementType.SoftwareRequirement;
                item.RequirementCategory = GetWorkItemField(workitem, "Custom.SoftwareRequirementType");
                if (item.RequirementCategory == String.Empty) //default sometimes comes back as empty
                {
                    item.RequirementCategory = "Software Requirement";
                }
                softwareRequirements.Add(item);
            }

            logger.Info("Retrieving and processing testcases.");
            var testCaseQuery = new Wiql()
            {
                Query = $"Select [Id] From WorkItems Where [Work Item Type] = 'Test Case' And [System.TeamProject] = '{projectName}'",
            };

            foreach (var workitem in AzureDevOpsUtilities.PerformWorkItemQuery(witClient, testCaseQuery))
            {
                var item = ConvertToTestCaseItem(workitem);
                testCases.Add(item);
            }

            logger.Info("Retrieving and processing bugs and issues.");
            var bugQuery = new Wiql()
            {
                Query = $"Select [Id] From WorkItems Where ( [Work Item Type] = 'Issue' Or [Work Item Type] = 'Bug' ) And [System.TeamProject] = '{projectName}'",
            };

            foreach (var workitem in AzureDevOpsUtilities.PerformWorkItemQuery(witClient, bugQuery))
            {
                var item = ConvertToBugItem(workitem);
                bugs.Add(item);
            }

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
            };

            foreach (var workitem in AzureDevOpsUtilities.PerformWorkItemQuery(witClient, productRequirementLinksQuery))
            {
                var item = ConvertToRequirementItem(workitem);
                item.TypeOfRequirement = RequirementType.SoftwareRequirement;
                softwareRequirements.Add(item);
            }*/
        }

        
    }
}
