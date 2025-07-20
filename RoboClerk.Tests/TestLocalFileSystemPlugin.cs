using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using RoboClerk.Configuration;

namespace RoboClerk.Tests
{
    [TestFixture]
    public class TestLocalFileSystemPlugin
    {
        private MockFileSystem _mockFileSystem;
        private IFileProviderPlugin _plugin;

        [SetUp]
        public void Setup()
        {
            _mockFileSystem = new MockFileSystem();
            _plugin = new LocalFileSystemPlugin(_mockFileSystem);
        }

        [UnitTestAttribute(
        Identifier = "A1B2CVD4-E5F6-7890-ABCD-EF1234567890",
        Purpose = "LocalFileSystemPlugin constructor throws ArgumentNullException when IFileSystem is null",
        PostCondition = "ArgumentNullException is thrown with appropriate parameter name")]
        [Test]
        public void Constructor_WithNullFileSystem_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new LocalFileSystemPlugin(null));
        }

        [UnitTestAttribute(
        Identifier = "B2C3D4E5-F6A7-8901-BCDE-F23456789012",
        Purpose = "FileExists returns true when file exists in mock file system",
        PostCondition = "Method returns true for existing file")]
        [Test]
        public void FileExists_WhenFileExists_ReturnsTrue()
        {
            // Arrange
            var filePath = TestingHelpers.ConvertFilePath("C:\\test\\file.txt");
            _mockFileSystem.AddFile(filePath, "test content");

            // Act
            var result = _plugin.FileExists(filePath);

            // Assert
            Assert.That(result, Is.True);
        }

        [UnitTestAttribute(
        Identifier = "C3D4E5F6-A789-0123-CDEF-34567890123A",
        Purpose = "FileExists returns false when file does not exist in mock file system",
        PostCondition = "Method returns false for non-existing file")]
        [Test]
        public void FileExists_WhenFileDoesNotExist_ReturnsFalse()
        {
            // Arrange
            var filePath = TestingHelpers.ConvertFilePath("C:\\test\\nonexistent.txt");

            // Act
            var result = _plugin.FileExists(filePath);

            // Assert
            Assert.That(result, Is.False);
        }

        [UnitTestAttribute(
        Identifier = "D4E5F6A7-B890-1234-DEF0-4567890123AB",
        Purpose = "DirectoryExists returns true when directory exists in mock file system",
        PostCondition = "Method returns true for existing directory")]
        [Test]
        public void DirectoryExists_WhenDirectoryExists_ReturnsTrue()
        {
            // Arrange
            var directoryPath = TestingHelpers.ConvertFilePath("C:\\test\\subdir");
            _mockFileSystem.AddDirectory(directoryPath);

            // Act
            var result = _plugin.DirectoryExists(directoryPath);

            // Assert
            Assert.That(result, Is.True);
        }

        [UnitTestAttribute(
        Identifier = "E5F6A7B8-C901-2345-EF01-567890123ABC",
        Purpose = "DirectoryExists returns false when directory does not exist in mock file system",
        PostCondition = "Method returns false for non-existing directory")]
        [Test]
        public void DirectoryExists_WhenDirectoryDoesNotExist_ReturnsFalse()
        {
            // Arrange
            var directoryPath = TestingHelpers.ConvertFilePath("C:\\test\\nonexistent");

            // Act
            var result = _plugin.DirectoryExists(directoryPath);

            // Assert
            Assert.That(result, Is.False);
        }

        [UnitTestAttribute(
        Identifier = "F6A7B8C9-D012-3456-F012-67890123ABCD",
        Purpose = "ReadAllText returns file content when file exists in mock file system",
        PostCondition = "Method returns correct file content")]
        [Test]
        public void ReadAllText_WhenFileExists_ReturnsContent()
        {
            // Arrange
            var filePath = TestingHelpers.ConvertFilePath("C:\\test\\file.txt");
            var expectedContent = "test content";
            _mockFileSystem.AddFile(filePath, expectedContent);

            // Act
            var result = _plugin.ReadAllText(filePath);

            // Assert
            Assert.That(result, Is.EqualTo(expectedContent));
        }

        [UnitTestAttribute(
        Identifier = "A7B8C9D0-E123-4567-A123-7890123ABCDE",
        Purpose = "ReadAllText throws FileNotFoundException when file does not exist",
        PostCondition = "FileNotFoundException is thrown with appropriate message")]
        [Test]
        public void ReadAllText_WhenFileDoesNotExist_ThrowsFileNotFoundException()
        {
            // Arrange
            var filePath = TestingHelpers.ConvertFilePath("C:\\test\\nonexistent.txt");

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => _plugin.ReadAllText(filePath));
        }

        [UnitTestAttribute(
        Identifier = "B8C9D0E1-F234-5678-B234-890123ABCDEF",
        Purpose = "WriteAllText creates file with content in mock file system",
        PostCondition = "File is created with correct content")]
        [Test]
        public void WriteAllText_CreatesFileWithContent()
        {
            // Arrange
            var filePath = TestingHelpers.ConvertFilePath("C:\\test\\newfile.txt");
            var content = "new content";

            // Act
            _plugin.WriteAllText(filePath, content);

            // Assert
            Assert.That(_mockFileSystem.FileExists(filePath), Is.True);
            Assert.That(_mockFileSystem.GetFile(filePath).TextContents, Is.EqualTo(content));
        }

        [UnitTestAttribute(
        Identifier = "C9D0E1F2-A345-6789-C345-90123ABCDEF0",
        Purpose = "WriteAllText creates directory structure if it does not exist",
        PostCondition = "Directory structure is created before writing file")]
        [Test]
        public void WriteAllText_CreatesDirectoryIfNotExists()
        {
            // Arrange
            var filePath = TestingHelpers.ConvertFilePath("C:\\newdir\\subdir\\file.txt");
            var content = "test content";

            // Act
            _plugin.WriteAllText(filePath, content);

            // Assert
            Assert.That(_mockFileSystem.Directory.Exists(TestingHelpers.ConvertFilePath("C:\\newdir")), Is.True);
            Assert.That(_mockFileSystem.Directory.Exists(TestingHelpers.ConvertFilePath("C:\\newdir\\subdir")), Is.True);
            Assert.That(_mockFileSystem.FileExists(filePath), Is.True);
        }

        [UnitTestAttribute(
        Identifier = "D0E1F2A3-B456-789A-D456-0123ABCDEF01",
        Purpose = "CreateDirectory creates directory in mock file system",
        PostCondition = "Directory is created successfully")]
        [Test]
        public void CreateDirectory_CreatesDirectory()
        {
            // Arrange
            var directoryPath = TestingHelpers.ConvertFilePath("C:\\test\\newdir");

            // Act
            _plugin.CreateDirectory(directoryPath);

            // Assert
            Assert.That(_mockFileSystem.Directory.Exists(directoryPath), Is.True);
        }

        [UnitTestAttribute(
        Identifier = "E1F2A3B4-C567-89AB-E567-123ABCDEF012",
        Purpose = "DeleteFile removes file from mock file system when file exists",
        PostCondition = "File is deleted successfully")]
        [Test]
        public void DeleteFile_WhenFileExists_DeletesFile()
        {
            // Arrange
            var filePath = TestingHelpers.ConvertFilePath("C:\\test\\file.txt");
            _mockFileSystem.AddFile(filePath, "test content");

            // Act
            _plugin.DeleteFile(filePath);

            // Assert
            Assert.That(_mockFileSystem.FileExists(filePath), Is.False);
        }

        [UnitTestAttribute(
        Identifier = "F2A3B4C5-D678-9ABC-F678-234ABCDEF013",
        Purpose = "DeleteFile throws FileNotFoundException when file does not exist",
        PostCondition = "FileNotFoundException is thrown with appropriate message")]
        [Test]
        public void DeleteFile_WhenFileDoesNotExist_ThrowsFileNotFoundException()
        {
            // Arrange
            var filePath = TestingHelpers.ConvertFilePath("C:\\test\\nonexistent.txt");

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => _plugin.DeleteFile(filePath));
        }

        [UnitTestAttribute(
        Identifier = "A3B4C5D6-E789-ABCD-0789-345ABCDEF014",
        Purpose = "GetFiles returns matching files based on search pattern",
        PostCondition = "Only files matching the pattern are returned")]
        [Test]
        public void GetFiles_ReturnsMatchingFiles()
        {
            // Arrange
            var directory = TestingHelpers.ConvertFilePath("C:\\test");
            _mockFileSystem.AddFile(TestingHelpers.ConvertFilePath("C:\\test\\file1.txt"), "content1");
            _mockFileSystem.AddFile(TestingHelpers.ConvertFilePath("C:\\test\\file2.txt"), "content2");
            _mockFileSystem.AddFile(TestingHelpers.ConvertFilePath("C:\\test\\file3.doc"), "content3");

            // Act
            var result = _plugin.GetFiles(directory, "*.txt");

            // Assert
            Assert.That(result.Length, Is.EqualTo(2));
            Assert.That(result, Contains.Item(TestingHelpers.ConvertFilePath("C:\\test\\file1.txt")));
            Assert.That(result, Contains.Item(TestingHelpers.ConvertFilePath("C:\\test\\file2.txt")));
        }

        [UnitTestAttribute(
        Identifier = "B4C5D6E7-F890-1234-B890-456789ABCDEF",
        Purpose = "GetFileSize returns correct file size from mock file system",
        PostCondition = "Method returns correct file size in bytes")]
        [Test]
        public void GetFileSize_ReturnsCorrectSize()
        {
            // Arrange
            var filePath = TestingHelpers.ConvertFilePath("C:\\test\\file.txt");
            var content = "test content";
            _mockFileSystem.AddFile(filePath, content);

            // Act
            var result = _plugin.GetFileSize(filePath);

            // Assert
            Assert.That(result, Is.EqualTo(content.Length));
        }

        [UnitTestAttribute(
        Identifier = "C5D6E7F8-9012-3456-C901-567890ABCDEF",
        Purpose = "Combine correctly combines multiple path segments",
        PostCondition = "Method returns correctly combined path")]
        [Test]
        public void Combine_CombinesPathsCorrectly()
        {
            // Arrange
            var path1 = TestingHelpers.ConvertFilePath("C:\\test");
            var path2 = "subdir";
            var path3 = "file.txt";

            // Act
            var result = _plugin.Combine(path1, path2, path3);

            // Assert
            Assert.That(result, Is.EqualTo(TestingHelpers.ConvertFilePath("C:\\test\\subdir\\file.txt")));
        }

        [UnitTestAttribute(
        Identifier = "D6E7F8G9-0123-4567-D012-678901ABCDEF",
        Purpose = "GetFileName returns file name from full path",
        PostCondition = "Method returns correct file name")]
        [Test]
        public void GetFileName_ReturnsFileName()
        {
            // Arrange
            var filePath = TestingHelpers.ConvertFilePath("C:\\test\\subdir\\file.txt");

            // Act
            var result = _plugin.GetFileName(filePath);

            // Assert
            Assert.That(result, Is.EqualTo("file.txt"));
        }

        [UnitTestAttribute(
        Identifier = "E7F8G9H0-1234-5678-E123-456789ABCDEF",
        Purpose = "GetDirectoryName returns directory name from full path",
        PostCondition = "Method returns correct directory name")]
        [Test]
        public void GetDirectoryName_ReturnsDirectoryName()
        {
            // Arrange
            var filePath = TestingHelpers.ConvertFilePath("C:\\test\\subdir\\file.txt");

            // Act
            var result = _plugin.GetDirectoryName(filePath);

            // Assert
            Assert.That(result, Is.EqualTo(TestingHelpers.ConvertFilePath("C:\\test\\subdir")));
        }

        [UnitTestAttribute(
        Identifier = "F8G9H0I1-2345-6789-F234-567890ABCDEF",
        Purpose = "ConfigureServices registers IFileSystem in service collection",
        PostCondition = "IFileSystem is registered and can be resolved from service provider")]
        [Test]
        public void ConfigureServices_RegistersFileSystem()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            _plugin.ConfigureServices(services);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var fileSystem = serviceProvider.GetService<IFileSystem>();
            Assert.That(fileSystem, Is.Not.Null);
        }
    }
} 