using NUnit.Framework;
using NSubstitute;
using RoboClerk.Server.Configuration;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace RoboClerk.Tests.Server
{
    [TestFixture]
    [Description("Tests for the ServerConfigurationLoader that loads server configuration from TOML files")]
    public class TestServerConfigurationLoader
    {
        private IFileSystem mockFileSystem;
        private ServerConfigurationLoader loader;

        [SetUp]
        public void Setup()
        {
            mockFileSystem = new MockFileSystem();
            loader = new ServerConfigurationLoader(mockFileSystem);
        }

        [UnitTestAttribute(
            Identifier = "73A4BFFB-B180-4B8B-B16E-58D14ACE4770",
            Purpose = "ServerConfigurationLoader is created successfully",
            PostCondition = "Loader is initialized")]
        [Test]
        public void CreateLoader_Success()
        {
            // Arrange & Act
            var newLoader = new ServerConfigurationLoader(mockFileSystem);

            // Assert
            Assert.That(newLoader, Is.Not.Null);
        }

        [UnitTestAttribute(
            Identifier = "23CE5039-5E84-4D52-A38C-47B0247DF069",
            Purpose = "LoadConfiguration returns default values when file does not exist",
            PostCondition = "Default ServerConfiguration is returned")]
        [Test]
        public void LoadConfiguration_FileNotFound_ReturnsDefaults()
        {
            // Arrange
            var configPath = TestingHelpers.ConvertFilePath(@"C:\config\RoboClerk.Server.toml");

            // Act
            var config = loader.LoadConfiguration(configPath);

            // Assert
            Assert.That(config, Is.Not.Null);
            Assert.That(config.Server.HttpPort, Is.EqualTo(51046));
            Assert.That(config.Server.HttpsPort, Is.EqualTo(51045));
            Assert.That(config.Server.HostAddress, Is.EqualTo("localhost"));
        }

        [UnitTestAttribute(
            Identifier = "88F280F5-8B22-4064-9163-7B5CEC480A5D",
            Purpose = "LoadConfiguration parses Server section correctly",
            PostCondition = "Server settings are loaded from TOML")]
        [Test]
        public void LoadConfiguration_ServerSection_ParsedCorrectly()
        {
            // Arrange
            var configContent = @"
[Server]
HttpPort = 8080
HttpsPort = 8443
HostAddress = ""0.0.0.0""
UseHttpsRedirection = false
Environment = ""Production""
";
            var configPath = TestingHelpers.ConvertFilePath(@"C:\config\RoboClerk.Server.toml");
            mockFileSystem.Directory.CreateDirectory(TestingHelpers.ConvertFilePath(@"C:\config"));
            mockFileSystem.File.WriteAllText(configPath, configContent);

            // Act
            var config = loader.LoadConfiguration(configPath);

            // Assert
            Assert.That(config.Server.HttpPort, Is.EqualTo(8080));
            Assert.That(config.Server.HttpsPort, Is.EqualTo(8443));
            Assert.That(config.Server.HostAddress, Is.EqualTo("0.0.0.0"));
            Assert.That(config.Server.UseHttpsRedirection, Is.False);
            Assert.That(config.Server.Environment, Is.EqualTo("Production"));
        }

        [UnitTestAttribute(
            Identifier = "212620FD-E798-4A49-B520-BEED7367C6B1",
            Purpose = "LoadConfiguration parses API section correctly",
            PostCondition = "API settings are loaded from TOML")]
        [Test]
        public void LoadConfiguration_ApiSection_ParsedCorrectly()
        {
            // Arrange
            var configContent = @"
[API]
BasePath = ""/api/v1""
EnableSwaggerInProduction = true
SwaggerRoutePrefix = ""swagger""
MaxRequestBodySize = 52428800
RequestTimeoutSeconds = 600
";
            var configPath = TestingHelpers.ConvertFilePath(@"C:\config\RoboClerk.Server.toml");
            mockFileSystem.Directory.CreateDirectory(TestingHelpers.ConvertFilePath(@"C:\config"));
            mockFileSystem.File.WriteAllText(configPath, configContent);

            // Act
            var config = loader.LoadConfiguration(configPath);

            // Assert
            Assert.That(config.API.BasePath, Is.EqualTo("/api/v1"));
            Assert.That(config.API.EnableSwaggerInProduction, Is.True);
            Assert.That(config.API.SwaggerRoutePrefix, Is.EqualTo("swagger"));
            Assert.That(config.API.MaxRequestBodySize, Is.EqualTo(52428800));
            Assert.That(config.API.RequestTimeoutSeconds, Is.EqualTo(600));
        }

        [UnitTestAttribute(
            Identifier = "16EA61FB-74AB-4D16-96CC-6E36F442DAA9",
            Purpose = "LoadConfiguration parses CORS section correctly",
            PostCondition = "CORS settings are loaded from TOML")]
        [Test]
        public void LoadConfiguration_CorsSection_ParsedCorrectly()
        {
            // Arrange
            var configContent = @"
[CORS]
EnableCORS = false
AllowedOrigins = ""https://example.com""
AllowedMethods = ""GET,POST""
AllowedHeaders = ""Content-Type""
AllowCredentials = true
";
            var configPath = TestingHelpers.ConvertFilePath(@"C:\config\RoboClerk.Server.toml");
            mockFileSystem.Directory.CreateDirectory(TestingHelpers.ConvertFilePath(@"C:\config"));
            mockFileSystem.File.WriteAllText(configPath, configContent);

            // Act
            var config = loader.LoadConfiguration(configPath);

            // Assert
            Assert.That(config.CORS.EnableCORS, Is.False);
            Assert.That(config.CORS.AllowedOrigins, Is.EqualTo("https://example.com"));
            Assert.That(config.CORS.AllowedMethods, Is.EqualTo("GET,POST"));
            Assert.That(config.CORS.AllowedHeaders, Is.EqualTo("Content-Type"));
            Assert.That(config.CORS.AllowCredentials, Is.True);
        }

        [UnitTestAttribute(
            Identifier = "288DB479-A925-4263-9494-4D8C12031FE7",
            Purpose = "LoadConfiguration parses Logging section correctly",
            PostCondition = "Logging settings are loaded from TOML")]
        [Test]
        public void LoadConfiguration_LoggingSection_ParsedCorrectly()
        {
            // Arrange
            var configContent = @"
[Logging]
ServerLogLevel = ""DEBUG""
LogApiRequests = true
LogPerformanceMetrics = true
";
            var configPath = TestingHelpers.ConvertFilePath(@"C:\config\RoboClerk.Server.toml");
            mockFileSystem.Directory.CreateDirectory(TestingHelpers.ConvertFilePath(@"C:\config"));
            mockFileSystem.File.WriteAllText(configPath, configContent);

            // Act
            var config = loader.LoadConfiguration(configPath);

            // Assert
            Assert.That(config.Logging.ServerLogLevel, Is.EqualTo("DEBUG"));
            Assert.That(config.Logging.LogApiRequests, Is.True);
            Assert.That(config.Logging.LogPerformanceMetrics, Is.True);
        }

        [UnitTestAttribute(
            Identifier = "A869BEC8-5452-4BE2-8095-5A5823A23C92",
            Purpose = "LoadConfiguration parses Session section correctly",
            PostCondition = "Session settings are loaded from TOML")]
        [Test]
        public void LoadConfiguration_SessionSection_ParsedCorrectly()
        {
            // Arrange
            var configContent = @"
[Session]
ProjectSessionTimeoutMinutes = 120
MaxConcurrentProjects = 20
SessionCleanupIntervalMinutes = 30
";
            var configPath = TestingHelpers.ConvertFilePath(@"C:\config\RoboClerk.Server.toml");
            mockFileSystem.Directory.CreateDirectory(TestingHelpers.ConvertFilePath(@"C:\config"));
            mockFileSystem.File.WriteAllText(configPath, configContent);

            // Act
            var config = loader.LoadConfiguration(configPath);

            // Assert
            Assert.That(config.Session.ProjectSessionTimeoutMinutes, Is.EqualTo(120));
            Assert.That(config.Session.MaxConcurrentProjects, Is.EqualTo(20));
            Assert.That(config.Session.SessionCleanupIntervalMinutes, Is.EqualTo(30));
        }

        [UnitTestAttribute(
            Identifier = "BBB223A0-3213-4ABF-8F35-F044645968DC",
            Purpose = "LoadConfiguration parses Performance section correctly",
            PostCondition = "Performance settings are loaded from TOML")]
        [Test]
        public void LoadConfiguration_PerformanceSection_ParsedCorrectly()
        {
            // Arrange
            var configContent = @"
[Performance]
EnableResponseCaching = false
ProjectMetadataCacheDurationSeconds = 600
TemplateFilesCacheDurationSeconds = 1200
MaxProjectMemoryMB = 1000
";
            var configPath = TestingHelpers.ConvertFilePath(@"C:\config\RoboClerk.Server.toml");
            mockFileSystem.Directory.CreateDirectory(TestingHelpers.ConvertFilePath(@"C:\config"));
            mockFileSystem.File.WriteAllText(configPath, configContent);

            // Act
            var config = loader.LoadConfiguration(configPath);

            // Assert
            Assert.That(config.Performance.EnableResponseCaching, Is.False);
            Assert.That(config.Performance.ProjectMetadataCacheDurationSeconds, Is.EqualTo(600));
            Assert.That(config.Performance.TemplateFilesCacheDurationSeconds, Is.EqualTo(1200));
            Assert.That(config.Performance.MaxProjectMemoryMB, Is.EqualTo(1000));
        }

        [UnitTestAttribute(
            Identifier = "3D63522E-36FF-494F-861E-E857533D2935",
            Purpose = "LoadConfiguration parses Security section correctly",
            PostCondition = "Security settings are loaded from TOML")]
        [Test]
        public void LoadConfiguration_SecuritySection_ParsedCorrectly()
        {
            // Arrange
            var configContent = @"
[Security]
EnableApiKeyAuth = true
ApiKeyHeaderName = ""X-Custom-Key""
EnableRateLimiting = true
RateLimitRequestsPerMinute = 50
";
            var configPath = TestingHelpers.ConvertFilePath(@"C:\config\RoboClerk.Server.toml");
            mockFileSystem.Directory.CreateDirectory(TestingHelpers.ConvertFilePath(@"C:\config"));
            mockFileSystem.File.WriteAllText(configPath, configContent);

            // Act
            var config = loader.LoadConfiguration(configPath);

            // Assert
            Assert.That(config.Security.EnableApiKeyAuth, Is.True);
            Assert.That(config.Security.ApiKeyHeaderName, Is.EqualTo("X-Custom-Key"));
            Assert.That(config.Security.EnableRateLimiting, Is.True);
            Assert.That(config.Security.RateLimitRequestsPerMinute, Is.EqualTo(50));
        }

        [UnitTestAttribute(
            Identifier = "F46979E7-205C-4756-A0E6-5F848327A0FB",
            Purpose = "LoadConfiguration parses SharePoint section correctly",
            PostCondition = "SharePoint settings are loaded from TOML")]
        [Test]
        public void LoadConfiguration_SharePointSection_ParsedCorrectly()
        {
            // Arrange
            var configContent = @"
[SharePoint]
ClientId = ""client-id-123""
TenantId = ""tenant-id-456""
OperationTimeoutSeconds = 180
RetryCount = 5
RetryDelayMilliseconds = 2000
CacheAuthTokens = false
";
            var configPath = TestingHelpers.ConvertFilePath(@"C:\config\RoboClerk.Server.toml");
            mockFileSystem.Directory.CreateDirectory(TestingHelpers.ConvertFilePath(@"C:\config"));
            mockFileSystem.File.WriteAllText(configPath, configContent);

            // Act
            var config = loader.LoadConfiguration(configPath);

            // Assert
            Assert.That(config.SharePoint.ClientId, Is.EqualTo("client-id-123"));
            Assert.That(config.SharePoint.TenantId, Is.EqualTo("tenant-id-456"));
            Assert.That(config.SharePoint.OperationTimeoutSeconds, Is.EqualTo(180));
            Assert.That(config.SharePoint.RetryCount, Is.EqualTo(5));
            Assert.That(config.SharePoint.RetryDelayMilliseconds, Is.EqualTo(2000));
            Assert.That(config.SharePoint.CacheAuthTokens, Is.False);
        }

        [UnitTestAttribute(
            Identifier = "1FF7E0E5-4E32-4AC8-8EE1-D4D7C152D058",
            Purpose = "LoadConfiguration parses HealthCheck section correctly",
            PostCondition = "HealthCheck settings are loaded from TOML")]
        [Test]
        public void LoadConfiguration_HealthCheckSection_ParsedCorrectly()
        {
            // Arrange
            var configContent = @"
[HealthCheck]
EnableHealthChecks = false
HealthCheckPath = ""/healthz""
EnableDetailedHealthCheck = true
";
            var configPath = TestingHelpers.ConvertFilePath(@"C:\config\RoboClerk.Server.toml");
            mockFileSystem.Directory.CreateDirectory(TestingHelpers.ConvertFilePath(@"C:\config"));
            mockFileSystem.File.WriteAllText(configPath, configContent);

            // Act
            var config = loader.LoadConfiguration(configPath);

            // Assert
            Assert.That(config.HealthCheck.EnableHealthChecks, Is.False);
            Assert.That(config.HealthCheck.HealthCheckPath, Is.EqualTo("/healthz"));
            Assert.That(config.HealthCheck.EnableDetailedHealthCheck, Is.True);
        }

        [UnitTestAttribute(
            Identifier = "1ACD773E-D8CE-48AE-A36F-E409761F09DA",
            Purpose = "LoadConfiguration parses Monitoring section correctly",
            PostCondition = "Monitoring settings are loaded from TOML")]
        [Test]
        public void LoadConfiguration_MonitoringSection_ParsedCorrectly()
        {
            // Arrange
            var configContent = @"
[Monitoring]
EnableApplicationInsights = true
LogDetailedExceptions = false
MonitorMemoryUsage = true
MemoryCheckIntervalMinutes = 10
";
            var configPath = TestingHelpers.ConvertFilePath(@"C:\config\RoboClerk.Server.toml");
            mockFileSystem.Directory.CreateDirectory(TestingHelpers.ConvertFilePath(@"C:\config"));
            mockFileSystem.File.WriteAllText(configPath, configContent);

            // Act
            var config = loader.LoadConfiguration(configPath);

            // Assert
            Assert.That(config.Monitoring.EnableApplicationInsights, Is.True);
            Assert.That(config.Monitoring.LogDetailedExceptions, Is.False);
            Assert.That(config.Monitoring.MonitorMemoryUsage, Is.True);
            Assert.That(config.Monitoring.MemoryCheckIntervalMinutes, Is.EqualTo(10));
        }

        [UnitTestAttribute(
            Identifier = "094C4665-AD7E-442E-B67A-3A6359EAF247",
            Purpose = "LoadConfiguration applies command line overrides",
            PostCondition = "Command line options override file values")]
        [Test]
        public void LoadConfiguration_WithCommandLineOverrides_AppliesOverrides()
        {
            // Arrange
            var configContent = @"
[Server]
HttpPort = 8080
HostAddress = ""localhost""
";
            var configPath = TestingHelpers.ConvertFilePath(@"C:\config\RoboClerk.Server.toml");
            mockFileSystem.Directory.CreateDirectory(TestingHelpers.ConvertFilePath(@"C:\config"));
            mockFileSystem.File.WriteAllText(configPath, configContent);

            var cmdOptions = new Dictionary<string, string>
            {
                { "Server.HttpPort", "9090" },
                { "Server.HostAddress", "0.0.0.0" }
            };

            // Act
            var config = loader.LoadConfiguration(configPath, cmdOptions);

            // Assert
            Assert.That(config.Server.HttpPort, Is.EqualTo(9090));
            Assert.That(config.Server.HostAddress, Is.EqualTo("0.0.0.0"));
        }

        [UnitTestAttribute(
            Identifier = "1A7C36CC-89B2-494F-B578-66F47A4C4FFD",
            Purpose = "LoadConfiguration handles invalid TOML gracefully",
            PostCondition = "Default configuration is returned on parse error")]
        [Test]
        public void LoadConfiguration_InvalidToml_ReturnsDefaults()
        {
            // Arrange
            var configContent = @"
[Server
HttpPort = invalid_value
";
            var configPath = TestingHelpers.ConvertFilePath(@"C:\config\RoboClerk.Server.toml");
            mockFileSystem.Directory.CreateDirectory(TestingHelpers.ConvertFilePath(@"C:\config"));
            mockFileSystem.File.WriteAllText(configPath, configContent);

            // Act
            var config = loader.LoadConfiguration(configPath);

            // Assert
            Assert.That(config, Is.Not.Null);
            Assert.That(config.Server.HttpPort, Is.EqualTo(51046)); // Default value
        }

        [UnitTestAttribute(
            Identifier = "1692332B-DB97-4C74-9F79-7A507491428F",
            Purpose = "LoadConfiguration parses FileSystem section correctly",
            PostCondition = "FileSystem settings are loaded from TOML")]
        [Test]
        public void LoadConfiguration_FileSystemSection_ParsedCorrectly()
        {
            // Arrange
            var configContent = @"
[FileSystem]
EnableFileSystemValidation = false
AllowedTemplateExtensions = "".docx,.pdf""
MaxUploadFileSizeBytes = 104857600
TempFileCleanupIntervalMinutes = 60
";
            var configPath = TestingHelpers.ConvertFilePath(@"C:\config\RoboClerk.Server.toml");
            mockFileSystem.Directory.CreateDirectory(TestingHelpers.ConvertFilePath(@"C:\config"));
            mockFileSystem.File.WriteAllText(configPath, configContent);

            // Act
            var config = loader.LoadConfiguration(configPath);

            // Assert
            Assert.That(config.FileSystem.EnableFileSystemValidation, Is.False);
            Assert.That(config.FileSystem.AllowedTemplateExtensions, Is.EqualTo(".docx,.pdf"));
            Assert.That(config.FileSystem.MaxUploadFileSizeBytes, Is.EqualTo(104857600));
            Assert.That(config.FileSystem.TempFileCleanupIntervalMinutes, Is.EqualTo(60));
        }

        [UnitTestAttribute(
            Identifier = "252CF934-3B7B-453C-85DA-B7DC499809DC",
            Purpose = "LoadConfiguration parses complete configuration file",
            PostCondition = "All sections are parsed correctly")]
        [Test]
        public void LoadConfiguration_CompleteFile_ParsedCorrectly()
        {
            // Arrange
            var configContent = @"
[Server]
HttpPort = 5000
HttpsPort = 5001
HostAddress = ""127.0.0.1""
UseHttpsRedirection = true
Environment = ""Staging""

[API]
BasePath = ""/api""
EnableSwaggerInProduction = false

[CORS]
EnableCORS = true
AllowedOrigins = ""*""

[Logging]
ServerLogLevel = ""WARN""

[Session]
ProjectSessionTimeoutMinutes = 30

[Performance]
EnableResponseCaching = true

[Security]
EnableApiKeyAuth = false

[SharePoint]
ClientId = ""test-client""
TenantId = ""test-tenant""

[HealthCheck]
EnableHealthChecks = true

[Monitoring]
EnableApplicationInsights = false
";
            var configPath = TestingHelpers.ConvertFilePath(@"C:\config\RoboClerk.Server.toml");
            mockFileSystem.Directory.CreateDirectory(TestingHelpers.ConvertFilePath(@"C:\config"));
            mockFileSystem.File.WriteAllText(configPath, configContent);

            // Act
            var config = loader.LoadConfiguration(configPath);

            // Assert
            Assert.That(config.Server.HttpPort, Is.EqualTo(5000));
            Assert.That(config.Server.Environment, Is.EqualTo("Staging"));
            Assert.That(config.API.BasePath, Is.EqualTo("/api"));
            Assert.That(config.CORS.EnableCORS, Is.True);
            Assert.That(config.Logging.ServerLogLevel, Is.EqualTo("WARN"));
            Assert.That(config.Session.ProjectSessionTimeoutMinutes, Is.EqualTo(30));
            Assert.That(config.SharePoint.ClientId, Is.EqualTo("test-client"));
            Assert.That(config.HealthCheck.EnableHealthChecks, Is.True);
        }

        [UnitTestAttribute(
            Identifier = "8866840E-A217-4626-BDB8-2B173AAE8103",
            Purpose = "LoadConfiguration handles missing sections gracefully",
            PostCondition = "Default values used for missing sections")]
        [Test]
        public void LoadConfiguration_MissingSections_UsesDefaults()
        {
            // Arrange - Only Server section, others missing
            var configContent = @"
[Server]
HttpPort = 9000
";
            var configPath = TestingHelpers.ConvertFilePath(@"C:\config\RoboClerk.Server.toml");
            mockFileSystem.Directory.CreateDirectory(TestingHelpers.ConvertFilePath(@"C:\config"));
            mockFileSystem.File.WriteAllText(configPath, configContent);

            // Act
            var config = loader.LoadConfiguration(configPath);

            // Assert
            Assert.That(config.Server.HttpPort, Is.EqualTo(9000)); // From file
            Assert.That(config.API.BasePath, Is.EqualTo("")); // Default
            Assert.That(config.CORS.EnableCORS, Is.True); // Default
            Assert.That(config.Session.ProjectSessionTimeoutMinutes, Is.EqualTo(60)); // Default
        }
    }
}
