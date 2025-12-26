using NUnit.Framework;
using RoboClerk.Server.Configuration;

namespace RoboClerk.Tests.Server
{
    [TestFixture]
    [Description("Tests for the ServerConfiguration classes and their default values")]
    public class TestServerConfiguration
    {
        [UnitTestAttribute(
            Identifier = "4BABBB77-7892-488B-869D-13CA6323CF69",
            Purpose = "ServerConfiguration is created with default nested settings",
            PostCondition = "All nested settings are initialized")]
        [Test]
        public void ServerConfiguration_DefaultConstructor_AllSettingsInitialized()
        {
            // Arrange & Act
            var config = new ServerConfiguration();

            // Assert
            Assert.That(config.Server, Is.Not.Null);
            Assert.That(config.API, Is.Not.Null);
            Assert.That(config.CORS, Is.Not.Null);
            Assert.That(config.Logging, Is.Not.Null);
            Assert.That(config.Session, Is.Not.Null);
            Assert.That(config.Performance, Is.Not.Null);
            Assert.That(config.Security, Is.Not.Null);
            Assert.That(config.FileSystem, Is.Not.Null);
            Assert.That(config.SharePoint, Is.Not.Null);
            Assert.That(config.HealthCheck, Is.Not.Null);
            Assert.That(config.Monitoring, Is.Not.Null);
        }

        [UnitTestAttribute(
            Identifier = "BF559F98-53EA-442D-9D3E-F9023416956C",
            Purpose = "ServerSettings has correct default values",
            PostCondition = "Default values match expected")]
        [Test]
        public void ServerSettings_DefaultValues()
        {
            // Arrange & Act
            var settings = new ServerSettings();

            // Assert
            Assert.That(settings.HttpPort, Is.EqualTo(51046));
            Assert.That(settings.HttpsPort, Is.EqualTo(51045));
            Assert.That(settings.HostAddress, Is.EqualTo("localhost"));
            Assert.That(settings.UseHttpsRedirection, Is.True);
            Assert.That(settings.Environment, Is.EqualTo("Development"));
        }

        [UnitTestAttribute(
            Identifier = "F8BF8EFE-7719-4545-BEC8-943D1C17AE66",
            Purpose = "ApiSettings has correct default values",
            PostCondition = "Default values match expected")]
        [Test]
        public void ApiSettings_DefaultValues()
        {
            // Arrange & Act
            var settings = new ApiSettings();

            // Assert
            Assert.That(settings.BasePath, Is.EqualTo(""));
            Assert.That(settings.EnableSwaggerInProduction, Is.False);
            Assert.That(settings.SwaggerRoutePrefix, Is.EqualTo(""));
            Assert.That(settings.MaxRequestBodySize, Is.EqualTo(31457280)); // 30MB
            Assert.That(settings.RequestTimeoutSeconds, Is.EqualTo(300)); // 5 minutes
        }

        [UnitTestAttribute(
            Identifier = "D10B2282-751E-42E9-A68A-96998714B34E",
            Purpose = "CorsSettings has correct default values",
            PostCondition = "Default values match expected")]
        [Test]
        public void CorsSettings_DefaultValues()
        {
            // Arrange & Act
            var settings = new CorsSettings();

            // Assert
            Assert.That(settings.EnableCORS, Is.True);
            Assert.That(settings.AllowedOrigins, Is.EqualTo("*"));
            Assert.That(settings.AllowedMethods, Is.EqualTo("*"));
            Assert.That(settings.AllowedHeaders, Is.EqualTo("*"));
            Assert.That(settings.AllowCredentials, Is.False);
        }

        [UnitTestAttribute(
            Identifier = "C2B12DCA-2775-4CAA-8D19-071CB9F7CA98",
            Purpose = "LoggingSettings has correct default values",
            PostCondition = "Default values match expected")]
        [Test]
        public void LoggingSettings_DefaultValues()
        {
            // Arrange & Act
            var settings = new LoggingSettings();

            // Assert
            Assert.That(settings.ServerLogLevel, Is.EqualTo("INFO"));
            Assert.That(settings.LogApiRequests, Is.False);
            Assert.That(settings.LogPerformanceMetrics, Is.False);
        }

