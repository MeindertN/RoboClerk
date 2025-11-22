using Microsoft.AspNetCore.Mvc;
using RoboClerk.Server.Models;
using RoboClerk.Server.Services;
using RoboClerk.ContentCreators;

namespace RoboClerk.Server.Controllers
{
    [ApiController]
    [Route("api/word-addin")]
    public class WordAddInController : ControllerBase
    {
        private readonly IProjectManager projectManager;
        private readonly IContentCreatorMetadataService metadataService;
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public WordAddInController(IProjectManager projectManager, IContentCreatorMetadataService metadataService)
        {
            this.projectManager = projectManager;
            this.metadataService = metadataService;
        }

        /// <summary>
        /// Get metadata for all available content creators
        /// </summary>
        [HttpGet("content-creators/metadata")]
        public ActionResult<List<ContentCreatorMetadata>> GetContentCreatorMetadata()
        {
            try
            {
                logger.Debug("Getting metadata for all content creators");
                
                var metadata = metadataService.GetAllContentCreatorMetadata();
                
                logger.Info($"Returning metadata for {metadata.Count} content creators");
                return Ok(metadata);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error getting content creator metadata");
                return StatusCode(500, "Failed to get content creator metadata");
            }
        }

        /// <summary>
        /// Get metadata for a specific content creator by source
        /// </summary>
        [HttpGet("content-creators/metadata/{source}")]
        public ActionResult<ContentCreatorMetadata> GetContentCreatorMetadataBySource(string source)
        {
            try
            {
                logger.Debug($"Getting metadata for content creator source: {source}");
                
                var metadata = metadataService.GetContentCreatorMetadata(source);
                if (metadata == null)
                {
                    logger.Warn($"Content creator metadata not found for source: {source}");
                    return NotFound($"Content creator with source '{source}' not found");
                }
                
                logger.Info($"Returning metadata for content creator: {source}");
                return Ok(metadata);
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error getting content creator metadata for source: {source}");
                return StatusCode(500, "Failed to get content creator metadata");
            }
        }

        /// <summary>
        /// Load a SharePoint project for the Word add-in session
        /// </summary>
        [HttpPost("project/load")]
        public async Task<ActionResult<ProjectLoadResult>> LoadSharePointProject([FromBody] LoadProjectRequest request)
        {
            try
            {
                logger.Info($"Loading SharePoint project: {request.ProjectPath}");
                
                var result = await projectManager.LoadProjectAsync(request);
                if (!result.Success)
                {
                    logger.Warn($"Failed to load project: {result.Error}");
                    return BadRequest(result);
                }
                
                // Automatically validate the project for Word add-in use
                var isValid = projectManager.ValidateProjectForWordAddInAsync(result.ProjectId!,request);
                if (!isValid)
                {
                    await projectManager.UnloadProjectAsync(result.ProjectId!);
                    return BadRequest(new ProjectLoadResult 
                    { 
                        Success = false, 
                        Error = "Project is not properly configured for Word add-in use with SharePoint" 
                    });
                }

                logger.Info($"Successfully loaded and validated SharePoint project: {result.ProjectId}");
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error loading SharePoint project");
                return StatusCode(500, "Failed to load SharePoint project");
            }
        }

        /// <summary>
        /// Refresh a project to discover all available templates and RoboClerk content controls
        /// Can take a long time when all content controls are processed
        /// </summary>
        [HttpGet("project/{projectId}/refresh")]
        public async Task<ActionResult<RefreshResult>> RefreshProject(string projectId, [FromQuery] bool processTags = false)
        {
            try
            {
                logger.Info($"Refreshing project {projectId} with processTags={processTags}");
                
                var result = await projectManager.RefreshProjectDocumentsAsync(projectId, processTags);
                if (!result.Success)
                {
                    logger.Warn($"Failed to refresh project: {result.Error}");
                    return BadRequest(result);
                }

                logger.Info($"Project refresh complete for {projectId}");
                return Ok(result);
            }
            catch (ArgumentException)
            {
                logger.Warn($"Project {projectId} not found or not loaded");
                return NotFound("SharePoint project not loaded. Please load the project first.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error refreshing project {projectId}");
                return StatusCode(500, "Failed to refresh project");
            }
        }

