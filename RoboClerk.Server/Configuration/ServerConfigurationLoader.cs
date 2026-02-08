using System.IO.Abstractions;
using Tomlyn;
using Tomlyn.Model;

namespace RoboClerk.Server.Configuration
{
    /// <summary>
    /// Loads and parses the RoboClerk.Server.toml configuration file
    /// </summary>
    public class ServerConfigurationLoader
    {
        private readonly IFileSystem fileSystem;
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public ServerConfigurationLoader(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        /// <summary>
        /// Load server configuration from the specified TOML file
        /// </summary>
        /// <param name="configFilePath">Path to the RoboClerk.Server.toml file</param>
        /// <param name="commandLineOptions">Optional command line overrides</param>
        /// <returns>Loaded server configuration</returns>
        public ServerConfiguration LoadConfiguration(string configFilePath, Dictionary<string, string>? commandLineOptions = null)
        {
            var config = new ServerConfiguration();

            try
            {
                if (!fileSystem.File.Exists(configFilePath))
                {
                    logger.Warn($"Server configuration file not found at {configFilePath}. Using default settings.");
                    return ApplyCommandLineOverrides(config, commandLineOptions);
                }

                logger.Info($"Loading server configuration from: {configFilePath}");
                
                var tomlContent = fileSystem.File.ReadAllText(configFilePath);
                var tomlModel = Toml.ToModel(tomlContent);

                // Parse each section
                ParseServerSection(tomlModel, config.Server);
                ParseApiSection(tomlModel, config.API);
                ParseCorsSection(tomlModel, config.CORS);
                ParseLoggingSection(tomlModel, config.Logging);
                ParseSessionSection(tomlModel, config.Session);
                ParsePerformanceSection(tomlModel, config.Performance);
                ParseSecuritySection(tomlModel, config.Security);
                ParseFileSystemSection(tomlModel, config.FileSystem);
                ParseSharePointSection(tomlModel, config.SharePoint);
                ParseGitSection(tomlModel, config.Git);
                ParseHealthCheckSection(tomlModel, config.HealthCheck);
                ParseMonitoringSection(tomlModel, config.Monitoring);

                logger.Info("Server configuration loaded successfully");
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error loading server configuration from {configFilePath}. Using default settings.");
            }

            return ApplyCommandLineOverrides(config, commandLineOptions);
        }

        private void ParseServerSection(TomlTable tomlModel, ServerSettings settings)
        {
            if (tomlModel.TryGetValue("Server", out var serverObj) && serverObj is TomlTable serverTable)
            {
                settings.HttpPort = GetValue(serverTable, "HttpPort", settings.HttpPort);
                settings.HttpsPort = GetValue(serverTable, "HttpsPort", settings.HttpsPort);
                settings.HostAddress = GetValue(serverTable, "HostAddress", settings.HostAddress);
                settings.UseHttpsRedirection = GetValue(serverTable, "UseHttpsRedirection", settings.UseHttpsRedirection);
                settings.Environment = GetValue(serverTable, "Environment", settings.Environment);
            }
        }

        private void ParseApiSection(TomlTable tomlModel, ApiSettings settings)
        {
            if (tomlModel.TryGetValue("API", out var apiObj) && apiObj is TomlTable apiTable)
            {
                settings.BasePath = GetValue(apiTable, "BasePath", settings.BasePath);
                settings.EnableSwaggerInProduction = GetValue(apiTable, "EnableSwaggerInProduction", settings.EnableSwaggerInProduction);
                settings.SwaggerRoutePrefix = GetValue(apiTable, "SwaggerRoutePrefix", settings.SwaggerRoutePrefix);
                settings.MaxRequestBodySize = GetValue(apiTable, "MaxRequestBodySize", settings.MaxRequestBodySize);
                settings.RequestTimeoutSeconds = GetValue(apiTable, "RequestTimeoutSeconds", settings.RequestTimeoutSeconds);
            }
        }

        private void ParseCorsSection(TomlTable tomlModel, CorsSettings settings)
        {
            if (tomlModel.TryGetValue("CORS", out var corsObj) && corsObj is TomlTable corsTable)
            {
                settings.EnableCORS = GetValue(corsTable, "EnableCORS", settings.EnableCORS);
                settings.AllowedOrigins = GetValue(corsTable, "AllowedOrigins", settings.AllowedOrigins);
                settings.AllowedMethods = GetValue(corsTable, "AllowedMethods", settings.AllowedMethods);
                settings.AllowedHeaders = GetValue(corsTable, "AllowedHeaders", settings.AllowedHeaders);
                settings.AllowCredentials = GetValue(corsTable, "AllowCredentials", settings.AllowCredentials);
            }
        }

        private void ParseLoggingSection(TomlTable tomlModel, LoggingSettings settings)
        {
            if (tomlModel.TryGetValue("Logging", out var loggingObj) && loggingObj is TomlTable loggingTable)
            {
                settings.ServerLogLevel = GetValue(loggingTable, "ServerLogLevel", settings.ServerLogLevel);
                settings.LogApiRequests = GetValue(loggingTable, "LogApiRequests", settings.LogApiRequests);
                settings.LogPerformanceMetrics = GetValue(loggingTable, "LogPerformanceMetrics", settings.LogPerformanceMetrics);
            }
        }

        private void ParseSessionSection(TomlTable tomlModel, SessionSettings settings)
        {
            if (tomlModel.TryGetValue("Session", out var sessionObj) && sessionObj is TomlTable sessionTable)
            {
                settings.ProjectSessionTimeoutMinutes = GetValue(sessionTable, "ProjectSessionTimeoutMinutes", settings.ProjectSessionTimeoutMinutes);
                settings.MaxConcurrentProjects = GetValue(sessionTable, "MaxConcurrentProjects", settings.MaxConcurrentProjects);
                settings.SessionCleanupIntervalMinutes = GetValue(sessionTable, "SessionCleanupIntervalMinutes", settings.SessionCleanupIntervalMinutes);
            }
        }

        private void ParsePerformanceSection(TomlTable tomlModel, PerformanceSettings settings)
        {
            if (tomlModel.TryGetValue("Performance", out var perfObj) && perfObj is TomlTable perfTable)
            {
                settings.EnableResponseCaching = GetValue(perfTable, "EnableResponseCaching", settings.EnableResponseCaching);
                settings.ProjectMetadataCacheDurationSeconds = GetValue(perfTable, "ProjectMetadataCacheDurationSeconds", settings.ProjectMetadataCacheDurationSeconds);
                settings.TemplateFilesCacheDurationSeconds = GetValue(perfTable, "TemplateFilesCacheDurationSeconds", settings.TemplateFilesCacheDurationSeconds);
                settings.MaxProjectMemoryMB = GetValue(perfTable, "MaxProjectMemoryMB", settings.MaxProjectMemoryMB);
            }
        }

        private void ParseSecuritySection(TomlTable tomlModel, SecuritySettings settings)
        {
            if (tomlModel.TryGetValue("Security", out var securityObj) && securityObj is TomlTable securityTable)
            {
                settings.EnableApiKeyAuth = GetValue(securityTable, "EnableApiKeyAuth", settings.EnableApiKeyAuth);
                settings.ApiKeyHeaderName = GetValue(securityTable, "ApiKeyHeaderName", settings.ApiKeyHeaderName);
                settings.EnableRateLimiting = GetValue(securityTable, "EnableRateLimiting", settings.EnableRateLimiting);
                settings.RateLimitRequestsPerMinute = GetValue(securityTable, "RateLimitRequestsPerMinute", settings.RateLimitRequestsPerMinute);
            }
        }

        private void ParseFileSystemSection(TomlTable tomlModel, FileSystemSettings settings)
        {
            if (tomlModel.TryGetValue("FileSystem", out var fsObj) && fsObj is TomlTable fsTable)
            {
                settings.EnableFileSystemValidation = GetValue(fsTable, "EnableFileSystemValidation", settings.EnableFileSystemValidation);
                settings.AllowedTemplateExtensions = GetValue(fsTable, "AllowedTemplateExtensions", settings.AllowedTemplateExtensions);
                settings.MaxUploadFileSizeBytes = GetValue(fsTable, "MaxUploadFileSizeBytes", settings.MaxUploadFileSizeBytes);
                settings.TempFileCleanupIntervalMinutes = GetValue(fsTable, "TempFileCleanupIntervalMinutes", settings.TempFileCleanupIntervalMinutes);
            }
        }

        private void ParseSharePointSection(TomlTable tomlModel, SharePointSettings settings)
        {
            if (tomlModel.TryGetValue("SharePoint", out var spObj) && spObj is TomlTable spTable)
            {
                settings.ClientId = GetValue(spTable, "ClientId", settings.ClientId);
                settings.TenantId = GetValue(spTable, "TenantId", settings.TenantId);
                settings.OperationTimeoutSeconds = GetValue(spTable, "OperationTimeoutSeconds", settings.OperationTimeoutSeconds);
                settings.RetryCount = GetValue(spTable, "RetryCount", settings.RetryCount);
                settings.RetryDelayMilliseconds = GetValue(spTable, "RetryDelayMilliseconds", settings.RetryDelayMilliseconds);
                settings.CacheAuthTokens = GetValue(spTable, "CacheAuthTokens", settings.CacheAuthTokens);
            }
        }

        private void ParseGitSection(TomlTable tomlModel, GitSettings settings)
        {
            if (tomlModel.TryGetValue("Git", out var gitObj) && gitObj is TomlTable gitTable)
            {
                settings.GitHubToken = GetValue(gitTable, "GitHubToken", settings.GitHubToken);
                settings.GitLabToken = GetValue(gitTable, "GitLabToken", settings.GitLabToken);
                settings.AzureDevOpsPat = GetValue(gitTable, "AzureDevOpsPat", settings.AzureDevOpsPat);
                settings.BitbucketToken = GetValue(gitTable, "BitbucketToken", settings.BitbucketToken);
                settings.CloneTimeoutSeconds = GetValue(gitTable, "CloneTimeoutSeconds", settings.CloneTimeoutSeconds);
                settings.MaxRepoSizeMB = GetValue(gitTable, "MaxRepoSizeMB", settings.MaxRepoSizeMB);
                settings.TempCloneDirectory = GetValue(gitTable, "TempCloneDirectory", settings.TempCloneDirectory);
                settings.DefaultShallowClone = GetValue(gitTable, "DefaultShallowClone", settings.DefaultShallowClone);
            }
        }

        private void ParseHealthCheckSection(TomlTable tomlModel, HealthCheckSettings settings)
        {
            if (tomlModel.TryGetValue("HealthCheck", out var hcObj) && hcObj is TomlTable hcTable)
            {
                settings.EnableHealthChecks = GetValue(hcTable, "EnableHealthChecks", settings.EnableHealthChecks);
                settings.HealthCheckPath = GetValue(hcTable, "HealthCheckPath", settings.HealthCheckPath);
                settings.EnableDetailedHealthCheck = GetValue(hcTable, "EnableDetailedHealthCheck", settings.EnableDetailedHealthCheck);
            }
        }

        private void ParseMonitoringSection(TomlTable tomlModel, MonitoringSettings settings)
        {
            if (tomlModel.TryGetValue("Monitoring", out var monitoringObj) && monitoringObj is TomlTable monitoringTable)
            {
                settings.EnableApplicationInsights = GetValue(monitoringTable, "EnableApplicationInsights", settings.EnableApplicationInsights);
                settings.LogDetailedExceptions = GetValue(monitoringTable, "LogDetailedExceptions", settings.LogDetailedExceptions);
                settings.MonitorMemoryUsage = GetValue(monitoringTable, "MonitorMemoryUsage", settings.MonitorMemoryUsage);
                settings.MemoryCheckIntervalMinutes = GetValue(monitoringTable, "MemoryCheckIntervalMinutes", settings.MemoryCheckIntervalMinutes);
            }
        }

        private T GetValue<T>(TomlTable table, string key, T defaultValue)
        {
            if (table.TryGetValue(key, out var value))
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch (Exception ex)
                {
                    logger.Warn(ex, $"Failed to convert configuration value '{key}' to type {typeof(T).Name}. Using default value.");
                }
            }
            return defaultValue;
        }

