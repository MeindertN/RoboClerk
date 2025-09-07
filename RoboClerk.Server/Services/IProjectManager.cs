using RoboClerk.Server.Models;

namespace RoboClerk.Server.Services
{
    public interface IProjectManager
    {
        Task<ProjectLoadResult> LoadProjectAsync(string projectPath);
        Task<DocumentLoadResult> LoadDocumentAsync(string projectId, string documentId);
        Task<List<ConfigurationValue>> GetProjectConfigurationAsync(string projectId);
        Task<RefreshResult> RefreshProjectAsync(string projectId);
        Task UnloadProjectAsync(string projectId);
        
        // Word Add-in specific methods for SharePoint integration
        Task<bool> ValidateProjectForWordAddInAsync(string projectId);
        Task<DocumentAnalysisResult> AnalyzeDocumentForWordAddInAsync(string projectId, string documentId);
        
        // Content control-based tag content generation
        Task<TagContentResult> GetTagContentWithContentControlAsync(string projectId, RoboClerkContentControlTagRequest tagRequest);
    }
}