using System.Threading;
using System.Threading.Tasks;

namespace RoboClerk.SourceProviders
{
    /// <summary>
    /// Service for cloning Git repositories
    /// </summary>
    public interface IGitCloneService
    {
        /// <summary>
        /// Clones a repository to the specified target directory
        /// </summary>
        /// <param name="config">The Git source configuration</param>
        /// <param name="targetDirectory">The directory to clone into</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The path to the cloned repository</returns>
        Task<string> CloneRepositoryAsync(
            GitSourceConfiguration config,
            string targetDirectory,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the default branch name for a repository
        /// </summary>
        /// <param name="config">The Git source configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The default branch name</returns>
        Task<string> GetDefaultBranchAsync(
            GitSourceConfiguration config,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if git is available on the system
        /// </summary>
        /// <returns>True if git is available</returns>
        bool IsGitAvailable();

        /// <summary>
        /// Gets the version of git installed on the system
        /// </summary>
        /// <returns>The git version string</returns>
        string GetGitVersion();
    }
}
