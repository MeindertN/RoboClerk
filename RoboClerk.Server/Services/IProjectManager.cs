using RoboClerk.Server.Models;

namespace RoboClerk.Server.Services
{
    public interface IProjectManager
    {
        Task<List<ProjectInfo>> GetAvailableProjectsAsync();
        Task<ProjectLoadResult> LoadProjectAsync(string projectPath);
        Task<List<DocumentInfo>> GetProjectDocumentsAsync(string projectId);
        Task<DocumentLoadResult> LoadDocumentAsync(string projectId, string documentId);
        Task<List<ConfigurationValue>> GetProjectConfigurationAsync(string projectId);
        Task<TagContentResult> GetTagContentAsync(string projectId, RoboClerkTagRequest tagRequest);
        Task<RefreshResult> RefreshProjectAsync(string projectId);
        Task UnloadProjectAsync(string projectId);
        
        // Word Add-in specific methods for SharePoint integration
        Task<bool> ValidateProjectForWordAddInAsync(string projectId);
        Task<DocumentAnalysisResult> AnalyzeDocumentForWordAddInAsync(string projectId, string documentId);
        
        // Enhanced method that leverages RoboClerkDocxTag for better OpenXML conversion
        Task<TagContentResult> GetTagContentWithContentControlAsync(string projectId, RoboClerkContentControlTagRequest tagRequest);
    }
}