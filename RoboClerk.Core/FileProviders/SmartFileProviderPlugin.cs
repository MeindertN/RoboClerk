using Microsoft.Extensions.DependencyInjection;
using RoboClerk.Core.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RoboClerk.Core.FileProviders
{
    /// <summary>
    /// Smart file provider that routes file operations based on URI-style path prefixes.
    /// Each registered provider declares its own prefix (e.g., "sp://" for SharePoint).
    /// Paths without a prefix are routed to the default (local) file provider.
    /// </summary>
    public class SmartFileProviderPlugin : IFileProviderPlugin
    {
        private readonly IFileProviderPlugin defaultProvider;
        private readonly Dictionary<string, IFileProviderPlugin> prefixProviders = new();
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Creates a smart file provider with a default (local) provider.
        /// Additional providers can be registered using RegisterProvider().
        /// </summary>
        /// <param name="defaultLocalProvider">The default provider for paths without prefixes (typically local file system).</param>
        public SmartFileProviderPlugin(IFileProviderPlugin defaultLocalProvider)
        {
            this.defaultProvider = defaultLocalProvider ?? throw new ArgumentNullException(nameof(defaultLocalProvider));
            logger.Info("Smart file provider initialized with default provider");
        }

        public string Name => "SmartFileProviderPlugin";
        public string Description => "Routes file operations based on path prefixes. No prefix = local file system.";

        /// <summary>
        /// Gets the path prefix for this smart provider (returns null as it routes to other providers).
        /// </summary>
        public string GetPathPrefix() => null;

        /// <summary>
        /// Registers a specialized file provider that handles a specific path prefix.
        /// The provider's GetPathPrefix() method determines which paths it handles.
        /// </summary>
        /// <param name="provider">The file provider to register.</param>
        /// <exception cref="ArgumentException">Thrown if the provider has no prefix or prefix is already registered.</exception>
        public void RegisterProvider(IFileProviderPlugin provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            var prefix = provider.GetPathPrefix();
            if (string.IsNullOrEmpty(prefix))
            {
                logger.Warn($"Provider {provider.Name} has no prefix, cannot register as specialized provider");
                throw new ArgumentException($"Provider {provider.Name} must have a non-empty prefix to be registered");
            }

            // Normalize prefix to lowercase for case-insensitive matching
            var normalizedPrefix = prefix.ToLowerInvariant();
            
            if (prefixProviders.ContainsKey(normalizedPrefix))
            {
                throw new ArgumentException($"A provider for prefix '{prefix}' is already registered");
            }

            prefixProviders[normalizedPrefix] = provider;
            logger.Info($"Registered provider '{provider.Name}' for prefix '{prefix}'");
        }

        /// <summary>
        /// Routes a file path to the appropriate provider based on its prefix.
        /// Rule: Explicit prefix uses specialized provider, no prefix uses default (local) provider.
        /// </summary>
        private (IFileProviderPlugin provider, string actualPath) RouteRequest(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));

            // Normalize path separators for cross-platform compatibility
            var normalizedPath = path.Replace('\\', '/');

            // Check for registered prefixes (case-insensitive)
            foreach (var kvp in prefixProviders)
            {
                if (normalizedPath.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    // Strip prefix and route to specialized provider
                    var actualPath = normalizedPath.Substring(kvp.Key.Length);
                    logger.Debug($"Routing '{path}' to {kvp.Value.Name} (stripped to: '{actualPath}')");
                    return (kvp.Value, actualPath);
                }
            }

            // No prefix = default (local) file system
            logger.Debug($"No prefix detected, routing '{path}' to default provider ({defaultProvider.Name})");
            return (defaultProvider, path);
        }

        /// <summary>
        /// Re-adds the appropriate prefix to a path after it's been processed by a specialized provider.
        /// </summary>
        private string AddPrefixIfNeeded(string path, IFileProviderPlugin provider)
        {
            if (provider == defaultProvider || string.IsNullOrEmpty(path))
                return path;

            var prefix = prefixProviders.FirstOrDefault(kvp => kvp.Value == provider).Key;
            return string.IsNullOrEmpty(prefix) ? path : prefix + path;
        }

        // IFileProviderPlugin implementation - all methods route through RouteRequest

        public bool FileExists(string path)
        {
            var (provider, actualPath) = RouteRequest(path);
            return provider.FileExists(actualPath);
        }

        public bool DirectoryExists(string path)
        {
            var (provider, actualPath) = RouteRequest(path);
            return provider.DirectoryExists(actualPath);
        }

        public string ReadAllText(string path)
        {
            var (provider, actualPath) = RouteRequest(path);
            return provider.ReadAllText(actualPath);
        }

        public async Task<string> ReadAllTextAsync(string path)
        {
            var (provider, actualPath) = RouteRequest(path);
            return await provider.ReadAllTextAsync(actualPath);
        }

        public List<string> ReadLines(string path)
        {
            var (provider, actualPath) = RouteRequest(path);
            return provider.ReadLines(actualPath);
        }

        public async Task<List<string>> ReadLinesAsync(string path)
        {
            var (provider, actualPath) = RouteRequest(path);
            return await provider.ReadLinesAsync(actualPath);
        }

        public byte[] ReadAllBytes(string path)
        {
            var (provider, actualPath) = RouteRequest(path);
            return provider.ReadAllBytes(actualPath);
        }

        public async Task<byte[]> ReadAllBytesAsync(string path)
        {
            var (provider, actualPath) = RouteRequest(path);
            return await provider.ReadAllBytesAsync(actualPath);
        }

        public void WriteAllText(string path, string contents)
        {
            var (provider, actualPath) = RouteRequest(path);
            provider.WriteAllText(actualPath, contents);
        }

        public async Task WriteAllTextAsync(string path, string contents)
        {
            var (provider, actualPath) = RouteRequest(path);
            await provider.WriteAllTextAsync(actualPath, contents);
        }

        public void WriteAllBytes(string path, byte[] bytes)
        {
            var (provider, actualPath) = RouteRequest(path);
            provider.WriteAllBytes(actualPath, bytes);
        }

        public async Task WriteAllBytesAsync(string path, byte[] bytes)
        {
            var (provider, actualPath) = RouteRequest(path);
            await provider.WriteAllBytesAsync(actualPath, bytes);
        }

        public Stream OpenRead(string path)
        {
            var (provider, actualPath) = RouteRequest(path);
            return provider.OpenRead(actualPath);
        }

        public Stream OpenWrite(string path, FileMode mode = FileMode.Create)
        {
            var (provider, actualPath) = RouteRequest(path);
            return provider.OpenWrite(actualPath, mode);
        }

        public void CreateDirectory(string path)
        {
            var (provider, actualPath) = RouteRequest(path);
            provider.CreateDirectory(actualPath);
        }

        public void DeleteFile(string path)
        {
            var (provider, actualPath) = RouteRequest(path);
            provider.DeleteFile(actualPath);
        }

        public void DeleteDirectory(string path, bool recursive = false)
        {
            var (provider, actualPath) = RouteRequest(path);
            provider.DeleteDirectory(actualPath, recursive);
        }

        public string[] GetFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            var (provider, actualPath) = RouteRequest(path);
            var files = provider.GetFiles(actualPath, searchPattern, searchOption);
            
            // Re-prefix results if they came from a specialized provider
            return files.Select(f => AddPrefixIfNeeded(f, provider)).ToArray();
        }

        public string[] GetDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            var (provider, actualPath) = RouteRequest(path);
            var directories = provider.GetDirectories(actualPath, searchPattern, searchOption);
            
            // Re-prefix results
            return directories.Select(d => AddPrefixIfNeeded(d, provider)).ToArray();
        }

        public void CopyFile(string sourcePath, string destinationPath, bool overwrite = false)
        {
            var (sourceProvider, actualSourcePath) = RouteRequest(sourcePath);
            var (destProvider, actualDestPath) = RouteRequest(destinationPath);
            
            // If both paths use the same provider, use its native copy
            if (sourceProvider == destProvider)
            {
                sourceProvider.CopyFile(actualSourcePath, actualDestPath, overwrite);
            }
            else
            {
                // Cross-provider copy: read from source, write to destination
                var bytes = sourceProvider.ReadAllBytes(actualSourcePath);
                destProvider.WriteAllBytes(actualDestPath, bytes);
            }
        }

        public void MoveFile(string sourcePath, string destinationPath, bool overwrite = false)
        {
            var (sourceProvider, actualSourcePath) = RouteRequest(sourcePath);
            var (destProvider, actualDestPath) = RouteRequest(destinationPath);
            
            // If both paths use the same provider, use its native move
            if (sourceProvider == destProvider)
            {
                sourceProvider.MoveFile(actualSourcePath, actualDestPath, overwrite);
            }
            else
            {
                // Cross-provider move: copy then delete
                CopyFile(sourcePath, destinationPath, overwrite);
                sourceProvider.DeleteFile(actualSourcePath);
            }
        }

        public DateTime GetLastWriteTime(string path)
        {
            var (provider, actualPath) = RouteRequest(path);
            return provider.GetLastWriteTime(actualPath);
        }

        public long GetFileSize(string path)
        {
            var (provider, actualPath) = RouteRequest(path);
            return provider.GetFileSize(actualPath);
        }

        public string Combine(params string[] paths)
        {
            if (paths == null || paths.Length == 0)
                return string.Empty;

            // Check if first path has a prefix - if so, preserve it
            var firstPath = paths[0];
            var (provider, actualFirstPath) = RouteRequest(firstPath);
            
            // Combine using the provider's logic
            var combinedPaths = new[] { actualFirstPath }.Concat(paths.Skip(1)).ToArray();
            var result = provider.Combine(combinedPaths);
            
            // Re-add prefix if needed
            return AddPrefixIfNeeded(result, provider);
        }

        public string GetDirectoryName(string path)
        {
            var (provider, actualPath) = RouteRequest(path);
            var dirName = provider.GetDirectoryName(actualPath);
            
            // Re-prefix if needed
            return AddPrefixIfNeeded(dirName, provider);
        }

        public string GetFileName(string path)
        {
            var (provider, actualPath) = RouteRequest(path);
            return provider.GetFileName(actualPath);
        }

        public string GetFileNameWithoutExtension(string path)
        {
            var (provider, actualPath) = RouteRequest(path);
            return provider.GetFileNameWithoutExtension(actualPath);
        }

        public string GetExtension(string path)
        {
            var (provider, actualPath) = RouteRequest(path);
            return provider.GetExtension(actualPath);
        }

        public string GetFullPath(string path)
        {
            var (provider, actualPath) = RouteRequest(path);
            var fullPath = provider.GetFullPath(actualPath);
            
            // Re-prefix if needed
            return AddPrefixIfNeeded(fullPath, provider);
        }

        public bool IsPathRooted(string path)
        {
            var (provider, actualPath) = RouteRequest(path);
            return provider.IsPathRooted(actualPath);
        }

        public string GetRelativePath(string relativeTo, string path)
        {
            var (relativeToProvider, actualRelativeTo) = RouteRequest(relativeTo);
            var (pathProvider, actualPath) = RouteRequest(path);
            
            // Both paths should use the same provider for relative path calculation
            if (relativeToProvider != pathProvider)
            {
                throw new ArgumentException("Cannot calculate relative path across different file providers");
            }
            
            return relativeToProvider.GetRelativePath(actualRelativeTo, actualPath);
        }

        public void InitializePlugin(IConfiguration configuration)
        {
            // Smart provider doesn't need configuration initialization
            // Individual providers handle their own initialization
            logger.Debug("Smart file provider initialization (no-op)");
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // Smart provider doesn't register services itself
            // Individual providers handle their own service registration
            logger.Debug("Smart file provider service configuration (no-op)");
        }
    }
}
