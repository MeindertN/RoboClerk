using Microsoft.AspNetCore.Mvc;
using RoboClerk.Server.Models;
using RoboClerk.Server.Services;

namespace RoboClerk.Server.Controllers
{
    [ApiController]
    [Route("api/word-addin")]
    public class WordAddInController : ControllerBase
    {
        private readonly IProjectManager projectManager;
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public WordAddInController(IProjectManager projectManager)
        {
            this.projectManager = projectManager;
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
                var isValid = await projectManager.ValidateProjectForWordAddInAsync(result.ProjectId!);
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
        /// </summary>
        [HttpGet("project/{projectId}/refresh")]
        public async Task<ActionResult<DocumentAnalysisResult>> RefreshProject(string projectId)
        {
            try
            {
                logger.Info($"Refreshing project {projectId}");
                
                var result = await projectManager.RefreshProjectAsync(projectId,false);
                if (!result.Success)
                {
                    logger.Warn($"Failed to refresh document: {result.Error}");
                    return BadRequest(result);
                }

                logger.Info($"Project refresh complete");
                return Ok(result);
            }
            catch (ArgumentException)
            {
                logger.Warn($"Project {projectId} not found or not loaded");
                return NotFound("SharePoint project not loaded. Please load the project first.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error analyzing project {projectId}");
                return StatusCode(500, "Failed to analyze project");
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
                logger.Info($"Generating content for content control {request.ContentControlId} in document {request.DocumentId}");
                
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
        /// Refresh data sources (e.g., when SharePoint data has been updated)
        /// </summary>
        [HttpPost("project/{projectId}/refreshds")]
        public async Task<ActionResult<RefreshResult>> RefreshDataSources(string projectId)
        {
            try
            {
                logger.Info($"Refreshing data sources for project {projectId}");
                
                var result = await projectManager.RefreshProjectAsync(projectId,true);
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
        /// Get project configuration information for diagnostic purposes
        /// </summary>
        [HttpGet("project/{projectId}/config")]
        public async Task<ActionResult<List<ConfigurationValue>>> GetProjectConfiguration(string projectId)
        {
            try
            {
                logger.Debug($"Getting configuration for project {projectId}");
                
                var config = await projectManager.GetProjectConfigurationAsync(projectId);
                return Ok(config);
            }
            catch (ArgumentException)
            {
                logger.Warn($"Project {projectId} not found or not loaded");
                return NotFound("SharePoint project not loaded. Please load the project first.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error getting project configuration for {projectId}");
                return StatusCode(500, "Failed to get project configuration");
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
                service = "RoboClerk Word Add-in API",
                timestamp = DateTime.UtcNow,
                version = GetType().Assembly.GetName().Version?.ToString()
            });
        }
    }
}