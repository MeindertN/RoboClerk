using NUnit.Framework;
using NSubstitute;
using RoboClerk.SourceProviders;
using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Collections.Generic;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("Tests for TempDirectoryManager")]
    public class TestTempDirectoryManager
    {
        private MockFileSystem mockFileSystem;

        [SetUp]
        public void Setup()
        {
            mockFileSystem = new MockFileSystem();
        }

        [UnitTestAttribute(
            Identifier = "d0123456-1234-5678-9abc-def012345678",
            Purpose = "Verify TempDirectoryManager creates base temp directory on initialization",
            PostCondition = "Base temp directory exists")]
        [Test]
        public void Constructor_CreatesBaseTempDirectory()
        {
            // Arrange & Act
            using var manager = new TempDirectoryManager(mockFileSystem);

            // Assert
            Assert.That(mockFileSystem.Directory.Exists(manager.BaseTempPath), Is.True);
        }

        [UnitTestAttribute(
            Identifier = "e1234567-2345-6789-abcd-ef0123456789",
            Purpose = "Verify CreateTempDirectory creates a directory with the given prefix",
            PostCondition = "Directory is created and path contains prefix")]
        [Test]
        public void CreateTempDirectory_WithPrefix_CreatesDirectoryWithPrefix()
        {
            // Arrange
            using var manager = new TempDirectoryManager(mockFileSystem);

            // Act
            var tempDir = manager.CreateTempDirectory("test_prefix");

            // Assert
            Assert.That(mockFileSystem.Directory.Exists(tempDir), Is.True);
            Assert.That(tempDir, Does.Contain("test_prefix"));
        }

        [UnitTestAttribute(
            Identifier = "f2345678-3456-789a-bcde-f01234567890",
            Purpose = "Verify CreateTempDirectory registers directory for cleanup",
            PostCondition = "Directory is in RegisteredDirectories list")]
        [Test]
        public void CreateTempDirectory_RegistersForCleanup()
        {
            // Arrange
            using var manager = new TempDirectoryManager(mockFileSystem);

            // Act
            var tempDir = manager.CreateTempDirectory("test");

            // Assert
            Assert.That(manager.RegisteredDirectories, Does.Contain(tempDir));
        }

        [UnitTestAttribute(
            Identifier = "a3456789-4567-89ab-cdef-012345678901",
            Purpose = "Verify Cleanup removes the specified directory",
            PostCondition = "Directory is deleted and removed from registered list")]
        [Test]
        public void Cleanup_RemovesDirectory()
        {
            // Arrange
            using var manager = new TempDirectoryManager(mockFileSystem);
            var tempDir = manager.CreateTempDirectory("test");

            // Act
            var result = manager.Cleanup(tempDir);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(mockFileSystem.Directory.Exists(tempDir), Is.False);
            Assert.That(manager.RegisteredDirectories, Does.Not.Contain(tempDir));
        }

        [UnitTestAttribute(
            Identifier = "b4567890-5678-9abc-def0-123456789012",
            Purpose = "Verify Cleanup returns false for non-existent directory",
            PostCondition = "Returns false without throwing")]
        [Test]
        public void Cleanup_NonExistentDirectory_ReturnsFalse()
        {
            // Arrange
            using var manager = new TempDirectoryManager(mockFileSystem);

            // Act
            var result = manager.Cleanup("/nonexistent/directory");

            // Assert
            Assert.That(result, Is.False);
        }

        [UnitTestAttribute(
            Identifier = "c5678901-6789-abcd-ef01-234567890123",
            Purpose = "Verify CleanupAll removes all registered directories",
            PostCondition = "All directories are deleted and list is empty")]
        [Test]
        public void CleanupAll_RemovesAllDirectories()
        {
            // Arrange
            using var manager = new TempDirectoryManager(mockFileSystem);
            var dir1 = manager.CreateTempDirectory("test1");
            var dir2 = manager.CreateTempDirectory("test2");

            // Act
            manager.CleanupAll();

            // Assert
            Assert.That(mockFileSystem.Directory.Exists(dir1), Is.False);
            Assert.That(mockFileSystem.Directory.Exists(dir2), Is.False);
            Assert.That(manager.RegisteredDirectories, Is.Empty);
        }

        [UnitTestAttribute(
            Identifier = "d6789012-789a-bcde-f012-345678901234",
            Purpose = "Verify RegisterForCleanup adds directory to list without creating it",
            PostCondition = "Directory is in list but not created")]
        [Test]
        public void RegisterForCleanup_AddsToListWithoutCreating()
        {
            // Arrange
            using var manager = new TempDirectoryManager(mockFileSystem);
            var externalDir = TestingHelpers.ConvertFilePath("/external/dir");

            // Act
            manager.RegisterForCleanup(externalDir);

            // Assert
            Assert.That(manager.RegisteredDirectories, Does.Contain(externalDir));
            Assert.That(mockFileSystem.Directory.Exists(externalDir), Is.False);
        }

        [UnitTestAttribute(
            Identifier = "e7890123-89ab-cdef-0123-456789012345",
            Purpose = "Verify RegisterForCleanup does not add duplicate entries",
            PostCondition = "Directory appears only once in list")]
        [Test]
        public void RegisterForCleanup_DuplicateEntry_OnlyAddsOnce()
        {
            // Arrange
            using var manager = new TempDirectoryManager(mockFileSystem);
            var tempDir = manager.CreateTempDirectory("test");

            // Act
            manager.RegisterForCleanup(tempDir); // Already registered by CreateTempDirectory

            // Assert
            var count = 0;
            foreach (var dir in manager.RegisteredDirectories)
            {
                if (dir == tempDir) count++;
            }
            Assert.That(count, Is.EqualTo(1));
        }

        [UnitTestAttribute(
            Identifier = "f8901234-9abc-def0-1234-567890123456",
            Purpose = "Verify Dispose calls CleanupAll",
            PostCondition = "All directories are cleaned up on dispose")]
        [Test]
        public void Dispose_CleansUpAllDirectories()
        {
            // Arrange
            var manager = new TempDirectoryManager(mockFileSystem);
            var dir1 = manager.CreateTempDirectory("test1");
            var dir2 = manager.CreateTempDirectory("test2");

            // Act
            manager.Dispose();

            // Assert
            Assert.That(mockFileSystem.Directory.Exists(dir1), Is.False);
            Assert.That(mockFileSystem.Directory.Exists(dir2), Is.False);
        }

        [UnitTestAttribute(
            Identifier = "a9012345-abcd-ef01-2345-678901234567",
            Purpose = "Verify CreateTempDirectory throws after dispose",
            PostCondition = "ObjectDisposedException is thrown")]
        [Test]
        public void CreateTempDirectory_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var manager = new TempDirectoryManager(mockFileSystem);
            manager.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => manager.CreateTempDirectory("test"));
        }

        [UnitTestAttribute(
            Identifier = "b0123456-bcde-f012-3456-789012345678",
            Purpose = "Verify custom base path is used when provided",
            PostCondition = "BaseTempPath matches the custom path")]
        [Test]
        public void Constructor_WithCustomBasePath_UsesCustomPath()
        {
            // Arrange
            var customPath = TestingHelpers.ConvertFilePath("/custom/temp/path");

            // Act
            using var manager = new TempDirectoryManager(mockFileSystem, customPath);

            // Assert
            Assert.That(manager.BaseTempPath, Is.EqualTo(customPath));
            Assert.That(mockFileSystem.Directory.Exists(customPath), Is.True);
        }

        [UnitTestAttribute(
            Identifier = "c1234567-cdef-0123-4567-890123456789",
            Purpose = "Verify RegisterForCleanup throws for null or empty directory",
            PostCondition = "ArgumentException is thrown")]
        [Test]
        public void RegisterForCleanup_NullOrEmpty_ThrowsArgumentException()
        {
            // Arrange
            using var manager = new TempDirectoryManager(mockFileSystem);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => manager.RegisterForCleanup(null));
            Assert.Throws<ArgumentException>(() => manager.RegisterForCleanup(""));
            Assert.Throws<ArgumentException>(() => manager.RegisterForCleanup("   "));
        }
    }
}
