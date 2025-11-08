using RoboClerk.Server.TestClient.Models;

namespace RoboClerk.Server.TestClient.Services
{
    public interface IRoboClerkServerClient
    {
        Task<HealthCheckResult?> HealthCheckAsync();
        Task<ProjectLoadResult?> LoadProjectAsync(LoadProjectRequest request);
        Task<DocumentAnalysisResult?> RefreshProjectAsync(string projectId);
        Task<RefreshResult?> RefreshDataSourcesAsync(string projectId);
        Task<TagContentResult?> GenerateContentAsync(string projectId, string documentId, string contentControlId);
        Task<List<ConfigurationValue>?> GetProjectConfigurationAsync(string projectId);
        Task<bool> UnloadProjectAsync(string projectId);
    }
}