        private ServerConfiguration ApplyCommandLineOverrides(ServerConfiguration config, Dictionary<string, string>? commandLineOptions)
        {
            if (commandLineOptions == null || !commandLineOptions.Any())
            {
                // Even without command line options, apply environment variable overrides
                return ApplyEnvironmentVariableOverrides(config);
            }

            logger.Info($"Applying {commandLineOptions.Count} command line configuration overrides");

            foreach (var option in commandLineOptions)
            {
                try
                {
                    ApplyCommandLineOverride(config, option.Key, option.Value);
                }
                catch (Exception ex)
                {
                    logger.Warn(ex, $"Failed to apply command line override for '{option.Key}' = '{option.Value}'");
                }
            }

            // Apply environment variable overrides after command line (highest priority)
            return ApplyEnvironmentVariableOverrides(config);
        }

        /// <summary>
        /// Apply environment variable overrides for sensitive settings.
        /// Environment variables take highest priority for secrets management.
        /// </summary>
        private ServerConfiguration ApplyEnvironmentVariableOverrides(ServerConfiguration config)
        {
            // SharePoint credentials - these should typically be set via environment variables in production
            var spClientId = Environment.GetEnvironmentVariable("SP_CLIENT_ID");
            if (!string.IsNullOrEmpty(spClientId))
            {
                config.SharePoint.ClientId = spClientId;
                logger.Info("SharePoint ClientId loaded from environment variable SP_CLIENT_ID");
            }

            var spTenantId = Environment.GetEnvironmentVariable("SP_TENANT_ID");
            if (!string.IsNullOrEmpty(spTenantId))
            {
                config.SharePoint.TenantId = spTenantId;
                logger.Info("SharePoint TenantId loaded from environment variable SP_TENANT_ID");
            }

            var spClientSecret = Environment.GetEnvironmentVariable("SP_CLIENT_SECRET");
            if (!string.IsNullOrEmpty(spClientSecret))
            {
                config.SharePoint.ClientSecret = spClientSecret;
                logger.Info("SharePoint ClientSecret loaded from environment variable SP_CLIENT_SECRET");
            }

            // Git credentials from environment variables
            var gitHubToken = Environment.GetEnvironmentVariable("GIT_GITHUB_TOKEN");
            if (!string.IsNullOrEmpty(gitHubToken))
            {
                config.Git.GitHubToken = gitHubToken;
                logger.Info("Git GitHub token loaded from environment variable GIT_GITHUB_TOKEN");
            }

            var gitLabToken = Environment.GetEnvironmentVariable("GIT_GITLAB_TOKEN");
            if (!string.IsNullOrEmpty(gitLabToken))
            {
                config.Git.GitLabToken = gitLabToken;
                logger.Info("Git GitLab token loaded from environment variable GIT_GITLAB_TOKEN");
            }

            var azureDevOpsPat = Environment.GetEnvironmentVariable("GIT_AZURE_DEVOPS_PAT");
            if (!string.IsNullOrEmpty(azureDevOpsPat))
            {
                config.Git.AzureDevOpsPat = azureDevOpsPat;
                logger.Info("Git Azure DevOps PAT loaded from environment variable GIT_AZURE_DEVOPS_PAT");
            }

            var bitbucketToken = Environment.GetEnvironmentVariable("GIT_BITBUCKET_TOKEN");
            if (!string.IsNullOrEmpty(bitbucketToken))
            {
                config.Git.BitbucketToken = bitbucketToken;
                logger.Info("Git Bitbucket token loaded from environment variable GIT_BITBUCKET_TOKEN");
            }

            var gitCloneTimeout = Environment.GetEnvironmentVariable("GIT_CLONE_TIMEOUT");
            if (!string.IsNullOrEmpty(gitCloneTimeout) && int.TryParse(gitCloneTimeout, out var timeout))
            {
                config.Git.CloneTimeoutSeconds = timeout;
                logger.Info($"Git clone timeout set from environment variable: {timeout}");
            }

            // Server settings from environment
            var httpPort = Environment.GetEnvironmentVariable("ROBOCLERK_HTTP_PORT");
            if (!string.IsNullOrEmpty(httpPort) && int.TryParse(httpPort, out var port))
            {
                config.Server.HttpPort = port;
                logger.Info($"HTTP port set from environment variable: {port}");
            }

            var hostAddress = Environment.GetEnvironmentVariable("ROBOCLERK_HOST_ADDRESS");
            if (!string.IsNullOrEmpty(hostAddress))
            {
                config.Server.HostAddress = hostAddress;
                logger.Info($"Host address set from environment variable: {hostAddress}");
            }

            // Logging level from environment
            var logLevel = Environment.GetEnvironmentVariable("ROBOCLERK_LOG_LEVEL");
            if (!string.IsNullOrEmpty(logLevel))
            {
                config.Logging.ServerLogLevel = logLevel;
                logger.Info($"Log level set from environment variable: {logLevel}");
            }

            // CORS settings from environment (useful for dynamic configuration)
            var corsOrigins = Environment.GetEnvironmentVariable("ROBOCLERK_CORS_ORIGINS");
            if (!string.IsNullOrEmpty(corsOrigins))
            {
                config.CORS.AllowedOrigins = corsOrigins;
                logger.Info($"CORS origins set from environment variable");
            }

            return config;
        }

