using RoboClerk.Server.Models;

namespace RoboClerk.Server.Services
{
    public interface IProjectManager
    {
        Task<ProjectLoadResult> LoadProjectAsync(LoadProjectRequest request);
        Task<RefreshResult> RefreshDocumentAsync(string projectId, string documentId);
        Task<RefreshResult> RefreshProjectDataSourcesAsync(string projectId);
        Task<RefreshResult> RefreshProjectDocumentsAsync(string projectId, bool processTags);
        Task<Dictionary<string, int>> GetVirtualTagStatisticsAsync(string projectId);
        Task UnloadProjectAsync(string projectId);
        
        // Word Add-in specific methods for SharePoint integration
        bool ValidateProjectForWordAddInAsync(string projectId, LoadProjectRequest request);
        
        // Content control-based tag content generation
        Task<TagContentResult> GetTagContentWithContentControlAsync(string projectId, RoboClerkContentControlTagRequest tagRequest);

        // Project configuration management
        /// <summary>
        /// Updates the project configuration file with new values
        /// </summary>
        /// <param name="projectId">The project ID</param>
        /// <param name="configUpdates">Dictionary of configuration keys and their new values</param>
        /// <returns>Result indicating success or failure</returns>
        Task<ConfigurationUpdateResult> UpdateProjectConfigurationAsync(string projectId, Dictionary<string, object> configUpdates);

        /// <summary>
        /// Gets the raw project configuration content as TOML
        /// </summary>
        /// <param name="projectId">The project ID</param>
        /// <returns>The raw TOML configuration content</returns>
        Task<string> GetProjectConfigurationContentAsync(string projectId);

        /// <summary>
        /// Validates proposed configuration changes without applying them
        /// </summary>
        /// <param name="projectId">The project ID</param>
        /// <param name="configUpdates">Dictionary of configuration keys and their new values</param>
        /// <returns>Validation result with any errors or warnings</returns>
        Task<ConfigurationValidationResult> ValidateConfigurationUpdatesAsync(string projectId, Dictionary<string, object> configUpdates);

        // Template file management
        /// <summary>
        /// Gets all DOCX template files in the template directory that are not configured as documents
        /// </summary>
        /// <param name="projectId">The project ID</param>
        /// <param name="includeConfiguredTemplates">Whether to include templates that are already configured as documents</param>
        /// <returns>Result containing available template files information</returns>
        Task<AvailableTemplateFilesResult> GetAvailableTemplateFilesAsync(string projectId, bool includeConfiguredTemplates = false);
    }
}