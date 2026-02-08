using System;

namespace RoboClerk.SourceProviders
{
    /// <summary>
    /// Authentication method for Git repositories
    /// </summary>
    public enum GitAuthMethod
    {
        /// <summary>
        /// No authentication required (public repository)
        /// </summary>
        None,
        
        /// <summary>
        /// Personal access token or OAuth token
        /// </summary>
        Token,
                
        /// <summary>
        /// Basic username/password authentication (deprecated but supported)
        /// </summary>
        Basic
    }

    /// <summary>
    /// Configuration for cloning and accessing a Git repository as a source for code analysis
    /// </summary>
    public class GitSourceConfiguration
    {
        /// <summary>
        /// The URL of the Git repository (HTTPS or SSH format)
        /// </summary>
        public string RepositoryUrl { get; set; } = string.Empty;

        /// <summary>
        /// The branch to checkout. If empty, the default branch will be used.
        /// This is ignored if CommitSha or Tag is specified.
        /// </summary>
        public string Branch { get; set; } = string.Empty;

        /// <summary>
        /// A specific commit SHA to checkout. Overrides Branch if specified.
        /// </summary>
        public string CommitSha { get; set; } = string.Empty;

        /// <summary>
        /// A specific tag to checkout. Overrides Branch if specified.
        /// </summary>
        public string Tag { get; set; } = string.Empty;

        /// <summary>
        /// The path within the repository to scan for source files.
        /// If empty, the entire repository will be scanned.
        /// </summary>
        public string SourcePath { get; set; } = string.Empty;

        /// <summary>
        /// The authentication method to use for accessing the repository
        /// </summary>
        public GitAuthMethod AuthMethod { get; set; } = GitAuthMethod.None;

        /// <summary>
        /// The key to use for retrieving credentials (e.g., environment variable name)
        /// </summary>
        public string CredentialKey { get; set; } = string.Empty;

        /// <summary>
        /// Whether to perform a shallow clone (depth=1) for better performance
        /// </summary>
        public bool ShallowClone { get; set; } = true;

        /// <summary>
        /// Timeout in seconds for clone operations
        /// </summary>
        public int TimeoutSeconds { get; set; } = 300;

        /// <summary>
        /// Gets the reference to checkout (CommitSha > Tag > Branch > default)
        /// </summary>
        public string GetCheckoutReference()
        {
            if (!string.IsNullOrEmpty(CommitSha))
                return CommitSha;
            if (!string.IsNullOrEmpty(Tag))
                return $"refs/tags/{Tag}";
            if (!string.IsNullOrEmpty(Branch))
                return Branch;
            return string.Empty; // Use default branch
        }

        /// <summary>
        /// Validates the configuration and throws if invalid
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(RepositoryUrl))
                throw new ArgumentException("RepositoryUrl is required for Git source configuration");

            if (!RepositoryUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                !RepositoryUrl.StartsWith("git@", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Invalid repository URL format: {RepositoryUrl}. Expected HTTPS URL.");
            }

            if (AuthMethod != GitAuthMethod.None && string.IsNullOrWhiteSpace(CredentialKey))
            {
                throw new ArgumentException($"CredentialKey is required when AuthMethod is {AuthMethod}");
            }

            if (TimeoutSeconds <= 0)
                throw new ArgumentException("TimeoutSeconds must be greater than 0");
        }

        /// <summary>
        /// Creates a clone of this configuration
        /// </summary>
        public GitSourceConfiguration Clone()
        {
            return new GitSourceConfiguration
            {
                RepositoryUrl = RepositoryUrl,
                Branch = Branch,
                CommitSha = CommitSha,
                Tag = Tag,
                SourcePath = SourcePath,
                AuthMethod = AuthMethod,
                CredentialKey = CredentialKey,
                ShallowClone = ShallowClone,
                TimeoutSeconds = TimeoutSeconds
            };
        }

        public override string ToString()
        {
            var reference = GetCheckoutReference();
            var refDisplay = string.IsNullOrEmpty(reference) ? "default" : reference;
            return $"{RepositoryUrl} @ {refDisplay}";
        }
    }
}
