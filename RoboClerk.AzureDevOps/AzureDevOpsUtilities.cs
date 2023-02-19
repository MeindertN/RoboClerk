using HtmlAgilityPack;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using WorkItem = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem;

namespace RoboClerk.AzureDevOps
{
    internal static class AzureDevOpsUtilities
    {
        internal static WorkItemTrackingHttpClient GetWorkItemTrackingHttpClient(string orgName, string accessToken)
        {
            Uri orgURL = new Uri($"https://dev.azure.com/{orgName}");
            return new WorkItemTrackingHttpClient(orgURL, new VssBasicCredential(string.Empty, accessToken));
        }

        internal static IEnumerable<WorkItem> PerformWorkItemQuery(WorkItemTrackingHttpClient witClient, Wiql wiql)
        {
            var result = witClient.QueryByWiqlAsync(wiql).Result;
            int[] ids = result.WorkItems.Select(item => item.Id).ToArray();

            List<WorkItem> items = new List<WorkItem>();
            foreach (var id in ids)
            {
                // Get the specified work item
                WorkItem workitem = witClient.GetWorkItemAsync(id, expand: WorkItemExpand.All).Result;
                items.Add(workitem);
            }
            return items;
        }

        internal static string GetWorkItemIDFromURL(string URL)
        {
            return URL.Substring(URL.LastIndexOf('/') + 1, URL.Length - (URL.LastIndexOf('/') + 1));
        }

    }

}