        [UnitTestAttribute(
            Identifier = "0BFCE25D-371B-4AE7-B834-02CB52E9946E",
            Purpose = "SessionSettings has correct default values",
            PostCondition = "Default values match expected")]
        [Test]
        public void SessionSettings_DefaultValues()
        {
            // Arrange & Act
            var settings = new SessionSettings();

            // Assert
            Assert.That(settings.ProjectSessionTimeoutMinutes, Is.EqualTo(60));
            Assert.That(settings.MaxConcurrentProjects, Is.EqualTo(10));
            Assert.That(settings.SessionCleanupIntervalMinutes, Is.EqualTo(15));
        }

        [UnitTestAttribute(
            Identifier = "33395FD5-16D2-4573-8B24-89E5B41D71BF",
            Purpose = "PerformanceSettings has correct default values",
            PostCondition = "Default values match expected")]
        [Test]
        public void PerformanceSettings_DefaultValues()
        {
            // Arrange & Act
            var settings = new PerformanceSettings();

            // Assert
            Assert.That(settings.EnableResponseCaching, Is.True);
            Assert.That(settings.ProjectMetadataCacheDurationSeconds, Is.EqualTo(300)); // 5 minutes
            Assert.That(settings.TemplateFilesCacheDurationSeconds, Is.EqualTo(600)); // 10 minutes
            Assert.That(settings.MaxProjectMemoryMB, Is.EqualTo(500));
        }

        [UnitTestAttribute(
            Identifier = "BCFE9ED1-3A71-458A-A653-7C50891AC467",
            Purpose = "SecuritySettings has correct default values",
            PostCondition = "Default values match expected")]
        [Test]
        public void SecuritySettings_DefaultValues()
        {
            // Arrange & Act
            var settings = new SecuritySettings();

            // Assert
            Assert.That(settings.EnableApiKeyAuth, Is.False);
            Assert.That(settings.ApiKeyHeaderName, Is.EqualTo("X-API-Key"));
            Assert.That(settings.EnableRateLimiting, Is.False);
            Assert.That(settings.RateLimitRequestsPerMinute, Is.EqualTo(100));
        }

        [UnitTestAttribute(
            Identifier = "52920019-BAE0-4EA2-88EE-729D13CFB0CA",
            Purpose = "FileSystemSettings has correct default values",
            PostCondition = "Default values match expected")]
        [Test]
        public void FileSystemSettings_DefaultValues()
        {
            // Arrange & Act
            var settings = new FileSystemSettings();

            // Assert
            Assert.That(settings.EnableFileSystemValidation, Is.True);
            Assert.That(settings.AllowedTemplateExtensions, Is.EqualTo(".docx,.dotx,.html,.adoc"));
            Assert.That(settings.MaxUploadFileSizeBytes, Is.EqualTo(52428800)); // 50MB
            Assert.That(settings.TempFileCleanupIntervalMinutes, Is.EqualTo(30));
        }

        [UnitTestAttribute(
            Identifier = "EF9DD510-FDDF-420A-A097-A3AF36339770",
            Purpose = "SharePointSettings has correct default values",
            PostCondition = "Default values match expected")]
        [Test]
        public void SharePointSettings_DefaultValues()
        {
            // Arrange & Act
            var settings = new SharePointSettings();

            // Assert
            Assert.That(settings.ClientId, Is.EqualTo(string.Empty));
            Assert.That(settings.TenantId, Is.EqualTo(string.Empty));
            Assert.That(settings.OperationTimeoutSeconds, Is.EqualTo(120));
            Assert.That(settings.RetryCount, Is.EqualTo(3));
            Assert.That(settings.RetryDelayMilliseconds, Is.EqualTo(1000));
            Assert.That(settings.CacheAuthTokens, Is.True);
        }

