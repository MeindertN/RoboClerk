using Microsoft.AspNetCore.Mvc;
using RoboClerk.Server.Services;
using System.Reflection;

namespace RoboClerk.Server.Controllers
{
    /// <summary>
    /// Health check endpoints for container orchestration and monitoring
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<HealthController> _logger;
        private static readonly DateTime _startTime = DateTime.UtcNow;

        public HealthController(IServiceProvider serviceProvider, ILogger<HealthController> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Basic liveness check - is the server process responding?
        /// Used by Kubernetes/Docker for liveness probe
        /// </summary>
        [HttpGet("live")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Liveness()
        {
            return Ok(new 
            { 
                status = "alive", 
                timestamp = DateTime.UtcNow 
            });
        }

        /// <summary>
        /// Readiness check - is the server ready to accept requests?
        /// Used by Kubernetes/Docker for readiness probe
        /// </summary>
        [HttpGet("ready")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public IActionResult Readiness()
        {
            try
            {
                // Check if critical services are available
                var projectManager = _serviceProvider.GetService<IProjectManager>();
                var sharePointService = _serviceProvider.GetService<ISharePointService>();

                var checks = new Dictionary<string, bool>
                {
                    ["projectManager"] = projectManager != null,
                    ["sharePointService"] = sharePointService != null
                };

                var allHealthy = checks.Values.All(v => v);

                if (allHealthy)
                {
                    return Ok(new
                    {
                        status = "ready",
                        timestamp = DateTime.UtcNow,
                        checks
                    });
                }
                else
                {
                    _logger.LogWarning("Readiness check failed: some services unavailable");
                    return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                    {
                        status = "not_ready",
                        timestamp = DateTime.UtcNow,
                        checks
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Readiness check failed with exception");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    status = "error",
                    timestamp = DateTime.UtcNow,
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Detailed health check with component status and metrics
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Health()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version?.ToString() ?? "unknown";
            var uptime = DateTime.UtcNow - _startTime;

            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                version,
                uptime = new
                {
                    totalSeconds = uptime.TotalSeconds,
                    formatted = $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s"
                },
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                runtime = new
                {
                    framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                    osDescription = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                    processArchitecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString()
                },
                memory = new
                {
                    workingSetMB = Math.Round(Environment.WorkingSet / 1024.0 / 1024.0, 2),
                    gcTotalMemoryMB = Math.Round(GC.GetTotalMemory(false) / 1024.0 / 1024.0, 2)
                }
            });
        }

        /// <summary>
        /// Startup check - has the server completed initialization?
        /// Used by Kubernetes for startup probe
        /// </summary>
        [HttpGet("startup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public IActionResult Startup()
        {
            try
            {
                // Check that essential services have been registered
                var configuration = _serviceProvider.GetService<RoboClerk.Core.Configuration.IConfiguration>();
                
                if (configuration != null)
                {
                    return Ok(new
                    {
                        status = "started",
                        timestamp = DateTime.UtcNow
                    });
                }
                
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    status = "starting",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Startup check failed");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    status = "error",
                    timestamp = DateTime.UtcNow,
                    error = ex.Message
                });
            }
        }
    }
}
