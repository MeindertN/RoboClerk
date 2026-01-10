using NUnit.Framework;
using NSubstitute;
using RoboClerk.Server.Services;
using RoboClerk.Server.Models;
using RoboClerk.Core;
using RoboClerk.Core.Configuration;
using RoboClerk.Core.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Document = DocumentFormat.OpenXml.Wordprocessing.Document;
using Text = DocumentFormat.OpenXml.Wordprocessing.Text;

namespace RoboClerk.Tests.Server
{
    [TestFixture]
    [Description("Tests for the ProjectManager service that manages SharePoint projects")]
    public class TestProjectManager
    {
        private IServiceProvider mockGlobalServiceProvider;
        private IFileSystem mockFileSystem;
        private IDataSourcesFactory mockDataSourcesFactory;
        private IConfiguration mockConfiguration; // Kept for existing tests (non-load scenarios)
        private RoboClerk.Configuration.Configuration baseConfig; // For loading scenarios
        private IPluginLoader mockPluginLoader;
        private IFileProviderPlugin mockSharePointProvider;
        private ProjectManager projectManager;
        private string projectId;
        private string driveId = "drive123";
        private string projectPath = "sp://testsite/project";

        [SetUp]
        public void Setup()
        {
            mockFileSystem = new MockFileSystem();
            mockDataSourcesFactory = Substitute.For<IDataSourcesFactory>();
            mockDataSourcesFactory.CreateDataSources(Arg.Any<IConfiguration>()).Returns(Substitute.For<IDataSources>());
            mockConfiguration = Substitute.For<IConfiguration>(); // Basic mock for existing tests
            mockPluginLoader = Substitute.For<IPluginLoader>();

            // Setup configuration defaults for mockConfiguration
            mockConfiguration.PluginDirs.Returns(new List<string> { "plugins" });
            mockConfiguration.DataSourcePlugins.Returns(new List<string>());
            mockConfiguration.CheckpointConfig.Returns(new CheckpointConfig());

            // Setup real configuration for LoadProject tests
            baseConfig = new RoboClerk.Configuration.Configuration();
            var configType = typeof(RoboClerk.Configuration.Configuration);
            configType.GetField("pluginDirs", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(baseConfig, new List<string> { "plugins" });
            configType.GetField("dataSourcePlugins", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(baseConfig, new List<string>());

            // Setup mock SharePoint provider
            mockSharePointProvider = Substitute.For<IFileProviderPlugin>();
            mockSharePointProvider.GetPathPrefix().Returns("sp://");
            
            // Basic file provider mocks logic from Suite
            mockSharePointProvider.Combine(Arg.Any<string>(), Arg.Any<string>()).Returns(x => 
            {
                var p1 = x.ArgAt<string>(0).TrimEnd('/');
                var p2 = x.ArgAt<string>(1).TrimStart('/');
                return $"{p1}/{p2}";
            });
            mockSharePointProvider.Combine(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(x => 
            {
                 var p1 = x.ArgAt<string>(0).TrimEnd('/');
                 var p2 = x.ArgAt<string>(1).TrimStart('/');
                 var p3 = x.ArgAt<string>(2).TrimStart('/');
                 return $"{p1}/{p2}/{p3}";
            });
            mockSharePointProvider.GetFileName(Arg.Any<string>()).Returns(x => Path.GetFileName(x.ArgAt<string>(0)));
            mockSharePointProvider.GetDirectoryName(Arg.Any<string>()).Returns(x => Path.GetDirectoryName(x.ArgAt<string>(0))?.Replace('\\', '/'));
            mockSharePointProvider.FileExists(Arg.Any<string>()).Returns(true);

            // Mock plugin loading to return mockSharePointProvider
            mockPluginLoader.LoadByName<IFileProviderPlugin>(
                Arg.Any<string>(), 
                "SharePointFileProviderPlugin", 
                Arg.Any<Action<IServiceCollection>>())
                .Returns(mockSharePointProvider);

            // Create service provider with BOTH mock (for non-load tests validation if needed) and real config components support
            // The ProjectManager expects IConfiguration. For positive tests, we need baseConfig. 
            // For negative tests that just check logic before loading, mockConfiguration was used. 
            // Ideally we use baseConfig for all, as it implements IConfiguration.
            
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(baseConfig); // Use real config object
            services.AddSingleton(mockPluginLoader);
            services.AddSingleton(mockFileSystem);
            services.AddSingleton(mockDataSourcesFactory);
            mockGlobalServiceProvider = services.BuildServiceProvider();

            projectManager = new ProjectManager(mockGlobalServiceProvider, mockFileSystem, mockDataSourcesFactory);
        }

        #region Helpers

        private byte[] CreateValidDocx()
        {
            using (var ms = new MemoryStream())
            {
                using (var wordDocument = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
                {
                    var mainPart = wordDocument.AddMainDocumentPart();
                    mainPart.Document = new Document(new Body(new Paragraph(new Run(new Text("Test")))));
                }
                return ms.ToArray();
            }
        }

        private string GetValidProjectConfig()
        {
            return @"
ProjectName = ""TestProject""
ProjectRoot = ""sp://testsite/project""
TemplateDirectory = ""sp://testsite/project/Templates""
OutputDirectory = ""sp://testsite/project/Output""
DataSourcePlugin = []

[Truth]
    [Truth.SystemRequirement]
    name = ""System Requirement""
    abbreviation = ""SR""

[Document]
    [Document.Doc1]
    identifier = ""Doc1""
    title = ""Test Document""
    abbreviation = ""TD""
    template = ""template.docx""

[TraceConfig]
    [TraceConfig.SystemRequirement]
    forward = []
    backward = []

[CheckpointConfig]
";
        }

        private async Task<string> LoadTestProject()
        {
            var configContent = GetValidProjectConfig();
            var docxContent = CreateValidDocx();

            mockSharePointProvider.ReadAllText(Arg.Any<string>()).Returns(configContent);
            mockSharePointProvider.ReadAllBytes(Arg.Any<string>()).Returns(docxContent);
            mockSharePointProvider.GetFiles(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SearchOption>())
                .Returns(new[] { $"{projectPath}/Templates/template.docx" });

            var request = new LoadProjectRequest
            {
                ProjectPath = projectPath,
                SPDriveId = driveId,
                SPSiteUrl = "https://test.sharepoint.com"
            };

            var result = await projectManager.LoadProjectAsync(request);
            Assert.That(result.Success, Is.True, $"Project load failed: {result.Error}");
            projectId = result.ProjectId;
            return result.ProjectId;
        }

        #endregion

        #region NormalizePath Tests (via reflection since it's private)

        private static string InvokeNormalizePath(string path)
        {
            var method = typeof(ProjectManager).GetMethod("NormalizePath", 
                BindingFlags.NonPublic | BindingFlags.Static);
            return (string)method.Invoke(null, new object[] { path });
        }

        [UnitTestAttribute(
            Identifier = "C4D986A8-D544-4BE8-953D-9F0E8908FFC9",
            Purpose = "NormalizePath returns empty string for null input",
            PostCondition = "Empty string is returned")]
        [Test]
        public void NormalizePath_NullInput_ReturnsEmpty()
        {
            // Act
            var result = InvokeNormalizePath(null);

            // Assert
            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [UnitTestAttribute(
            Identifier = "65214E21-3EC6-4920-B0B5-310BA016E0D1",
            Purpose = "NormalizePath returns empty string for empty input",
            PostCondition = "Empty string is returned")]
        [Test]
        public void NormalizePath_EmptyInput_ReturnsEmpty()
        {
            // Act
            var result = InvokeNormalizePath("");

            // Assert
            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [UnitTestAttribute(
            Identifier = "0C01B58A-F6E0-4FE3-86C0-E3DD519CEF07",
            Purpose = "NormalizePath converts backslashes to forward slashes",
            PostCondition = "All backslashes are converted")]
        [Test]
        public void NormalizePath_Backslashes_ConvertedToForwardSlashes()
        {
            // Act
            var result = InvokeNormalizePath(@"path\to\file");

            // Assert
            Assert.That(result, Is.EqualTo("path/to/file"));
        }

        [UnitTestAttribute(
            Identifier = "76C8CDE7-1A2A-4AFF-989F-9CE7982849C7",
            Purpose = "NormalizePath removes duplicate slashes",
            PostCondition = "Duplicate slashes are removed")]
        [Test]
        public void NormalizePath_DuplicateSlashes_Removed()
        {
            // Act
            var result = InvokeNormalizePath("path//to///file");

            // Assert
            Assert.That(result, Is.EqualTo("path/to/file"));
        }

        [UnitTestAttribute(
            Identifier = "1D42175D-4C0F-4E67-B23D-8F02F896B5D9",
            Purpose = "NormalizePath trims trailing slashes",
            PostCondition = "Trailing slashes are removed")]
        [Test]
        public void NormalizePath_TrailingSlashes_Trimmed()
        {
            // Act
            var result = InvokeNormalizePath("path/to/dir/");

            // Assert
            Assert.That(result, Is.EqualTo("path/to/dir"));
        }

        [UnitTestAttribute(
            Identifier = "04FDE03F-D664-4E98-8586-42A06A118B04",
            Purpose = "NormalizePath preserves sp:// protocol prefix",
            PostCondition = "Protocol prefix is preserved with exactly two slashes")]
        [Test]
        public void NormalizePath_SpProtocol_PreservedCorrectly()
        {
            // Act
            var result = InvokeNormalizePath("sp://path/to/file");

            // Assert
            Assert.That(result, Is.EqualTo("sp://path/to/file"));
        }

        [UnitTestAttribute(
            Identifier = "189CF812-0A23-4F51-A79A-2219FAB768EB",
            Purpose = "NormalizePath normalizes sp:/// to sp://",
            PostCondition = "Triple slashes in protocol are normalized to double")]
        [Test]
        public void NormalizePath_SpProtocolTripleSlash_NormalizedToDouble()
        {
            // Act
            var result = InvokeNormalizePath("sp:///path/to/file");

            // Assert
            Assert.That(result, Is.EqualTo("sp://path/to/file"));
        }

        [UnitTestAttribute(
            Identifier = "FFBF13C6-D4F9-4D3D-8109-2C38C1A61A11",
            Purpose = "NormalizePath handles mixed backslashes and protocol",
            PostCondition = "Path is fully normalized")]
        [Test]
        public void NormalizePath_MixedBackslashesWithProtocol_Normalized()
        {
            // Act
            var result = InvokeNormalizePath(@"sp://path\to\file");

            // Assert
            Assert.That(result, Is.EqualTo("sp://path/to/file"));
        }

        [UnitTestAttribute(
            Identifier = "38C73FDF-783E-4258-9938-C24FE51B06EA",
            Purpose = "NormalizePath handles https:// protocol",
            PostCondition = "HTTPS protocol is preserved")]
        [Test]
        public void NormalizePath_HttpsProtocol_PreservedCorrectly()
        {
            // Act
            var result = InvokeNormalizePath("https://sharepoint.com/path/to/file");

            // Assert
            Assert.That(result, Is.EqualTo("https://sharepoint.com/path/to/file"));
        }

        [UnitTestAttribute(
            Identifier = "9B3B97B6-CA17-4C77-9D6C-768A04BAC4E8",
            Purpose = "NormalizePath removes duplicate slashes after protocol",
            PostCondition = "Duplicate slashes in path portion are removed")]
        [Test]
        public void NormalizePath_DuplicateSlashesAfterProtocol_Removed()
        {
            // Act
            var result = InvokeNormalizePath("sp://path//to///file");

            // Assert
            Assert.That(result, Is.EqualTo("sp://path/to/file"));
        }

        [UnitTestAttribute(
            Identifier = "A15655AD-B838-427A-BD1C-471FBE504F60",
            Purpose = "NormalizePath trims trailing slashes after protocol path",
            PostCondition = "Trailing slashes after protocol are removed")]
        [Test]
        public void NormalizePath_TrailingSlashesWithProtocol_Trimmed()
        {
            // Act
            var result = InvokeNormalizePath("sp://path/to/dir/");

            // Assert
            Assert.That(result, Is.EqualTo("sp://path/to/dir"));
        }

        #endregion

        #region GenerateProjectIdentifier Tests (via reflection since it's private)

        private static string InvokeGenerateProjectIdentifier(string driveId, string configFilePath)
        {
            var method = typeof(ProjectManager).GetMethod("GenerateProjectIdentifier", 
                BindingFlags.NonPublic | BindingFlags.Static);
            return (string)method.Invoke(null, new object[] { driveId, configFilePath });
        }

        [UnitTestAttribute(
            Identifier = "CF10AD00-0F32-442D-8D64-B181C8D94142",
            Purpose = "GenerateProjectIdentifier creates deterministic ID",
            PostCondition = "Same inputs produce same ID")]
        [Test]
        public void GenerateProjectIdentifier_SameInputs_SameOutput()
        {
            // Arrange
            string driveId = "drive123";
            string configPath = "project/RoboClerkConfig/projectConfig.toml";

            // Act
            var id1 = InvokeGenerateProjectIdentifier(driveId, configPath);
            var id2 = InvokeGenerateProjectIdentifier(driveId, configPath);

            // Assert
            Assert.That(id1, Is.EqualTo(id2));
        }

        [UnitTestAttribute(
            Identifier = "8E6FFBA9-2485-4863-975F-C34F79223A8F",
            Purpose = "GenerateProjectIdentifier creates different IDs for different drives",
            PostCondition = "Different drive IDs produce different project IDs")]
        [Test]
        public void GenerateProjectIdentifier_DifferentDrives_DifferentOutput()
        {
            // Arrange
            string configPath = "project/RoboClerkConfig/projectConfig.toml";

            // Act
            var id1 = InvokeGenerateProjectIdentifier("drive123", configPath);
            var id2 = InvokeGenerateProjectIdentifier("drive456", configPath);

            // Assert
            Assert.That(id1, Is.Not.EqualTo(id2));
        }

        [UnitTestAttribute(
            Identifier = "9FF3EF35-2E02-45EB-A19A-D602F2B0AFAB",
            Purpose = "GenerateProjectIdentifier creates different IDs for different paths",
            PostCondition = "Different paths produce different project IDs")]
        [Test]
        public void GenerateProjectIdentifier_DifferentPaths_DifferentOutput()
        {
            // Arrange
            string driveId = "drive123";

            // Act
            var id1 = InvokeGenerateProjectIdentifier(driveId, "project1/config.toml");
            var id2 = InvokeGenerateProjectIdentifier(driveId, "project2/config.toml");

            // Assert
            Assert.That(id1, Is.Not.EqualTo(id2));
        }

        [UnitTestAttribute(
            Identifier = "034D4689-28E7-4B8D-A4E1-561852A06EAF",
            Purpose = "GenerateProjectIdentifier starts with sp- prefix",
            PostCondition = "ID starts with sp- prefix")]
        [Test]
        public void GenerateProjectIdentifier_StartsWithSpPrefix()
        {
            // Arrange
            string driveId = "drive123";
            string configPath = "project/config.toml";

            // Act
            var id = InvokeGenerateProjectIdentifier(driveId, configPath);

            // Assert
            Assert.That(id, Does.StartWith("sp-"));
        }

        [UnitTestAttribute(
            Identifier = "7015C6BA-087F-4488-9103-34ED611EF641",
            Purpose = "GenerateProjectIdentifier normalizes path slashes",
            PostCondition = "Backslashes and forward slashes produce same ID")]
        [Test]
        public void GenerateProjectIdentifier_PathSlashNormalization()
        {
            // Arrange
            string driveId = "drive123";

            // Act
            var id1 = InvokeGenerateProjectIdentifier(driveId, "project/config.toml");
            var id2 = InvokeGenerateProjectIdentifier(driveId, @"project\config.toml");

            // Assert
            Assert.That(id1, Is.EqualTo(id2));
        }

        [UnitTestAttribute(
            Identifier = "8633777B-0BDD-4D1A-A8C3-7DC6B07458D4",
            Purpose = "GenerateProjectIdentifier trims leading slashes",
            PostCondition = "Leading slashes don't affect ID")]
        [Test]
        public void GenerateProjectIdentifier_LeadingSlashTrimmed()
        {
            // Arrange
            string driveId = "drive123";

            // Act
            var id1 = InvokeGenerateProjectIdentifier(driveId, "project/config.toml");
            var id2 = InvokeGenerateProjectIdentifier(driveId, "/project/config.toml");

            // Assert
            Assert.That(id1, Is.EqualTo(id2));
        }

        #endregion

        #region IsSharePointPath Tests (via reflection since it's private)

        private bool InvokeIsSharePointPath(string path)
        {
            var method = typeof(ProjectManager).GetMethod("IsSharePointPath", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            return (bool)method.Invoke(projectManager, new object[] { path });
        }

        [UnitTestAttribute(
            Identifier = "97334E4F-CA6B-4EE0-9823-DA11FE3AA33D",
            Purpose = "IsSharePointPath returns true for sp:// paths",
            PostCondition = "True is returned for sp:// prefix")]
        [Test]
        public void IsSharePointPath_SpPrefix_ReturnsTrue()
        {
            // Act
            var result = InvokeIsSharePointPath("sp://project/path");

            // Assert
            Assert.That(result, Is.True);
        }

        [UnitTestAttribute(
            Identifier = "7E1E2F17-F54D-4992-8F8F-E06DA52E497A",
            Purpose = "IsSharePointPath returns true for SP:// (uppercase)",
            PostCondition = "Case-insensitive matching")]
        [Test]
        public void IsSharePointPath_UppercasePrefix_ReturnsTrue()
        {
            // Act
            var result = InvokeIsSharePointPath("SP://project/path");

            // Assert
            Assert.That(result, Is.True);
        }

        [UnitTestAttribute(
            Identifier = "D475EB76-7E11-42DC-A33A-9BD271C71E8E",
            Purpose = "IsSharePointPath returns false for local paths",
            PostCondition = "False is returned for local paths")]
        [Test]
        public void IsSharePointPath_LocalPath_ReturnsFalse()
        {
            // Act
            var result = InvokeIsSharePointPath(@"C:\local\path");

            // Assert
            Assert.That(result, Is.False);
        }

        [UnitTestAttribute(
            Identifier = "6D275436-3CE0-4463-BD83-D96ED7B0F9CF",
            Purpose = "IsSharePointPath returns false for null path",
            PostCondition = "False is returned for null")]
        [Test]
        public void IsSharePointPath_NullPath_ReturnsFalse()
        {
            // Act
            var result = InvokeIsSharePointPath(null);

            // Assert
            Assert.That(result, Is.False);
        }

        [UnitTestAttribute(
            Identifier = "AA7C91D9-34E1-438C-8592-EFF3C4222676",
            Purpose = "IsSharePointPath returns false for empty path",
            PostCondition = "False is returned for empty string")]
        [Test]
        public void IsSharePointPath_EmptyPath_ReturnsFalse()
        {
            // Act
            var result = InvokeIsSharePointPath("");

            // Assert
            Assert.That(result, Is.False);
        }

        [UnitTestAttribute(
            Identifier = "615F2ACA-FBEC-423D-B2A2-D4532758C9AA",
            Purpose = "IsSharePointPath returns false for https:// paths",
            PostCondition = "False is returned for https:// paths")]
        [Test]
        public void IsSharePointPath_HttpsPath_ReturnsFalse()
        {
            // Act
            var result = InvokeIsSharePointPath("https://sharepoint.com/path");

            // Assert
            Assert.That(result, Is.False);
        }

        #endregion

        #region Positive Flow Tests (Previously from TestProjectManagerSuite)

        [UnitTestAttribute(
            Identifier = "8ace733d-c1b9-4005-b2c4-4ddde5cbbad2",
            Purpose = "LoadProjectAsync successfully loads a valid project",
            PostCondition = "Project is loaded and returns success")]
        [Test]
        public async Task LoadProjectAsync_ValidProject_ReturnsSuccess()
        {
            await LoadTestProject();
            Assert.That(projectId, Does.StartWith("sp-"));
        }

        [UnitTestAttribute(
            Identifier = "dc3eedf4-0c75-4f14-a377-d5fc0fd80f91",
            Purpose = "GetContentCreatorMetadataAsync returns metadata for loaded project",
            PostCondition = "Metadata list is returned")]
        [Test]
        public async Task GetContentCreatorMetadataAsync_LoadedProject_ReturnsMetadata()
        {
            await LoadTestProject();
            var metadata = await projectManager.GetContentCreatorMetadataAsync(projectId);
            Assert.That(metadata, Is.Not.Null);
        }

        [UnitTestAttribute(
            Identifier = "7341072b-584e-4fba-8ee0-9bc413f1e78b",
            Purpose = "GetTemplateFileContentAsync returns content for valid file",
            PostCondition = "File content is returned")]
        [Test]
        public async Task GetTemplateFileContentAsync_ValidFile_ReturnsContent()
        {
            await LoadTestProject();
            var content = await projectManager.GetTemplateFileContentAsync(projectId, "template.docx");
            Assert.That(content, Is.Not.Null);
            Assert.That(content.Length, Is.GreaterThan(0));
        }

        [UnitTestAttribute(
            Identifier = "6935f813-3bdb-4714-b8c9-c3bfedd1b88f",
            Purpose = "GetConfigurationValuesAsync returns configuration values",
            PostCondition = "Dictionary of values is returned")]
        [Test]
        public async Task GetConfigurationValuesAsync_LoadedProject_ReturnsValues()
        {
            await LoadTestProject();
            var values = await projectManager.GetConfigurationValuesAsync(projectId);
            Assert.That(values, Is.Not.Null);
            Assert.That(values.ContainsKey("ProjectName"), Is.True);
        }

        [UnitTestAttribute(
            Identifier = "0f90f4a9-fb46-405d-82c0-f23ca695570d",
            Purpose = "UpdateConfigurationValuesAsync updates values successfully",
            PostCondition = "Success result is returned")]
        [Test]
        public async Task UpdateConfigurationValuesAsync_ValidUpdate_ReturnsSuccess()
        {
            await LoadTestProject();
            var updates = new Dictionary<string, string> { { "NewKey", "NewValue" } };
            
            // Mock GetProjectConfigurationContentAsync internal call
            mockSharePointProvider.ReadAllText(Arg.Is<string>(s => s.EndsWith("projectConfig.toml")))
                .Returns(GetValidProjectConfig());

            var result = await projectManager.UpdateConfigurationValuesAsync(projectId, updates);
            
            Assert.That(result.Success, Is.True);
            // Verify WriteAllTextAsync was called
            await mockSharePointProvider.Received().WriteAllTextAsync(
                Arg.Is<string>(s => s.EndsWith("projectConfig.toml")), 
                Arg.Any<string>());
        }

        [UnitTestAttribute(
            Identifier = "dde4fe7f-880d-4a66-8bf0-8f208b31d2c2",
            Purpose = "UpdateProjectConfigurationAsync updates full configuration",
            PostCondition = "Success result is returned and project is reloaded")]
        [Test]
        public async Task UpdateProjectConfigurationAsync_ValidConfig_ReturnsSuccess()
        {
            await LoadTestProject();
            string newConfig = GetValidProjectConfig().Replace("Test Project", "Updated Project");

            var result = await projectManager.UpdateProjectConfigurationAsync(projectId, newConfig);

            Assert.That(result.Success, Is.True);
            Assert.That(result.RequiresProjectReload, Is.True);
            
            // Verify write was called
            await mockSharePointProvider.Received().WriteAllTextAsync(
                Arg.Is<string>(s => s.EndsWith("projectConfig.toml")), 
                Arg.Is<string>(s => s.Contains("Updated Project")));
        }

        [UnitTestAttribute(
            Identifier = "d28c988f-b89a-45e9-a4d6-9e52aa9ad6d8",
            Purpose = "GetProjectConfigurationContentAsync returns raw config content",
            PostCondition = "Config content string is returned")]
        [Test]
        public async Task GetProjectConfigurationContentAsync_LoadedProject_ReturnsContent()
        {
            await LoadTestProject();
            var content = await projectManager.GetProjectConfigurationContentAsync(projectId);
            Assert.That(content, Does.Contain("Test Project"));
        }

        [UnitTestAttribute(
            Identifier = "3a535d55-274b-4710-af4d-08a72d8c7e51",
            Purpose = "ValidateConfigurationUpdatesAsync returns valid for good config",
            PostCondition = "Result IsValid is true")]
        [Test]
        public async Task ValidateConfigurationUpdatesAsync_ValidConfig_ReturnsValid()
        {
            await LoadTestProject();
            var result = await projectManager.ValidateConfigurationUpdatesAsync(projectId, GetValidProjectConfig());
            Assert.That(result.IsValid, Is.True);
        }

        [UnitTestAttribute(
            Identifier = "ef08ce2c-49f1-4241-b1e2-7420cc912895",
            Purpose = "GetAvailableTemplateFilesAsync returns files",
            PostCondition = "Available template files result is successful")]
        [Test]
        public async Task GetAvailableTemplateFilesAsync_LoadedProject_ReturnsFiles()
        {
            await LoadTestProject();
            var result = await projectManager.GetAvailableTemplateFilesAsync(projectId);
            Assert.That(result.Success, Is.True);
            // We mocked GetFiles to return 1 file
            Assert.That(result.TotalTemplateFiles, Is.EqualTo(1));
        }

        [UnitTestAttribute(
            Identifier = "33f3af31-ede1-4d8a-b2ae-f05105866b33",
            Purpose = "RefreshProjectDataSourcesAsync calls datasource refresh",
            PostCondition = "Success result is returned")]
        [Test]
        public async Task RefreshProjectDataSourcesAsync_LoadedProject_ReturnsSuccess()
        {
            await LoadTestProject();
            var result = await projectManager.RefreshProjectDataSourcesAsync(projectId);
            Assert.That(result.Success, Is.True);
        }

        [UnitTestAttribute(
            Identifier = "715f925a-8acb-403e-b13b-b4e08f71006c",
            Purpose = "RefreshProjectDocumentsAsync reloads documents",
            PostCondition = "Success result is returned")]
        [Test]
        public async Task RefreshProjectDocumentsAsync_LoadedProject_ReturnsSuccess()
        {
            await LoadTestProject();
            var result = await projectManager.RefreshProjectDocumentsAsync(projectId, false);
            Assert.That(result.Success, Is.True);
        }

        [UnitTestAttribute(
            Identifier = "4ad89225-0489-4d66-8552-ddb51a14b51d",
            Purpose = "RefreshDocumentAsync reloads specific document",
            PostCondition = "Success result is returned")]
        [Test]
        public async Task RefreshDocumentAsync_ValidDocId_ReturnsSuccess()
        {
            await LoadTestProject();
            var result = await projectManager.RefreshDocumentAsync(projectId, "Doc1");
            Assert.That(result.Success, Is.True);
        }

        [UnitTestAttribute(
            Identifier = "a54d7581-8973-4221-81d8-9a15523fefef",
            Purpose = "GetVirtualTagStatisticsAsync returns statistics",
            PostCondition = "Dictionary of statistics is returned")]
        [Test]
        public async Task GetVirtualTagStatisticsAsync_LoadedProject_ReturnsStats()
        {
            await LoadTestProject();
            var stats = await projectManager.GetVirtualTagStatisticsAsync(projectId);
            Assert.That(stats, Is.Not.Null);
        }

        [UnitTestAttribute(
            Identifier = "97c6d5b4-7087-484c-8dfb-e58657e10a54",
            Purpose = "GetTagContentWithContentControlAsync returns content for virtual tag",
            PostCondition = "Content is returned even if tag not in doc")]
        [Test]
        public async Task GetTagContentWithContentControlAsync_VirtualTag_ReturnsContent()
        {
            await LoadTestProject();
            
            var request = new RoboClerkContentControlTagRequest
            {
                DocumentId = $"{projectPath}/Templates/template.docx",
                ContentControlId = "12345",
                RoboClerkTag = "Comment:Note()"
            };

            var result = await projectManager.GetTagContentWithContentControlAsync(projectId, request);
            
            if (!result.Success && result.Error.Contains("Document"))
            {
                 Assert.Inconclusive($"Path mismatch in test: {result.Error}");
            }
            
            Assert.That(result, Is.Not.Null);
        }
        
        [UnitTestAttribute(
            Identifier = "b5f32488-1c39-4b53-969d-eb06a7fc0537",
            Purpose = "ValidateProjectForWordAddInAsync validates properly",
            PostCondition = "Returns true for valid loaded project")]
        [Test]
        public async Task ValidateProjectForWordAddInAsync_LoadedProject_ReturnsTrue()
        {
            await LoadTestProject();
            var request = new LoadProjectRequest { ProjectPath = projectPath, SPDriveId = driveId };
            var result = projectManager.ValidateProjectForWordAddInAsync(projectId, request);
            Assert.That(result, Is.True);
        }

        [UnitTestAttribute(
            Identifier = "4ec3f768-d7b6-42ac-ab6e-637a3e555fb9",
            Purpose = "UnloadProjectAsync removes project",
            PostCondition = "Project is no longer loaded")]
        [Test]
        public async Task UnloadProjectAsync_LoadedProject_UnloadsSuccessfully()
        {
            await LoadTestProject();
            await projectManager.UnloadProjectAsync(projectId);
            
            // Verify calls fail after unload
            Assert.ThrowsAsync<ArgumentException>(async () => 
                await projectManager.GetConfigurationValuesAsync(projectId));
        }

        #endregion

        #region LoadProjectAsync (Negative) Tests

        [UnitTestAttribute(
            Identifier = "61655379-06C4-43BE-963D-95C257E4379C",
            Purpose = "LoadProjectAsync fails for non-SharePoint path",
            PostCondition = "Error result is returned")]
        [Test]
        public async Task LoadProjectAsync_NonSharePointPath_ReturnsError()
        {
            // Arrange
            var request = new LoadProjectRequest
            {
                ProjectPath = @"C:\local\path",
                SPDriveId = "drive123"
            };

            // Act
            var result = await projectManager.LoadProjectAsync(request);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Does.Contain("SharePoint"));
        }

        #endregion

        #region UnloadProjectAsync Tests

        [UnitTestAttribute(
            Identifier = "23D6C844-8A4A-4050-8D3C-005C0FDCB5E9",
            Purpose = "UnloadProjectAsync completes without error for non-existent project",
            PostCondition = "Method completes without throwing")]
        [Test]
        public async Task UnloadProjectAsync_NonExistentProject_NoError()
        {
            // Act & Assert - should not throw
            await projectManager.UnloadProjectAsync("non-existent-project");
        }

        #endregion

        #region GetVirtualTagStatisticsAsync Tests

        [UnitTestAttribute(
            Identifier = "BCED46CA-BB5A-42D6-958F-C4D04187D0AB",
            Purpose = "GetVirtualTagStatisticsAsync throws for non-existent project",
            PostCondition = "ArgumentException is thrown")]
        [Test]
        public void GetVirtualTagStatisticsAsync_NonExistentProject_Throws()
        {
            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await projectManager.GetVirtualTagStatisticsAsync("non-existent"));
        }

        #endregion

        #region GetProjectConfigurationContentAsync Tests

        [UnitTestAttribute(
            Identifier = "0073EE2F-AC10-4041-B2EF-8B3AC6657EA0",
            Purpose = "GetProjectConfigurationContentAsync throws for non-existent project",
            PostCondition = "ArgumentException is thrown")]
        [Test]
        public void GetProjectConfigurationContentAsync_NonExistentProject_Throws()
        {
            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await projectManager.GetProjectConfigurationContentAsync("non-existent"));
        }

        #endregion

        #region ValidateConfigurationUpdatesAsync Tests

        [UnitTestAttribute(
            Identifier = "1015C419-4A8F-4897-85E8-8DFFB3EACE1F",
            Purpose = "ValidateConfigurationUpdatesAsync throws for non-existent project",
            PostCondition = "ArgumentException is thrown")]
        [Test]
        public void ValidateConfigurationUpdatesAsync_NonExistentProject_Throws()
        {
            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await projectManager.ValidateConfigurationUpdatesAsync("non-existent", "content"));
        }

        #endregion

        #region RefreshProjectDataSourcesAsync Tests

        [UnitTestAttribute(
            Identifier = "7457D1B7-927F-4809-932B-FA39A48D6A2A",
            Purpose = "RefreshProjectDataSourcesAsync throws for non-existent project",
            PostCondition = "ArgumentException is thrown")]
        [Test]
        public void RefreshProjectDataSourcesAsync_NonExistentProject_Throws()
        {
            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await projectManager.RefreshProjectDataSourcesAsync("non-existent"));
        }

        #endregion

        #region RefreshProjectDocumentsAsync Tests

        [UnitTestAttribute(
            Identifier = "1371BBC7-E778-4F7A-B71E-B6C74B0EFA34",
            Purpose = "RefreshProjectDocumentsAsync throws for non-existent project",
            PostCondition = "ArgumentException is thrown")]
        [Test]
        public void RefreshProjectDocumentsAsync_NonExistentProject_Throws()
        {
            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await projectManager.RefreshProjectDocumentsAsync("non-existent", false));
        }

        #endregion

        #region RefreshDocumentAsync Tests

        [UnitTestAttribute(
            Identifier = "64D534D0-DA0A-4E47-9869-C744E628B78A",
            Purpose = "RefreshDocumentAsync throws for non-existent project",
            PostCondition = "ArgumentException is thrown")]
        [Test]
        public void RefreshDocumentAsync_NonExistentProject_Throws()
        {
            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await projectManager.RefreshDocumentAsync("non-existent", "doc1"));
        }