        /// <summary>
        /// Refresh a specific document in the project and clear its virtual content controls
        /// </summary>
        [HttpPost("project/{projectId}/document/{documentId}/refresh")]
        public async Task<ActionResult<RefreshResult>> RefreshDocument(string projectId, string documentId)
        {
            try
            {
                logger.Info($"Refreshing document {documentId} in project {projectId}");
                
                var result = await projectManager.RefreshDocumentAsync(projectId, documentId);
                if (!result.Success)
                {
                    logger.Warn($"Failed to refresh document: {result.Error}");
                    return BadRequest(result);
                }

                logger.Info($"Document {documentId} refreshed successfully");
                return Ok(result);
            }
            catch (ArgumentException)
            {
                logger.Warn($"Project {projectId} not found or not loaded");
                return NotFound("SharePoint project not loaded. Please load the project first.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error refreshing document {documentId} in project {projectId}");
                return StatusCode(500, "Failed to refresh document");
            }
        }

        /// <summary>
        /// Generate content for a specific content control in the Word document
        /// </summary>
        [HttpPost("project/{projectId}/content")]
        public async Task<ActionResult<TagContentResult>> GenerateContentForContentControl(
            string projectId, 
            [FromBody] RoboClerkContentControlTagRequest request)
        {
            try
            {
                logger.Info($"Generating content for content control {request.ContentControlId} with tag {request.RoboClerkTag} in document {request.DocumentId}");

                var result = await projectManager.GetTagContentWithContentControlAsync(projectId, request);
                if (!result.Success)
                {
                    logger.Warn($"Failed to generate content: {result.Error}");
                    return BadRequest(result);
                }

                var contentLength = result.Content?.Length ?? 0;
                logger.Info($"Successfully generated {contentLength} characters of OpenXML content");
                return Ok(result);
            }
            catch (ArgumentException)
            {
                logger.Warn($"Project {projectId} not found or not loaded");
                return NotFound("SharePoint project not loaded. Please load the project first.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error generating content for content control {request.ContentControlId}");
                return StatusCode(500, "Failed to generate content");
            }
        }

        /// <summary>
        /// Refresh data sources (e.g., when Redmine data has been updated)
        /// </summary>
        [HttpPost("project/{projectId}/refreshds")]
        public async Task<ActionResult<RefreshResult>> RefreshDataSources(string projectId)
        {
            try
            {
                logger.Info($"Refreshing data sources for project {projectId}");
                
                var result = await projectManager.RefreshProjectDataSourcesAsync(projectId);
                if (!result.Success)
                {
                    logger.Warn($"Failed to refresh data sources: {result.Error}");
                    return BadRequest(result);
                }

                logger.Info("Data sources refreshed successfully");
                return Ok(result);
            }
            catch (ArgumentException)
            {
                logger.Warn($"Project {projectId} not found or not loaded");
                return NotFound("SharePoint project not loaded. Please load the project first.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error refreshing data sources for project {projectId}");
                return StatusCode(500, "Failed to refresh data sources");
            }
        }

        /// <summary>
        /// Get virtual tag statistics for debugging purposes
        /// </summary>
        [HttpGet("project/{projectId}/virtual-tags/stats")]
        public async Task<ActionResult<Dictionary<string, int>>> GetVirtualTagStatistics(string projectId)
        {
            try
            {
                logger.Debug($"Getting virtual tag statistics for project {projectId}");
                
                var stats = await projectManager.GetVirtualTagStatisticsAsync(projectId);
                return Ok(stats);
            }
            catch (ArgumentException)
            {
                logger.Warn($"Project {projectId} not found or not loaded");
                return NotFound("SharePoint project not loaded. Please load the project first.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error getting virtual tag statistics for {projectId}");
                return StatusCode(500, "Failed to get virtual tag statistics");
            }
        }