        [UnitTestAttribute(
            Identifier = "3E6E68AF-3773-4AAB-B304-8156789E1B7B",
            Purpose = "HealthCheckSettings has correct default values",
            PostCondition = "Default values match expected")]
        [Test]
        public void HealthCheckSettings_DefaultValues()
        {
            // Arrange & Act
            var settings = new HealthCheckSettings();

            // Assert
            Assert.That(settings.EnableHealthChecks, Is.True);
            Assert.That(settings.HealthCheckPath, Is.EqualTo("/health"));
            Assert.That(settings.EnableDetailedHealthCheck, Is.False);
        }

        [UnitTestAttribute(
            Identifier = "17271BD3-4BC8-4768-9893-638BEF1F985B",
            Purpose = "MonitoringSettings has correct default values",
            PostCondition = "Default values match expected")]
        [Test]
        public void MonitoringSettings_DefaultValues()
        {
            // Arrange & Act
            var settings = new MonitoringSettings();

            // Assert
            Assert.That(settings.EnableApplicationInsights, Is.False);
            Assert.That(settings.LogDetailedExceptions, Is.True);
            Assert.That(settings.MonitorMemoryUsage, Is.False);
            Assert.That(settings.MemoryCheckIntervalMinutes, Is.EqualTo(5));
        }

        [UnitTestAttribute(
            Identifier = "FC2046B9-DEB6-4924-8F1D-E9862D69E232",
            Purpose = "ServerSettings properties can be modified",
            PostCondition = "Properties are set correctly")]
        [Test]
        public void ServerSettings_PropertiesCanBeModified()
        {
            // Arrange
            var settings = new ServerSettings();

            // Act
            settings.HttpPort = 8080;
            settings.HttpsPort = 8443;
            settings.HostAddress = "0.0.0.0";
            settings.UseHttpsRedirection = false;
            settings.Environment = "Production";

            // Assert
            Assert.That(settings.HttpPort, Is.EqualTo(8080));
            Assert.That(settings.HttpsPort, Is.EqualTo(8443));
            Assert.That(settings.HostAddress, Is.EqualTo("0.0.0.0"));
            Assert.That(settings.UseHttpsRedirection, Is.False);
            Assert.That(settings.Environment, Is.EqualTo("Production"));
        }

        [UnitTestAttribute(
            Identifier = "1A96CC70-69FC-41B5-8261-979510571170",
            Purpose = "SharePointSettings properties can be modified",
            PostCondition = "Properties are set correctly")]
        [Test]
        public void SharePointSettings_PropertiesCanBeModified()
        {
            // Arrange
            var settings = new SharePointSettings();

            // Act
            settings.ClientId = "client-123";
            settings.TenantId = "tenant-456";
            settings.OperationTimeoutSeconds = 300;
            settings.RetryCount = 5;
            settings.RetryDelayMilliseconds = 2000;
            settings.CacheAuthTokens = false;

            // Assert
            Assert.That(settings.ClientId, Is.EqualTo("client-123"));
            Assert.That(settings.TenantId, Is.EqualTo("tenant-456"));
            Assert.That(settings.OperationTimeoutSeconds, Is.EqualTo(300));
            Assert.That(settings.RetryCount, Is.EqualTo(5));
            Assert.That(settings.RetryDelayMilliseconds, Is.EqualTo(2000));
            Assert.That(settings.CacheAuthTokens, Is.False);
        }

        [UnitTestAttribute(
            Identifier = "3175284E-B01F-42CC-82D2-990D2828537A",
            Purpose = "ServerConfiguration nested settings can be replaced",
            PostCondition = "Nested settings are replaced correctly")]
        [Test]
        public void ServerConfiguration_NestedSettingsCanBeReplaced()
        {
            // Arrange
            var config = new ServerConfiguration();
            var newServerSettings = new ServerSettings { HttpPort = 9000 };

            // Act
            config.Server = newServerSettings;

            // Assert
            Assert.That(config.Server.HttpPort, Is.EqualTo(9000));
        }
    }
}
