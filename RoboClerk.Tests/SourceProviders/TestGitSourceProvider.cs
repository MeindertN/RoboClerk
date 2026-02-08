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
    [Description("Tests for GitSourceProvider")]
    public class TestGitSourceProvider
    {
        private IGitCloneService mockCloneService;
        private ITempDirectoryManager mockTempManager;
        private IFileProviderPlugin mockFileProvider;

        [SetUp]
        public void Setup()
        {
            mockCloneService = Substitute.For<IGitCloneService>();
            mockTempManager = Substitute.For<ITempDirectoryManager>();
            mockFileProvider = Substitute.For<IFileProviderPlugin>();
        }

        [UnitTestAttribute(
            Identifier = "a7890123-cdef-0123-4567-456789012345",
            Purpose = "Verify GitSourceProvider.RequiresCleanup returns true",
            PostCondition = "RequiresCleanup is true since cloned repos should be cleaned up")]
        [Test]
        public void RequiresCleanup_ReturnsTrue()
        {
            // Arrange
            var config = new GitSourceConfiguration
            {
                RepositoryUrl = "https://github.com/user/repo.git"
            };
            var provider = new GitSourceProvider(config, mockCloneService, mockTempManager, mockFileProvider);

            // Act & Assert
            Assert.That(provider.RequiresCleanup, Is.True);
        }

        [UnitTestAttribute(
            Identifier = "b8901234-def0-1234-5678-567890123456",
            Purpose = "Verify GitSourceProvider.IsPrepared is false before PrepareSourceAsync",
            PostCondition = "IsPrepared is false initially")]
        [Test]
        public void IsPrepared_BeforePrepare_ReturnsFalse()
        {
            // Arrange
            var config = new GitSourceConfiguration
            {
                RepositoryUrl = "https://github.com/user/repo.git"
            };
            var provider = new GitSourceProvider(config, mockCloneService, mockTempManager, mockFileProvider);

            // Act & Assert
            Assert.That(provider.IsPrepared, Is.False);
        }

        [UnitTestAttribute(
            Identifier = "c9012345-ef01-2345-6789-678901234567",
            Purpose = "Verify PrepareSourceAsync clones repository and returns scan path",
            PostCondition = "Repository is cloned and scan path is returned")]
        [Test]
        public async Task PrepareSourceAsync_ClonesRepository_ReturnsScanPath()
        {
            // Arrange
            var config = new GitSourceConfiguration
            {
                RepositoryUrl = "https://github.com/user/repo.git",
                SourcePath = "src"
            };
            
            var tempDir = "/tmp/git_repo_123";
            var expectedScanPath = "/tmp/git_repo_123/src";
            
            mockTempManager.CreateTempDirectory(Arg.Any<string>()).Returns(tempDir);
            mockCloneService.CloneRepositoryAsync(Arg.Any<GitSourceConfiguration>(), tempDir, Arg.Any<CancellationToken>())
                .Returns(expectedScanPath);
            mockFileProvider.DirectoryExists(expectedScanPath).Returns(true);

            var provider = new GitSourceProvider(config, mockCloneService, mockTempManager, mockFileProvider);

            // Act
            var result = await provider.PrepareSourceAsync();

            // Assert
            Assert.That(result, Is.EqualTo(expectedScanPath));
            Assert.That(provider.IsPrepared, Is.True);
            await mockCloneService.Received(1).CloneRepositoryAsync(Arg.Any<GitSourceConfiguration>(), tempDir, Arg.Any<CancellationToken>());
        }

        [UnitTestAttribute(
            Identifier = "d0123456-f012-3456-789a-789012345678",
            Purpose = "Verify PrepareSourceAsync throws when clone fails",
            PostCondition = "Exception is propagated and temp directory is cleaned up")]
        [Test]
        public void PrepareSourceAsync_CloneFails_ThrowsAndCleansUp()
        {
            // Arrange
            var config = new GitSourceConfiguration
            {
                RepositoryUrl = "https://github.com/user/repo.git"
            };
            
            var tempDir = "/tmp/git_repo_123";
            mockTempManager.CreateTempDirectory(Arg.Any<string>()).Returns(tempDir);
            mockCloneService.CloneRepositoryAsync(Arg.Any<GitSourceConfiguration>(), tempDir, Arg.Any<CancellationToken>())
                .Returns<Task<string>>(x => throw new InvalidOperationException("Clone failed"));

            var provider = new GitSourceProvider(config, mockCloneService, mockTempManager, mockFileProvider);

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await provider.PrepareSourceAsync());
            mockTempManager.Received(1).Cleanup(tempDir);
        }

        [UnitTestAttribute(
            Identifier = "e1234567-0123-4567-89ab-890123456789",
            Purpose = "Verify GetScanPath throws when called before PrepareSourceAsync",
            PostCondition = "InvalidOperationException is thrown")]
        [Test]
        public void GetScanPath_BeforePrepare_ThrowsInvalidOperationException()
        {
            // Arrange
            var config = new GitSourceConfiguration
            {
                RepositoryUrl = "https://github.com/user/repo.git"
            };
            var provider = new GitSourceProvider(config, mockCloneService, mockTempManager, mockFileProvider);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => provider.GetScanPath());
        }

        [UnitTestAttribute(
            Identifier = "f2345678-1234-5678-9abc-901234567890",
            Purpose = "Verify SourceIdentifier contains git prefix and repository info",
            PostCondition = "SourceIdentifier contains 'git:' and URL")]
        [Test]
        public void SourceIdentifier_ContainsGitPrefixAndRepoInfo()
        {
            // Arrange
            var config = new GitSourceConfiguration
            {
                RepositoryUrl = "https://github.com/user/repo.git",
                Branch = "main"
            };
            var provider = new GitSourceProvider(config, mockCloneService, mockTempManager, mockFileProvider);

            // Act
            var identifier = provider.SourceIdentifier;

            // Assert
            Assert.That(identifier, Does.StartWith("git:"));
            Assert.That(identifier, Does.Contain("github.com/user/repo.git"));
            Assert.That(identifier, Does.Contain("@main"));
        }

        [UnitTestAttribute(
            Identifier = "a3456789-2345-6789-abcd-012345678901",
            Purpose = "Verify Dispose cleans up cloned repository",
            PostCondition = "Temp directory cleanup is called")]
        [Test]
        public async Task Dispose_CleansUpClonedRepository()
        {
            // Arrange
            var config = new GitSourceConfiguration
            {
                RepositoryUrl = "https://github.com/user/repo.git"
            };
            
            var tempDir = "/tmp/git_repo_123";
            mockTempManager.CreateTempDirectory(Arg.Any<string>()).Returns(tempDir);
            mockCloneService.CloneRepositoryAsync(Arg.Any<GitSourceConfiguration>(), tempDir, Arg.Any<CancellationToken>())
                .Returns(tempDir);
            mockFileProvider.DirectoryExists(tempDir).Returns(true);

            var provider = new GitSourceProvider(config, mockCloneService, mockTempManager, mockFileProvider);
            await provider.PrepareSourceAsync();

            // Act
            provider.Dispose();

            // Assert
            mockTempManager.Received(1).Cleanup(tempDir);
        }

        [UnitTestAttribute(
            Identifier = "b4567890-3456-789a-bcde-123456789012",
            Purpose = "Verify PrepareSourceAsync throws ObjectDisposedException after dispose",
            PostCondition = "ObjectDisposedException is thrown")]
        [Test]
        public void PrepareSourceAsync_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var config = new GitSourceConfiguration
            {
                RepositoryUrl = "https://github.com/user/repo.git"
            };
            var provider = new GitSourceProvider(config, mockCloneService, mockTempManager, mockFileProvider);
            provider.Dispose();

            // Act & Assert
            Assert.ThrowsAsync<ObjectDisposedException>(async () => await provider.PrepareSourceAsync());
        }

        [UnitTestAttribute(
            Identifier = "c5678901-4567-89ab-cdef-234567890123",
            Purpose = "Verify PrepareSourceAsync returns cached result on subsequent calls",
            PostCondition = "Clone is only called once")]
        [Test]
        public async Task PrepareSourceAsync_CalledTwice_OnlyClonesOnce()
        {
            // Arrange
            var config = new GitSourceConfiguration
            {
                RepositoryUrl = "https://github.com/user/repo.git"
            };
            
            var tempDir = "/tmp/git_repo_123";
            mockTempManager.CreateTempDirectory(Arg.Any<string>()).Returns(tempDir);
            mockCloneService.CloneRepositoryAsync(Arg.Any<GitSourceConfiguration>(), tempDir, Arg.Any<CancellationToken>())
                .Returns(tempDir);
            mockFileProvider.DirectoryExists(tempDir).Returns(true);

            var provider = new GitSourceProvider(config, mockCloneService, mockTempManager, mockFileProvider);

            // Act
            await provider.PrepareSourceAsync();
            await provider.PrepareSourceAsync(); // Call again

            // Assert
            await mockCloneService.Received(1).CloneRepositoryAsync(Arg.Any<GitSourceConfiguration>(), tempDir, Arg.Any<CancellationToken>());
        }

        [UnitTestAttribute(
            Identifier = "d6789012-5678-9abc-def0-345678901234",
            Purpose = "Verify constructor throws for null config",
            PostCondition = "ArgumentNullException is thrown")]
        [Test]
        public void Constructor_NullConfig_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new GitSourceProvider(null, mockCloneService, mockTempManager, mockFileProvider));
        }

        [UnitTestAttribute(
            Identifier = "e7890123-6789-abcd-ef01-456789012345",
            Purpose = "Verify constructor throws for null clone service",
            PostCondition = "ArgumentNullException is thrown")]
        [Test]
        public void Constructor_NullCloneService_ThrowsArgumentNullException()
        {
            // Arrange
            var config = new GitSourceConfiguration { RepositoryUrl = "https://github.com/user/repo.git" };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new GitSourceProvider(config, null, mockTempManager, mockFileProvider));
        }

        [UnitTestAttribute(
            Identifier = "f8901234-789a-bcde-f012-567890123456",
            Purpose = "Verify PrepareSourceAsync throws when scan path doesn't exist after clone",
            PostCondition = "DirectoryNotFoundException is thrown")]
        [Test]
        public void PrepareSourceAsync_ScanPathNotExists_ThrowsDirectoryNotFoundException()
        {
            // Arrange
            var config = new GitSourceConfiguration
            {
                RepositoryUrl = "https://github.com/user/repo.git"
            };
            
            var tempDir = "/tmp/git_repo_123";
            mockTempManager.CreateTempDirectory(Arg.Any<string>()).Returns(tempDir);
            mockCloneService.CloneRepositoryAsync(Arg.Any<GitSourceConfiguration>(), tempDir, Arg.Any<CancellationToken>())
                .Returns(tempDir);
            mockFileProvider.DirectoryExists(tempDir).Returns(false);

            var provider = new GitSourceProvider(config, mockCloneService, mockTempManager, mockFileProvider);

            // Act & Assert
            Assert.ThrowsAsync<System.IO.DirectoryNotFoundException>(async () => await provider.PrepareSourceAsync());
        }
    }
}