        /// <summary>
        /// Update project configuration through the Word add-in
        /// </summary>
        [HttpPut("project/{projectId}/configuration")]
        public async Task<ActionResult<ConfigurationUpdateResult>> UpdateProjectConfiguration(
            string projectId, 
            [FromBody] Dictionary<string, object> configUpdates)
        {
            try
            {
                logger.Info($"Updating configuration for project {projectId} with {configUpdates.Count} changes");

                // Validate updates first
                var validation = await projectManager.ValidateConfigurationUpdatesAsync(projectId, configUpdates);
                if (!validation.IsValid)
                {
                    logger.Warn($"Configuration validation failed: {string.Join(", ", validation.Errors)}");
                    return BadRequest(new { Errors = validation.Errors, Warnings = validation.Warnings });
                }

                var result = await projectManager.UpdateProjectConfigurationAsync(projectId, configUpdates);
                if (!result.Success)
                {
                    logger.Warn($"Failed to update configuration: {result.Error}");
                    return BadRequest(result);
                }

                logger.Info($"Successfully updated {result.UpdatedKeys.Count} configuration keys. Reload required: {result.RequiresProjectReload}");
                return Ok(result);
            }
            catch (ArgumentException)
            {
                logger.Warn($"Project {projectId} not found or not loaded");
                return NotFound("SharePoint project not loaded. Please load the project first.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error updating configuration for project {projectId}");
                return StatusCode(500, "Failed to update configuration");
            }
        }

        /// <summary>
        /// Get raw project configuration content as TOML
        /// </summary>
        [HttpGet("project/{projectId}/configuration/raw")]
        public async Task<ActionResult<string>> GetProjectConfigurationContent(string projectId)
        {
            try
            {
                logger.Debug($"Getting raw configuration content for project {projectId}");
                
                var content = await projectManager.GetProjectConfigurationContentAsync(projectId);
                return Ok(content);
            }
            catch (ArgumentException)
            {
                logger.Warn($"Project {projectId} not found or not loaded");
                return NotFound("SharePoint project not loaded. Please load the project first.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error getting configuration content for project {projectId}");
                return StatusCode(500, "Failed to get configuration content");
            }
        }

        /// <summary>
        /// Validate configuration changes without applying them
        /// </summary>
        [HttpPost("project/{projectId}/configuration/validate")]
        public async Task<ActionResult<ConfigurationValidationResult>> ValidateConfigurationUpdates(
            string projectId, 
            [FromBody] Dictionary<string, object> configUpdates)
        {
            try
            {
                logger.Debug($"Validating configuration changes for project {projectId}");
                
                var result = await projectManager.ValidateConfigurationUpdatesAsync(projectId, configUpdates);
                return Ok(result);
            }
            catch (ArgumentException)
            {
                logger.Warn($"Project {projectId} not found or not loaded");
                return NotFound("SharePoint project not loaded. Please load the project first.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error validating configuration for project {projectId}");
                return StatusCode(500, "Failed to validate configuration");
            }
        }

        /// <summary>
        /// Get available template files for use with TemplateSection content creator
        /// </summary>
        [HttpGet("project/{projectId}/template-files")]
        public async Task<ActionResult<AvailableTemplateFilesResult>> GetAvailableTemplateFiles(
            string projectId, 
            [FromQuery] bool includeConfigured = false)
        {
            try
            {
                logger.Debug($"Getting available template files for project {projectId}, includeConfigured: {includeConfigured}");
                
                var result = await projectManager.GetAvailableTemplateFilesAsync(projectId, includeConfigured);
                if (!result.Success)
                {
                    logger.Warn($"Failed to get template files: {result.Error}");
                    return BadRequest(result);
                }

                logger.Info($"Found {result.AvailableTemplateFiles.Count} available template files for project {projectId}");
                return Ok(result);
            }
            catch (ArgumentException)
            {
                logger.Warn($"Project {projectId} not found or not loaded");
                return NotFound("SharePoint project not loaded. Please load the project first.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error getting available template files for project {projectId}");
                return StatusCode(500, "Failed to get available template files");
            }
        }

        /// <summary>
        /// End Word add-in session and cleanup resources
        /// </summary>
        [HttpDelete("project/{projectId}")]
        public async Task<ActionResult> EndSession(string projectId)
        {
            try
            {
                logger.Info($"Ending Word add-in session for project {projectId}");
                
                await projectManager.UnloadProjectAsync(projectId);
                
                logger.Info("Word add-in session ended successfully");
                return NoContent();
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error ending session for project {projectId}");
                return StatusCode(500, "Failed to end session");
            }
        }

        /// <summary>
        /// Health check endpoint for Word add-in connectivity
        /// </summary>
        [HttpGet("health")]
        public ActionResult<object> HealthCheck()
        {
            return Ok(new 
            { 
                status = "healthy", 
                service = "RoboClerk Server API",
                timestamp = DateTime.UtcNow,
                version = GetType().Assembly.GetName().Version?.ToString()
            });
        }
    }
}