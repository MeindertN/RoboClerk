using NUnit.Framework;
using NSubstitute;
using RoboClerk.SourceProviders;
using RoboClerk.Core.FileProviders;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("Tests for LocalSourceProvider")]
    public class TestLocalSourceProvider
    {
        private IFileProviderPlugin mockFileProvider;

        [SetUp]
        public void Setup()
        {
            mockFileProvider = Substitute.For<IFileProviderPlugin>();
        }

        [UnitTestAttribute(
            Identifier = "a1234567-89ab-cdef-0123-456789abcdef",
            Purpose = "Verify LocalSourceProvider prepares source successfully when directory exists",
            PostCondition = "Returns the resolved path")]
        [Test]
        public async Task PrepareSourceAsync_DirectoryExists_ReturnsResolvedPath()
        {
            // Arrange
            var directoryPath = "/project/src";
            var projectRoot = "/project";
            mockFileProvider.DirectoryExists(Arg.Any<string>()).Returns(true);
            mockFileProvider.GetFullPath(Arg.Any<string>()).Returns(x => x.Arg<string>());

            var provider = new LocalSourceProvider(directoryPath, projectRoot, mockFileProvider);

            // Act
            var result = await provider.PrepareSourceAsync();

            // Assert
            Assert.That(result, Is.Not.Empty);
            Assert.That(provider.IsPrepared, Is.True);
        }

        [UnitTestAttribute(
            Identifier = "b2345678-9abc-def0-1234-56789abcdef0",
            Purpose = "Verify LocalSourceProvider throws when directory does not exist",
            PostCondition = "DirectoryNotFoundException is thrown")]
        [Test]
        public void PrepareSourceAsync_DirectoryNotExists_ThrowsDirectoryNotFoundException()
        {
            // Arrange
            var directoryPath = "/project/nonexistent";
            var projectRoot = "/project";
            mockFileProvider.DirectoryExists(Arg.Any<string>()).Returns(false);
            mockFileProvider.GetFullPath(Arg.Any<string>()).Returns(x => x.Arg<string>());

            var provider = new LocalSourceProvider(directoryPath, projectRoot, mockFileProvider);

            // Act & Assert
            Assert.ThrowsAsync<System.IO.DirectoryNotFoundException>(async () => 
                await provider.PrepareSourceAsync());
        }

        [UnitTestAttribute(
            Identifier = "c3456789-abcd-ef01-2345-6789abcdef01",
            Purpose = "Verify LocalSourceProvider resolves PROJECTROOT placeholder",
            PostCondition = "Path with placeholder is resolved correctly")]
        [Test]
        public async Task PrepareSourceAsync_WithProjectRootPlaceholder_ResolvesCorrectly()
        {
            // Arrange
            var directoryPath = "{PROJECTROOT}src";
            var projectRoot = "/project/";
            mockFileProvider.DirectoryExists(Arg.Any<string>()).Returns(true);
            mockFileProvider.GetFullPath(Arg.Is<string>(s => s.Contains("/project/src"))).Returns("/project/src");

            var provider = new LocalSourceProvider(directoryPath, projectRoot, mockFileProvider);

            // Act
            var result = await provider.PrepareSourceAsync();

            // Assert
            Assert.That(result, Does.Contain("src"));
        }

        [UnitTestAttribute(
            Identifier = "d4567890-bcde-f012-3456-789abcdef012",
            Purpose = "Verify LocalSourceProvider.RequiresCleanup returns false",
            PostCondition = "RequiresCleanup is false since local directories should not be cleaned up")]
        [Test]
        public void RequiresCleanup_LocalSource_ReturnsFalse()
        {
            // Arrange
            var provider = new LocalSourceProvider("/path", "/root", mockFileProvider);

            // Act & Assert
            Assert.That(provider.RequiresCleanup, Is.False);
        }

        [UnitTestAttribute(
            Identifier = "e5678901-cdef-0123-4567-89abcdef0123",
            Purpose = "Verify LocalSourceProvider.IsPrepared is false before PrepareSourceAsync",
            PostCondition = "IsPrepared is false initially")]
        [Test]
        public void IsPrepared_BeforePrepare_ReturnsFalse()
        {
            // Arrange
            var provider = new LocalSourceProvider("/path", "/root", mockFileProvider);

            // Act & Assert
            Assert.That(provider.IsPrepared, Is.False);
        }

        [UnitTestAttribute(
            Identifier = "f6789012-def0-1234-5678-9abcdef01234",
            Purpose = "Verify GetScanPath throws when called before PrepareSourceAsync",
            PostCondition = "InvalidOperationException is thrown")]
        [Test]
        public void GetScanPath_BeforePrepare_ThrowsInvalidOperationException()
        {
            // Arrange
            var provider = new LocalSourceProvider("/path", "/root", mockFileProvider);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => provider.GetScanPath());
        }

        [UnitTestAttribute(
            Identifier = "a7890123-ef01-2345-6789-abcdef012345",
            Purpose = "Verify SourceIdentifier contains local prefix and path",
            PostCondition = "SourceIdentifier starts with 'local:' and contains the path")]
        [Test]
        public async Task SourceIdentifier_AfterPrepare_ContainsLocalPrefix()
        {
            // Arrange
            var directoryPath = "/project/src";
            mockFileProvider.DirectoryExists(Arg.Any<string>()).Returns(true);
            mockFileProvider.GetFullPath(Arg.Any<string>()).Returns(directoryPath);

            var provider = new LocalSourceProvider(directoryPath, "/project", mockFileProvider);
            await provider.PrepareSourceAsync();

            // Act
            var identifier = provider.SourceIdentifier;

            // Assert
            Assert.That(identifier, Does.StartWith("local:"));
        }

        [UnitTestAttribute(
            Identifier = "b8901234-f012-3456-789a-bcdef0123456",
            Purpose = "Verify LocalSourceProvider can be disposed multiple times safely",
            PostCondition = "No exception is thrown on multiple dispose calls")]
        [Test]
        public void Dispose_MultipleTimes_NoException()
        {
            // Arrange
            var provider = new LocalSourceProvider("/path", "/root", mockFileProvider);

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                provider.Dispose();
                provider.Dispose();
            });
        }

        [UnitTestAttribute(
            Identifier = "c9012345-0123-4567-89ab-cdef01234567",
            Purpose = "Verify PrepareSourceAsync throws ObjectDisposedException after dispose",
            PostCondition = "ObjectDisposedException is thrown")]
        [Test]
        public void PrepareSourceAsync_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var provider = new LocalSourceProvider("/path", "/root", mockFileProvider);
            provider.Dispose();

            // Act & Assert
            Assert.ThrowsAsync<ObjectDisposedException>(async () => 
                await provider.PrepareSourceAsync());
        }
    }
}
