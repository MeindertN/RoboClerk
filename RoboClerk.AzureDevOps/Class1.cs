using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using RoboClerk;

namespace RoboClerk.AzureDevOps
{
    public class AzureDevOpsSLMS
    {
        public AzureDevOpsSLMS()
        {

        }

        public void UpdateWorkItemStructure()
        {
            string organizationName = "meindert";
            string projectName = "RoboClerk";
            Uri orgURL = new Uri("https://dev.azure.com/meindert");
            string accessToken = "ailboebvbt75z3pgb2t6rynp6z2ywwmeg2dhduomfc7p4wtkhl3a";

            VssConnection connection = new VssConnection(orgURL, new VssBasicCredential(string.Empty, accessToken));

            var wiql = new Wiql()
            {
                Query = $"Select [Id] From WorkItems Where [Work Item Type] = 'Epic' And [System.TeamProject] = 'RoboClerk'",
            };

            ShowWorkItemDetails(connection, wiql).Wait();
        }
        private async Task ShowWorkItemDetails(VssConnection connection, Wiql wiql)
        {
            // Get an instance of the work item tracking client
            WorkItemTrackingHttpClient witClient = connection.GetClient<WorkItemTrackingHttpClient>();

            var result = await witClient.QueryByWiqlAsync(wiql).ConfigureAwait(false);
            var ids = result.WorkItems.Select(item => item.Id).ToArray();

            foreach (var id in ids)
            {
                try
                {
                    // Get the specified work item
                    WorkItem workitem = await witClient.GetWorkItemAsync(id);

                    // Output the work item's field values
                    foreach (var field in workitem.Fields)
                    {
                        Console.WriteLine("  {0}: {1}", field.Key, field.Value);
                    }
                }
                catch (AggregateException aex)
                {
                    VssServiceException vssex = aex.InnerException as VssServiceException;
                    if (vssex != null)
                    {
                        Console.WriteLine(vssex.Message);
                    }
                }
            }
        }
    }

}
