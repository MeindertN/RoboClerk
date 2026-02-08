using NUnit.Framework;
using RoboClerk.SourceProviders;
using System;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("Tests for GitSourceConfiguration")]
    public class TestGitSourceConfiguration
    {
        [UnitTestAttribute(
            Identifier = "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
            Purpose = "Verify GitSourceConfiguration validates successfully with valid HTTPS URL",
            PostCondition = "No exception is thrown")]
        [Test]
        public void Validate_ValidHttpsUrl_NoException()
        {
            // Arrange
            var config = new GitSourceConfiguration
            {
                RepositoryUrl = "https://github.com/user/repo.git",
                AuthMethod = GitAuthMethod.None
            };

            // Act & Assert
            Assert.DoesNotThrow(() => config.Validate());
        }

        [UnitTestAttribute(
            Identifier = "b2c3d4e5-f6a7-8901-bcde-f23456789012",
            Purpose = "Verify GitSourceConfiguration validates successfully with SSH URL",
            PostCondition = "No exception is thrown")]
        [Test]
        public void Validate_ValidSshUrl_NoException()
        {
            // Arrange
            var config = new GitSourceConfiguration
            {
                RepositoryUrl = "git@github.com:user/repo.git",
                AuthMethod = GitAuthMethod.None
            };

            // Act & Assert
            Assert.DoesNotThrow(() => config.Validate());
        }

        [UnitTestAttribute(
            Identifier = "c3d4e5f6-a7b8-9012-cdef-345678901234",
            Purpose = "Verify GitSourceConfiguration throws when RepositoryUrl is empty",
            PostCondition = "ArgumentException is thrown")]
        [Test]
        public void Validate_EmptyRepositoryUrl_ThrowsArgumentException()
        {
            // Arrange
            var config = new GitSourceConfiguration
            {
                RepositoryUrl = "",
                AuthMethod = GitAuthMethod.None
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => config.Validate());
        }

        [UnitTestAttribute(
            Identifier = "d4e5f6a7-b8c9-0123-def0-456789012345",
            Purpose = "Verify GitSourceConfiguration throws when invalid URL format is provided",
            PostCondition = "ArgumentException is thrown")]
        [Test]
        public void Validate_InvalidUrlFormat_ThrowsArgumentException()
        {
            // Arrange
            var config = new GitSourceConfiguration
            {
                RepositoryUrl = "not-a-valid-url",
                AuthMethod = GitAuthMethod.None
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => config.Validate());
        }

        [UnitTestAttribute(
            Identifier = "e5f6a7b8-c9d0-1234-ef01-567890123456",
            Purpose = "Verify GitSourceConfiguration throws when AuthMethod requires credentials but CredentialKey is empty",
            PostCondition = "ArgumentException is thrown")]
        [Test]
        public void Validate_TokenAuthWithoutCredentialKey_ThrowsArgumentException()
        {
            // Arrange
            var config = new GitSourceConfiguration
            {
                RepositoryUrl = "https://github.com/user/repo.git",
                AuthMethod = GitAuthMethod.Token,
                CredentialKey = ""
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => config.Validate());
        }

        [UnitTestAttribute(
            Identifier = "f6a7b8c9-d0e1-2345-f012-678901234567",
            Purpose = "Verify GitSourceConfiguration validates when AuthMethod is Token with valid CredentialKey",
            PostCondition = "No exception is thrown")]
        [Test]
        public void Validate_TokenAuthWithCredentialKey_NoException()
        {
            // Arrange
            var config = new GitSourceConfiguration
            {
                RepositoryUrl = "https://github.com/user/repo.git",
                AuthMethod = GitAuthMethod.Token,
                CredentialKey = "GITHUB_TOKEN"
            };

            // Act & Assert
            Assert.DoesNotThrow(() => config.Validate());
        }

        [UnitTestAttribute(
            Identifier = "a7b8c9d0-e1f2-3456-0123-789012345678",
            Purpose = "Verify GetCheckoutReference returns CommitSha when specified",
            PostCondition = "CommitSha is returned")]
        [Test]
        public void GetCheckoutReference_CommitShaSpecified_ReturnsCommitSha()
        {
            // Arrange
            var config = new GitSourceConfiguration
            {
                RepositoryUrl = "https://github.com/user/repo.git",
                CommitSha = "abc123def",
                Tag = "v1.0",
                Branch = "main"
            };

            // Act
            var reference = config.GetCheckoutReference();

            // Assert
            Assert.That(reference, Is.EqualTo("abc123def"));
        }

        [UnitTestAttribute(
            Identifier = "b8c9d0e1-f2a3-4567-1234-890123456789",
            Purpose = "Verify GetCheckoutReference returns Tag when CommitSha is not specified",
            PostCondition = "Tag with refs/tags prefix is returned")]
        [Test]
        public void GetCheckoutReference_TagSpecified_ReturnsTagReference()
        {
            // Arrange
            var config = new GitSourceConfiguration
            {
                RepositoryUrl = "https://github.com/user/repo.git",
                CommitSha = "",
                Tag = "v1.0",
                Branch = "main"
            };

            // Act
            var reference = config.GetCheckoutReference();

            // Assert
            Assert.That(reference, Is.EqualTo("refs/tags/v1.0"));
        }

        [UnitTestAttribute(
            Identifier = "c9d0e1f2-a3b4-5678-2345-901234567890",
            Purpose = "Verify GetCheckoutReference returns Branch when neither CommitSha nor Tag is specified",
            PostCondition = "Branch name is returned")]
        [Test]
        public void GetCheckoutReference_BranchSpecified_ReturnsBranch()
        {
            // Arrange
            var config = new GitSourceConfiguration
            {
                RepositoryUrl = "https://github.com/user/repo.git",
                CommitSha = "",
                Tag = "",
                Branch = "develop"
            };

            // Act
            var reference = config.GetCheckoutReference();

            // Assert
            Assert.That(reference, Is.EqualTo("develop"));
        }

        [UnitTestAttribute(
            Identifier = "d0e1f2a3-b4c5-6789-3456-012345678901",
            Purpose = "Verify GetCheckoutReference returns empty string when no reference is specified",
            PostCondition = "Empty string is returned indicating default branch")]
        [Test]
        public void GetCheckoutReference_NothingSpecified_ReturnsEmptyString()
        {
            // Arrange
            var config = new GitSourceConfiguration
            {
                RepositoryUrl = "https://github.com/user/repo.git",
                CommitSha = "",
                Tag = "",
                Branch = ""
            };

            // Act
            var reference = config.GetCheckoutReference();

            // Assert
            Assert.That(reference, Is.EqualTo(string.Empty));
        }

        [UnitTestAttribute(
            Identifier = "e1f2a3b4-c5d6-7890-4567-123456789012",
            Purpose = "Verify Clone creates an independent copy of the configuration",
            PostCondition = "Cloned configuration has same values but is a different instance")]
        [Test]
        public void Clone_CreatesIndependentCopy()
        {
            // Arrange
            var original = new GitSourceConfiguration
            {
                RepositoryUrl = "https://github.com/user/repo.git",
                Branch = "main",
                CommitSha = "abc123",
                Tag = "v1.0",
                SourcePath = "src",
                AuthMethod = GitAuthMethod.Token,
                CredentialKey = "TOKEN",
                ShallowClone = true,
                TimeoutSeconds = 600
            };

            // Act
            var clone = original.Clone();

            // Assert
            Assert.That(clone, Is.Not.SameAs(original));
            Assert.That(clone.RepositoryUrl, Is.EqualTo(original.RepositoryUrl));
            Assert.That(clone.Branch, Is.EqualTo(original.Branch));
            Assert.That(clone.CommitSha, Is.EqualTo(original.CommitSha));
            Assert.That(clone.Tag, Is.EqualTo(original.Tag));
            Assert.That(clone.SourcePath, Is.EqualTo(original.SourcePath));
            Assert.That(clone.AuthMethod, Is.EqualTo(original.AuthMethod));
            Assert.That(clone.CredentialKey, Is.EqualTo(original.CredentialKey));
            Assert.That(clone.ShallowClone, Is.EqualTo(original.ShallowClone));
            Assert.That(clone.TimeoutSeconds, Is.EqualTo(original.TimeoutSeconds));
        }

        [UnitTestAttribute(
            Identifier = "f2a3b4c5-d6e7-8901-5678-234567890123",
            Purpose = "Verify ToString provides meaningful description",
            PostCondition = "String contains repository URL and reference")]
        [Test]
        public void ToString_ReturnsDescriptiveString()
        {
            // Arrange
            var config = new GitSourceConfiguration
            {
                RepositoryUrl = "https://github.com/user/repo.git",
                Branch = "main"
            };

            // Act
            var result = config.ToString();

            // Assert
            Assert.That(result, Does.Contain("https://github.com/user/repo.git"));
            Assert.That(result, Does.Contain("main"));
        }
    }
}
