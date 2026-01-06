using NUnit.Framework;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using RoboClerk.Core.Configuration;
using RoboClerk.Core;

namespace RoboClerk.Tests
{
    [TestFixture]
    public class TestGitRepository
    {
        private IConfiguration _config;
        private IFileProviderPlugin _fileSystem;
        private string _projectRoot;

        private class TestableGitRepository : GitRepository
        {
            private readonly Dictionary<string, string> _gitResponses = new Dictionary<string, string>();

            public TestableGitRepository(IConfiguration config, IFileProviderPlugin fileSystem) 
                : base(config, fileSystem)
            {
            }

            public void SetupGitCommand(string commandContains, string output)
            {
                _gitResponses[commandContains] = output;
            }

            public void ClearGitCommands()
            {
                _gitResponses.Clear();
            }

            protected override string RunGitCommand(string arguments)
            {
                // Check configured responses first
                foreach (var key in _gitResponses.Keys)
                {
                    if (arguments.Contains(key))
                    {
                        return _gitResponses[key];
                    }
                }

                // Default responses for constructor
                if (arguments == "--version") return "git version 2.30.0";
                if (arguments.Contains("config --global")) return "";
                
                return "";
            }
        }

        [SetUp]
        public void Setup()
        {
            _config = Substitute.For<IConfiguration>();
            // Use a rooted path appropriate for the OS
            _projectRoot = Path.GetFullPath(Path.Combine("C:", "ProjectRoot")); 
            if (!Path.IsPathRooted(_projectRoot)) // Fallback for non-Windows if needed, though Path.GetFullPath should handle it
            {
                _projectRoot = Path.GetFullPath("/ProjectRoot");
            }
            // Actually, let's just use the current directory or a temp path to be safe across platforms
            _projectRoot = Path.Combine(Path.GetTempPath(), "RoboClerkTest");
            
            _config.ProjectRoot.Returns(_projectRoot);
            _fileSystem = Substitute.For<IFileProviderPlugin>();
        }

        [UnitTestAttribute(
            Identifier = "11223344-5566-7788-99AA-BBCCDDEEFF00",
            Purpose = "Test that GitRepository initializes correctly when git is present",
            PostCondition = "No exception is thrown")]
        [Test]
        public void Constructor_WhenGitIsPresent_ShouldInitialize()
        {
            Assert.That(() => new TestableGitRepository(_config, _fileSystem), Throws.Nothing);
        }

        [UnitTestAttribute(
            Identifier = "22334455-6677-8899-AABB-CCDDEEFF0011",
            Purpose = "Test GetFileLocallyUpdated returns true when git status indicates modified",
            PostCondition = "Returns true")]
        [Test]
        public void GetFileLocallyUpdated_WhenFileIsModified_ReturnsTrue()
        {
            var repo = new TestableGitRepository(_config, _fileSystem);
            var file = Path.Combine(_projectRoot, "test.cs");
            repo.SetupGitCommand($"status \"{file}\"", "modified: test.cs");

            var result = repo.GetFileLocallyUpdated(file);

            Assert.That(result, Is.True);
        }

        [UnitTestAttribute(
            Identifier = "33445566-7788-99AA-BBCC-DDEEFF001122",
            Purpose = "Test GetFileLocallyUpdated returns false when git status indicates clean",
            PostCondition = "Returns false")]
        [Test]
        public void GetFileLocallyUpdated_WhenFileIsClean_ReturnsFalse()
        {
            var repo = new TestableGitRepository(_config, _fileSystem);
            var file = Path.Combine(_projectRoot, "test.cs");
            repo.SetupGitCommand($"status \"{file}\"", "");

            var result = repo.GetFileLocallyUpdated(file);

            Assert.That(result, Is.False);
        }

        [UnitTestAttribute(
            Identifier = "44556677-8899-AABB-CCDD-EEFF00112233",
            Purpose = "Test GetFileVersion returns commit hash",
            PostCondition = "Returns correct hash")]
        [Test]
        public void GetFileVersion_WhenFileExists_ReturnsHash()
        {
            var repo = new TestableGitRepository(_config, _fileSystem);
            var file = Path.Combine(_projectRoot, "test.cs");
            repo.SetupGitCommand($"log -n 1 --pretty=format:%H -- \"{file}\"", "abcdef1234567890");

            var result = repo.GetFileVersion(file);

            Assert.That(result, Is.EqualTo("abcdef1"));
        }

        [UnitTestAttribute(
            Identifier = "55667788-99AA-BBCC-DDEE-FF0011223344",
            Purpose = "Test GetFileLastUpdated returns date from git",
            PostCondition = "Returns correct date")]
        [Test]
        public void GetFileLastUpdated_WhenFileExistsInGit_ReturnsGitDate()
        {
            var repo = new TestableGitRepository(_config, _fileSystem);
            var file = Path.Combine(_projectRoot, "test.cs");
            var gitDateString = "2023-01-01 12:00:00 +0000";
            DateTime.TryParse(gitDateString, out var expectedDate);
            
            repo.SetupGitCommand($"log -n 1 --format=\"%ai\" -- \"{file}\"", gitDateString);

            var result = repo.GetFileLastUpdated(file);

            Assert.That(result, Is.EqualTo(expectedDate));
        }

        [UnitTestAttribute(
            Identifier = "66778899-AABB-CCDD-EEFF-001122334455",
            Purpose = "Test PreloadDirectoryInfo caches information correctly",
            PostCondition = "Cache is used for subsequent calls")]
        [Test]
        public void PreloadDirectoryInfo_CachesInformation()
        {
            var repo = new TestableGitRepository(_config, _fileSystem);
            var dir = Path.Combine(_projectRoot, "src");
            var relativeFile = Path.Combine("src", "test.cs");
            var file = Path.Combine(_projectRoot, relativeFile);
            var gitDateString = "2023-01-01 12:00:00 +0000";
            DateTime.TryParse(gitDateString, out var expectedDate);
            
            // Setup ls-files
            // Note: ls-files usually returns relative paths
            repo.SetupGitCommand($"ls-files \"{dir}\"", relativeFile.Replace("\\", "/"));
            
            // Setup status --porcelain for modified check
            // status --porcelain returns " M path/to/file"
            repo.SetupGitCommand("status --porcelain", $" M {relativeFile.Replace("\\", "/")}");
            
            // Setup log for versions and dates
            // The command in GetFileVersionsAndDates constructs args with relative paths
            repo.SetupGitCommand("log --name-only", $"abcdef1234567890|{gitDateString}\n{relativeFile.Replace("\\", "/")}");

            repo.PreloadDirectoryInfo(dir);

            // Now clear commands to ensure we hit cache
            repo.ClearGitCommands();

            var isModified = repo.GetFileLocallyUpdated(file);
            var version = repo.GetFileVersion(file);
            var date = repo.GetFileLastUpdated(file);

            Assert.That(isModified, Is.True, "Should be modified from cache");
            Assert.That(version, Is.EqualTo("abcdef1"), "Should get version from cache");
            Assert.That(date, Is.EqualTo(expectedDate), "Should get date from cache");
        }
    }
}
