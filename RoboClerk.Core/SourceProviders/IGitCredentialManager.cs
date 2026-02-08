using System;

namespace RoboClerk.SourceProviders
{
    /// <summary>
    /// Provides credentials for Git authentication
    /// </summary>
    public interface IGitCredentialProvider
    {
        /// <summary>
        /// Gets the priority of this provider (higher = checked first)
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Checks if this provider can provide credentials for the given key
        /// </summary>
        /// <param name="credentialKey">The credential key to check</param>
        /// <returns>True if this provider can provide the credential</returns>
        bool CanProvide(string credentialKey);

        /// <summary>
        /// Gets the credential value for the given key
        /// </summary>
        /// <param name="credentialKey">The credential key</param>
        /// <returns>The credential value</returns>
        string GetCredential(string credentialKey);
    }

    /// <summary>
    /// Manages Git credentials from multiple providers
    /// </summary>
    public interface IGitCredentialManager
    {
        /// <summary>
        /// Gets credentials for the specified authentication method and key
        /// </summary>
        /// <param name="authMethod">The authentication method</param>
        /// <param name="credentialKey">The credential key</param>
        /// <returns>The credential value, or null if not found</returns>
        string? GetCredentials(GitAuthMethod authMethod, string credentialKey);

        /// <summary>
        /// Registers a credential provider
        /// </summary>
        /// <param name="provider">The provider to register</param>
        void RegisterProvider(IGitCredentialProvider provider);

        /// <summary>
        /// Builds an authenticated URL for the given repository
        /// </summary>
        /// <param name="repositoryUrl">The original repository URL</param>
        /// <param name="authMethod">The authentication method</param>
        /// <param name="credential">The credential value</param>
        /// <returns>The authenticated URL</returns>
        string BuildAuthenticatedUrl(string repositoryUrl, GitAuthMethod authMethod, string credential);
    }
}