        private void ApplyCommandLineOverride(ServerConfiguration config, string key, string value)
        {
            // Support dot notation for nested properties (e.g., "Server.HttpPort")
            var parts = key.Split('.');
            if (parts.Length != 2) return;

            var section = parts[0];
            var property = parts[1];

            switch (section.ToLowerInvariant())
            {
                case "server":
                    ApplyServerOverride(config.Server, property, value);
                    break;
                case "api":
                    ApplyApiOverride(config.API, property, value);
                    break;
                case "cors":
                    ApplyCorsOverride(config.CORS, property, value);
                    break;
                case "logging":
                    ApplyLoggingOverride(config.Logging, property, value);
                    break;
                // Add other sections as needed
            }
        }

        private void ApplyServerOverride(ServerSettings settings, string property, string value)
        {
            switch (property.ToLowerInvariant())
            {
                case "httpport":
                    if (int.TryParse(value, out var httpPort)) settings.HttpPort = httpPort;
                    break;
                case "httpsport":
                    if (int.TryParse(value, out var httpsPort)) settings.HttpsPort = httpsPort;
                    break;
                case "hostaddress":
                    settings.HostAddress = value;
                    break;
                case "usehttpsredirection":
                    if (bool.TryParse(value, out var useHttps)) settings.UseHttpsRedirection = useHttps;
                    break;
                case "environment":
                    settings.Environment = value;
                    break;
            }
        }

        private void ApplyApiOverride(ApiSettings settings, string property, string value)
        {
            switch (property.ToLowerInvariant())
            {
                case "basepath":
                    settings.BasePath = value;
                    break;
                case "enableswaggerinproduction":
                    if (bool.TryParse(value, out var enableSwagger)) settings.EnableSwaggerInProduction = enableSwagger;
                    break;
                // Add other API properties as needed
            }
        }

        private void ApplyCorsOverride(CorsSettings settings, string property, string value)
        {
            switch (property.ToLowerInvariant())
            {
                case "enablecors":
                    if (bool.TryParse(value, out var enableCors)) settings.EnableCORS = enableCors;
                    break;
                case "allowedorigins":
                    settings.AllowedOrigins = value;
                    break;
                // Add other CORS properties as needed
            }
        }

        private void ApplyLoggingOverride(LoggingSettings settings, string property, string value)
        {
            switch (property.ToLowerInvariant())
            {
                case "serverloglevel":
                    settings.ServerLogLevel = value;
                    break;
                case "logapirequests":
                    if (bool.TryParse(value, out var logApi)) settings.LogApiRequests = logApi;
                    break;
                // Add other logging properties as needed
            }
        }
    }
}