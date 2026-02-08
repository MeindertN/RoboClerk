using CliWrap;
using CliWrap.Buffered;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RoboClerk.SourceProviders
{
    /// <summary>
    /// Git clone service using the git CLI
    /// </summary>
    public class GitCloneService : IGitCloneService
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IGitCredentialManager credentialManager;
        private string? cachedGitVersion;

        /// <summary>
        /// Creates a new GitCloneService
        /// </summary>
        /// <param name="credentialManager">The credential manager for authentication</param>
        public GitCloneService(IGitCredentialManager credentialManager)
        {
            this.credentialManager = credentialManager ?? throw new ArgumentNullException(nameof(credentialManager));
        }

        /// <inheritdoc />
        public bool IsGitAvailable()
        {
            try
            {
                var version = GetGitVersion();
                return !string.IsNullOrEmpty(version) && version.Contains("git version");
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc />
        public string GetGitVersion()
        {
            if (cachedGitVersion != null)
                return cachedGitVersion;

            try
            {
                var result = RunGitCommandAsync("--version", Environment.CurrentDirectory, TimeSpan.FromSeconds(30))
                    .GetAwaiter().GetResult();
                cachedGitVersion = result.Trim();
                return cachedGitVersion;
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to get git version: {ex.Message}");
                throw new InvalidOperationException("Git is not available. Please ensure git is installed and in your PATH.", ex);
            }
        }

        /// <inheritdoc />
        public async Task<string> CloneRepositoryAsync(
            GitSourceConfiguration config,
            string targetDirectory,
            CancellationToken cancellationToken = default)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (string.IsNullOrWhiteSpace(targetDirectory))
                throw new ArgumentException("Target directory cannot be null or empty", nameof(targetDirectory));

            config.Validate();

            if (!IsGitAvailable())
                throw new InvalidOperationException("Git is not available on this system");

            logger.Info($"Cloning repository: {config.RepositoryUrl}");

            // Get authenticated URL if needed
            string cloneUrl = config.RepositoryUrl;
            if (config.AuthMethod != GitAuthMethod.None)
            {
                var credential = credentialManager.GetCredentials(config.AuthMethod, config.CredentialKey);
                if (string.IsNullOrEmpty(credential))
                {
                    throw new InvalidOperationException(
                        $"Failed to get credentials for key '{config.CredentialKey}'. " +
                        $"Ensure the credential is available via environment variable or other configured provider.");
                }
                cloneUrl = credentialManager.BuildAuthenticatedUrl(config.RepositoryUrl, config.AuthMethod, credential);
            }

            // Build clone command
            var cloneArgs = BuildCloneArguments(config, cloneUrl, targetDirectory);

            try
            {
                var timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);
                await RunGitCommandAsync(cloneArgs, Environment.CurrentDirectory, timeout, cancellationToken);

                // If a specific commit SHA is requested, checkout that commit
                if (!string.IsNullOrEmpty(config.CommitSha))
                {
                    logger.Debug($"Checking out specific commit: {config.CommitSha}");
                    await RunGitCommandAsync($"checkout {config.CommitSha}", targetDirectory, TimeSpan.FromSeconds(60), cancellationToken);
                }

                // Determine the effective scan path
                var scanPath = targetDirectory;
                if (!string.IsNullOrEmpty(config.SourcePath))
                {
                    scanPath = System.IO.Path.Combine(targetDirectory, config.SourcePath);
                    if (!System.IO.Directory.Exists(scanPath))
                    {
                        throw new System.IO.DirectoryNotFoundException(
                            $"Source path '{config.SourcePath}' not found in cloned repository");
                    }
                }

                logger.Info($"Successfully cloned repository to: {targetDirectory}");
                return scanPath;
            }
            catch (OperationCanceledException)
            {
                logger.Warn($"Clone operation was cancelled for: {config.RepositoryUrl}");
                throw;
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to clone repository {config.RepositoryUrl}: {ex.Message}");
                throw new InvalidOperationException($"Failed to clone repository: {config.RepositoryUrl}", ex);
            }
        }

        /// <inheritdoc />
        public async Task<string> GetDefaultBranchAsync(
            GitSourceConfiguration config,
            CancellationToken cancellationToken = default)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            config.Validate();

            string repoUrl = config.RepositoryUrl;
            if (config.AuthMethod != GitAuthMethod.None)
            {
                var credential = credentialManager.GetCredentials(config.AuthMethod, config.CredentialKey);
                if (!string.IsNullOrEmpty(credential))
                {
                    repoUrl = credentialManager.BuildAuthenticatedUrl(config.RepositoryUrl, config.AuthMethod, credential);
                }
            }

            try
            {
                var output = await RunGitCommandAsync(
                    $"ls-remote --symref \"{repoUrl}\" HEAD",
                    Environment.CurrentDirectory,
                    TimeSpan.FromSeconds(30),
                    cancellationToken);

                // Parse output to find default branch
                // Format: ref: refs/heads/main	HEAD
                foreach (var line in output.Split('\n'))
                {
                    if (line.StartsWith("ref: refs/heads/"))
                    {
                        var branchName = line.Substring("ref: refs/heads/".Length).Split('\t')[0].Trim();
                        logger.Debug($"Default branch for {config.RepositoryUrl}: {branchName}");
                        return branchName;
                    }
                }

                // Fallback to common defaults
                logger.Warn($"Could not determine default branch for {config.RepositoryUrl}, using 'main'");
                return "main";
            }
            catch (Exception ex)
            {
                logger.Warn($"Failed to get default branch: {ex.Message}, using 'main'");
                return "main";
            }
        }

        private string BuildCloneArguments(GitSourceConfiguration config, string cloneUrl, string targetDirectory)
        {
            var args = new StringBuilder("clone");

            // Shallow clone for performance (unless we need full history)
            if (config.ShallowClone && string.IsNullOrEmpty(config.CommitSha))
            {
                args.Append(" --depth 1");
            }

            // Single branch (unless we need full history for SHA checkout)
            if (string.IsNullOrEmpty(config.CommitSha))
            {
                args.Append(" --single-branch");

                // Specify branch or tag
                if (!string.IsNullOrEmpty(config.Tag))
                {
                    args.Append($" --branch \"{config.Tag}\"");
                }
                else if (!string.IsNullOrEmpty(config.Branch))
                {
                    args.Append($" --branch \"{config.Branch}\"");
                }
            }

            // Progress output for logging
            args.Append(" --progress");

            // Repository URL and target directory
            args.Append($" \"{cloneUrl}\" \"{targetDirectory}\"");

            return args.ToString();
        }

        private async Task<string> RunGitCommandAsync(
            string arguments,
            string workingDirectory,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            var stdOut = new StringBuilder();
            var stdErr = new StringBuilder();

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(timeout);

                var result = await Cli.Wrap("git")
                    .WithArguments(arguments)
                    .WithWorkingDirectory(workingDirectory)
                    .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOut))
                    .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErr))
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteAsync(cts.Token);

                var output = stdOut.ToString();
                var error = stdErr.ToString();

                // Git often writes progress to stderr, so only treat as error if exit code is non-zero
                if (result.ExitCode != 0)
                {
                    var errorMessage = !string.IsNullOrEmpty(error) ? error : output;
                    
                    // Sanitize error message to remove credentials
                    errorMessage = SanitizeCredentials(errorMessage);
                    
                    throw new InvalidOperationException($"Git command failed (exit code {result.ExitCode}): {errorMessage}");
                }

                return output;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException($"Git command timed out after {timeout.TotalSeconds} seconds");
            }
        }

        /// <summary>
        /// Removes credentials from error messages to prevent logging sensitive information
        /// </summary>
        private static string SanitizeCredentials(string message)
        {
            if (string.IsNullOrEmpty(message))
                return message;

            // Remove various credential patterns from URLs
            // Pattern: https://username:password@host or https://token@host
            var sanitized = System.Text.RegularExpressions.Regex.Replace(
                message,
                @"(https?://)([^:@]+):([^@]+)@",
                "$1***:***@");

            sanitized = System.Text.RegularExpressions.Regex.Replace(
                sanitized,
                @"(https?://)([^@]+)@",
                "$1***@");

            return sanitized;
        }
    }
}
