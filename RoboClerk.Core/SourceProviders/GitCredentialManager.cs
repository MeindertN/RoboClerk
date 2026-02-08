using System;
using System.Collections.Generic;
using System.Linq;

namespace RoboClerk.SourceProviders
{
    /// <summary>
    /// Default implementation of IGitCredentialManager
    /// </summary>
    public class GitCredentialManager : IGitCredentialManager
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly List<IGitCredentialProvider> providers = new();

        /// <summary>
        /// Creates a new GitCredentialManager with the default environment provider
        /// </summary>
        public GitCredentialManager()
        {
            // Register the default environment provider
            RegisterProvider(new EnvironmentCredentialProvider());
        }

        /// <inheritdoc />
        public void RegisterProvider(IGitCredentialProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            providers.Add(provider);
            logger.Debug($"Registered credential provider: {provider.GetType().Name} (Priority: {provider.Priority})");
        }

        /// <inheritdoc />
        public string? GetCredentials(GitAuthMethod authMethod, string credentialKey)
        {
            if (authMethod == GitAuthMethod.None)
                return null;

            if (string.IsNullOrWhiteSpace(credentialKey))
            {
                logger.Warn($"No credential key provided for auth method: {authMethod}");
                return null;
            }

            // Try providers in priority order (highest first)
            foreach (var provider in providers.OrderByDescending(p => p.Priority))
            {
                try
                {
                    if (provider.CanProvide(credentialKey))
                    {
                        var credential = provider.GetCredential(credentialKey);
                        logger.Debug($"Credential '{credentialKey}' retrieved from {provider.GetType().Name}");
                        return credential;
                    }
                }
                catch (Exception ex)
                {
                    logger.Warn($"Provider {provider.GetType().Name} failed to get credential '{credentialKey}': {ex.Message}");
                }
            }

            logger.Warn($"No provider could supply credential for key: {credentialKey}");
            return null;
        }

        /// <inheritdoc />
        public string BuildAuthenticatedUrl(string repositoryUrl, GitAuthMethod authMethod, string credential)
        {
            if (string.IsNullOrWhiteSpace(repositoryUrl))
                throw new ArgumentException("Repository URL cannot be null or empty", nameof(repositoryUrl));

            if (authMethod == GitAuthMethod.None || string.IsNullOrEmpty(credential))
                return repositoryUrl;

            // SSH URLs don't need modification for token auth
            if (repositoryUrl.StartsWith("git@") || repositoryUrl.StartsWith("ssh://"))
            {
                logger.Debug("SSH URL detected, returning original URL (credentials handled via SSH agent)");
                return repositoryUrl;
            }

            try
            {
                var uri = new Uri(repositoryUrl);
                var host = uri.Host.ToLowerInvariant();

                string authenticatedUrl = authMethod switch
                {
                    // GitHub token authentication
                    GitAuthMethod.Token when host.Contains("github") =>
                        BuildHttpsUrl(uri, $"x-access-token:{credential}"),

                    // GitLab token authentication
                    GitAuthMethod.Token when host.Contains("gitlab") =>
                        BuildHttpsUrl(uri, $"oauth2:{credential}"),

                    // Azure DevOps token authentication
                    GitAuthMethod.Token when host.Contains("dev.azure.com") || host.Contains("visualstudio.com") =>
                        BuildHttpsUrl(uri, $":{credential}"),

                    // Bitbucket token authentication
                    GitAuthMethod.Token when host.Contains("bitbucket") =>
                        BuildHttpsUrl(uri, $"x-token-auth:{credential}"),

                    // Generic token authentication (use as username)
                    GitAuthMethod.Token =>
                        BuildHttpsUrl(uri, $"{credential}:x-oauth-basic"),

                    // Basic authentication (username:password format expected in credential)
                    GitAuthMethod.Basic =>
                        BuildHttpsUrl(uri, credential),

                    // Other methods - return original URL
                    _ => repositoryUrl
                };

                logger.Debug($"Built authenticated URL for {host} using {authMethod}");
                return authenticatedUrl;
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to build authenticated URL: {ex.Message}");
                throw new InvalidOperationException($"Failed to build authenticated URL for {repositoryUrl}", ex);
            }
        }

        private static string BuildHttpsUrl(Uri originalUri, string credentials)
        {
            // Build URL with credentials: https://credentials@host/path
            var builder = new UriBuilder(originalUri)
            {
                UserName = credentials.Contains(':') ? credentials.Split(':')[0] : credentials,
                Password = credentials.Contains(':') ? credentials.Split(':')[1] : string.Empty
            };

            return builder.Uri.ToString();
        }
    }
}
