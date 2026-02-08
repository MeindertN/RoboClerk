using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;

namespace RoboClerk.SourceProviders
{
    /// <summary>
    /// Default implementation of ITempDirectoryManager
    /// </summary>
    public class TempDirectoryManager : ITempDirectoryManager
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IFileSystem fileSystem;
        private readonly List<string> registeredDirectories = new();
        private readonly object lockObject = new();
        private readonly string baseTempPath;
        private bool disposed;

        /// <summary>
        /// Creates a new TempDirectoryManager with the default system temp path
        /// </summary>
        /// <param name="fileSystem">File system abstraction for testing</param>
        public TempDirectoryManager(IFileSystem fileSystem) 
            : this(fileSystem, null)
        {
        }

        /// <summary>
        /// Creates a new TempDirectoryManager with a custom base path
        /// </summary>
        /// <param name="fileSystem">File system abstraction for testing</param>
        /// <param name="customBasePath">Custom base path for temp directories (null for system default)</param>
        public TempDirectoryManager(IFileSystem fileSystem, string? customBasePath)
        {
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            
            if (string.IsNullOrWhiteSpace(customBasePath))
            {
                baseTempPath = fileSystem.Path.Combine(
                    fileSystem.Path.GetTempPath(), 
                    "RoboClerk");
            }
            else
            {
                baseTempPath = customBasePath;
            }

            // Ensure base directory exists
            if (!fileSystem.Directory.Exists(baseTempPath))
            {
                fileSystem.Directory.CreateDirectory(baseTempPath);
                logger.Debug($"Created base temp directory: {baseTempPath}");
            }
        }

        /// <inheritdoc />
        public string BaseTempPath => baseTempPath;

        /// <inheritdoc />
        public IReadOnlyList<string> RegisteredDirectories
        {
            get
            {
                lock (lockObject)
                {
                    return registeredDirectories.ToList().AsReadOnly();
                }
            }
        }

        /// <inheritdoc />
        public string CreateTempDirectory(string prefix = "roboclerk")
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(TempDirectoryManager));

            var dirName = $"{prefix}_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}";
            var fullPath = fileSystem.Path.Combine(baseTempPath, dirName);

            fileSystem.Directory.CreateDirectory(fullPath);
            RegisterForCleanup(fullPath);

            logger.Debug($"Created temp directory: {fullPath}");
            return fullPath;
        }

        /// <inheritdoc />
        public void RegisterForCleanup(string directory)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(TempDirectoryManager));

            if (string.IsNullOrWhiteSpace(directory))
                throw new ArgumentException("Directory path cannot be null or empty", nameof(directory));

            lock (lockObject)
            {
                if (!registeredDirectories.Contains(directory))
                {
                    registeredDirectories.Add(directory);
                    logger.Debug($"Registered directory for cleanup: {directory}");
                }
            }
        }

        /// <inheritdoc />
        public bool Cleanup(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
                return false;

            try
            {
                if (fileSystem.Directory.Exists(directory))
                {
                    // Remove read-only attributes recursively (git files are often read-only)
                    RemoveReadOnlyAttributes(directory);
                    fileSystem.Directory.Delete(directory, recursive: true);
                    logger.Info($"Cleaned up directory: {directory}");
                }

                lock (lockObject)
                {
                    registeredDirectories.Remove(directory);
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.Warn($"Failed to cleanup directory {directory}: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc />
        public void CleanupAll()
        {
            List<string> dirsToClean;
            lock (lockObject)
            {
                dirsToClean = registeredDirectories.ToList();
            }

            foreach (var dir in dirsToClean)
            {
                Cleanup(dir);
            }

            logger.Info($"Cleaned up {dirsToClean.Count} temporary directories");
        }

        private void RemoveReadOnlyAttributes(string directory)
        {
            try
            {
                var dirInfo = fileSystem.DirectoryInfo.New(directory);
                
                foreach (var file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
                {
                    if (file.Attributes.HasFlag(FileAttributes.ReadOnly))
                    {
                        file.Attributes &= ~FileAttributes.ReadOnly;
                    }
                }

                foreach (var subDir in dirInfo.GetDirectories("*", SearchOption.AllDirectories))
                {
                    if (subDir.Attributes.HasFlag(FileAttributes.ReadOnly))
                    {
                        subDir.Attributes &= ~FileAttributes.ReadOnly;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Debug($"Error removing read-only attributes in {directory}: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!disposed)
            {
                CleanupAll();
                disposed = true;
            }
        }
    }
}
