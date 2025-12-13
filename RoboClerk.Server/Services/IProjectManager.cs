using RoboClerk.Server.Models;
using RoboClerk.ContentCreators;

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
        /// <param name="configurationContent">The full configuration content as a string</param>
        /// <returns>Result indicating success or failure</returns>
        Task<ConfigurationUpdateResult> UpdateProjectConfigurationAsync(string projectId, string configurationContent);

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
        /// <param name="configurationContent">The full configuration content as a string</param>
        /// <returns>Validation result with any errors or warnings</returns>
        Task<ConfigurationValidationResult> ValidateConfigurationUpdatesAsync(string projectId, string configurationContent);

        // Template file management
        /// <summary>
        /// Gets all DOCX template files in the template directory
        /// </summary>
        /// <param name="projectId">The project ID</param>
        /// <param name="includeConfiguredTemplates">Whether to include templates that are already configured as documents</param>
        /// <returns>Result containing available template files information</returns>
        Task<AvailableTemplateFilesResult> GetAvailableTemplateFilesAsync(string projectId, bool includeConfiguredTemplates = false);

        /// <summary>
        /// Gets metadata for all available content creators for a specific project
        /// </summary>
        /// <param name="projectId">The project ID</param>
        /// <param name="refresh">Whether to refresh the metadata by reloading configuration</param>
        /// <returns>List of content creator metadata</returns>
        Task<List<ContentCreatorMetadata>> GetContentCreatorMetadataAsync(string projectId, bool refresh = false);

        /// <summary>
        /// Gets the content of a specific template file
        /// </summary>
        /// <param name="projectId">The project ID</param>
        /// <param name="fileName">The name of the template file (must be in template directory)</param>
        /// <returns>The file content as bytes</returns>
        Task<byte[]> GetTemplateFileContentAsync(string projectId, string fileName);
    }
}