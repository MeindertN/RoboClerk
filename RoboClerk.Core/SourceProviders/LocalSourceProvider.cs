using System;
using System.Threading;
using System.Threading.Tasks;
using RoboClerk.Core.FileProviders;

namespace RoboClerk.SourceProviders
{
    /// <summary>
    /// Source provider for local filesystem directories
    /// </summary>
    public class LocalSourceProvider : ISourceProvider
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly string directoryPath;
        private readonly string projectRoot;
        private readonly IFileProviderPlugin fileProvider;
        private string resolvedPath = string.Empty;
        private bool disposed;

        /// <summary>
        /// Creates a new LocalSourceProvider
        /// </summary>
        /// <param name="directoryPath">The directory path (may contain {PROJECTROOT} placeholder)</param>
        /// <param name="projectRoot">The project root path for placeholder substitution</param>
        /// <param name="fileProvider">File provider for filesystem operations</param>
        public LocalSourceProvider(string directoryPath, string projectRoot, IFileProviderPlugin fileProvider)
        {
            this.directoryPath = directoryPath ?? throw new ArgumentNullException(nameof(directoryPath));
            this.projectRoot = projectRoot ?? string.Empty;
            this.fileProvider = fileProvider ?? throw new ArgumentNullException(nameof(fileProvider));
        }

        /// <inheritdoc />
        public string SourceIdentifier => $"local:{resolvedPath ?? directoryPath}";

        /// <inheritdoc />
        public bool RequiresCleanup => false;

        /// <inheritdoc />
        public bool IsPrepared => !string.IsNullOrEmpty(resolvedPath);

        /// <inheritdoc />
        public Task<string> PrepareSourceAsync(CancellationToken cancellationToken = default)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(LocalSourceProvider));

            // Resolve the path by substituting placeholders
            resolvedPath = ResolvePath(directoryPath);

            // Verify the directory exists
            if (!fileProvider.DirectoryExists(resolvedPath))
            {
                throw new System.IO.DirectoryNotFoundException(
                    $"Source directory not found: {resolvedPath}");
            }

            logger.Debug($"Local source prepared: {resolvedPath}");
            return Task.FromResult(resolvedPath);
        }

        /// <inheritdoc />
        public string GetScanPath()
        {
            if (!IsPrepared)
                throw new InvalidOperationException("Source has not been prepared. Call PrepareSourceAsync first.");

            return resolvedPath;
        }

        private string ResolvePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            // Replace {PROJECTROOT} placeholder
            var resolved = path.Replace("{PROJECTROOT}", projectRoot);

            // Ensure proper path separators
            resolved = resolved.Replace('/', System.IO.Path.DirectorySeparatorChar)
                              .Replace('\\', System.IO.Path.DirectorySeparatorChar);

            // Get full path
            return fileProvider.GetFullPath(resolved);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!disposed)
            {
                // No cleanup needed for local directories
                disposed = true;
            }
        }
    }
}
