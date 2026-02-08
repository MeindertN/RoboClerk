using NUnit.Framework;
using RoboClerk.SourceProviders;
using Tomlyn;
using Tomlyn.Model;
using System;

// Use explicit alias to avoid conflict with the test fixture class TestConfiguration
using SourceTestConfiguration = RoboClerk.TestConfiguration;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("Tests for TestConfiguration GitSource parsing")]
    public class TestTestConfigurationGitSource
    {
        [UnitTestAttribute(
            Identifier = "a9012345-89ab-cdef-0123-678901234567",
            Purpose = "Verify TestConfiguration parses GitSource section correctly",
            PostCondition = "GitSource properties are populated from TOML")]
        [Test]
        public void FromToml_WithGitSource_ParsesCorrectly()
        {
            // Arrange
            var toml = @"
Language = ""csharp""
Project = ""TestProject""
SubDirs = true
FileMasks = [""*.cs""]

[GitSource]
RepositoryUrl = ""https://github.com/user/repo.git""
Branch = ""main""
SourcePath = ""src/tests""
AuthMethod = ""token""
CredentialKey = ""GITHUB_TOKEN""
ShallowClone = true
TimeoutSeconds = 600
";
            var model = Toml.ToModel(toml);
            var testConfig = new SourceTestConfiguration();

            // Act
            testConfig.FromToml(model);

            // Assert
            Assert.That(testConfig.Language, Is.EqualTo("csharp"));
            Assert.That(testConfig.Project, Is.EqualTo("TestProject"));
            Assert.That(testConfig.UsesGitSource, Is.True);
            Assert.That(testConfig.GitSource, Is.Not.Null);
            Assert.That(testConfig.GitSource.RepositoryUrl, Is.EqualTo("https://github.com/user/repo.git"));
            Assert.That(testConfig.GitSource.Branch, Is.EqualTo("main"));
            Assert.That(testConfig.GitSource.SourcePath, Is.EqualTo("src/tests"));
            Assert.That(testConfig.GitSource.AuthMethod, Is.EqualTo(GitAuthMethod.Token));
            Assert.That(testConfig.GitSource.CredentialKey, Is.EqualTo("GITHUB_TOKEN"));
            Assert.That(testConfig.GitSource.ShallowClone, Is.True);
            Assert.That(testConfig.GitSource.TimeoutSeconds, Is.EqualTo(600));
        }

        [UnitTestAttribute(
            Identifier = "b0123456-9abc-def0-1234-789012345678",
            Purpose = "Verify TestConfiguration parses GitSource with CommitSha correctly",
            PostCondition = "CommitSha is populated")]
        [Test]
        public void FromToml_WithGitSourceCommitSha_ParsesCorrectly()
        {
            // Arrange
            var toml = @"
Language = ""python""
Project = ""PythonTests""
FileMasks = [""test_*.py""]

[GitSource]
RepositoryUrl = ""https://github.com/user/repo.git""
CommitSha = ""abc123def456""
SourcePath = ""tests""
AuthMethod = ""none""
";
            var model = Toml.ToModel(toml);
            var testConfig = new SourceTestConfiguration();

            // Act
            testConfig.FromToml(model);

            // Assert
            Assert.That(testConfig.GitSource.CommitSha, Is.EqualTo("abc123def456"));
            Assert.That(testConfig.GitSource.AuthMethod, Is.EqualTo(GitAuthMethod.None));
        }

        [UnitTestAttribute(
            Identifier = "c1234567-abcd-ef01-2345-890123456789",
            Purpose = "Verify TestConfiguration parses GitSource with Tag correctly",
            PostCondition = "Tag is populated")]
        [Test]
        public void FromToml_WithGitSourceTag_ParsesCorrectly()
        {
            // Arrange
            var toml = @"
Language = ""csharp""
Project = ""ReleasedTests""
FileMasks = [""*Tests.cs""]

[GitSource]
RepositoryUrl = ""https://github.com/user/repo.git""
Tag = ""v1.2.3""
SourcePath = ""tests/unit""
AuthMethod = ""none""
";
            var model = Toml.ToModel(toml);
            var testConfig = new SourceTestConfiguration();

            // Act
            testConfig.FromToml(model);

            // Assert
            Assert.That(testConfig.GitSource.Tag, Is.EqualTo("v1.2.3"));
        }

        [UnitTestAttribute(
            Identifier = "d2345678-bcde-f012-3456-901234567890",
            Purpose = "Verify TestConfiguration.UsesGitSource is false when no GitSource",
            PostCondition = "UsesGitSource returns false")]
        [Test]
        public void FromToml_WithoutGitSource_UsesGitSourceIsFalse()
        {
            // Arrange
            var toml = @"
Language = ""csharp""
Project = ""LocalTests""
TestDirectory = ""/path/to/tests""
FileMasks = [""*.cs""]
";
            var model = Toml.ToModel(toml);
            var testConfig = new SourceTestConfiguration();

            // Act
            testConfig.FromToml(model);

            // Assert
            Assert.That(testConfig.UsesGitSource, Is.False);
            Assert.That(testConfig.GitSource, Is.Null);
        }

        [UnitTestAttribute(
            Identifier = "e3456789-cdef-0123-4567-012345678901",
            Purpose = "Verify TestConfiguration throws when both TestDirectory and GitSource are specified",
            PostCondition = "InvalidOperationException is thrown")]
        [Test]
        public void FromToml_BothTestDirectoryAndGitSource_ThrowsInvalidOperationException()
        {
            // Arrange
            var toml = @"
Language = ""csharp""
Project = ""ConflictingConfig""
TestDirectory = ""/path/to/tests""
FileMasks = [""*.cs""]

[GitSource]
RepositoryUrl = ""https://github.com/user/repo.git""
Branch = ""main""
";
            var model = Toml.ToModel(toml);
            var testConfig = new SourceTestConfiguration();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => testConfig.FromToml(model));
        }

        [UnitTestAttribute(
            Identifier = "f4567890-def0-1234-5678-123456789012",
            Purpose = "Verify TestConfiguration throws when neither TestDirectory nor GitSource is specified",
            PostCondition = "InvalidOperationException is thrown")]
        [Test]
        public void FromToml_NeitherTestDirectoryNorGitSource_ThrowsInvalidOperationException()
        {
            // Arrange
            var toml = @"
Language = ""csharp""
Project = ""IncompleteConfig""
FileMasks = [""*.cs""]
";
            var model = Toml.ToModel(toml);
            var testConfig = new SourceTestConfiguration();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => testConfig.FromToml(model));
        }

        [UnitTestAttribute(
            Identifier = "a5678901-ef01-2345-6789-234567890123",
            Purpose = "Verify TestConfiguration parses different AuthMethod values",
            PostCondition = "AuthMethod is parsed correctly for each value")]
        [Test]
        [TestCase("none", GitAuthMethod.None)]
        [TestCase("token", GitAuthMethod.Token)]
        [TestCase("basic", GitAuthMethod.Basic)]
        [TestCase("Token", GitAuthMethod.Token)] // Test case-insensitive
        [TestCase("TOKEN", GitAuthMethod.Token)]
        [TestCase("BASIC", GitAuthMethod.Basic)]
        public void FromToml_GitSourceAuthMethod_ParsesCorrectly(string authMethodStr, GitAuthMethod expectedMethod)
        {
            // Arrange
            var credKey = expectedMethod == GitAuthMethod.None ? "" : "SOME_KEY";
            var toml = $@"
Language = ""csharp""
Project = ""AuthTest""
FileMasks = [""*.cs""]

[GitSource]
RepositoryUrl = ""https://github.com/user/repo.git""
AuthMethod = ""{authMethodStr}""
CredentialKey = ""{credKey}""
";
            var model = Toml.ToModel(toml);
            var testConfig = new SourceTestConfiguration();

            // Act
            testConfig.FromToml(model);

            // Assert
            Assert.That(testConfig.GitSource.AuthMethod, Is.EqualTo(expectedMethod));
        }

        [UnitTestAttribute(
            Identifier = "b6789012-f012-3456-789a-345678901234",
            Purpose = "Verify TestConfiguration uses defaults for optional GitSource fields",
            PostCondition = "Default values are used for unspecified fields")]
        [Test]
        public void FromToml_GitSourceMinimalConfig_UsesDefaults()
        {
            // Arrange
            var toml = @"
Language = ""csharp""
Project = ""MinimalConfig""
FileMasks = [""*.cs""]

[GitSource]
RepositoryUrl = ""https://github.com/user/repo.git""
";
            var model = Toml.ToModel(toml);
            var testConfig = new SourceTestConfiguration();

            // Act
            testConfig.FromToml(model);

            // Assert
            Assert.That(testConfig.GitSource.Branch, Is.EqualTo(string.Empty));
            Assert.That(testConfig.GitSource.CommitSha, Is.EqualTo(string.Empty));
            Assert.That(testConfig.GitSource.Tag, Is.EqualTo(string.Empty));
            Assert.That(testConfig.GitSource.SourcePath, Is.EqualTo(string.Empty));
            Assert.That(testConfig.GitSource.AuthMethod, Is.EqualTo(GitAuthMethod.None));
            Assert.That(testConfig.GitSource.ShallowClone, Is.True); // Default
            Assert.That(testConfig.GitSource.TimeoutSeconds, Is.EqualTo(300)); // Default
        }

        [UnitTestAttribute(
            Identifier = "c7890123-0123-4567-89ab-456789012345",
            Purpose = "Verify EffectiveScanDirectory is initially empty",
            PostCondition = "EffectiveScanDirectory is empty string")]
        [Test]
        public void FromToml_EffectiveScanDirectory_InitiallyEmpty()
        {
            // Arrange
            var toml = @"
Language = ""csharp""
Project = ""Test""
FileMasks = [""*.cs""]

[GitSource]
RepositoryUrl = ""https://github.com/user/repo.git""
";
            var model = Toml.ToModel(toml);
            var testConfig = new SourceTestConfiguration();

            // Act
            testConfig.FromToml(model);

            // Assert
            Assert.That(testConfig.EffectiveScanDirectory, Is.EqualTo(string.Empty));
        }
    }
}
