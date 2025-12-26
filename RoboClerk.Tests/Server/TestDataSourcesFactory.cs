using NUnit.Framework;
using NSubstitute;
using RoboClerk.Server.Services;
using RoboClerk.Core;
using RoboClerk.Core.Configuration;
using RoboClerk.Core.FileProviders;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace RoboClerk.Tests.Server
{
    [TestFixture]
    [Description("Tests for the DataSourcesFactory that creates data source instances")]
    public class TestDataSourcesFactory
    {
        private IServiceProvider mockServiceProvider;
        private IPluginLoader mockPluginLoader;
        private IFileProviderPlugin mockFileProvider;
        private IConfiguration mockConfiguration;
        private MockFileSystem mockFileSystem;

        [SetUp]
        public void Setup()
        {
            mockPluginLoader = Substitute.For<IPluginLoader>();
            mockFileProvider = Substitute.For<IFileProviderPlugin>();
            mockConfiguration = Substitute.For<IConfiguration>();
            
            // Setup default configuration behavior
            var checkpointConfig = new CheckpointConfig();
            mockConfiguration.CheckpointConfig.Returns(checkpointConfig);
            mockConfiguration.PluginDirs.Returns(new List<string> { "plugins" });
            mockConfiguration.DataSourcePlugins.Returns(new List<string>());
            
            // Create mock file system
            mockFileSystem = new MockFileSystem();
            
            // Create service provider with required services
            var services = new ServiceCollection();
            services.AddSingleton(mockPluginLoader);
            services.AddSingleton(mockFileProvider);
            services.AddSingleton(mockConfiguration);
            mockServiceProvider = services.BuildServiceProvider();
        }

        [UnitTestAttribute(
            Identifier = "866CB64A-2BD7-487D-BB3A-100BD6B8F706",
            Purpose = "DataSourcesFactory is created successfully",
            PostCondition = "Factory is initialized")]
        [Test]
        public void CreateFactory_Success()
        {
            // Arrange & Act
            var factory = new DataSourcesFactory(mockServiceProvider);

            // Assert
            Assert.That(factory, Is.Not.Null);
        }

        [UnitTestAttribute(
            Identifier = "87C99E85-0A32-45A1-85A0-F132F768C993",
            Purpose = "CreateDataSources returns PluginDataSources when no checkpoint file",
            PostCondition = "PluginDataSources instance is returned")]
        [Test]
        public void CreateDataSources_NoCheckpointFile_ReturnsPluginDataSources()
        {
            // Arrange
            var checkpointConfig = new CheckpointConfig { CheckpointFile = string.Empty };
            mockConfiguration.CheckpointConfig.Returns(checkpointConfig);
            
            var factory = new DataSourcesFactory(mockServiceProvider);

            // Act
            var dataSources = factory.CreateDataSources(mockConfiguration);

            // Assert
            Assert.That(dataSources, Is.Not.Null);
            Assert.That(dataSources, Is.InstanceOf<PluginDataSources>());
        }

        [UnitTestAttribute(
            Identifier = "CB6A8A40-C081-4DC3-8CD9-E7E486A85F91",
            Purpose = "CreateDataSources returns CheckpointDataSources when checkpoint file specified",
            PostCondition = "CheckpointDataSources instance is returned")]
        [Test]
        public void CreateDataSources_WithCheckpointFile_ReturnsCheckpointDataSources()
        {
            // Arrange
            // Create a mock file system with the checkpoint file
            var checkpointFilePath = TestingHelpers.ConvertFilePath(@"C:\temp\checkpoint.json");
            var checkpointContent = @"{
                ""SystemRequirements"": [],
                ""SoftwareRequirements"": [],
                ""SoftwareSystemTests"": [],
                ""UnitTests"": [],
                ""Anomalies"": [],
                ""SOUPs"": [],
                ""Risks"": [],
                ""DocumentationRequirements"": [],
                ""DocContents"": [],
                ""ExternalDependencies"": []
            }";
            
            var fileSystemWithFile = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { checkpointFilePath, new MockFileData(checkpointContent) }
            });
            
            var localFileProvider = new LocalFileSystemPlugin(fileSystemWithFile);
            
            var checkpointConfig = new CheckpointConfig { CheckpointFile = checkpointFilePath };
            mockConfiguration.CheckpointConfig.Returns(checkpointConfig);
            
            // Create new service provider with the local file provider
            var services = new ServiceCollection();
            services.AddSingleton(mockPluginLoader);
            services.AddSingleton<IFileProviderPlugin>(localFileProvider);
            services.AddSingleton(mockConfiguration);
            var serviceProvider = services.BuildServiceProvider();
            
            var factory = new DataSourcesFactory(serviceProvider);

            // Act
            var dataSources = factory.CreateDataSources(mockConfiguration);

            // Assert
            Assert.That(dataSources, Is.Not.Null);
            Assert.That(dataSources, Is.InstanceOf<CheckpointDataSources>());
        }

        [UnitTestAttribute(
            Identifier = "8A9B3BD6-E136-401D-9797-400A9E1E1259",
            Purpose = "CreateDataSources uses plugin loader from service provider",
            PostCondition = "Plugin loader is retrieved from service provider")]
        [Test]
        public void CreateDataSources_UsesPluginLoader()
        {
            // Arrange
            var checkpointConfig = new CheckpointConfig { CheckpointFile = string.Empty };
            mockConfiguration.CheckpointConfig.Returns(checkpointConfig);
            
            var factory = new DataSourcesFactory(mockServiceProvider);

            // Act
            var dataSources = factory.CreateDataSources(mockConfiguration);

            // Assert - verify service was resolved (indirectly by not throwing)
            Assert.That(dataSources, Is.Not.Null);
        }

        [UnitTestAttribute(
            Identifier = "287BBB47-2A5B-42A9-B2EA-1313C4061132",
            Purpose = "CreateDataSources uses file provider from service provider",
            PostCondition = "File provider is retrieved from service provider")]
        [Test]
        public void CreateDataSources_UsesFileProvider()
        {
            // Arrange
            var checkpointConfig = new CheckpointConfig { CheckpointFile = string.Empty };
            mockConfiguration.CheckpointConfig.Returns(checkpointConfig);
            
            var factory = new DataSourcesFactory(mockServiceProvider);

            // Act
            var dataSources = factory.CreateDataSources(mockConfiguration);

            // Assert - verify service was resolved (indirectly by not throwing)
            Assert.That(dataSources, Is.Not.Null);
        }

        [UnitTestAttribute(
            Identifier = "5F75EBA1-107D-46C3-8B26-512F59F6E3C0",
            Purpose = "CreateDataSources throws when plugin loader is missing",
            PostCondition = "Exception is thrown")]
        [Test]
        public void CreateDataSources_MissingPluginLoader_Throws()
        {
            // Arrange - Create service provider without plugin loader
            var services = new ServiceCollection();
            services.AddSingleton(mockFileProvider);
            var incompleteServiceProvider = services.BuildServiceProvider();
            
            var factory = new DataSourcesFactory(incompleteServiceProvider);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => factory.CreateDataSources(mockConfiguration));
        }

        [UnitTestAttribute(
            Identifier = "3551342A-3EC3-41D8-909C-AB9F4375F233",
            Purpose = "CreateDataSources throws when file provider is missing",
            PostCondition = "Exception is thrown")]
        [Test]
        public void CreateDataSources_MissingFileProvider_Throws()
        {
            // Arrange - Create service provider without file provider
            var services = new ServiceCollection();
            services.AddSingleton(mockPluginLoader);
            var incompleteServiceProvider = services.BuildServiceProvider();
            
            var factory = new DataSourcesFactory(incompleteServiceProvider);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => factory.CreateDataSources(mockConfiguration));
        }
    }
}
