using Microsoft.Extensions.Logging;

namespace RoboClerk.Server.TestClient.Services
{
    public class WordAddInSimulator : IWordAddInSimulator
    {
        private readonly IRoboClerkServerClient serverClient;
        private readonly ILogger<WordAddInSimulator> logger;

        public WordAddInSimulator(IRoboClerkServerClient serverClient, ILogger<WordAddInSimulator> logger)
        {
            this.serverClient = serverClient;
            this.logger = logger;
        }

        public async Task<bool> SimulateWordAddInWorkflowAsync(string serverUrl, string sharePointProjectUrl, string documentName)
        {
            logger.LogInformation("?? Starting Word Add-in Workflow Simulation");
            logger.LogInformation("================================================");
            
            try
            {
                // Configure the server client
                if (serverClient is RoboClerkServerClient client)
                {
                    client.SetBaseAddress(serverUrl);
                }

                // Step 1: Health Check
                logger.LogInformation("?? Step 1: Server Health Check");
                logger.LogInformation("------------------------------");
                
                var healthResult = await serverClient.HealthCheckAsync();
                if (healthResult == null)
                {
                    logger.LogError("? Server health check failed - cannot continue");
                    return false;
                }
                
                await Task.Delay(1000); // Simulate user interaction delay
                
                // Step 2: Load SharePoint Project
                logger.LogInformation("");
                logger.LogInformation("?? Step 2: Load SharePoint Project");
                logger.LogInformation("----------------------------------");
                
                var projectResult = await serverClient.LoadProjectAsync(sharePointProjectUrl);
                if (projectResult?.Success != true || string.IsNullOrEmpty(projectResult.ProjectId))
                {
                    logger.LogError("? Failed to load SharePoint project - cannot continue");
                    return false;
                }
                
                var projectId = projectResult.ProjectId;
                var targetDocument = projectResult.Documents?.FirstOrDefault(d => 
                    string.Equals(d.DocumentId, documentName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(d.Title, documentName, StringComparison.OrdinalIgnoreCase));
                
                if (targetDocument == null)
                {
                    logger.LogError("? Document '{DocumentName}' not found in project", documentName);
                    logger.LogInformation("Available documents:");
                    foreach (var doc in projectResult.Documents ?? [])
                    {
                        logger.LogInformation("  - {DocumentId}: {Title} ({Template})", doc.DocumentId, doc.Title, doc.Template);
                    }
                    return false;
                }
                
                await Task.Delay(1000);
                
                // Step 3: Load Document
                logger.LogInformation("");
                logger.LogInformation("?? Step 3: Load Document from SharePoint");
                logger.LogInformation("----------------------------------------");
                
                var documentResult = await serverClient.LoadDocumentAsync(projectId, targetDocument.DocumentId);
                if (documentResult?.Success != true)
                {
                    logger.LogError("? Failed to load document - cannot continue");
                    await CleanupProject(projectId);
                    return false;
                }
                
                if (documentResult.Tags?.Any() != true)
                {
                    logger.LogWarning("?? No content controls found in document - workflow complete but no content to generate");
                    await CleanupProject(projectId);
                    return true;
                }
                
                await Task.Delay(1000);
                
                // Step 4: Analyze Document
                logger.LogInformation("");
                logger.LogInformation("?? Step 4: Analyze Document Content Controls");
                logger.LogInformation("--------------------------------------------");
                
                var analysisResult = await serverClient.AnalyzeDocumentAsync(projectId, targetDocument.DocumentId);
                if (analysisResult?.Success != true)
                {
                    logger.LogError("? Failed to analyze document");
                    await CleanupProject(projectId);
                    return false;
                }
                
                var supportedTags = analysisResult.AvailableTags?.Where(t => t.IsSupported).ToList() ?? [];
                if (!supportedTags.Any())
                {
                    logger.LogWarning("?? No supported content controls found - workflow complete but no content to generate");
                    await CleanupProject(projectId);
                    return true;
                }
                
                await Task.Delay(1500);
                
                // Step 5: Generate Content for Each Supported Content Control
                logger.LogInformation("");
                logger.LogInformation("?? Step 5: Generate Content for Content Controls");
                logger.LogInformation("-----------------------------------------------");
                
                var successfulGenerations = 0;
                var totalGenerations = Math.Min(supportedTags.Count, 5); // Limit to first 5 for demo
                
                for (int i = 0; i < totalGenerations; i++)
                {
                    var tag = supportedTags[i];
                    logger.LogInformation("?? Generating content {Index}/{Total} for control: {ContentControlId}", 
                        i + 1, totalGenerations, tag.ContentControlId);
                    
                    var contentResult = await serverClient.GenerateContentAsync(
                        projectId, 
                        targetDocument.DocumentId, 
                        tag.ContentControlId!);
                    
                    if (contentResult?.Success == true)
                    {
                        successfulGenerations++;
                        logger.LogInformation("? Content generated successfully for {ContentControlId}", tag.ContentControlId);
                        
                        // Simulate Word add-in inserting content into document
                        await Task.Delay(500);
                        logger.LogInformation("?? [SIMULATED] Content inserted into Word document");
                    }
                    else
                    {
                        logger.LogError("? Failed to generate content for {ContentControlId}: {Error}", 
                            tag.ContentControlId, contentResult?.Error);
                    }
                    
                    await Task.Delay(1000); // Simulate user interaction delay
                }
                
                // Step 6: Test Data Source Refresh (optional)
                if (successfulGenerations > 0)
                {
                    logger.LogInformation("");
                    logger.LogInformation("?? Step 6: Test Data Source Refresh");
                    logger.LogInformation("----------------------------------");
                    
                    var refreshResult = await serverClient.RefreshProjectAsync(projectId);
                    if (refreshResult?.Success == true)
                    {
                        logger.LogInformation("? Data sources refreshed successfully");
                        
                        // Regenerate one piece of content to show refresh worked
                        logger.LogInformation("?? Re-generating content for first control to demonstrate refresh...");
                        var firstTag = supportedTags.First();
                        var retestResult = await serverClient.GenerateContentAsync(
                            projectId, 
                            targetDocument.DocumentId, 
                            firstTag.ContentControlId!);
                        
                        if (retestResult?.Success == true)
                        {
                            logger.LogInformation("? Content re-generation after refresh successful");
                        }
                    }
                    else
                    {
                        logger.LogWarning("?? Data source refresh failed: {Error}", refreshResult?.Error);
                    }
                    
                    await Task.Delay(1000);
                }
                
                // Step 7: Get Configuration (for diagnostics)
                logger.LogInformation("");
                logger.LogInformation("?? Step 7: Get Project Configuration (Diagnostic)");
                logger.LogInformation("------------------------------------------------");
                
                var configResult = await serverClient.GetProjectConfigurationAsync(projectId);
                // Configuration logging is handled in the client
                
                await Task.Delay(500);
                
                // Step 8: Cleanup
                logger.LogInformation("");
                logger.LogInformation("?? Step 8: Cleanup and End Session");
                logger.LogInformation("----------------------------------");
                
                var cleanupSuccess = await CleanupProject(projectId);
                
                // Final Summary
                logger.LogInformation("");
                logger.LogInformation("?? WORKFLOW SUMMARY");
                logger.LogInformation("==================");
                logger.LogInformation("? Project loaded: {ProjectName}", projectResult.ProjectName);
                logger.LogInformation("? Document loaded: {DocumentTitle}", targetDocument.Title);
                logger.LogInformation("? Content controls found: {TotalCount}", analysisResult.TotalTagCount);
                logger.LogInformation("? Supported content controls: {SupportedCount}", analysisResult.SupportedTagCount);
                logger.LogInformation("? Successful content generations: {SuccessfulCount}/{AttemptedCount}", 
                    successfulGenerations, totalGenerations);
                logger.LogInformation("? Session cleanup: {CleanupStatus}", cleanupSuccess ? "Success" : "Failed");
                
                var overallSuccess = successfulGenerations > 0 && cleanupSuccess;
                logger.LogInformation("?? Overall workflow result: {Result}", 
                    overallSuccess ? "SUCCESS" : "PARTIAL SUCCESS");
                
                return overallSuccess;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "?? Workflow simulation failed with exception");
                return false;
            }
        }

        private async Task<bool> CleanupProject(string projectId)
        {
            try
            {
                return await serverClient.UnloadProjectAsync(projectId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "?? Exception during project cleanup");
                return false;
            }
        }
    }
}