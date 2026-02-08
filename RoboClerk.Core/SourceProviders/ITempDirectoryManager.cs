using System;
using System.Collections.Generic;

namespace RoboClerk.SourceProviders
{
    /// <summary>
    /// Manages temporary directories for cloned repositories and other temporary files
    /// </summary>
    public interface ITempDirectoryManager : IDisposable
    {
        /// <summary>
        /// Creates a new temporary directory with an optional prefix
        /// </summary>
        /// <param name="prefix">Optional prefix for the directory name</param>
        /// <returns>The full path to the created temporary directory</returns>
        string CreateTempDirectory(string prefix = "roboclerk");

        /// <summary>
        /// Registers an existing directory for cleanup when the manager is disposed
        /// </summary>
        /// <param name="directory">The directory path to register</param>
        void RegisterForCleanup(string directory);

        /// <summary>
        /// Cleans up a specific directory immediately
        /// </summary>
        /// <param name="directory">The directory to clean up</param>
        /// <returns>True if cleanup was successful, false otherwise</returns>
        bool Cleanup(string directory);

        /// <summary>
        /// Cleans up all registered directories
        /// </summary>
        void CleanupAll();

        /// <summary>
        /// Gets the base temporary directory path
        /// </summary>
        string BaseTempPath { get; }

        /// <summary>
        /// Gets the list of currently registered directories
        /// </summary>
        IReadOnlyList<string> RegisteredDirectories { get; }
    }
}
