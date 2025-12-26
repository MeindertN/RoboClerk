using NUnit.Framework;
using RoboClerk.Server.Models;
using System;

namespace RoboClerk.Tests.Server
{
    [TestFixture]
    [Description("Tests for the API models used in RoboClerk.Server")]
    public class TestApiModels
    {
        [UnitTestAttribute(
            Identifier = "8E55DB97-A0AD-4BC7-A8C3-C1B800585FAC",
            Purpose = "ProjectInfo record is created correctly",
            PostCondition = "Record has expected values")]
        [Test]
        public void ProjectInfo_Created_Success()
        {
            // Arrange & Act
            var info = new ProjectInfo("TestProject", "/path/to/project");

            // Assert
            Assert.That(info.Name, Is.EqualTo("TestProject"));
            Assert.That(info.Path, Is.EqualTo("/path/to/project"));
        }

        [UnitTestAttribute(
            Identifier = "6BA1238C-BE7C-4294-8CD5-904F801A396E",
            Purpose = "LoadProjectRequest record is created with default values",
            PostCondition = "All properties are null by default")]
        [Test]
        public void LoadProjectRequest_DefaultValues()
        {
            // Arrange & Act
            var request = new LoadProjectRequest();

            // Assert
            Assert.That(request.DocumentUrl, Is.Null);
            Assert.That(request.ProjectPath, Is.Null);
            Assert.That(request.SPDriveId, Is.Null);
            Assert.That(request.ProjectIdentifier, Is.Null);
            Assert.That(request.SPSiteUrl, Is.Null);
        }

        [UnitTestAttribute(
            Identifier = "68473DC9-C492-401C-871B-323EEF7E27FE",
            Purpose = "LoadProjectRequest record is created with init properties",
            PostCondition = "Properties are set correctly")]
        [Test]
        public void LoadProjectRequest_WithProperties()
        {
            // Arrange & Act
            var request = new LoadProjectRequest
            {
                DocumentUrl = "https://sharepoint.com/doc.docx",
                ProjectPath = "sp://project",
                SPDriveId = "drive123",
                ProjectIdentifier = "proj-001",
                SPSiteUrl = "https://site.sharepoint.com"
            };

            // Assert
            Assert.That(request.DocumentUrl, Is.EqualTo("https://sharepoint.com/doc.docx"));
            Assert.That(request.ProjectPath, Is.EqualTo("sp://project"));
            Assert.That(request.SPDriveId, Is.EqualTo("drive123"));
            Assert.That(request.ProjectIdentifier, Is.EqualTo("proj-001"));
            Assert.That(request.SPSiteUrl, Is.EqualTo("https://site.sharepoint.com"));
        }

        [UnitTestAttribute(
            Identifier = "616B899C-EB6A-407A-97DF-38B30DBFDAC3",
            Purpose = "ProjectLoadResult record has correct success result",
            PostCondition = "Success result is properly initialized")]
        [Test]
        public void ProjectLoadResult_Success()
        {
            // Arrange & Act
            var result = new ProjectLoadResult
            {
                Success = true,
                ProjectId = "proj-123",
                ProjectName = "Test Project",
                LastUpdated = DateTime.UtcNow,
                Documents = new System.Collections.Generic.List<DocumentInfo>
                {
                    new DocumentInfo("doc1", "Document 1", "template1.docx")
                }
            };

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.ProjectId, Is.EqualTo("proj-123"));
            Assert.That(result.ProjectName, Is.EqualTo("Test Project"));
            Assert.That(result.Documents.Count, Is.EqualTo(1));
        }

