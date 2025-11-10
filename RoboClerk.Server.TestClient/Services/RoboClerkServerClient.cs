using Microsoft.Extensions.Logging;
using RoboClerk.Server.TestClient.Models;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace RoboClerk.Server.TestClient.Services
{
    public class RoboClerkServerClient : IRoboClerkServerClient
    {
        private readonly HttpClient httpClient;
        private readonly ILogger<RoboClerkServerClient> logger;
        private readonly JsonSerializerOptions jsonOptions;

        public RoboClerkServerClient(HttpClient httpClient, ILogger<RoboClerkServerClient> logger)
        {
            this.httpClient = httpClient;
            this.logger = logger;
            this.jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }

        public void SetBaseAddress(string baseUrl)
        {
            httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        }

        public async Task<HealthCheckResult?> HealthCheckAsync()
        {
            try
            {
                logger.LogInformation("?? Checking server health...");
                
                var response = await httpClient.GetAsync("api/word-addin/health");
                
                if (response.IsSuccessStatusCode)
                {
                    // The server returns lowercase property names, so we need to handle that
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var jsonDoc = JsonDocument.Parse(jsonString);
                    var root = jsonDoc.RootElement;
                    
                    var result = new HealthCheckResult
                    {
                        Status = root.GetProperty("status").GetString() ?? string.Empty,
                        Service = root.GetProperty("service").GetString() ?? string.Empty,
                        Timestamp = root.GetProperty("timestamp").GetDateTime(),
                        Version = root.TryGetProperty("version", out var versionElement) ? versionElement.GetString() : null
                    };
                    
                    logger.LogInformation("? Server health check passed: {Status}", result.Status);
                    return result;
                }
                else
                {
                    logger.LogWarning("?? Health check failed with status: {StatusCode}", response.StatusCode);
                    return null;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "? Health check failed with exception");
                return null;
            }
        }

        public async Task<ProjectLoadResult?> LoadProjectAsync(LoadProjectRequest request)
        {
            try
            {
                logger.LogInformation("?? Loading SharePoint project: {ProjectPath}", request.ProjectPath);
                
                var response = await httpClient.PostAsJsonAsync("api/word-addin/project/load", request, jsonOptions);
                
                var result = await response.Content.ReadFromJsonAsync<ProjectLoadResult>(jsonOptions);
                
                if (result?.Success == true)
                {
                    logger.LogInformation("? Project loaded successfully: {ProjectId} - {ProjectName}", 
                        result.ProjectId, result.ProjectName);
                    logger.LogInformation("?? Found {DocumentCount} documents", result.Documents?.Count ?? 0);
                }
                else
                {
                    logger.LogError("? Project load failed: {Error}", result?.Error);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "?? Exception loading project");
                return null;
            }
        }

        public async Task<DocumentAnalysisResult?> RefreshProjectAsync(string projectId)
        {
            try
            {
                logger.LogInformation("🔄 Refreshing project: {ProjectId}", projectId);
                
                var response = await httpClient.GetAsync($"api/word-addin/project/{projectId}/refresh");
                var result = await response.Content.ReadFromJsonAsync<DocumentAnalysisResult>(jsonOptions);
                
                if (result?.Success == true)
                {
                    logger.LogInformation("✅ Project refresh complete");
                }
                else
                {
                    logger.LogError("❌ Project refresh failed: {Error}", result?.Error);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "💥 Exception refreshing project");
                return null;
            }
        }

        public async Task<RefreshResult?> RefreshDataSourcesAsync(string projectId)
        {
            try
            {
                logger.LogInformation("?? Refreshing project data sources...");
                
                var response = await httpClient.PostAsync($"api/word-addin/project/{projectId}/refreshds", null);
                var result = await response.Content.ReadFromJsonAsync<RefreshResult>(jsonOptions);
                
                if (result?.Success == true)
                {
                    logger.LogInformation("? Project data sources refreshed successfully");
                }
                else
                {
                    logger.LogError("? Project data source refresh failed: {Error}", result?.Error);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "?? Exception refreshing project data sources");
                return null;
            }
        }

        public async Task<TagContentResult?> GenerateContentAsync(string projectId, string documentId, string contentControlId, string roboclerkTag)
        {
            try
            {
                logger.LogInformation("?? Generating content for control: {ContentControlId}", contentControlId);
                
                var request = new RoboClerkContentControlTagRequest 
                { 
                    DocumentId = documentId, 
                    ContentControlId = contentControlId,
                    RoboClerkTag = roboclerkTag
                };
                
                var response = await httpClient.PostAsJsonAsync($"api/word-addin/project/{projectId}/content", request, jsonOptions);
                var result = await response.Content.ReadFromJsonAsync<TagContentResult>(jsonOptions);
                
                if (result?.Success == true)
                {
                    var contentLength = result.Content?.Length ?? 0;
                    logger.LogInformation("? Content generated successfully: {ContentLength} characters of OpenXML", contentLength);
                    
                    if (contentLength > 0 && contentLength < 500)
                    {
                        logger.LogInformation("?? Generated OpenXML:");
                        logger.LogInformation("{Content}", result.Content);
                    }
                    else if (contentLength > 0)
                    {
                        logger.LogInformation("?? Generated OpenXML (first 200 chars):");
                        logger.LogInformation("{Content}...", result.Content?[..Math.Min(200, contentLength)]);
                    }
                }
                else
                {
                    logger.LogError("? Content generation failed: {Error}", result?.Error);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "?? Exception generating content");
                return null;
            }
        }

        public async Task<List<ConfigurationValue>?> GetProjectConfigurationAsync(string projectId)
        {
            try
            {
                logger.LogInformation("?? Getting project configuration...");
                
                var response = await httpClient.GetAsync($"api/word-addin/project/{projectId}/config");
                var result = await response.Content.ReadFromJsonAsync<List<ConfigurationValue>>(jsonOptions);
                
                if (result?.Any() == true)
                {
                    logger.LogInformation("? Project configuration retrieved: {ConfigCount} values", result.Count);
                    
                    foreach (var config in result.Take(10)) // Show first 10 config values
                    {
                        logger.LogInformation("  {Key}: {Value}", config.Key, config.Value);
                    }
                    
                    if (result.Count > 10)
                    {
                        logger.LogInformation("  ... and {MoreCount} more configuration values", result.Count - 10);
                    }
                }
                else
                {
                    logger.LogWarning("?? No project configuration found");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "?? Exception getting project configuration");
                return null;
            }
        }

        public async Task<bool> UnloadProjectAsync(string projectId)
        {
            try
            {
                logger.LogInformation("??? Unloading project and cleaning up resources...");
                
                var response = await httpClient.DeleteAsync($"api/word-addin/project/{projectId}");
                
                if (response.IsSuccessStatusCode)
                {
                    logger.LogInformation("? Project unloaded successfully");
                    return true;
                }
                else
                {
                    logger.LogError("? Project unload failed with status: {StatusCode}", response.StatusCode);
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "?? Exception unloading project");
                return false;
            }
        }
    }
}