using NUnit.Framework;
using NSubstitute;
using RoboClerk.Configuration;
using RoboClerk.AISystem;
using RoboClerk.ContentCreators;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using NLog.Targets;
using System.Linq;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.IO;
using NLog;
using Tomlyn;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("These tests test the Program class functionality")]
    internal class TestProgram
    {
        private IConfiguration mockConfiguration = null;
        private IPluginLoader mockPluginLoader = null;
        private IServiceCollection mockServiceCollection = null;
        private MockFileSystem mockFileSystem = null;
        private ILogger mockLogger = null;

        [SetUp]
        public void TestSetup()
        {
            mockConfiguration = Substitute.For<IConfiguration>();
            mockPluginLoader = Substitute.For<IPluginLoader>();
            mockServiceCollection = new ServiceCollection();
            mockFileSystem = new MockFileSystem();
            mockLogger = Substitute.For<ILogger>();
        }

        [TearDown]
        public void TestTearDown()
        {
            // Clean up NLog configuration after each test
            NLog.LogManager.Configuration = null;
        }

        #region AI Plugin Tests (existing)

        [UnitTestAttribute(
            Identifier = "A1B2C3D4-E5F6-7890-ABCD-EF123B567890",
            Purpose = "RegisterAIPlugin validation when AI plugin is configured and found",
            PostCondition = "AI plugin is registered in service collection")]
        [Test]
        public void RegisterAIPlugin_PluginConfiguredAndFound_PluginRegistered()
        {
            // Setup
            var mockAIPlugin = Substitute.For<IAISystemPlugin>();
            mockAIPlugin.Name.Returns("TestAIPlugin");
            mockConfiguration.AIPlugin.Returns("TestAIPlugin");
            mockConfiguration.PluginDirs.Returns(new List<string> { @"c:\temp" });
            mockPluginLoader.LoadByName<IAISystemPlugin>(Arg.Any<string>(), Arg.Is("TestAIPlugin"), Arg.Any<Action<IServiceCollection>>()).Returns(mockAIPlugin);

            // Act
            Assert.DoesNotThrow(() => Program.RegisterAIPlugin(mockServiceCollection, mockConfiguration, mockPluginLoader));

            // Assert: Verify plugin was loaded
            mockPluginLoader.Received().LoadByName<IAISystemPlugin>(
                Arg.Is(@"c:\temp"),
                Arg.Is("TestAIPlugin"),
                Arg.Any<Action<IServiceCollection>>());

            // Verify plugin was registered as singleton
            var serviceDescriptor = mockServiceCollection.FirstOrDefault(s => s.ServiceType == typeof(IAISystemPlugin));
            Assert.That(serviceDescriptor, Is.Not.Null, "AI plugin should be registered in service collection");
            Assert.That(serviceDescriptor.Lifetime, Is.EqualTo(ServiceLifetime.Singleton), "AI plugin should be registered as singleton");
        }

        [UnitTestAttribute(
            Identifier = "B2C3D4E5-F6G7-8901-BCDE-F2W856789012",
            Purpose = "RegisterAIPlugin validation when AI plugin is not configured",
            PostCondition = "No AI plugin is registered in service collection")]
        [Test]
        public void RegisterAIPlugin_PluginNotConfigured_NoPluginRegistered()
        {
            // Setup: Empty AI plugin configuration
            mockConfiguration.AIPlugin.Returns("");

            // Act
            Assert.DoesNotThrow(() => Program.RegisterAIPlugin(mockServiceCollection, mockConfiguration, mockPluginLoader));

            // Assert: Verify plugin loader was not called
            mockPluginLoader.DidNotReceive().LoadByName<IAISystemPlugin>(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Action<IServiceCollection>>());

            // Verify no AI plugin was registered
            var serviceDescriptor = mockServiceCollection.FirstOrDefault(s => s.ServiceType == typeof(IAISystemPlugin));
            Assert.That(serviceDescriptor, Is.Null, "No AI plugin should be registered when not configured");
        }

        [UnitTestAttribute(
            Identifier = "C3D4E5F6-G7H8-9012-CDEF-G23456789012",
            Purpose = "RegisterAIPlugin validation when AI plugin is null",
            PostCondition = "No AI plugin is registered in service collection")]
        [Test]
        public void RegisterAIPlugin_PluginNull_NoPluginRegistered()
        {
            // Setup: Null AI plugin configuration
            mockConfiguration.AIPlugin.Returns((string)null);

            // Act
            Assert.DoesNotThrow(() => Program.RegisterAIPlugin(mockServiceCollection, mockConfiguration, mockPluginLoader));

            // Assert: Verify plugin loader was not called
            mockPluginLoader.DidNotReceive().LoadByName<IAISystemPlugin>(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Action<IServiceCollection>>());

            // Verify no AI plugin was registered
            var serviceDescriptor = mockServiceCollection.FirstOrDefault(s => s.ServiceType == typeof(IAISystemPlugin));
            Assert.That(serviceDescriptor, Is.Null, "No AI plugin should be registered when configuration is null");
        }

        [UnitTestAttribute(
            Identifier = "D4E5F6G7-H8I9-0123-DEFG-H34567890123",
            Purpose = "LoadAIPlugin validation when plugin is found",
            PostCondition = "Plugin is loaded and initialized successfully")]
        [Test]
        public void LoadAIPlugin_PluginFound_PluginLoadedAndInitialized()
        {
            // Setup
            var mockAIPlugin = Substitute.For<IAISystemPlugin>();
            mockAIPlugin.Name.Returns("TestAIPlugin");
            mockConfiguration.AIPlugin.Returns("TestAIPlugin");
            mockConfiguration.PluginDirs.Returns(new List<string> { @"c:\temp" });
            mockPluginLoader.LoadByName<IAISystemPlugin>(Arg.Any<string>(), Arg.Is("TestAIPlugin"), Arg.Any<Action<IServiceCollection>>()).Returns(mockAIPlugin);

            // Act
            var result = Program.LoadAIPlugin(mockConfiguration, mockPluginLoader);

            // Assert
            Assert.That(result, Is.Not.Null, "LoadAIPlugin should return a plugin instance");
            Assert.That(result, Is.EqualTo(mockAIPlugin), "LoadAIPlugin should return the mock plugin");

            // Verify plugin was loaded from correct directory
            mockPluginLoader.Received().LoadByName<IAISystemPlugin>(
                Arg.Is(@"c:\temp"),
                Arg.Is("TestAIPlugin"),
                Arg.Any<Action<IServiceCollection>>());

            // Verify plugin was initialized
            mockAIPlugin.Received().InitializePlugin(mockConfiguration);
        }

        [UnitTestAttribute(
            Identifier = "E5F6G7H8-I9J0-1234-EFGH-I45678901234",
            Purpose = "LoadAIPlugin validation when plugin is not found in any directory",
            PostCondition = "Null is returned and warning is logged")]
        [Test]
        public void LoadAIPlugin_PluginNotFound_NullReturnedAndWarningLogged()
        {
            // Setup logging target to capture messages
            var logTarget = new MemoryTarget("TestTarget");
            logTarget.Layout = "${level}: ${message}";

            var nlogConfig = NLog.LogManager.Configuration ?? new NLog.Config.LoggingConfiguration();
            nlogConfig.AddTarget(logTarget);
            nlogConfig.AddRuleForAllLevels(logTarget);
            NLog.LogManager.Configuration = nlogConfig;

            try
            {
                // Setup: Plugin not found in any directory
                mockConfiguration.AIPlugin.Returns("NonExistentPlugin");
                mockConfiguration.PluginDirs.Returns(new List<string> { "dir1", "dir2" });
                mockPluginLoader.LoadByName<IAISystemPlugin>(Arg.Any<string>(), Arg.Is("NonExistentPlugin"), Arg.Any<Action<IServiceCollection>>()).Returns((IAISystemPlugin)null);

                // Act
                var result = Program.LoadAIPlugin(mockConfiguration, mockPluginLoader);

                // Assert
                Assert.That(result, Is.Null, "LoadAIPlugin should return null when plugin is not found");

                // Verify plugin loader was called for each directory
                mockPluginLoader.Received().LoadByName<IAISystemPlugin>(
                    Arg.Is("dir1"),
                    Arg.Is("NonExistentPlugin"),
                    Arg.Any<Action<IServiceCollection>>());

                mockPluginLoader.Received().LoadByName<IAISystemPlugin>(
                    Arg.Is("dir2"),
                    Arg.Is("NonExistentPlugin"),
                    Arg.Any<Action<IServiceCollection>>());

                // Verify warning was logged
                var logMessages = logTarget.Logs;
                var warningMessages = logMessages.Where(log => log.Contains("Warn:")).ToList();
                Assert.That(warningMessages.Any(msg => msg.Contains("Could not find AI plugin 'NonExistentPlugin'")), 
                    Is.True, "Expected warning message about plugin not found");
            }
            finally
            {
                // Clean up
                NLog.LogManager.Configuration = null;
            }
        }

        [UnitTestAttribute(
            Identifier = "F6G7H8I9-J0K1-2345-FGHI-J56789012345",
            Purpose = "LoadAIPlugin validation when plugin is found in second directory",
            PostCondition = "Plugin is loaded from second directory after first fails")]
        [Test]
        public void LoadAIPlugin_PluginFoundInSecondDirectory_PluginLoadedFromSecondDirectory()
        {
            // Setup
            var mockAIPlugin = Substitute.For<IAISystemPlugin>();
            mockAIPlugin.Name.Returns("TestAIPlugin");
            mockConfiguration.AIPlugin.Returns("TestAIPlugin");
            mockConfiguration.PluginDirs.Returns(new List<string> { "dir1", "dir2" });

            // First directory returns null, second directory returns plugin
            mockPluginLoader.LoadByName<IAISystemPlugin>(Arg.Is("dir1"), Arg.Is("TestAIPlugin"), Arg.Any<Action<IServiceCollection>>()).Returns((IAISystemPlugin)null);
            mockPluginLoader.LoadByName<IAISystemPlugin>(Arg.Is("dir2"), Arg.Is("TestAIPlugin"), Arg.Any<Action<IServiceCollection>>()).Returns(mockAIPlugin);

            // Act
            var result = Program.LoadAIPlugin(mockConfiguration, mockPluginLoader);

            // Assert
            Assert.That(result, Is.Not.Null, "LoadAIPlugin should return a plugin instance");
            Assert.That(result, Is.EqualTo(mockAIPlugin), "LoadAIPlugin should return the mock plugin");

            // Verify plugin loader was called for both directories
            mockPluginLoader.Received().LoadByName<IAISystemPlugin>(
                Arg.Is("dir1"),
                Arg.Is("TestAIPlugin"),
                Arg.Any<Action<IServiceCollection>>());

            mockPluginLoader.Received().LoadByName<IAISystemPlugin>(
                Arg.Is("dir2"),
                Arg.Is("TestAIPlugin"),
                Arg.Any<Action<IServiceCollection>>());

            // Verify plugin was initialized
            mockAIPlugin.Received().InitializePlugin(mockConfiguration);
        }

        [UnitTestAttribute(
            Identifier = "G7H8I9J0-K1L2-3456-GHIJ-K67890123456",
            Purpose = "LoadAIPlugin validation when plugin loader throws exception",
            PostCondition = "Exception is caught, logged, and next directory is tried")]
        [Test]
        public void LoadAIPlugin_PluginLoaderThrowsException_ExceptionHandledAndNextDirectoryTried()
        {
            // Setup logging target to capture messages
            var logTarget = new MemoryTarget("TestTarget");
            logTarget.Layout = "${level}: ${message}";

            var nlogConfig = NLog.LogManager.Configuration ?? new NLog.Config.LoggingConfiguration();
            nlogConfig.AddTarget(logTarget);
            nlogConfig.AddRuleForAllLevels(logTarget);
            NLog.LogManager.Configuration = nlogConfig;

            try
            {
                // Setup
                var mockAIPlugin = Substitute.For<IAISystemPlugin>();
                mockAIPlugin.Name.Returns("TestAIPlugin");
                mockConfiguration.AIPlugin.Returns("TestAIPlugin");
                mockConfiguration.PluginDirs.Returns(new List<string> { "dir1", "dir2" });

                // First directory throws exception, second directory returns plugin
                mockPluginLoader.When(x => x.LoadByName<IAISystemPlugin>(Arg.Is("dir1"), Arg.Is("TestAIPlugin"), Arg.Any<Action<IServiceCollection>>()))
                    .Do(x => { throw new Exception("Plugin loader error in dir1"); });
                mockPluginLoader.LoadByName<IAISystemPlugin>(Arg.Is("dir2"), Arg.Is("TestAIPlugin"), Arg.Any<Action<IServiceCollection>>()).Returns(mockAIPlugin);

                // Act
                var result = Program.LoadAIPlugin(mockConfiguration, mockPluginLoader);

                // Assert
                Assert.That(result, Is.Not.Null, "LoadAIPlugin should return a plugin instance despite exception in first directory");
                Assert.That(result, Is.EqualTo(mockAIPlugin), "LoadAIPlugin should return the mock plugin from second directory");

                // Verify warning was logged for the exception
                var logMessages = logTarget.Logs;
                var warningMessages = logMessages.Where(log => log.Contains("Warn:")).ToList();
                Assert.That(warningMessages.Any(msg => msg.Contains("Error loading AI plugin from directory dir1")), 
                    Is.True, "Expected warning message about plugin loading error");

                // Verify plugin was initialized
                mockAIPlugin.Received().InitializePlugin(mockConfiguration);
            }
            finally
            {
                // Clean up
                NLog.LogManager.Configuration = null;
            }
        }

        [UnitTestAttribute(
            Identifier = "H8I9J0K1-L2M3-4567-HIJK-L78901234567",
            Purpose = "LoadAIPlugin validation when plugin initialization throws exception",
            PostCondition = "null is returned due to initialization failure")]
        [Test]
        public void LoadAIPlugin_PluginInitializationThrowsException_PluginReturnedDespiteInitFailure()
        {
            // Setup
            var mockAIPlugin = Substitute.For<IAISystemPlugin>();
            mockAIPlugin.Name.Returns("TestAIPlugin");
            mockAIPlugin.When(x => x.InitializePlugin(Arg.Any<IConfiguration>())).Do(x => { throw new Exception("Plugin initialization failed"); });
            mockConfiguration.AIPlugin.Returns("TestAIPlugin");
            mockConfiguration.PluginDirs.Returns(new List<string> { @"c:\temp" });
            mockPluginLoader.LoadByName<IAISystemPlugin>(Arg.Any<string>(), Arg.Is("TestAIPlugin"), Arg.Any<Action<IServiceCollection>>()).Returns(mockAIPlugin);

            // Act & Assert: Should not throw exception
            var result = Program.LoadAIPlugin(mockConfiguration, mockPluginLoader);

            // Assert
            Assert.That(result, Is.Null, "LoadAIPlugin should return null when initialization fails");

            // Verify plugin was loaded
            mockPluginLoader.Received().LoadByName<IAISystemPlugin>(
                Arg.Is(@"c:\temp"),
                Arg.Is("TestAIPlugin"),
                Arg.Any<Action<IServiceCollection>>());

            // Verify initialization was attempted
            mockAIPlugin.Received().InitializePlugin(mockConfiguration);
        }

        #endregion

        #region ConfigureLogging Tests

        [UnitTestAttribute(
            Identifier = "I9J0K1L2-M3N4-5678-IJKL-M89012345678",
            Purpose = "ConfigureLogging validation with DEBUG log level",
            PostCondition = "NLog configuration is set up with DEBUG level")]
        [Test]
        public void ConfigureLogging_DebugLevel_DebugConfigurationSet()
        {
            // Setup
            var configContent = @"
LogLevel = ""DEBUG""
OutputDirectory = ""C:\\temp\\output""
";
            var configFile = @"C:\\temp\\config.toml";
            mockFileSystem.AddFile(configFile, configContent);

            // Use real file system for this test since ConfigureLogging uses File.ReadAllText directly
            var tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, configContent);

                // Act
                Assert.DoesNotThrow(() => Program.ConfigureLogging(tempFile));

                // Assert: Verify NLog configuration was set
                Assert.That(NLog.LogManager.Configuration, Is.Not.Null, "NLog configuration should be set");
                
                var rules = NLog.LogManager.Configuration.LoggingRules;
                Assert.That(rules.Count, Is.GreaterThan(0), "Should have logging rules configured");
                
                var debugRule = rules.FirstOrDefault(r => r.Levels.Contains(LogLevel.Debug));
                Assert.That(debugRule, Is.Not.Null, "Should have DEBUG level rule configured");
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [UnitTestAttribute(
            Identifier = "J0K1L2M3-N4O5-6789-JKLM-N90123456789",
            Purpose = "ConfigureLogging validation with WARN log level",
            PostCondition = "NLog configuration is set up with WARN level")]
        [Test]
        public void ConfigureLogging_WarnLevel_WarnConfigurationSet()
        {
            // Setup
            var configContent = @"
LogLevel = ""WARN""
OutputDirectory = ""C:\\temp\\output""
";
            var tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, configContent);

                // Act
                Assert.DoesNotThrow(() => Program.ConfigureLogging(tempFile));

                // Assert: Verify NLog configuration was set
                Assert.That(NLog.LogManager.Configuration, Is.Not.Null, "NLog configuration should be set");
                
                var rules = NLog.LogManager.Configuration.LoggingRules;
                var warnRule = rules.FirstOrDefault(r => r.Levels.Contains(LogLevel.Warn) && !r.Levels.Contains(LogLevel.Debug));
                Assert.That(warnRule, Is.Not.Null, "Should have WARN level rule configured without DEBUG");
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [UnitTestAttribute(
            Identifier = "K1L2M3N4-O5P6-7890-KLMN-O01234567890",
            Purpose = "ConfigureLogging validation with default (INFO) log level",
            PostCondition = "NLog configuration is set up with INFO level")]
        [Test]
        public void ConfigureLogging_DefaultLevel_InfoConfigurationSet()
        {
            // Setup
            var configContent = @"
LogLevel = ""INFO""
OutputDirectory = ""C:\\temp\\output""
";
            var tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, configContent);

                // Act
                Assert.DoesNotThrow(() => Program.ConfigureLogging(tempFile));

                // Assert: Verify NLog configuration was set
                Assert.That(NLog.LogManager.Configuration, Is.Not.Null, "NLog configuration should be set");
                
                var rules = NLog.LogManager.Configuration.LoggingRules;
                var infoRule = rules.FirstOrDefault(r => r.Levels.Contains(LogLevel.Info) && !r.Levels.Contains(LogLevel.Debug));
                Assert.That(infoRule, Is.Not.Null, "Should have INFO level rule configured without DEBUG");
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [UnitTestAttribute(
            Identifier = "L2M3N4O5-P6Q7-8901-LMNO-P12345678901",
            Purpose = "ConfigureLogging validation with invalid file",
            PostCondition = "Exception is thrown for invalid configuration file")]
        [Test]
        public void ConfigureLogging_InvalidFile_ExceptionThrown()
        {
            // Setup: Non-existent file
            var invalidFile = @"C:\\config.toml";

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => Program.ConfigureLogging(invalidFile));
        }

        #endregion

        #region GetConfigOptions Tests

        [UnitTestAttribute(
            Identifier = "M3N4O5P6-Q7R8-9012-MNOP-Q23456789012",
            Purpose = "GetConfigOptions validation with valid command line options",
            PostCondition = "Dictionary with parsed options is returned")]
        [Test]
        public void GetConfigOptions_ValidOptions_DictionaryReturned()
        {
            // Setup
            var commandlineOptions = new List<string> { "Key1=Value1", "Key2=Value2", "Key3=Value3" };

            // Act
            var result = Program.GetConfigOptions(commandlineOptions, mockLogger);

            // Assert
            Assert.That(result, Is.Not.Null, "Result should not be null");
            Assert.That(result.Count, Is.EqualTo(3), "Should have 3 parsed options");
            Assert.That(result["Key1"], Is.EqualTo("Value1"), "Key1 should have correct value");
            Assert.That(result["Key2"], Is.EqualTo("Value2"), "Key2 should have correct value");
            Assert.That(result["Key3"], Is.EqualTo("Value3"), "Key3 should have correct value");
        }

        [UnitTestAttribute(
            Identifier = "N4O5P6Q7-R8S9-0123-NOPQ-R34567890123",
            Purpose = "GetConfigOptions validation with comma separation",
            PostCondition = "Comma separators are ignored")]
        [Test]
        public void GetConfigOptions_WithCommaSeparators_CommasIgnored()
        {
            // Setup
            var commandlineOptions = new List<string> { "Key1=Value1", ",", "Key2=Value2", ",", "Key3=Value3" };

            // Act
            var result = Program.GetConfigOptions(commandlineOptions, mockLogger);

            // Assert
            Assert.That(result, Is.Not.Null, "Result should not be null");
            Assert.That(result.Count, Is.EqualTo(3), "Should have 3 parsed options (commas ignored)");
            Assert.That(result["Key1"], Is.EqualTo("Value1"), "Key1 should have correct value");
            Assert.That(result["Key2"], Is.EqualTo("Value2"), "Key2 should have correct value");
            Assert.That(result["Key3"], Is.EqualTo("Value3"), "Key3 should have correct value");
        }

        [UnitTestAttribute(
            Identifier = "O5P6Q7R8-S9T0-1234-OPQR-S45678901234",
            Purpose = "GetConfigOptions validation with invalid format",
            PostCondition = "Exception is thrown for malformed options")]
        [Test]
        public void GetConfigOptions_InvalidFormat_ExceptionThrown()
        {
            // Setup
            var commandlineOptions = new List<string> { "Key1=Value1", "InvalidOptionWithoutEquals", "Key3=Value3" };

            // Act & Assert
            var ex = Assert.Throws<Exception>(() => Program.GetConfigOptions(commandlineOptions, mockLogger));
            Assert.That(ex.Message, Does.Contain("Error parsing commandline options"), "Should contain appropriate error message");

            // Verify error was logged
            mockLogger.Received().Error(Arg.Is<string>(s => s.Contains("InvalidOptionWithoutEquals")));
        }

        [UnitTestAttribute(
            Identifier = "P6Q7R8S9-T0U1-2345-PQRS-T56789012345",
            Purpose = "GetConfigOptions validation with empty input",
            PostCondition = "Empty dictionary is returned")]
        [Test]
        public void GetConfigOptions_EmptyInput_EmptyDictionaryReturned()
        {
            // Setup
            var commandlineOptions = new List<string>();

            // Act
            var result = Program.GetConfigOptions(commandlineOptions, mockLogger);

            // Assert
            Assert.That(result, Is.Not.Null, "Result should not be null");
            Assert.That(result.Count, Is.EqualTo(0), "Should have empty dictionary");
        }

        #endregion

        #region CleanOutputDirectory Tests

        [UnitTestAttribute(
            Identifier = "Q7R8S9T0-U1V2-3456-QRST-U67890123456",
            Purpose = "CleanOutputDirectory validation with mixed files",
            PostCondition = "Only specified files are preserved")]
        [Test]
        public void CleanOutputDirectory_WithMixedFiles_CorrectFilesDeleted()
        {
            // Setup
            var outputDir = @"C:\temp\output";
            var files = new[]
            {
                @"C:\temp\output\document1.html",
                @"C:\temp\output\document2.adoc",
                @"C:\temp\output\RoboClerkLog.txt",
                @"C:\temp\output\.gitignore",
                @"C:\temp\output\temp.tmp"
            };

            // Create a real temporary directory for this test
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                // Create test files
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    var testFile = Path.Combine(tempDir, fileName);
                    File.WriteAllText(testFile, "test content");
                }

                // Act
                Assert.DoesNotThrow(() => Program.CleanOutputDirectory(tempDir, mockLogger));

                // Assert
                var remainingFiles = Directory.GetFiles(tempDir);
                Assert.That(remainingFiles.Length, Is.EqualTo(2), "Should have 2 files remaining");
                
                var fileNames = remainingFiles.Select(Path.GetFileName).ToList();
                Assert.That(fileNames, Does.Contain("RoboClerkLog.txt"), "RoboClerkLog.txt should be preserved");
                Assert.That(fileNames, Does.Contain(".gitignore"), ".gitignore should be preserved");
                Assert.That(fileNames, Does.Not.Contain("document1.html"), "document1.html should be deleted");
                Assert.That(fileNames, Does.Not.Contain("document2.adoc"), "document2.adoc should be deleted");
                Assert.That(fileNames, Does.Not.Contain("temp.tmp"), "temp.tmp should be deleted");

                // Verify info log was called
                mockLogger.Received().Info("Cleaning output directory.");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [UnitTestAttribute(
            Identifier = "R8S9T0U1-V2W3-4567-RSTU-V78901234567",
            Purpose = "CleanOutputDirectory validation with non-existent directory",
            PostCondition = "Exception is thrown for non-existent directory")]
        [Test]
        public void CleanOutputDirectory_NonExistentDirectory_ExceptionThrown()
        {
            // Setup
            var nonExistentDir = @"C:\nonexistent\directory";

            // Act & Assert
            Assert.Throws<DirectoryNotFoundException>(() => Program.CleanOutputDirectory(nonExistentDir, mockLogger));
        }

        #endregion

        #region RegisterContentCreators Tests

        [UnitTestAttribute(
            Identifier = "S9T0U1V2-W3X4-5678-STUV-W89012345678",
            Purpose = "RegisterContentCreators validation with service collection",
            PostCondition = "Content creators are registered in service collection")]
        [Test]
        public void RegisterContentCreators_WithServiceCollection_ContentCreatorsRegistered()
        {
            // Setup
            var serviceCollection = new ServiceCollection();

            // Act
            Assert.DoesNotThrow(() => Program.RegisterContentCreators(serviceCollection));

            // Assert
            var registeredTypes = serviceCollection.Select(s => s.ServiceType).ToList();
            
            // Verify that some expected content creators are registered
            Assert.That(registeredTypes, Does.Contain(typeof(AIContentCreator)), "AIContentCreator should be registered");
            Assert.That(registeredTypes, Does.Contain(typeof(DocumentationRequirement)), "DocumentationRequirement should be registered");
            Assert.That(registeredTypes, Does.Contain(typeof(SoftwareRequirement)), "SoftwareRequirement should be registered");
            Assert.That(registeredTypes, Does.Contain(typeof(SystemRequirement)), "SystemRequirement should be registered");

            // Verify all are registered as transient
            var contentCreatorServices = serviceCollection.Where(s => typeof(IContentCreator).IsAssignableFrom(s.ServiceType)).ToList();
            Assert.That(contentCreatorServices.All(s => s.Lifetime == ServiceLifetime.Transient), 
                Is.True, "All content creators should be registered as transient");

            // Verify we found a reasonable number of content creators
            Assert.That(contentCreatorServices.Count, Is.GreaterThan(5), "Should register multiple content creators");
        }

        [UnitTestAttribute(
            Identifier = "T0U1V2W3-X4Y5-6789-TUVW-X90123456789",
            Purpose = "RegisterContentCreators validation with null service collection",
            PostCondition = "Exception is thrown for null service collection")]
        [Test]
        public void RegisterContentCreators_NullServiceCollection_ExceptionThrown()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => Program.RegisterContentCreators(null));
        }

        #endregion
    }
} 