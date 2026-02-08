using NUnit.Framework;
using NSubstitute;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using RoboClerk.Server.Controllers;
using RoboClerk.Server.Services;
using RoboClerk.Server.Models;
using RoboClerk.Server.Configuration;
using RoboClerk.ContentCreators;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RoboClerk.Core.Configuration;

namespace RoboClerk.Tests.Server
{
    [TestFixture]
    [Description("Tests for the WordAddInController")]
    public class TestWordAddInController
    {
        private IProjectManager mockProjectManager;
        private ISharePointService mockSharePointService;
        private ServerConfiguration serverConfiguration;
        private WordAddInController controller;
        private DefaultHttpContext mockHttpContext;
        private IConfiguration mockConfiguration;

        [SetUp]
        public void Setup()
        {
            mockProjectManager = Substitute.For<IProjectManager>();
            mockSharePointService = Substitute.For<ISharePointService>();
            mockConfiguration = Substitute.For<IConfiguration>();
            serverConfiguration = new ServerConfiguration();

            controller = new WordAddInController(mockProjectManager, mockSharePointService, serverConfiguration);

            // Mock HttpContext for methods that need it
            mockHttpContext = new DefaultHttpContext();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(mockConfiguration);
            mockHttpContext.RequestServices = serviceCollection.BuildServiceProvider();
            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = mockHttpContext
            };
        }

