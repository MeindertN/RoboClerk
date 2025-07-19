using NUnit.Framework;
using NSubstitute;
using RoboClerk.Configuration;
using RoboClerk.AISystem;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using NLog.Targets;
using System.Linq;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("These tests test the Program class AI plugin loading functionality")]
    internal class TestProgram
    {
        private IConfiguration mockConfiguration = null;
        private IPluginLoader mockPluginLoader = null;
        private IServiceCollection mockServiceCollection = null;

        [SetUp]
        public void TestSetup()
        {
            mockConfiguration = Substitute.For<IConfiguration>();
            mockPluginLoader = Substitute.For<IPluginLoader>();
            mockServiceCollection = new ServiceCollection();
        }

        [UnitTestAttribute(
            Identifier = "A1B2C3D4-E5F6-7890-ABCD-EF1234567890",
            Purpose = "RegisterAIPlugin validation when AI plugin is configured and found",
            PostCondition = "AI plugin is registered in service collection")]
        [Test]
        public void RegisterAIPlugin_PluginConfiguredAndFound_VERIFIES_PluginRegistered()
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
            Identifier = "B2C3D4E5-F6G7-8901-BCDE-F23456789012",
            Purpose = "RegisterAIPlugin validation when AI plugin is not configured",
            PostCondition = "No AI plugin is registered in service collection")]
        [Test]
        public void RegisterAIPlugin_PluginNotConfigured_VERIFIES_NoPluginRegistered()
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
        public void RegisterAIPlugin_PluginNull_VERIFIES_NoPluginRegistered()
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
        public void LoadAIPlugin_PluginFound_VERIFIES_PluginLoadedAndInitialized()
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
        public void LoadAIPlugin_PluginNotFound_VERIFIES_NullReturnedAndWarningLogged()
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
        public void LoadAIPlugin_PluginFoundInSecondDirectory_VERIFIES_PluginLoadedFromSecondDirectory()
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
        public void LoadAIPlugin_PluginLoaderThrowsException_VERIFIES_ExceptionHandledAndNextDirectoryTried()
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
        public void LoadAIPlugin_PluginInitializationThrowsException_VERIFIES_PluginReturnedDespiteInitFailure()
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
            Assert.That(result, Is.Null, "LoadAIPlugin should return null if initialization fails");

            // Verify plugin was loaded
            mockPluginLoader.Received().LoadByName<IAISystemPlugin>(
                Arg.Is(@"c:\temp"),
                Arg.Is("TestAIPlugin"),
                Arg.Any<Action<IServiceCollection>>());

            // Verify initialization was attempted
            mockAIPlugin.Received().InitializePlugin(mockConfiguration);
        }
    }
} 