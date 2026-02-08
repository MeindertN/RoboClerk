using System;
using System.Threading;
using System.Threading.Tasks;

namespace RoboClerk.SourceProviders
{
    /// <summary>
    /// Provides access to source code from various sources (local filesystem, Git repositories, etc.)
    /// </summary>
    public interface ISourceProvider : IDisposable
    {
        /// <summary>
        /// Prepares the source for scanning. For local sources, this is a no-op.
        /// For remote sources (Git), this clones/downloads the source to a local directory.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The local path where the source can be accessed</returns>
        Task<string> PrepareSourceAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the effective local path to scan for source files
        /// </summary>
        string GetScanPath();

        /// <summary>
        /// Gets a unique identifier for this source (e.g., local path, repo URL + ref)
        /// </summary>
        string SourceIdentifier { get; }

        /// <summary>
        /// Indicates if this source uses a temporary directory that should be cleaned up
        /// </summary>
        bool RequiresCleanup { get; }

        /// <summary>
        /// Indicates if the source has been prepared and is ready for scanning
        /// </summary>
        bool IsPrepared { get; }
    }
}
