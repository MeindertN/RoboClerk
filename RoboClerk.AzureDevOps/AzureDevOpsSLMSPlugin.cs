using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Tomlyn;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;


namespace RoboClerk.AzureDevOps
{
    class AzureDevOpsSLMSPlugin : ISLMSPlugin
    {
        private string name;
        private string description;
        private string organizationName;
        private string projectName;
        List<RequirementItem> productRequirements = new List<RequirementItem>();
        List<RequirementItem> softwareRequirements = new List<RequirementItem>();
        private WorkItemTrackingHttpClient witClient;

        public AzureDevOpsSLMSPlugin()
        {
            name = "AzureDevOpsSLMSPlugin";
            description = "A plugin that interfaces with azure devops to retrieve information needed by RoboClerk to create documentation.";
        }

        public string Name => name;

        public string Description => description;

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

        public List<TraceItem> GetTestCases()
        {
            throw new NotImplementedException();
        }

        public void Initialize()
        {
            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            var assembly = Assembly.GetAssembly(this.GetType());
            var configFileLocation = $"{Path.GetDirectoryName(assembly.Location)}/Configuration/AzureDevOpsPlugin.toml";
            var config = Toml.Parse(File.ReadAllText(configFileLocation)).ToModel();
            organizationName = (string)config["organizationName"];
            projectName = (string)config["projectName"];
            witClient = AzureDevOpsUtilities.GetWorkItemTrackingHttpClient(organizationName, (string)config["accessToken"]);
        }

        private void AddLinksToWorkItems(IList<WorkItemRelation> links, RequirementItem item)
        {
            if (links != null) //check for links
            {
                foreach (var rel in links)
                {
                    if (rel.Rel == "System.LinkTypes.Hierarchy-Forward")
                    {
                        //this is a child link
                        var id = AzureDevOpsUtilities.GetWorkItemIDFromURL(rel.Url);
                        item.AddChild(id);
                        continue;
                    }
                    if (rel.Rel == "System.LinkTypes.Hierarchy-Reverse")
                    {
                        //this is a parent link
                        var id = AzureDevOpsUtilities.GetWorkItemIDFromURL(rel.Url);
                        item.AddParent(id);
                        item.RequirementParentID = id;
                        item.RequirementParentLink = new Uri($"https://dev.azure.com/{organizationName}/{projectName}/_workitems/edit/{id}/");
                        continue;
                    }
                }
            }
        }

        private string GetWorkItemField(WorkItem workitem, string field)
        {
            if(workitem.Fields.ContainsKey(field))
            {
                return workitem.Fields[field].ToString();
            }
            else
            {
                return String.Empty;
            }
        }

        private RequirementItem ConvertToRequirementItem(WorkItem workitem)
        {
            RequirementItem item = new RequirementItem();
            item.RequirementID = workitem.Id.ToString();
            item.RequirementLink = new Uri($"https://dev.azure.com/{organizationName}/{projectName}/_workitems/edit/{workitem.Id}/");
            item.RequirementRevision = workitem.Rev.ToString();
            item.RequirementState = GetWorkItemField(workitem,"System.State");
            item.RequirementDescription = GetWorkItemField(workitem, "System.Description");
            item.RequirementTitle = GetWorkItemField(workitem, "System.Title");
            item.RequirementCategory = GetWorkItemField(workitem, "System.WorkItemType");
            AddLinksToWorkItems(workitem.Relations, item);
            return item;
        }

        public void RefreshItems()
        {
            var productRequirementQuery = new Wiql()
            {
                Query = $"SELECT [Id] FROM WorkItems WHERE [Work Item Type] = 'Epic' AND [System.TeamProject] = '{projectName}'",
            };

            foreach( var workitem in AzureDevOpsUtilities.PerformWorkItemQuery(witClient,productRequirementQuery))
            {
                var item = ConvertToRequirementItem(workitem);
                item.TypeOfRequirement = RequirementType.ProductRequirement;
                productRequirements.Add(item);
            }

            var softwareRequirementQuery = new Wiql()
            {
                Query = $"Select [Id] From WorkItems Where [Work Item Type] = 'User Story' And [System.TeamProject] = '{projectName}'",
            };

            foreach (var workitem in AzureDevOpsUtilities.PerformWorkItemQuery(witClient, softwareRequirementQuery))
            {
                var item = ConvertToRequirementItem(workitem);
                item.TypeOfRequirement = RequirementType.SoftwareRequirement;
                softwareRequirements.Add(item);
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
