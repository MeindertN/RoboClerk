using NUnit.Framework;
using NSubstitute;
using RoboClerk.SourceProviders;
using System;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("Tests for GitCredentialManager and EnvironmentCredentialProvider")]
    public class TestGitCredentialManager
    {
        [UnitTestAttribute(
            Identifier = "d2345678-def0-1234-5678-901234567890",
            Purpose = "Verify EnvironmentCredentialProvider returns true for existing env var",
            PostCondition = "CanProvide returns true")]
        [Test]
        public void EnvironmentCredentialProvider_CanProvide_ExistingVar_ReturnsTrue()
        {
            // Arrange
            var provider = new EnvironmentCredentialProvider();
            var testKey = "PATH"; // This should exist on all systems

            // Act
            var result = provider.CanProvide(testKey);

            // Assert
            Assert.That(result, Is.True);
        }

        [UnitTestAttribute(
            Identifier = "e3456789-ef01-2345-6789-012345678901",
            Purpose = "Verify EnvironmentCredentialProvider returns false for non-existent env var",
            PostCondition = "CanProvide returns false")]
        [Test]
        public void EnvironmentCredentialProvider_CanProvide_NonExistentVar_ReturnsFalse()
        {
            // Arrange
            var provider = new EnvironmentCredentialProvider();
            var testKey = "ROBOCLERK_NONEXISTENT_VAR_" + Guid.NewGuid().ToString();

            // Act
            var result = provider.CanProvide(testKey);

            // Assert
            Assert.That(result, Is.False);
        }

        [UnitTestAttribute(
            Identifier = "f4567890-f012-3456-789a-123456789012",
            Purpose = "Verify EnvironmentCredentialProvider returns false for null key",
            PostCondition = "CanProvide returns false")]
        [Test]
        public void EnvironmentCredentialProvider_CanProvide_NullKey_ReturnsFalse()
        {
            // Arrange
            var provider = new EnvironmentCredentialProvider();

            // Act
            var result = provider.CanProvide(null);

            // Assert
            Assert.That(result, Is.False);
        }

        [UnitTestAttribute(
            Identifier = "a5678901-0123-4567-89ab-234567890123",
            Purpose = "Verify EnvironmentCredentialProvider throws for non-existent var when getting credential",
            PostCondition = "InvalidOperationException is thrown")]
        [Test]
        public void EnvironmentCredentialProvider_GetCredential_NonExistentVar_ThrowsInvalidOperationException()
        {
            // Arrange
            var provider = new EnvironmentCredentialProvider();
            var testKey = "ROBOCLERK_NONEXISTENT_VAR_" + Guid.NewGuid().ToString();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => provider.GetCredential(testKey));
        }

        [UnitTestAttribute(
            Identifier = "b6789012-1234-5678-9abc-345678901234",
            Purpose = "Verify EnvironmentCredentialProvider priority is high (100)",
            PostCondition = "Priority is 100")]
        [Test]
        public void EnvironmentCredentialProvider_Priority_Is100()
        {
            // Arrange
            var provider = new EnvironmentCredentialProvider();

            // Assert
            Assert.That(provider.Priority, Is.EqualTo(100));
        }

        [UnitTestAttribute(
            Identifier = "c7890123-2345-6789-abcd-456789012345",
            Purpose = "Verify GitCredentialManager returns null for None auth method",
            PostCondition = "GetCredentials returns null")]
        [Test]
        public void GitCredentialManager_GetCredentials_NoneAuthMethod_ReturnsNull()
        {
            // Arrange
            var manager = new GitCredentialManager();

            // Act
            var result = manager.GetCredentials(GitAuthMethod.None, "any_key");

            // Assert
            Assert.That(result, Is.Null);
        }

        [UnitTestAttribute(
            Identifier = "d8901234-3456-789a-bcde-567890123456",
            Purpose = "Verify GitCredentialManager returns null for empty credential key",
            PostCondition = "GetCredentials returns null")]
        [Test]
        public void GitCredentialManager_GetCredentials_EmptyKey_ReturnsNull()
        {
            // Arrange
            var manager = new GitCredentialManager();

            // Act
            var result = manager.GetCredentials(GitAuthMethod.Token, "");

            // Assert
            Assert.That(result, Is.Null);
        }

        [UnitTestAttribute(
            Identifier = "e9012345-4567-89ab-cdef-678901234567",
            Purpose = "Verify BuildAuthenticatedUrl returns original URL for None auth method",
            PostCondition = "Original URL is returned unchanged")]
        [Test]
        public void GitCredentialManager_BuildAuthenticatedUrl_NoneAuth_ReturnsOriginalUrl()
        {
            // Arrange
            var manager = new GitCredentialManager();
            var originalUrl = "https://github.com/user/repo.git";

            // Act
            var result = manager.BuildAuthenticatedUrl(originalUrl, GitAuthMethod.None, "");

            // Assert
            Assert.That(result, Is.EqualTo(originalUrl));
        }

        [UnitTestAttribute(
            Identifier = "f0123456-5678-9abc-def0-789012345678",
            Purpose = "Verify BuildAuthenticatedUrl returns original URL for SSH URL",
            PostCondition = "SSH URL is returned unchanged")]
        [Test]
        public void GitCredentialManager_BuildAuthenticatedUrl_SshUrl_ReturnsOriginalUrl()
        {
            // Arrange
            var manager = new GitCredentialManager();
            var sshUrl = "git@github.com:user/repo.git";

            // Act
            var result = manager.BuildAuthenticatedUrl(sshUrl, GitAuthMethod.Token, "mytoken");

            // Assert
            Assert.That(result, Is.EqualTo(sshUrl));
        }

        [UnitTestAttribute(
            Identifier = "a1234567-6789-abcd-ef01-890123456789",
            Purpose = "Verify BuildAuthenticatedUrl builds GitHub authenticated URL correctly",
            PostCondition = "URL contains x-access-token credential format")]
        [Test]
        public void GitCredentialManager_BuildAuthenticatedUrl_GitHub_BuildsCorrectUrl()
        {
            // Arrange
            var manager = new GitCredentialManager();
            var originalUrl = "https://github.com/user/repo.git";

            // Act
            var result = manager.BuildAuthenticatedUrl(originalUrl, GitAuthMethod.Token, "mytoken");

            // Assert
            Assert.That(result, Does.Contain("x-access-token"));
            Assert.That(result, Does.Contain("mytoken"));
            Assert.That(result, Does.Contain("github.com"));
        }

        [UnitTestAttribute(
            Identifier = "b2345678-789a-bcde-f012-901234567890",
            Purpose = "Verify BuildAuthenticatedUrl builds GitLab authenticated URL correctly",
            PostCondition = "URL contains oauth2 credential format")]
        [Test]
        public void GitCredentialManager_BuildAuthenticatedUrl_GitLab_BuildsCorrectUrl()
        {
            // Arrange
            var manager = new GitCredentialManager();
            var originalUrl = "https://gitlab.com/user/repo.git";

            // Act
            var result = manager.BuildAuthenticatedUrl(originalUrl, GitAuthMethod.Token, "mytoken");

            // Assert
            Assert.That(result, Does.Contain("oauth2"));
            Assert.That(result, Does.Contain("gitlab.com"));
        }

        [UnitTestAttribute(
            Identifier = "c3456789-89ab-cdef-0123-012345678901",
            Purpose = "Verify BuildAuthenticatedUrl builds Azure DevOps authenticated URL correctly",
            PostCondition = "URL contains credential in correct format")]
        [Test]
        public void GitCredentialManager_BuildAuthenticatedUrl_AzureDevOps_BuildsCorrectUrl()
        {
            // Arrange
            var manager = new GitCredentialManager();
            var originalUrl = "https://dev.azure.com/org/project/_git/repo";

            // Act
            var result = manager.BuildAuthenticatedUrl(originalUrl, GitAuthMethod.Token, "mytoken");

            // Assert
            Assert.That(result, Does.Contain("dev.azure.com"));
            Assert.That(result, Does.Contain("mytoken"));
        }

        [UnitTestAttribute(
            Identifier = "d4567890-9abc-def0-1234-123456789012",
            Purpose = "Verify BuildAuthenticatedUrl throws for null URL",
            PostCondition = "ArgumentException is thrown")]
        [Test]
        public void GitCredentialManager_BuildAuthenticatedUrl_NullUrl_ThrowsArgumentException()
        {
            // Arrange
            var manager = new GitCredentialManager();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                manager.BuildAuthenticatedUrl(null, GitAuthMethod.Token, "token"));
        }

        [UnitTestAttribute(
            Identifier = "e5678901-abcd-ef01-2345-234567890123",
            Purpose = "Verify custom credential provider can be registered",
            PostCondition = "Provider is registered and used")]
        [Test]
        public void GitCredentialManager_RegisterProvider_CustomProvider_IsUsed()
        {
            // Arrange
            var manager = new GitCredentialManager();
            var customProvider = Substitute.For<IGitCredentialProvider>();
            customProvider.Priority.Returns(200); // Higher than default
            customProvider.CanProvide("CUSTOM_KEY").Returns(true);
            customProvider.GetCredential("CUSTOM_KEY").Returns("custom_value");

            manager.RegisterProvider(customProvider);

            // Act
            var result = manager.GetCredentials(GitAuthMethod.Token, "CUSTOM_KEY");

            // Assert
            Assert.That(result, Is.EqualTo("custom_value"));
            customProvider.Received().GetCredential("CUSTOM_KEY");
        }

        [UnitTestAttribute(
            Identifier = "f6789012-bcde-f012-3456-345678901234",
            Purpose = "Verify RegisterProvider throws for null provider",
            PostCondition = "ArgumentNullException is thrown")]
        [Test]
        public void GitCredentialManager_RegisterProvider_Null_ThrowsArgumentNullException()
        {
            // Arrange
            var manager = new GitCredentialManager();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => manager.RegisterProvider(null));
        }
    }
}
