using NUnit.Framework;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using RoboClerk.Core.FileProviders;
using System.Linq;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("Tests for the SmartFileProviderPlugin that routes file operations based on path prefixes")]
    public class TestSmartFileProviderPlugin
    {
        private IFileSystem mockFileSystem;
        private IFileProviderPlugin localProvider;
        private IFileProviderPlugin mockSharePointProvider;
        private SmartFileProviderPlugin smartProvider;

        [SetUp]
        public void Setup()
        {
            // Create mock file system with test files
            mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFilePath(@"C:\local\test.txt"), new MockFileData("local file content") },
                { TestingHelpers.ConvertFilePath(@"C:\local\subdir\file.txt"), new MockFileData("local subdir content") },
                { TestingHelpers.ConvertFilePath(@"C:\source\code.cs"), new MockFileData("source code content") }
            });

            // Create local file provider
            localProvider = new LocalFileSystemPlugin(mockFileSystem);

            // Create mock SharePoint provider
            mockSharePointProvider = Substitute.For<IFileProviderPlugin>();
            mockSharePointProvider.GetPathPrefix().Returns("sp://");
            mockSharePointProvider.Name.Returns("SharePointFileProviderPlugin");

            // Create smart provider with local as default
            smartProvider = new SmartFileProviderPlugin(localProvider);
        }

        [UnitTestAttribute(
            Identifier = "16F14EFD-5384-449D-8B3C-AD2E83C6CB33",
            Purpose = "Smart provider is created with local provider as default",
            PostCondition = "No exception is thrown and provider is initialized")]
        [Test]
        public void CreateSmartProvider_WithLocalProvider_Success()
        {
            // Arrange & Act
            var provider = new SmartFileProviderPlugin(localProvider);

            // Assert
            Assert.That(provider, Is.Not.Null);
            Assert.That(provider.Name, Is.EqualTo("SmartFileProviderPlugin"));
            Assert.That(provider.GetPathPrefix(), Is.Null);
        }

        [UnitTestAttribute(
            Identifier = "011A50CC-C550-4B46-BAC6-6F6418B79B99",
            Purpose = "Smart provider throws exception when created with null local provider",
            PostCondition = "ArgumentNullException is thrown")]
        [Test]
        public void CreateSmartProvider_WithNullProvider_ThrowsException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SmartFileProviderPlugin(null));
        }

        [UnitTestAttribute(
            Identifier = "F257A054-CF2B-48E5-AD1F-5153AA7C05AC",
            Purpose = "Register SharePoint provider with sp:// prefix",
            PostCondition = "Provider is registered successfully")]
        [Test]
        public void RegisterProvider_SharePointProvider_Success()
        {
            // Arrange
            var provider = new SmartFileProviderPlugin(localProvider);

            // Act
            provider.RegisterProvider(mockSharePointProvider);

            // Assert - verify by trying to use a SharePoint path
            mockSharePointProvider.FileExists("test.txt").Returns(true);
            var exists = provider.FileExists("sp://test.txt");
            
            mockSharePointProvider.Received(1).FileExists("test.txt");
        }

        [UnitTestAttribute(
            Identifier = "9C800752-8037-4E33-B677-1CD5FD086433",
            Purpose = "Register provider with null prefix throws exception",
            PostCondition = "ArgumentException is thrown")]
        [Test]
        public void RegisterProvider_WithNullPrefix_ThrowsException()
        {
            // Arrange
            var provider = new SmartFileProviderPlugin(localProvider);
            var providerWithoutPrefix = Substitute.For<IFileProviderPlugin>();
            providerWithoutPrefix.GetPathPrefix().Returns((string)null);
            providerWithoutPrefix.Name.Returns("TestProvider");

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => provider.RegisterProvider(providerWithoutPrefix));
            Assert.That(ex.Message, Does.Contain("must have a non-empty prefix"));
        }

        [UnitTestAttribute(
            Identifier = "EB237A5C-6B7E-4004-AE4A-44DBE1F3BBD9",
            Purpose = "Register duplicate prefix throws exception",
            PostCondition = "ArgumentException is thrown")]
        [Test]
        public void RegisterProvider_DuplicatePrefix_ThrowsException()
        {
            // Arrange
            var provider = new SmartFileProviderPlugin(localProvider);
            provider.RegisterProvider(mockSharePointProvider);

            var anotherSharePointProvider = Substitute.For<IFileProviderPlugin>();
            anotherSharePointProvider.GetPathPrefix().Returns("sp://");
            anotherSharePointProvider.Name.Returns("AnotherSharePointProvider");

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => provider.RegisterProvider(anotherSharePointProvider));
            Assert.That(ex.Message, Does.Contain("already registered"));
        }

        [UnitTestAttribute(
            Identifier = "1929FC58-3168-451B-96A5-6271672B98F8",
            Purpose = "Local path without prefix routes to local provider",
            PostCondition = "File is read from local file system")]
        [Test]
        public void ReadAllText_LocalPath_RoutesToLocalProvider()
        {
            // Arrange
            smartProvider.RegisterProvider(mockSharePointProvider);

            // Act
            var content = smartProvider.ReadAllText(TestingHelpers.ConvertFilePath(@"C:\local\test.txt"));

            // Assert
            Assert.That(content, Is.EqualTo("local file content"));
            mockSharePointProvider.DidNotReceive().ReadAllText(Arg.Any<string>());
        }

        [UnitTestAttribute(
            Identifier = "050E7ABD-87B2-4784-B09D-24CB341AE6A1",
            Purpose = "SharePoint path with sp:// prefix routes to SharePoint provider",
            PostCondition = "File is read from SharePoint provider")]
        [Test]
        public void ReadAllText_SharePointPath_RoutesToSharePointProvider()
        {
            // Arrange
            smartProvider.RegisterProvider(mockSharePointProvider);
            mockSharePointProvider.ReadAllText("RoboClerk/test.txt").Returns("sharepoint content");

            // Act
            var content = smartProvider.ReadAllText("sp://RoboClerk/test.txt");

            // Assert
            Assert.That(content, Is.EqualTo("sharepoint content"));
            mockSharePointProvider.Received(1).ReadAllText("RoboClerk/test.txt");
        }

        [UnitTestAttribute(
            Identifier = "2DBDFAF6-4ABD-4A01-B81F-4116D6AAC349",
            Purpose = "Path prefix matching is case-insensitive",
            PostCondition = "SP:// routes to SharePoint provider")]
        [Test]
        public void ReadAllText_UppercasePrefix_RoutesToSharePointProvider()
        {
            // Arrange
            smartProvider.RegisterProvider(mockSharePointProvider);
            mockSharePointProvider.ReadAllText("test.txt").Returns("sharepoint content");

            // Act
            var content = smartProvider.ReadAllText("SP://test.txt");

            // Assert
            Assert.That(content, Is.EqualTo("sharepoint content"));
            mockSharePointProvider.Received(1).ReadAllText("test.txt");
        }

        [UnitTestAttribute(
            Identifier = "0DAEFDAA-3397-473B-9879-720CEB72B26F",
            Purpose = "GetFiles returns paths with prefix preserved for SharePoint",
            PostCondition = "Returned paths include sp:// prefix")]
        [Test]
        public void GetFiles_SharePointPath_PreservesPrefix()
        {
            // Arrange
            smartProvider.RegisterProvider(mockSharePointProvider);
            mockSharePointProvider.GetFiles("RoboClerk", "*", SearchOption.TopDirectoryOnly)
                .Returns(new[] { "RoboClerk/file1.txt", "RoboClerk/file2.txt" });

            // Act
            var files = smartProvider.GetFiles("sp://RoboClerk", "*", SearchOption.TopDirectoryOnly);

            // Assert
            Assert.That(files.Length, Is.EqualTo(2));
            Assert.That(files[0], Is.EqualTo("sp://RoboClerk/file1.txt"));
            Assert.That(files[1], Is.EqualTo("sp://RoboClerk/file2.txt"));
        }

        [UnitTestAttribute(
            Identifier = "13942A26-94C2-4EAD-B2A2-A60E643F1A8A",
            Purpose = "GetFiles returns paths without prefix for local files",
            PostCondition = "Returned paths have no prefix")]
        [Test]
        public void GetFiles_LocalPath_NoPrefix()
        {
            // Arrange
            smartProvider.RegisterProvider(mockSharePointProvider);

            // Act
            var files = smartProvider.GetFiles(TestingHelpers.ConvertFilePath(@"C:\local"), "*", SearchOption.AllDirectories);

            // Assert
            Assert.That(files.Length, Is.EqualTo(2));
            Assert.That(files.All(f => !f.StartsWith("sp://")), Is.True);
        }

        [Test]
        [Description("FileExists on local path routes to local provider")]
        public void FileExists_LocalPath_RoutesToLocalProvider()
        {
            // Arrange
            smartProvider.RegisterProvider(mockSharePointProvider);

            // Act
            var exists = smartProvider.FileExists(TestingHelpers.ConvertFilePath(@"C:\local\test.txt"));

            // Assert
            Assert.That(exists, Is.True);
            mockSharePointProvider.DidNotReceive().FileExists(Arg.Any<string>());
        }

        [Test]
        [Description("FileExists on SharePoint path routes to SharePoint provider")]
        public void FileExists_SharePointPath_RoutesToSharePointProvider()
        {
            // Arrange
            smartProvider.RegisterProvider(mockSharePointProvider);
            mockSharePointProvider.FileExists("RoboClerk/test.txt").Returns(true);

            // Act
            var exists = smartProvider.FileExists("sp://RoboClerk/test.txt");

            // Assert
            Assert.That(exists, Is.True);
            mockSharePointProvider.Received(1).FileExists("RoboClerk/test.txt");
        }

        [Test]
        [Description("WriteAllText routes correctly based on path prefix")]
        public void WriteAllText_WithPrefix_RoutesCorrectly()
        {
            // Arrange
            smartProvider.RegisterProvider(mockSharePointProvider);

            // Act - SharePoint
            smartProvider.WriteAllText("sp://RoboClerk/output.txt", "content");

            // Assert
            mockSharePointProvider.Received(1).WriteAllText("RoboClerk/output.txt", "content");
        }

        [Test]
        [Description("Combine preserves prefix from first path")]
        public void Combine_SharePointPath_PreservesPrefix()
        {
            // Arrange
            smartProvider.RegisterProvider(mockSharePointProvider);
            mockSharePointProvider.Combine("RoboClerk", "subdir", "file.txt")
                .Returns("RoboClerk/subdir/file.txt");

            // Act
            var combined = smartProvider.Combine("sp://RoboClerk", "subdir", "file.txt");

            // Assert
            Assert.That(combined, Is.EqualTo("sp://RoboClerk/subdir/file.txt"));
        }

        [Test]
        [Description("Combine with local path uses local provider")]
        public void Combine_LocalPath_UsesLocalProvider()
        {
            // Arrange
            smartProvider.RegisterProvider(mockSharePointProvider);

            // Act
            var combined = smartProvider.Combine(TestingHelpers.ConvertFilePath(@"C:\local"), "subdir", "file.txt");

            // Assert
            Assert.That(combined, Does.StartWith(TestingHelpers.ConvertFilePath(@"C:\local")));
            mockSharePointProvider.DidNotReceive().Combine(Arg.Any<string[]>());
        }

        [Test]
        [Description("GetFullPath preserves prefix for SharePoint paths")]
        public void GetFullPath_SharePointPath_PreservesPrefix()
        {
            // Arrange
            smartProvider.RegisterProvider(mockSharePointProvider);
            mockSharePointProvider.GetFullPath("RoboClerk/test.txt").Returns("/RoboClerk/test.txt");

            // Act
            var fullPath = smartProvider.GetFullPath("sp://RoboClerk/test.txt");

            // Assert
            Assert.That(fullPath, Is.EqualTo("sp:///RoboClerk/test.txt"));
        }

        [Test]
        [Description("GetDirectoryName preserves prefix for SharePoint paths")]
        public void GetDirectoryName_SharePointPath_PreservesPrefix()
        {
            // Arrange
            smartProvider.RegisterProvider(mockSharePointProvider);
            mockSharePointProvider.GetDirectoryName("RoboClerk/subdir/file.txt").Returns("RoboClerk/subdir");

            // Act
            var dirName = smartProvider.GetDirectoryName("sp://RoboClerk/subdir/file.txt");

            // Assert
            Assert.That(dirName, Is.EqualTo("sp://RoboClerk/subdir"));
        }

        [Test]
        [Description("GetFileName strips prefix and returns just filename")]
        public void GetFileName_SharePointPath_ReturnsFilename()
        {
            // Arrange
            smartProvider.RegisterProvider(mockSharePointProvider);
            mockSharePointProvider.GetFileName("RoboClerk/test.txt").Returns("test.txt");

            // Act
            var fileName = smartProvider.GetFileName("sp://RoboClerk/test.txt");

            // Assert
            Assert.That(fileName, Is.EqualTo("test.txt"));
        }

        [Test]
        [Description("CopyFile within same provider uses native copy")]
        public void CopyFile_SameProvider_UsesNativeCopy()
        {
            // Arrange
            smartProvider.RegisterProvider(mockSharePointProvider);

            // Act
            smartProvider.CopyFile("sp://source.txt", "sp://dest.txt", false);

            // Assert
            mockSharePointProvider.Received(1).CopyFile("source.txt", "dest.txt", false);
        }

        [Test]
        [Description("CopyFile across providers reads and writes")]
        public void CopyFile_DifferentProviders_ReadsAndWrites()
        {
            // Arrange
            smartProvider.RegisterProvider(mockSharePointProvider);
            mockSharePointProvider.ReadAllBytes("source.txt").Returns(new byte[] { 1, 2, 3 });

            // Act - Copy from SharePoint to local
            smartProvider.CopyFile("sp://source.txt", TestingHelpers.ConvertFilePath(@"C:\local\dest.txt"), false);

            // Assert
            mockSharePointProvider.Received(1).ReadAllBytes("source.txt");
            Assert.That(mockFileSystem.File.Exists(TestingHelpers.ConvertFilePath(@"C:\local\dest.txt")), Is.True);
        }

        [Test]
        [Description("MoveFile within same provider uses native move")]
        public void MoveFile_SameProvider_UsesNativeMove()
        {
            // Arrange
            smartProvider.RegisterProvider(mockSharePointProvider);

            // Act
            smartProvider.MoveFile("sp://source.txt", "sp://dest.txt", false);

            // Assert
            mockSharePointProvider.Received(1).MoveFile("source.txt", "dest.txt", false);
        }

        [Test]
        [Description("MoveFile across providers copies then deletes")]
        public void MoveFile_DifferentProviders_CopiesAndDeletes()
        {
            // Arrange
            smartProvider.RegisterProvider(mockSharePointProvider);
            mockSharePointProvider.ReadAllBytes("source.txt").Returns(new byte[] { 1, 2, 3 });

            // Act - Move from SharePoint to local
            smartProvider.MoveFile("sp://source.txt", TestingHelpers.ConvertFilePath(@"C:\local\dest.txt"), false);

            // Assert
            mockSharePointProvider.Received(1).ReadAllBytes("source.txt");
            mockSharePointProvider.Received(1).DeleteFile("source.txt");
            Assert.That(mockFileSystem.File.Exists(TestingHelpers.ConvertFilePath(@"C:\local\dest.txt")), Is.True);
        }

        [Test]
        [Description("GetRelativePath requires both paths use same provider")]
        public void GetRelativePath_DifferentProviders_ThrowsException()
        {
            // Arrange
            smartProvider.RegisterProvider(mockSharePointProvider);

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => 
                smartProvider.GetRelativePath("sp://base", TestingHelpers.ConvertFilePath(@"C:\local\file.txt")));
            Assert.That(ex.Message, Does.Contain("different file providers"));
        }

        [Test]
        [Description("GetRelativePath works within same provider")]
        public void GetRelativePath_SameProvider_ReturnsRelativePath()
        {
            // Arrange
            smartProvider.RegisterProvider(mockSharePointProvider);
            mockSharePointProvider.GetRelativePath("RoboClerk", "RoboClerk/subdir/file.txt")
                .Returns("subdir/file.txt");

            // Act
            var relativePath = smartProvider.GetRelativePath("sp://RoboClerk", "sp://RoboClerk/subdir/file.txt");

            // Assert
            Assert.That(relativePath, Is.EqualTo("subdir/file.txt"));
        }

        [Test]
        [Description("Backslash paths are normalized to forward slashes")]
        public void ReadAllText_BackslashInSharePointPath_Normalized()
        {
            // Arrange
            smartProvider.RegisterProvider(mockSharePointProvider);
            mockSharePointProvider.ReadAllText("RoboClerk/test.txt").Returns("content");

            // Act
            var content = smartProvider.ReadAllText(@"sp://RoboClerk\test.txt");

            // Assert
            Assert.That(content, Is.EqualTo("content"));
            mockSharePointProvider.Received(1).ReadAllText("RoboClerk/test.txt");
        }

        [Test]
        [Description("Empty path throws ArgumentException")]
        public void ReadAllText_EmptyPath_ThrowsException()
        {
            // Arrange
            smartProvider.RegisterProvider(mockSharePointProvider);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => smartProvider.ReadAllText(""));
        }

        [Test]
        [Description("Null path throws ArgumentException")]
        public void ReadAllText_NullPath_ThrowsException()
        {
            // Arrange
            smartProvider.RegisterProvider(mockSharePointProvider);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => smartProvider.ReadAllText(null));
        }

        [Test]
        [Description("GetDirectories returns directories with prefix preserved")]
        public void GetDirectories_SharePointPath_PreservesPrefix()
        {
            // Arrange
            smartProvider.RegisterProvider(mockSharePointProvider);
            mockSharePointProvider.GetDirectories("RoboClerk", "*", SearchOption.TopDirectoryOnly)
                .Returns(new[] { "RoboClerk/dir1", "RoboClerk/dir2" });

            // Act
            var dirs = smartProvider.GetDirectories("sp://RoboClerk", "*", SearchOption.TopDirectoryOnly);

            // Assert
            Assert.That(dirs.Length, Is.EqualTo(2));
            Assert.That(dirs[0], Is.EqualTo("sp://RoboClerk/dir1"));
            Assert.That(dirs[1], Is.EqualTo("sp://RoboClerk/dir2"));
        }

        [Test]
        [Description("Multiple specialized providers can be registered")]
        public void RegisterProvider_MultipleProviders_Success()
        {
            // Arrange
            var azureProvider = Substitute.For<IFileProviderPlugin>();
            azureProvider.GetPathPrefix().Returns("azure://");
            azureProvider.Name.Returns("AzureBlobProvider");
            azureProvider.ReadAllText("container/file.txt").Returns("azure content");

            var s3Provider = Substitute.For<IFileProviderPlugin>();
            s3Provider.GetPathPrefix().Returns("s3://");
            s3Provider.Name.Returns("S3Provider");
            s3Provider.ReadAllText("bucket/file.txt").Returns("s3 content");

            // Act
            smartProvider.RegisterProvider(mockSharePointProvider);
            smartProvider.RegisterProvider(azureProvider);
            smartProvider.RegisterProvider(s3Provider);

            mockSharePointProvider.ReadAllText("sp-file.txt").Returns("sp content");

            // Assert
            Assert.That(smartProvider.ReadAllText("sp://sp-file.txt"), Is.EqualTo("sp content"));
            Assert.That(smartProvider.ReadAllText("azure://container/file.txt"), Is.EqualTo("azure content"));
            Assert.That(smartProvider.ReadAllText("s3://bucket/file.txt"), Is.EqualTo("s3 content"));
        }
    }
}
