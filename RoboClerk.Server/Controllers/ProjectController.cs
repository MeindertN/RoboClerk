using Microsoft.AspNetCore.Mvc;
using RoboClerk.Server.Models;
using RoboClerk.Server.Services;

namespace RoboClerk.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectController : ControllerBase
    {
        private readonly IProjectManager projectManager;
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public ProjectController(IProjectManager projectManager)
        {
            this.projectManager = projectManager;
        }

        [HttpGet("available")]
        public async Task<ActionResult<List<ProjectInfo>>> GetAvailableProjects()
        {
            try
            {
                var projects = await projectManager.GetAvailableProjectsAsync();
                return Ok(projects);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error getting available projects");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("load")]
        public async Task<ActionResult<ProjectLoadResult>> LoadProject([FromBody] LoadProjectRequest request)
        {
            try
            {
                var result = await projectManager.LoadProjectAsync(request.ProjectPath);
                if (!result.Success)
                    return BadRequest(result);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error loading project");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{projectId}/documents")]
        public async Task<ActionResult<List<DocumentInfo>>> GetProjectDocuments(string projectId)
        {
            try
            {
                var documents = await projectManager.GetProjectDocumentsAsync(projectId);
                return Ok(documents);
            }
            catch (ArgumentException)
            {
                return NotFound("Project not loaded");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error getting project documents");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("{projectId}/documents/{documentId}/load")]
        public async Task<ActionResult<DocumentLoadResult>> LoadDocument(string projectId, string documentId)
        {
            try
            {
                var result = await projectManager.LoadDocumentAsync(projectId, documentId);
                if (!result.Success)
                    return BadRequest(result);
                
                return Ok(result);
            }
            catch (ArgumentException)
            {
                return NotFound("Project not loaded");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error loading document");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{projectId}/configuration")]
        public async Task<ActionResult<List<ConfigurationValue>>> GetProjectConfiguration(string projectId)
        {
            try
            {
                var config = await projectManager.GetProjectConfigurationAsync(projectId);
                return Ok(config);
            }
            catch (ArgumentException)
            {
                return NotFound("Project not loaded");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error getting project configuration");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("{projectId}/content")]
        public async Task<ActionResult<TagContentResult>> GetTagContent(string projectId, [FromBody] RoboClerkTagRequest request)
        {
            try
            {
                var result = await projectManager.GetTagContentAsync(projectId, request);
                if (!result.Success)
                    return BadRequest(result);
                
                return Ok(result);
            }
            catch (ArgumentException)
            {
                return NotFound("Project not loaded");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error getting tag content");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("{projectId}/refresh")]
        public async Task<ActionResult<RefreshResult>> RefreshProject(string projectId)
        {
            try
            {
                var result = await projectManager.RefreshProjectAsync(projectId);
                if (!result.Success)
                    return BadRequest(result);
                
                return Ok(result);
            }
            catch (ArgumentException)
            {
                return NotFound("Project not loaded");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error refreshing project");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{projectId}")]
        public async Task<ActionResult> UnloadProject(string projectId)
        {
            try
            {
                await projectManager.UnloadProjectAsync(projectId);
                return NoContent();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error unloading project");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}