using System.ComponentModel.DataAnnotations;

namespace RoboClerk.Server.Configuration
{
    /// <summary>
    /// Server-specific configuration settings for RoboClerk Server
    /// </summary>
    public class ServerConfiguration
    {
        public ServerSettings Server { get; set; } = new();
        public ApiSettings API { get; set; } = new();
        public CorsSettings CORS { get; set; } = new();
        public LoggingSettings Logging { get; set; } = new();
        public SessionSettings Session { get; set; } = new();
        public PerformanceSettings Performance { get; set; } = new();
        public SecuritySettings Security { get; set; } = new();
        public FileSystemSettings FileSystem { get; set; } = new();
        public SharePointSettings SharePoint { get; set; } = new();
        public HealthCheckSettings HealthCheck { get; set; } = new();
        public MonitoringSettings Monitoring { get; set; } = new();
    }

    public class ServerSettings
    {
        [Range(1, 65535)]
        public int HttpPort { get; set; } = 51046;
        
        [Range(1, 65535)]
        public int HttpsPort { get; set; } = 51045;
        
        public string HostAddress { get; set; } = "localhost";
        public bool UseHttpsRedirection { get; set; } = true;
        public string Environment { get; set; } = "Development";
    }

    public class ApiSettings
    {
        public string BasePath { get; set; } = "";
        public bool EnableSwaggerInProduction { get; set; } = false;
        public string SwaggerRoutePrefix { get; set; } = "";
        
        [Range(1024, int.MaxValue)]
        public long MaxRequestBodySize { get; set; } = 31457280; // 30MB
        
        [Range(1, 3600)]
        public int RequestTimeoutSeconds { get; set; } = 300; // 5 minutes
    }

    public class CorsSettings
    {
        public bool EnableCORS { get; set; } = true;
        public string AllowedOrigins { get; set; } = "*";
        public string AllowedMethods { get; set; } = "*";
        public string AllowedHeaders { get; set; } = "*";
        public bool AllowCredentials { get; set; } = false;
    }

    public class LoggingSettings
    {
        public string ServerLogLevel { get; set; } = "INFO";
        public bool LogApiRequests { get; set; } = false;
        public bool LogPerformanceMetrics { get; set; } = false;
    }

    public class SessionSettings
    {
        [Range(1, 1440)] // 1 minute to 24 hours
        public int ProjectSessionTimeoutMinutes { get; set; } = 60;
        
        [Range(1, 100)]
        public int MaxConcurrentProjects { get; set; } = 10;
        
        [Range(1, 60)]
        public int SessionCleanupIntervalMinutes { get; set; } = 15;
    }

    public class PerformanceSettings
    {
        public bool EnableResponseCaching { get; set; } = true;
        
        [Range(0, 3600)]
        public int ProjectMetadataCacheDurationSeconds { get; set; } = 300; // 5 minutes
        
        [Range(0, 3600)]
        public int TemplateFilesCacheDurationSeconds { get; set; } = 600; // 10 minutes
        
        [Range(100, 5000)]
        public int MaxProjectMemoryMB { get; set; } = 500;
    }

    public class SecuritySettings
    {
        public bool EnableApiKeyAuth { get; set; } = false;
        public string ApiKeyHeaderName { get; set; } = "X-API-Key";
        public bool EnableRateLimiting { get; set; } = false;
        
        [Range(1, 1000)]
        public int RateLimitRequestsPerMinute { get; set; } = 100;
    }

    public class FileSystemSettings
    {
        public bool EnableFileSystemValidation { get; set; } = true;
        public string AllowedTemplateExtensions { get; set; } = ".docx,.dotx,.html,.adoc";
        
        [Range(1024, long.MaxValue)]
        public long MaxUploadFileSizeBytes { get; set; } = 52428800; // 50MB
        
        [Range(1, 1440)]
        public int TempFileCleanupIntervalMinutes { get; set; } = 30;
    }

    public class SharePointSettings
    {
        /// <summary>
        /// SharePoint App Client ID for authentication
        /// This is the Application (client) ID from Azure AD app registration
        /// </summary>
        public string ClientId { get; set; } = string.Empty;
        
        /// <summary>
        /// Azure AD Tenant ID for SharePoint authentication
        /// This is the Directory (tenant) ID from Azure AD
        /// </summary>
        public string TenantId { get; set; } = string.Empty;
        
        [Range(1, 600)]
        public int OperationTimeoutSeconds { get; set; } = 120;
        
        [Range(0, 10)]
        public int RetryCount { get; set; } = 3;
        
        [Range(100, 10000)]
        public int RetryDelayMilliseconds { get; set; } = 1000;
        
        public bool CacheAuthTokens { get; set; } = true;
    }

    public class HealthCheckSettings
    {
        public bool EnableHealthChecks { get; set; } = true;
        public string HealthCheckPath { get; set; } = "/health";
        public bool EnableDetailedHealthCheck { get; set; } = false;
    }

    public class MonitoringSettings
    {
        public bool EnableApplicationInsights { get; set; } = false;
        public bool LogDetailedExceptions { get; set; } = true;
        public bool MonitorMemoryUsage { get; set; } = false;
        
        [Range(1, 60)]
        public int MemoryCheckIntervalMinutes { get; set; } = 5;
    }
}