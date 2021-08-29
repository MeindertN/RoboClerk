using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk.AzureDevOps
{
    class AzureDevOpsSLMSPlugin : ISLMSPlugin
    {
        private string name;
        private string description;
        private string organizationName;
        private string projectName;
        List<Item> productRequirements = new List<Item>();
        List<Item> softwareRequirements = new List<Item>();
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

        public IEnumerable<Item> GetProductRequirements()
        {
            throw new NotImplementedException();
        }

        public List<TraceItem> GetSoftwareRequirements()
        {
            throw new NotImplementedException();
        }

        public List<TraceItem> GetTestCases()
        {
            throw new NotImplementedException();
        }

        public void Initialize(string orgName, string proName, string accessToken)
        {
            organizationName = orgName;
            projectName = proName;
            witClient = AzureDevOpsUtilities.GetWorkItemTrackingHttpClient(organizationName, accessToken);
            RefreshItems();
        }

        private RequirementItem ConvertToRequirementItem(WorkItem workitem)
        {
            RequirementItem item = new RequirementItem();
            item.RequirementID = workitem.Id.ToString();
            item.RequirementLink = new Uri($"https://dev.azure.com/{organizationName}/{projectName}/_workitems/edit/{workitem.Id}/");
            item.RequirementRevision = workitem.Rev.ToString();
            //check out relations to see what they are
            item.RequirementState = workitem.Fields["System.State"].ToString();
            item.RequirementDescription = workitem.Fields["System.Description"].ToString();
            item.RequirementTitle = workitem.Fields["System.Title"].ToString();
            item.RequirementCategory = workitem.Fields["System.WorkItemType"].ToString();
            return item;
        }

        public void RefreshItems()
        {
            var productRequirementQuery = new Wiql()
            {
                Query = $"Select [Id] From WorkItems Where [Work Item Type] = 'Epic' And [System.TeamProject] = '{projectName}'",
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

            var productRequirementLinksQuery = new Wiql()
            {
                Query = $"SELECT [System.Id] FROM WorkItemLinks " +
                $"WHERE ([Source].[System.WorkItemType] = 'Epic' " +
                $"       AND [Source].[System.TeamProject] = '{projectName}' ) " +
                $"AND   ([System.Links.LinkType] = 'System.LinkTypes.Hierarchy-Forward') " +
                $"AND   ([Target].[System.WorkItemType] = 'User Story' " +
                $"       AND [Target].[System.State] != 'Removed' " +
                $"       AND [Target].[System.TeamProject] = '{projectName}' ) " +
                $"MODE (Recursive)",
            };

            foreach (var workitem in AzureDevOpsUtilities.PerformWorkItemQuery(witClient, softwareRequirementQuery))
            {
                var item = ConvertToRequirementItem(workitem);
                item.TypeOfRequirement = RequirementType.SoftwareRequirement;
                softwareRequirements.Add(item);
            }


            /*
            var softwareRequirementLinksQuery = new Wiql()
            {
                Query = $"SELECT [System.Id] FROM WorkItemLinks " +
                $"WHERE ([Source].[System.WorkItemType] = 'User Story') " +
                $"AND   ([System.Links.LinkType] = 'Parent') " +
                $"AND   ([Target].[System.State] != 'Removed') " +
                $"MODE (MustContain)",
            };*/


        }
    }
}
