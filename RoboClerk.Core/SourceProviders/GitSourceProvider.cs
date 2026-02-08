using System;
using System.Threading;
using System.Threading.Tasks;
using RoboClerk.Core.FileProviders;

namespace RoboClerk.SourceProviders
{
    /// <summary>
    /// Source provider that clones from a Git repository
    /// </summary>
    public class GitSourceProvider : ISourceProvider
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly GitSourceConfiguration config;
        private readonly IGitCloneService cloneService;
        private readonly ITempDirectoryManager tempManager;
        private readonly IFileProviderPlugin fileProvider;
        
        private string? clonedPath;
        private string? scanPath;
        private bool disposed;

        /// <summary>
        /// Creates a new GitSourceProvider
        /// </summary>
        /// <param name="config">Git source configuration</param>
        /// <param name="cloneService">Service for cloning repositories</param>
        /// <param name="tempManager">Manager for temporary directories</param>
        /// <param name="fileProvider">File provider for filesystem operations</param>
        public GitSourceProvider(
            GitSourceConfiguration config,
            IGitCloneService cloneService,
            ITempDirectoryManager tempManager,
            IFileProviderPlugin fileProvider)
        {
            this.config = config?.Clone() ?? throw new ArgumentNullException(nameof(config));
            this.cloneService = cloneService ?? throw new ArgumentNullException(nameof(cloneService));
            this.tempManager = tempManager ?? throw new ArgumentNullException(nameof(tempManager));
            this.fileProvider = fileProvider ?? throw new ArgumentNullException(nameof(fileProvider));
        }

        /// <inheritdoc />
        public string SourceIdentifier
        {
            get
            {
                var reference = config.GetCheckoutReference();
                var refPart = string.IsNullOrEmpty(reference) ? "" : $"@{reference}";
                var pathPart = string.IsNullOrEmpty(config.SourcePath) ? "" : $":{config.SourcePath}";
                return $"git:{config.RepositoryUrl}{refPart}{pathPart}";
            }
        }

        /// <inheritdoc />
        public bool RequiresCleanup => true;

        /// <inheritdoc />
        public bool IsPrepared => !string.IsNullOrEmpty(scanPath);

        /// <inheritdoc />
        public async Task<string> PrepareSourceAsync(CancellationToken cancellationToken = default)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(GitSourceProvider));

            if (IsPrepared)
            {
                logger.Debug($"Source already prepared: {scanPath}");
                return scanPath!;
            }

            logger.Info($"Preparing Git source: {SourceIdentifier}");

            try
            {
                // Create a temporary directory for the clone
                var repoName = ExtractRepoName(config.RepositoryUrl);
                clonedPath = tempManager.CreateTempDirectory($"git_{repoName}");

                // Clone the repository
                scanPath = await cloneService.CloneRepositoryAsync(config, clonedPath, cancellationToken);

                // Verify the scan path exists
                if (!fileProvider.DirectoryExists(scanPath))
                {
                    throw new System.IO.DirectoryNotFoundException(
                        $"Scan path does not exist after clone: {scanPath}");
                }

                logger.Info($"Git source prepared successfully: {scanPath}");
                return scanPath;
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to prepare Git source: {ex.Message}");
                
                // Cleanup on failure
                if (!string.IsNullOrEmpty(clonedPath))
                {
                    tempManager.Cleanup(clonedPath);
                    clonedPath = null;
                }
                scanPath = null;
                
                throw;
            }
        }

        /// <inheritdoc />
        public string GetScanPath()
        {
            if (!IsPrepared)
                throw new InvalidOperationException("Source has not been prepared. Call PrepareSourceAsync first.");

            return scanPath!;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!disposed)
            {
                if (!string.IsNullOrEmpty(clonedPath))
                {
                    logger.Debug($"Cleaning up cloned repository: {clonedPath}");
                    tempManager.Cleanup(clonedPath);
                    clonedPath = null;
                }
                scanPath = null;
                disposed = true;
            }
        }

        private static string ExtractRepoName(string repositoryUrl)
        {
            try
            {
                // Handle various URL formats
                // https://github.com/user/repo.git -> repo
                // git@github.com:user/repo.git -> repo
                // https://dev.azure.com/org/project/_git/repo -> repo

                var url = repositoryUrl.TrimEnd('/');
                
                // Remove .git suffix
                if (url.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
                {
                    url = url.Substring(0, url.Length - 4);
                }

                // Get the last segment
                var lastSlash = url.LastIndexOf('/');
                if (lastSlash >= 0)
                {
                    var name = url.Substring(lastSlash + 1);
                    
                    // Handle git@host:path format
                    var colonIndex = name.IndexOf(':');
                    if (colonIndex >= 0)
                    {
                        name = name.Substring(colonIndex + 1);
                        lastSlash = name.LastIndexOf('/');
                        if (lastSlash >= 0)
                        {
                            name = name.Substring(lastSlash + 1);
                        }
                    }

                    // Sanitize the name for use in directory
                    return SanitizeForPath(name);
                }

                return "repo";
            }
            catch
            {
                return "repo";
            }
        }

        private static string SanitizeForPath(string name)
        {
            // Remove invalid path characters
            var invalid = System.IO.Path.GetInvalidFileNameChars();
            foreach (var c in invalid)
            {
                name = name.Replace(c, '_');
            }
            return name;
        }
    }
}