        [UnitTestAttribute(
            Identifier = "145228e0-0dc8-4d01-9f68-12e77197ad00",
            Purpose = "GetContentCreatorMetadata returns metadata when project is found",
            PostCondition = "OkResult with metadata is returned")]
        [Test]
        public async Task GetContentCreatorMetadata_ProjectFound_ReturnsOk()
        {
            // Arrange
            string projectId = "project1";
            var expectedMetadata = new List<ContentCreatorMetadata> { new ContentCreatorMetadata("test", "Test Creator", "Description") };
            mockProjectManager.GetContentCreatorMetadataAsync(projectId, false).Returns(expectedMetadata);

            // Act
            var result = await controller.GetContentCreatorMetadata(projectId);

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.EqualTo(expectedMetadata));
        }

        [UnitTestAttribute(
            Identifier = "9f4a5bf2-1f64-468c-bc8f-5145cf9091db",
            Purpose = "GetContentCreatorMetadata returns BadRequest when projectId is empty",
            PostCondition = "BadRequestResult is returned")]
        [Test]
        public async Task GetContentCreatorMetadata_EmptyProjectId_ReturnsBadRequest()
        {
            // Act
            var result = await controller.GetContentCreatorMetadata("");

            // Assert
            var badRequestResult = result.Result as BadRequestObjectResult;
            Assert.That(badRequestResult, Is.Not.Null);
        }

        [UnitTestAttribute(
            Identifier = "b0711f4d-5c73-42dd-a11b-ca01493f5d4e",
            Purpose = "GetContentCreatorMetadata returns NotFound when ProjectManager throws ArgumentException",
            PostCondition = "NotFoundObjectResult is returned")]
        [Test]
        public async Task GetContentCreatorMetadata_ProjectNotFound_ReturnsNotFound()
        {
            // Arrange
            string projectId = "unknown";
            mockProjectManager.GetContentCreatorMetadataAsync(projectId, Arg.Any<bool>())
                .Returns(Task.FromException<List<ContentCreatorMetadata>>(new ArgumentException()));

            // Act
            var result = await controller.GetContentCreatorMetadata(projectId);

            // Assert
            var notFoundResult = result.Result as NotFoundObjectResult;
            Assert.That(notFoundResult, Is.Not.Null);
        }

        [UnitTestAttribute(
            Identifier = "42161e42-027a-42b4-8395-ef1420c992ef",
            Purpose = "LoadSharePointProject (legacy) returns Ok when successful",
            PostCondition = "OkObjectResult with result is returned")]
        [Test]
        public async Task LoadSharePointProject_LegacySuccess_ReturnsOk()
        {
            // Arrange
            var request = new LoadProjectRequest { ProjectPath = "sp://path", SPDriveId = "drive1" };
            var expectedResult = new ProjectLoadResult { Success = true, ProjectId = "proj1" };
            mockProjectManager.LoadProjectAsync(request).Returns(expectedResult);
            mockProjectManager.ValidateProjectForWordAddInAsync("proj1", request).Returns(true);

            // Act
            var result = await controller.LoadSharePointProject(request);

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.EqualTo(expectedResult));
        }

        [UnitTestAttribute(
            Identifier = "c491337d-20a6-405d-8092-bda52d7a9e51",
            Purpose = "LoadSharePointProject (legacy) returns BadRequest when required fields are missing",
            PostCondition = "BadRequestObjectResult is returned")]
        [Test]
        public async Task LoadSharePointProject_LegacyMissingFields_ReturnsBadRequest()
        {
            // Arrange
            var request = new LoadProjectRequest { ProjectPath = "sp://path" }; // Missing SPDriveId

            // Act
            var result = await controller.LoadSharePointProject(request);

            // Assert
            var badRequestResult = result.Result as BadRequestObjectResult;
            Assert.That(badRequestResult, Is.Not.Null);
        }

        [UnitTestAttribute(
            Identifier = "459670d5-de60-4ccb-9a24-5bf0d8d3951c",
            Purpose = "LoadSharePointProject (legacy) returns BadRequest when validation fails",
            PostCondition = "BadRequestObjectResult is returned and project is unloaded")]
        [Test]
        public async Task LoadSharePointProject_LegacyValidationFail_ReturnsBadRequestAndUnloads()
        {
            // Arrange
            var request = new LoadProjectRequest { ProjectPath = "sp://path", SPDriveId = "drive1" };
            var loadResult = new ProjectLoadResult { Success = true, ProjectId = "proj1" };
            mockProjectManager.LoadProjectAsync(request).Returns(loadResult);
            mockProjectManager.ValidateProjectForWordAddInAsync("proj1", request).Returns(false);

            // Act
            var result = await controller.LoadSharePointProject(request);

            // Assert
            var badRequestResult = result.Result as BadRequestObjectResult;
            Assert.That(badRequestResult, Is.Not.Null);
            await mockProjectManager.Received(1).UnloadProjectAsync("proj1");
        }

        [UnitTestAttribute(
            Identifier = "1f35acd1-8eb5-4b56-bf3a-f453fb2a911b",
            Purpose = "LoadSharePointProject (URL) returns Ok when extraction succeeds",
            PostCondition = "OkObjectResult is returned using extracted info")]
        [Test]
        public async Task LoadSharePointProject_UrlSuccess_ReturnsOk()
        {
            // Arrange
            var request = new LoadProjectRequest { DocumentUrl = "https://sharepoint.com/doc.docx" };
            var spInfo = new SharePointProjectInfo 
            { 
                Success = true, 
                ProjectPath = "sp://path", 
                DriveId = "drive1", 
                SiteUrl = "https://site" 
            };
            
            mockConfiguration.IsConfigOverridden("SPClientSecret").Returns(true);
            mockConfiguration.GetConfigOverrideValue("SPClientSecret").Returns("secret");
            
            mockSharePointService.ExtractProjectInfoFromDocumentUrlAsync(request.DocumentUrl, "secret").Returns(spInfo);
            
            var expectedResult = new ProjectLoadResult { Success = true, ProjectId = "proj1" };
            mockProjectManager.LoadProjectAsync(Arg.Is<LoadProjectRequest>(r => 
                r.ProjectPath == spInfo.ProjectPath && 
                r.SPDriveId == spInfo.DriveId && 
                r.SPSiteUrl == spInfo.SiteUrl
            )).Returns(expectedResult);
            
            mockProjectManager.ValidateProjectForWordAddInAsync("proj1", Arg.Any<LoadProjectRequest>()).Returns(true);

            // Act
            var result = await controller.LoadSharePointProject(request);

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.EqualTo(expectedResult));
        }

        [UnitTestAttribute(
            Identifier = "c184b380-9877-4da4-9f72-346e37862953",
            Purpose = "LoadSharePointProject (URL) returns BadRequest when extraction fails",
            PostCondition = "BadRequestObjectResult is returned")]
        [Test]
        public async Task LoadSharePointProject_UrlExtractionFail_ReturnsBadRequest()
        {
            // Arrange
            var request = new LoadProjectRequest { DocumentUrl = "https://sharepoint.com/doc.docx" };
            var spInfo = new SharePointProjectInfo { Success = false, Error = "Failed" };
            
            mockConfiguration.IsConfigOverridden("SPClientSecret").Returns(true);
            mockConfiguration.GetConfigOverrideValue("SPClientSecret").Returns("secret");
            
            mockSharePointService.ExtractProjectInfoFromDocumentUrlAsync(request.DocumentUrl, "secret").Returns(spInfo);

            // Act
            var result = await controller.LoadSharePointProject(request);

            // Assert
            var badRequestResult = result.Result as BadRequestObjectResult;
            Assert.That(badRequestResult, Is.Not.Null);
        }

        [UnitTestAttribute(
            Identifier = "886cb8c2-36b0-43a5-ba1b-c21ff53e8643",
            Purpose = "RefreshProject returns Ok when success",
            PostCondition = "OkObjectResult is returned")]
        [Test]
        public async Task RefreshProject_Success_ReturnsOk()
        {
            // Arrange
            string projectId = "proj1";
            var expectedResult = new RefreshResult { Success = true };
            mockProjectManager.RefreshProjectDocumentsAsync(projectId, false).Returns(expectedResult);

            // Act
            var result = await controller.RefreshProject(projectId);

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.EqualTo(expectedResult));
        }

        [UnitTestAttribute(
            Identifier = "7bb21148-795e-40d4-80c8-88e49a3221f0",
            Purpose = "RefreshProject returns NotFound when project manager throws ArgumentException",
            PostCondition = "NotFoundObjectResult is returned")]
        [Test]
        public async Task RefreshProject_ProjectNotFound_ReturnsNotFound()
        {
            // Arrange
            string projectId = "proj1";
            mockProjectManager.RefreshProjectDocumentsAsync(projectId, false).Returns(Task.FromException<RefreshResult>(new ArgumentException()));

            // Act
            var result = await controller.RefreshProject(projectId);

            // Assert
            var notFoundResult = result.Result as NotFoundObjectResult;
            Assert.That(notFoundResult, Is.Not.Null);
        }

        [UnitTestAttribute(
            Identifier = "601d04a6-02b6-4595-8f1b-87f3194d8e24",
            Purpose = "RefreshDocument returns Ok when success",
            PostCondition = "OkObjectResult is returned")]
        [Test]
        public async Task RefreshDocument_Success_ReturnsOk()
        {
            // Arrange
            string projectId = "proj1";
            string docId = "doc1";
            var expectedResult = new RefreshResult { Success = true };
            mockProjectManager.RefreshDocumentAsync(projectId, docId).Returns(expectedResult);

            // Act
            var result = await controller.RefreshDocument(projectId, docId);

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.EqualTo(expectedResult));
        }

        [UnitTestAttribute(
            Identifier = "dd200ab3-5608-4da3-89e9-0405ac595f65",
            Purpose = "GenerateContentForContentControl returns Ok when success",
            PostCondition = "OkObjectResult is returned")]
        [Test]
        public async Task GenerateContentForContentControl_Success_ReturnsOk()
        {
            // Arrange
            string projectId = "proj1";
            var request = new RoboClerkContentControlTagRequest { DocumentId = "d1", ContentControlId = "cc1", RoboClerkTag = "tag" };
            var expectedResult = new TagContentResult { Success = true, Content = "content" };
            mockProjectManager.GetTagContentWithContentControlAsync(projectId, request).Returns(expectedResult);

            // Act
            var result = await controller.GenerateContentForContentControl(projectId, request);

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.EqualTo(expectedResult));
        }

        [UnitTestAttribute(
            Identifier = "868d99f5-19a3-492d-99dc-621f8767649a",
            Purpose = "RefreshDataSources returns Ok when success",
            PostCondition = "OkObjectResult is returned")]
        [Test]
        public async Task RefreshDataSources_Success_ReturnsOk()
        {
            // Arrange
            string projectId = "proj1";
            var expectedResult = new RefreshResult { Success = true };
            mockProjectManager.RefreshProjectDataSourcesAsync(projectId).Returns(expectedResult);

            // Act
            var result = await controller.RefreshDataSources(projectId);

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.EqualTo(expectedResult));
        }

        [UnitTestAttribute(
            Identifier = "d47bcdbe-871d-47eb-be13-244b95984f28",
            Purpose = "GetVirtualTagStatistics returns Ok with stats",
            PostCondition = "OkObjectResult is returned")]
        [Test]
        public async Task GetVirtualTagStatistics_Success_ReturnsOk()
        {
            // Arrange
            string projectId = "proj1";
            var stats = new Dictionary<string, int> { { "doc1", 5 } };
            mockProjectManager.GetVirtualTagStatisticsAsync(projectId).Returns(stats);

            // Act
            var result = await controller.GetVirtualTagStatistics(projectId);

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.EqualTo(stats));
        }

        [UnitTestAttribute(
            Identifier = "47a9c4ba-21cd-4388-ac97-dfc0c8f7f138",
            Purpose = "UpdateProjectConfiguration returns Ok when success",
            PostCondition = "OkObjectResult is returned")]
        [Test]
        public async Task UpdateProjectConfiguration_Success_ReturnsOk()
        {
            // Arrange
            string projectId = "proj1";
            var request = new ConfigurationContentRequest { Content = "config" };
            var validationResult = new ConfigurationValidationResult { IsValid = true };
            var updateResult = new ConfigurationUpdateResult { Success = true };

            mockProjectManager.ValidateConfigurationUpdatesAsync(projectId, request.Content).Returns(validationResult);
            mockProjectManager.UpdateProjectConfigurationAsync(projectId, request.Content).Returns(updateResult);

            // Act
            var result = await controller.UpdateProjectConfiguration(projectId, request);

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.EqualTo(updateResult));
        }

        [UnitTestAttribute(
            Identifier = "7b5e4cef-ae49-4262-9814-d11b027a7280",
            Purpose = "UpdateProjectConfiguration returns BadRequest when validation fails",
            PostCondition = "BadRequestObjectResult is returned")]
        [Test]
        public async Task UpdateProjectConfiguration_ValidationFail_ReturnsBadRequest()
        {
            // Arrange
            string projectId = "proj1";
            var request = new ConfigurationContentRequest { Content = "config" };
            var validationResult = new ConfigurationValidationResult { IsValid = false, Errors = new List<string> { "error" } };

            mockProjectManager.ValidateConfigurationUpdatesAsync(projectId, request.Content).Returns(validationResult);

            // Act
            var result = await controller.UpdateProjectConfiguration(projectId, request);

            // Assert
            var badRequestResult = result.Result as BadRequestObjectResult;
            Assert.That(badRequestResult, Is.Not.Null);
        }

        [UnitTestAttribute(
            Identifier = "0d40a363-393f-4261-92bf-958d6971c54f",
            Purpose = "GetProjectConfigurationContent returns Ok with content",
            PostCondition = "OkObjectResult is returned")]
        [Test]
        public async Task GetProjectConfigurationContent_Success_ReturnsOk()
        {
            // Arrange
            string projectId = "proj1";
            string content = "config content";
            mockProjectManager.GetProjectConfigurationContentAsync(projectId).Returns(content);

            // Act
            var result = await controller.GetProjectConfigurationContent(projectId);

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.EqualTo(content));
        }

        [UnitTestAttribute(
            Identifier = "521ee5ea-dfe3-4026-b47f-29bb9a504e7d",
            Purpose = "GetConfigurationValues returns Ok with values",
            PostCondition = "OkObjectResult is returned")]
        [Test]
        public async Task GetConfigurationValues_Success_ReturnsOk()
        {
            // Arrange
            string projectId = "proj1";
            var values = new Dictionary<string, string> { { "key", "val" } };
            mockProjectManager.GetConfigurationValuesAsync(projectId).Returns(values);

            // Act
            var result = await controller.GetConfigurationValues(projectId);

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.EqualTo(values));
        }

        [UnitTestAttribute(
            Identifier = "30e65a0d-d1a8-4a13-bdc2-c0f47def6dce",
            Purpose = "UpdateConfigurationValues returns Ok when success",
            PostCondition = "OkObjectResult is returned")]
        [Test]
        public async Task UpdateConfigurationValues_Success_ReturnsOk()
        {
            // Arrange
            string projectId = "proj1";
            var values = new Dictionary<string, string> { { "key", "val" } };
            var expectedResult = new ConfigurationUpdateResult { Success = true };
            mockProjectManager.UpdateConfigurationValuesAsync(projectId, values).Returns(expectedResult);

            // Act
            var result = await controller.UpdateConfigurationValues(projectId, values);

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.EqualTo(expectedResult));
        }

        [UnitTestAttribute(
            Identifier = "3a9990de-fe92-421e-a504-97fe7fbfef93",
            Purpose = "ValidateConfigurationUpdates returns Ok with result",
            PostCondition = "OkObjectResult is returned")]
        [Test]
        public async Task ValidateConfigurationUpdates_Success_ReturnsOk()
        {
            // Arrange
            string projectId = "proj1";
            var request = new ConfigurationContentRequest { Content = "config" };
            var expectedResult = new ConfigurationValidationResult { IsValid = true };
            mockProjectManager.ValidateConfigurationUpdatesAsync(projectId, request.Content).Returns(expectedResult);

            // Act
            var result = await controller.ValidateConfigurationUpdates(projectId, request);

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.EqualTo(expectedResult));
        }

        [UnitTestAttribute(
            Identifier = "6d63762c-63e9-41c2-8ae6-f36d3b8aa1e5",
            Purpose = "GetAvailableTemplateFileNames returns Ok when success",
            PostCondition = "OkObjectResult is returned")]
        [Test]
        public async Task GetAvailableTemplateFileNames_Success_ReturnsOk()
        {
            // Arrange
            string projectId = "proj1";
            var expectedResult = new AvailableTemplateFilesResult { Success = true };
            mockProjectManager.GetAvailableTemplateFilesAsync(projectId, false).Returns(expectedResult);

            // Act
            var result = await controller.GetAvailableTemplateFileNames(projectId);

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.EqualTo(expectedResult));
        }

        [UnitTestAttribute(
            Identifier = "3d6598f9-a1db-4a17-a5dc-31f0c86f7bb5",
            Purpose = "GetTemplateFile returns FileResult when success",
            PostCondition = "FileContentResult is returned")]
        [Test]
        public async Task GetTemplateFile_Success_ReturnsFile()
        {
            // Arrange
            string projectId = "proj1";
            string fileName = "test.docx";
            byte[] fileContent = new byte[] { 1, 2, 3 };
            mockProjectManager.GetTemplateFileContentAsync(projectId, fileName).Returns(fileContent);

            // Act
            var result = await controller.GetTemplateFile(projectId, fileName);

            // Assert
            var fileResult = result as FileContentResult;
            Assert.That(fileResult, Is.Not.Null);
            Assert.That(fileResult.FileContents, Is.EqualTo(fileContent));
            Assert.That(fileResult.ContentType, Is.EqualTo("application/vnd.openxmlformats-officedocument.wordprocessingml.document"));
            Assert.That(fileResult.FileDownloadName, Is.EqualTo(fileName));
        }

        [UnitTestAttribute(
            Identifier = "7403b2d2-485d-4f31-8fd7-24baf11677a6",
            Purpose = "EndSession calls UnloadProjectAsync and returns NoContent",
            PostCondition = "NoContentResult is returned")]
        [Test]
        public async Task EndSession_Success_ReturnsNoContent()
        {
            // Arrange
            string projectId = "proj1";

            // Act
            var result = await controller.EndSession(projectId);

            // Assert
            Assert.That(result, Is.InstanceOf<NoContentResult>());
            await mockProjectManager.Received(1).UnloadProjectAsync(projectId);
        }

        [UnitTestAttribute(
            Identifier = "ab80db78-c108-4262-8582-498471ebf57c",
            Purpose = "HealthCheck returns Ok with status",
            PostCondition = "OkObjectResult with status is returned")]
        [Test]
        public void HealthCheck_ReturnsOk()
        {
            // Act
            var result = controller.HealthCheck();

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            // We can't easily checking anonymous object props in unit tests without reflection or dynamic, 
            // but checking it's not null and 200 OK is sufficient for controller test.
        }
    }
}
