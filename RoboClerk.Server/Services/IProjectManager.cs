using RoboClerk.Server.Models;

namespace RoboClerk.Server.Services
{
    public interface IProjectManager
    {
        Task<ProjectLoadResult> LoadProjectAsync(LoadProjectRequest request);
        Task<List<ConfigurationValue>> GetProjectConfigurationAsync(string projectId);
        Task<RefreshResult> RefreshProjectAsync(string projectId, bool full);
        Task UnloadProjectAsync(string projectId);
        
        // Word Add-in specific methods for SharePoint integration
        bool ValidateProjectForWordAddInAsync(string projectId, LoadProjectRequest request);
        
        // Content control-based tag content generation
        Task<TagContentResult> GetTagContentWithContentControlAsync(string projectId, RoboClerkContentControlTagRequest tagRequest);
    }
}