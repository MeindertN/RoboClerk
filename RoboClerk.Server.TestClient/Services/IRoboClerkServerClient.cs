using RoboClerk.Server.TestClient.Models;

namespace RoboClerk.Server.TestClient.Services
{
    public interface IRoboClerkServerClient
    {
        Task<HealthCheckResult?> HealthCheckAsync();
        Task<ProjectLoadResult?> LoadProjectAsync(string projectPath);
        Task<DocumentLoadResult?> LoadDocumentAsync(string projectId, string documentId);
        Task<DocumentAnalysisResult?> AnalyzeDocumentAsync(string projectId, string documentId);
        Task<TagContentResult?> GenerateContentAsync(string projectId, string documentId, string contentControlId);
        Task<RefreshResult?> RefreshProjectAsync(string projectId);
        Task<List<ConfigurationValue>?> GetProjectConfigurationAsync(string projectId);
        Task<bool> UnloadProjectAsync(string projectId);
    }
}