        [UnitTestAttribute(
            Identifier = "444230E8-6306-4895-8F89-E8E6642DA4E9",
            Purpose = "ProjectLoadResult record has correct failure result",
            PostCondition = "Failure result is properly initialized")]
        [Test]
        public void ProjectLoadResult_Failure()
        {
            // Arrange & Act
            var result = new ProjectLoadResult
            {
                Success = false,
                Error = "Failed to load project"
            };

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo("Failed to load project"));
            Assert.That(result.ProjectId, Is.Null);
        }

        [UnitTestAttribute(
            Identifier = "EBDC7F95-C22B-456B-A542-A85257236D74",
            Purpose = "DocumentInfo record is created correctly",
            PostCondition = "Record has expected values")]
        [Test]
        public void DocumentInfo_Created_Success()
        {
            // Arrange & Act
            var info = new DocumentInfo("DOC001", "Software Requirements", "SRS.docx");

            // Assert
            Assert.That(info.DocumentId, Is.EqualTo("DOC001"));
            Assert.That(info.Title, Is.EqualTo("Software Requirements"));
            Assert.That(info.Template, Is.EqualTo("SRS.docx"));
        }

        [UnitTestAttribute(
            Identifier = "F82D9882-BA4E-4E5B-ABD0-51C00B65E618",
            Purpose = "RoboClerkContentControlTagRequest has default values",
            PostCondition = "Default values are empty strings")]
        [Test]
        public void RoboClerkContentControlTagRequest_DefaultValues()
        {
            // Arrange & Act
            var request = new RoboClerkContentControlTagRequest();

            // Assert
            Assert.That(request.DocumentId, Is.EqualTo(string.Empty));
            Assert.That(request.ContentControlId, Is.EqualTo(string.Empty));
            Assert.That(request.RoboClerkTag, Is.EqualTo(string.Empty));
        }

        [UnitTestAttribute(
            Identifier = "562370AC-1498-4E37-9BFA-CD880286CCF6",
            Purpose = "RoboClerkContentControlTagRequest is created with properties",
            PostCondition = "Properties are set correctly")]
        [Test]
        public void RoboClerkContentControlTagRequest_WithProperties()
        {
            // Arrange & Act
            var request = new RoboClerkContentControlTagRequest
            {
                DocumentId = "sp://docs/test.docx",
                ContentControlId = "cc-123",
                RoboClerkTag = "@@SLMS:SoftwareRequirement()@@"
            };

            // Assert
            Assert.That(request.DocumentId, Is.EqualTo("sp://docs/test.docx"));
            Assert.That(request.ContentControlId, Is.EqualTo("cc-123"));
            Assert.That(request.RoboClerkTag, Is.EqualTo("@@SLMS:SoftwareRequirement()@@"));
        }

        [UnitTestAttribute(
            Identifier = "8D2BC0C0-8223-4BBF-B9BD-909729C937D1",
            Purpose = "TagContentResult success result is properly initialized",
            PostCondition = "Success result contains content")]
        [Test]
        public void TagContentResult_Success()
        {
            // Arrange & Act
            var result = new TagContentResult
            {
                Success = true,
                Content = "<w:p>Generated content</w:p>"
            };

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Content, Is.EqualTo("<w:p>Generated content</w:p>"));
        }

        [UnitTestAttribute(
            Identifier = "12EEA016-A376-4A54-A04A-9AFAC3E17529",
            Purpose = "TagContentResult failure result is properly initialized",
            PostCondition = "Failure result contains error")]
        [Test]
        public void TagContentResult_Failure()
        {
            // Arrange & Act
            var result = new TagContentResult
            {
                Success = false,
                Error = "Content creation failed"
            };

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo("Content creation failed"));
            Assert.That(result.Content, Is.Null);
        }

        [UnitTestAttribute(
            Identifier = "D82B39DF-A9DE-4D68-AB57-F1E92F222347",
            Purpose = "RefreshResult success is properly initialized",
            PostCondition = "Success result is correct")]
        [Test]
        public void RefreshResult_Success()
        {
            // Arrange & Act
            var result = new RefreshResult { Success = true };

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
        }

        [UnitTestAttribute(
            Identifier = "79A37337-BEE3-4796-8967-F7E3FF216ECD",
            Purpose = "RefreshResult failure is properly initialized",
            PostCondition = "Failure result contains error")]
        [Test]
        public void RefreshResult_Failure()
        {
            // Arrange & Act
            var result = new RefreshResult
            {
                Success = false,
                Error = "Refresh failed"
            };

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo("Refresh failed"));
        }

        [UnitTestAttribute(
            Identifier = "77C79D0D-D9E3-4509-8B0A-E7430F6A418C",
            Purpose = "ConfigurationUpdateResult success is properly initialized",
            PostCondition = "Success result has updated keys")]
        [Test]
        public void ConfigurationUpdateResult_Success()
        {
            // Arrange & Act
            var result = new ConfigurationUpdateResult
            {
                Success = true,
                UpdatedKeys = new System.Collections.Generic.List<string> { "key1", "key2" },
                RequiresProjectReload = false
            };

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.UpdatedKeys.Count, Is.EqualTo(2));
            Assert.That(result.RequiresProjectReload, Is.False);
        }

        [UnitTestAttribute(
            Identifier = "49691702-8B27-4800-B31B-D24E5E52AE9F",
            Purpose = "ConfigurationUpdateResult has default empty list",
            PostCondition = "UpdatedKeys defaults to empty list")]
        [Test]
        public void ConfigurationUpdateResult_DefaultList()
        {
            // Arrange & Act
            var result = new ConfigurationUpdateResult();

            // Assert
            Assert.That(result.UpdatedKeys, Is.Not.Null);
            Assert.That(result.UpdatedKeys.Count, Is.EqualTo(0));
        }

        [UnitTestAttribute(
            Identifier = "840BA5CD-3013-406B-9622-24C7A7068D95",
            Purpose = "ConfigurationValidationResult valid result",
            PostCondition = "Valid result has no errors")]
        [Test]
        public void ConfigurationValidationResult_Valid()
        {
            // Arrange & Act
            var result = new ConfigurationValidationResult
            {
                IsValid = true,
                Errors = new System.Collections.Generic.List<string>(),
                Warnings = new System.Collections.Generic.List<string> { "Minor warning" }
            };

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors.Count, Is.EqualTo(0));
            Assert.That(result.Warnings.Count, Is.EqualTo(1));
        }

        [UnitTestAttribute(
            Identifier = "E753D64D-2AE9-4C9A-BEC0-99E279A6C0C7",
            Purpose = "ConfigurationValidationResult invalid result",
            PostCondition = "Invalid result has errors")]
        [Test]
        public void ConfigurationValidationResult_Invalid()
        {
            // Arrange & Act
            var result = new ConfigurationValidationResult
            {
                IsValid = false,
                Errors = new System.Collections.Generic.List<string> { "Error 1", "Error 2" }
            };

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(2));
        }

        [UnitTestAttribute(
            Identifier = "25449FE6-9C01-40D6-883F-B7D4A6126C18",
            Purpose = "ConfigurationContentRequest has default empty content",
            PostCondition = "Content defaults to empty string")]
        [Test]
        public void ConfigurationContentRequest_Default()
        {
            // Arrange & Act
            var request = new ConfigurationContentRequest();

            // Assert
            Assert.That(request.Content, Is.EqualTo(string.Empty));
        }

        [UnitTestAttribute(
            Identifier = "C8F44E71-C8A8-4202-8551-2956449C4621",
            Purpose = "TemplateFileInfo is created with properties",
            PostCondition = "All properties are set correctly")]
        [Test]
        public void TemplateFileInfo_WithProperties()
        {
            // Arrange
            var lastModified = DateTime.UtcNow;

            // Act
            var info = new TemplateFileInfo
            {
                FileName = "template.docx",
                RelativePath = "templates/template.docx",
                FullPath = "sp://project/templates/template.docx",
                FileSizeBytes = 12345,
                LastModified = lastModified,
                IsDocx = true
            };

            // Assert
            Assert.That(info.FileName, Is.EqualTo("template.docx"));
            Assert.That(info.RelativePath, Is.EqualTo("templates/template.docx"));
            Assert.That(info.FullPath, Is.EqualTo("sp://project/templates/template.docx"));
            Assert.That(info.FileSizeBytes, Is.EqualTo(12345));
            Assert.That(info.LastModified, Is.EqualTo(lastModified));
            Assert.That(info.IsDocx, Is.True);
        }

        [UnitTestAttribute(
            Identifier = "581B53BB-1CB4-4569-BDA6-6FCDFD5BACD4",
            Purpose = "TemplateFileInfo has default empty strings",
            PostCondition = "String properties default to empty")]
        [Test]
        public void TemplateFileInfo_DefaultValues()
        {
            // Arrange & Act
            var info = new TemplateFileInfo();

            // Assert
            Assert.That(info.FileName, Is.EqualTo(string.Empty));
            Assert.That(info.RelativePath, Is.EqualTo(string.Empty));
            Assert.That(info.FullPath, Is.EqualTo(string.Empty));
            Assert.That(info.FileSizeBytes, Is.EqualTo(0));
            Assert.That(info.IsDocx, Is.False);
        }

        [UnitTestAttribute(
            Identifier = "93D52547-D75B-428A-9547-5EF84A8FA51F",
            Purpose = "AvailableTemplateFilesResult success result",
            PostCondition = "Success result has template files")]
        [Test]
        public void AvailableTemplateFilesResult_Success()
        {
            // Arrange & Act
            var result = new AvailableTemplateFilesResult
            {
                Success = true,
                AvailableTemplateFiles = new System.Collections.Generic.List<TemplateFileInfo>
                {
                    new TemplateFileInfo { FileName = "test.docx" }
                },
                TotalTemplateFiles = 5,
                ConfiguredDocuments = 3,
                UnconfiguredTemplateFiles = 2
            };

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.AvailableTemplateFiles.Count, Is.EqualTo(1));
            Assert.That(result.TotalTemplateFiles, Is.EqualTo(5));
            Assert.That(result.ConfiguredDocuments, Is.EqualTo(3));
            Assert.That(result.UnconfiguredTemplateFiles, Is.EqualTo(2));
        }

        [UnitTestAttribute(
            Identifier = "94890CD7-091D-4046-A461-0A16DDB9E918",
            Purpose = "AvailableTemplateFilesResult failure result",
            PostCondition = "Failure result has error")]
        [Test]
        public void AvailableTemplateFilesResult_Failure()
        {
            // Arrange & Act
            var result = new AvailableTemplateFilesResult
            {
                Success = false,
                Error = "Failed to list templates"
            };

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo("Failed to list templates"));
        }

        [UnitTestAttribute(
            Identifier = "9759E079-3844-461B-861B-E354FFC0E74B",
            Purpose = "AvailableTemplateFilesResult has default empty list",
            PostCondition = "AvailableTemplateFiles defaults to empty list")]
        [Test]
        public void AvailableTemplateFilesResult_DefaultList()
        {
            // Arrange & Act
            var result = new AvailableTemplateFilesResult();

            // Assert
            Assert.That(result.AvailableTemplateFiles, Is.Not.Null);
            Assert.That(result.AvailableTemplateFiles.Count, Is.EqualTo(0));
        }
    }
}
