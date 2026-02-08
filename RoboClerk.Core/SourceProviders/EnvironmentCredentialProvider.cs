using System;

namespace RoboClerk.SourceProviders
{
    /// <summary>
    /// Credential provider that reads from environment variables
    /// </summary>
    public class EnvironmentCredentialProvider : IGitCredentialProvider
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        /// <inheritdoc />
        public int Priority => 100; // High priority - environment variables are commonly used

        /// <inheritdoc />
        public bool CanProvide(string credentialKey)
        {
            if (string.IsNullOrWhiteSpace(credentialKey))
                return false;

            var value = Environment.GetEnvironmentVariable(credentialKey);
            return !string.IsNullOrEmpty(value);
        }

        /// <inheritdoc />
        public string GetCredential(string credentialKey)
        {
            var value = Environment.GetEnvironmentVariable(credentialKey);
            
            if (string.IsNullOrEmpty(value))
            {
                logger.Warn($"Environment variable '{credentialKey}' is not set or empty");
                throw new InvalidOperationException($"Environment variable '{credentialKey}' is not set");
            }

            logger.Debug($"Retrieved credential from environment variable: {credentialKey}");
            return value;
        }
    }
}
