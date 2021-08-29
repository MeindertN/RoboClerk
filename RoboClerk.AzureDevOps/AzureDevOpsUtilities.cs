using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RoboClerk.AzureDevOps
{
    internal static class AzureDevOpsUtilities
    {
        internal static VssConnection GetConnection(string orgName, string accessToken)
        {
            Uri orgURL = new Uri($"https://dev.azure.com/{orgName}");
            
            return new VssConnection(orgURL, new VssBasicCredential(string.Empty, accessToken));            
        }

        internal static WorkItemTrackingHttpClient GetWorkItemTrackingHttpClient(string orgName, string accessToken)
        {
            Uri orgURL = new Uri($"https://dev.azure.com/{orgName}");
            return new WorkItemTrackingHttpClient(orgURL, new VssBasicCredential(string.Empty, accessToken));
        }

        internal static IEnumerable<WorkItem> PerformWorkItemQuery(WorkItemTrackingHttpClient witClient, Wiql wiql)
        {
            var result = witClient.QueryByWiqlAsync(wiql).Result;
            var ids = result.WorkItems.Select(item => item.Id).ToArray();

            List<WorkItem> items = new List<WorkItem>();
            foreach (var id in ids)
            {
                // Get the specified work item
                WorkItem workitem = witClient.GetWorkItemAsync(id).Result;
                items.Add(workitem);
            }
            return items;
        }

        /*internal static List<Item> GetProductWorkItemsFromQueryResult()
        {

        }*/
    }
}
