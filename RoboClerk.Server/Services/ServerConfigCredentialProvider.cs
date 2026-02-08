using RoboClerk.Server.Configuration;
using RoboClerk.SourceProviders;
using System;

namespace RoboClerk.Server.Services
{
    /// <summary>
    /// Credential provider that reads Git tokens from ServerConfiguration
    /// This allows tokens loaded from the server config file or environment variables
    /// to be used for Git repository authentication.
    /// </summary>
    public class ServerConfigCredentialProvider : IGitCredentialProvider
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly ServerConfiguration serverConfig;

        /// <summary>
        /// Well-known credential keys that map to ServerConfiguration properties.
        /// These match the environment variable names used in docker-compose files.
        /// </summary>
        public static class WellKnownKeys
        {
            public const string GitHubToken = "GIT_GITHUB_TOKEN";
            public const string GitLabToken = "GIT_GITLAB_TOKEN";
            public const string AzureDevOpsPat = "GIT_AZURE_DEVOPS_PAT";
            public const string BitbucketToken = "GIT_BITBUCKET_TOKEN";
        }

        public ServerConfigCredentialProvider(ServerConfiguration serverConfig)
        {
            this.serverConfig = serverConfig ?? throw new ArgumentNullException(nameof(serverConfig));
        }

        /// <inheritdoc />
        public int Priority => 50; // Lower than environment (100), so env vars take precedence

        /// <inheritdoc />
        public bool CanProvide(string credentialKey)
        {
            if (string.IsNullOrWhiteSpace(credentialKey))
                return false;

            var credential = GetCredentialFromConfig(credentialKey);
            return !string.IsNullOrEmpty(credential);
        }

        /// <inheritdoc />
        public string GetCredential(string credentialKey)
        {
            var credential = GetCredentialFromConfig(credentialKey);
            
            if (string.IsNullOrEmpty(credential))
            {
                logger.Warn($"Credential key '{credentialKey}' not found in server configuration");
                throw new InvalidOperationException($"Credential key '{credentialKey}' not found in server configuration");
            }

            logger.Debug($"Retrieved credential '{credentialKey}' from server configuration");
            return credential;
        }

        private string? GetCredentialFromConfig(string credentialKey)
        {
            // Map well-known keys to config properties
            // Supports both GIT_* format (docker-compose) and short format for convenience
            return credentialKey.ToUpperInvariant() switch
            {
                // GitHub token
                "GIT_GITHUB_TOKEN" or "GITHUB_TOKEN" => 
                    !string.IsNullOrEmpty(serverConfig.Git.GitHubToken) ? serverConfig.Git.GitHubToken : null,
                    
                // GitLab token
                "GIT_GITLAB_TOKEN" or "GITLAB_TOKEN" => 
                    !string.IsNullOrEmpty(serverConfig.Git.GitLabToken) ? serverConfig.Git.GitLabToken : null,
                    
                // Azure DevOps PAT
                "GIT_AZURE_DEVOPS_PAT" or "AZURE_DEVOPS_PAT" => 
                    !string.IsNullOrEmpty(serverConfig.Git.AzureDevOpsPat) ? serverConfig.Git.AzureDevOpsPat : null,
                    
                // Bitbucket token
                "GIT_BITBUCKET_TOKEN" or "BITBUCKET_TOKEN" => 
                    !string.IsNullOrEmpty(serverConfig.Git.BitbucketToken) ? serverConfig.Git.BitbucketToken : null,
                    
                _ => null // Unknown key - let other providers handle it
            };
        }
    }
}