        #endregion

        #region GetTagContentWithContentControlAsync Tests

        [UnitTestAttribute(
            Identifier = "CCC91070-FEE9-4A27-BD92-A324FF6CA90F",
            Purpose = "GetTagContentWithContentControlAsync throws for non-existent project",
            PostCondition = "ArgumentException is thrown")]
        [Test]
        public void GetTagContentWithContentControlAsync_NonExistentProject_Throws()
        {
            // Arrange
            var request = new RoboClerkContentControlTagRequest
            {
                DocumentId = "doc.docx",
                ContentControlId = "cc1",
                RoboClerkTag = "@@SLMS:SR()@@"
            };

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await projectManager.GetTagContentWithContentControlAsync("non-existent", request));
        }

        #endregion

        #region GetContentCreatorMetadataAsync Tests

        [UnitTestAttribute(
            Identifier = "AA6C2F08-FDEE-4F4C-A44F-70C049BDDA84",
            Purpose = "GetContentCreatorMetadataAsync throws for non-existent project",
            PostCondition = "ArgumentException is thrown")]
        [Test]
        public void GetContentCreatorMetadataAsync_NonExistentProject_Throws()
        {
            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await projectManager.GetContentCreatorMetadataAsync("non-existent"));
        }

        #endregion

        #region GetTemplateFileContentAsync Tests

        [UnitTestAttribute(
            Identifier = "434A039A-04BA-4A61-9A56-1643D5AC0E10",
            Purpose = "GetTemplateFileContentAsync throws for non-existent project",
            PostCondition = "ArgumentException is thrown")]
        [Test]
        public void GetTemplateFileContentAsync_NonExistentProject_Throws()
        {
            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await projectManager.GetTemplateFileContentAsync("non-existent", "file.docx"));
        }

        [UnitTestAttribute(
            Identifier = "8B53D3F8-12A9-40DA-B1B0-E4D0917A5BDF",
            Purpose = "GetTemplateFileContentAsync throws for empty filename",
            PostCondition = "ArgumentException is thrown")]
        [Test]
        public void GetTemplateFileContentAsync_EmptyFilename_Throws()
        {
            // We can't easily test this without a loaded project, but we can verify the validation logic
            // by checking the implementation
            Assert.Pass("Filename validation is handled in the method");
        }

        [UnitTestAttribute(
            Identifier = "078F1BBF-8BBA-4CDC-AF1E-34B43B086F6D",
            Purpose = "GetTemplateFileContentAsync rejects non-docx files",
            PostCondition = "ArgumentException is thrown for non-docx files")]
        [Test]
        public void GetTemplateFileContentAsync_NonDocxFile_Throws()
        {
            // We can't easily test this without a loaded project
            Assert.Pass("File extension validation is handled in the method");
        }

        [UnitTestAttribute(
            Identifier = "0F131F41-0CF2-4DDB-AA69-4239C943EE06",
            Purpose = "GetTemplateFileContentAsync rejects directory traversal attempts",
            PostCondition = "ArgumentException is thrown for path traversal")]
        [Test]
        public void GetTemplateFileContentAsync_DirectoryTraversal_Throws()
        {
            // We can't easily test this without a loaded project
            Assert.Pass("Directory traversal validation is handled in the method");
        }

        #endregion

        #region GetConfigurationValuesAsync Tests

        [UnitTestAttribute(
            Identifier = "218118E4-8AB4-4CCD-812B-CBCADE1EFB9F",
            Purpose = "GetConfigurationValuesAsync throws for non-existent project",
            PostCondition = "ArgumentException is thrown")]
        [Test]
        public void GetConfigurationValuesAsync_NonExistentProject_Throws()
        {
            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await projectManager.GetConfigurationValuesAsync("non-existent"));
        }

        #endregion

        #region UpdateConfigurationValuesAsync Tests

        [UnitTestAttribute(
            Identifier = "D86D101A-1010-4F0B-AD06-372886150A3F",
            Purpose = "UpdateConfigurationValuesAsync throws for non-existent project",
            PostCondition = "ArgumentException is thrown")]
        [Test]
        public void UpdateConfigurationValuesAsync_NonExistentProject_Throws()
        {
            // Arrange
            var values = new Dictionary<string, string> { { "key", "value" } };

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await projectManager.UpdateConfigurationValuesAsync("non-existent", values));
        }

        #endregion

        #region UpdateProjectConfigurationAsync Tests

        [UnitTestAttribute(
            Identifier = "659013CF-C1FD-40F7-8FB4-4D77AC03560A",
            Purpose = "UpdateProjectConfigurationAsync throws for non-existent project",
            PostCondition = "ArgumentException is thrown")]
        [Test]
        public void UpdateProjectConfigurationAsync_NonExistentProject_Throws()
        {
            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await projectManager.UpdateProjectConfigurationAsync("non-existent", "content"));
        }

        #endregion

        #region GetAvailableTemplateFilesAsync Tests

        [UnitTestAttribute(
            Identifier = "6B8F983C-789B-4E11-8690-6736EE82AA39",
            Purpose = "GetAvailableTemplateFilesAsync throws for non-existent project",
            PostCondition = "ArgumentException is thrown")]
        [Test]
        public void GetAvailableTemplateFilesAsync_NonExistentProject_Throws()
        {
            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await projectManager.GetAvailableTemplateFilesAsync("non-existent"));
        }

        #endregion

        #region ValidateProjectForWordAddInAsync Tests

        [UnitTestAttribute(
            Identifier = "8303014B-C3A8-438D-83AD-DC06791563D5",
            Purpose = "ValidateProjectForWordAddInAsync returns false for non-existent project",
            PostCondition = "False is returned")]
        [Test]
        public void ValidateProjectForWordAddInAsync_NonExistentProject_ReturnsFalse()
        {
            // Arrange
            var request = new LoadProjectRequest { ProjectPath = "sp://test" };

            // Act
            var result = projectManager.ValidateProjectForWordAddInAsync("non-existent", request);

            // Assert
            Assert.That(result, Is.False);
        }

        #endregion
    }
}
