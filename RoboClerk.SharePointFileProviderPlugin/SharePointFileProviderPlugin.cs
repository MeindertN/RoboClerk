using System.Text;
using RoboClerk.Configuration;
using Tomlyn;
using Tomlyn.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Azure.Identity;

namespace RoboClerk.SharePointFileProvider
{
    /// <summary>
    /// SharePoint file provider plugin that provides access to SharePoint Online document libraries
    /// using Microsoft Graph SDK with OAuth2 authentication.
    /// </summary>
    public class SharePointFileProviderPlugin : FileProviderPluginBase, IFileProviderPlugin
    {
        private string siteUrl = string.Empty;
        private string clientId = string.Empty;
        private string clientSecret = string.Empty;
        private string tenantId = string.Empty;
        private string driveId = string.Empty;
        private GraphServiceClient graphClient = null;
        private IFileProviderPlugin localFileSystem = null;

        public SharePointFileProviderPlugin(IFileProviderPlugin localFileSystem)
        {
            this.localFileSystem = localFileSystem;
            name = "SharePointFileProviderPlugin";
            description = "Provides access to SharePoint Online document libraries using Microsoft Graph SDK.";
        }

        public override void InitializePlugin(IConfiguration configuration)
        {
            try
            {
                var config = GetConfigurationTable(configuration.PluginConfigDir, $"{name}.toml");
                siteUrl = GetObjectForKey<string>(config, "SiteUrl", true);
                clientId = GetObjectForKey<string>(config, "ClientId", true);
                clientSecret = GetObjectForKey<string>(config, "ClientSecret", true);
                tenantId = GetObjectForKey<string>(config, "TenantId", true);
                driveId = GetObjectForKey<string>(config, "DriveId", true);

                // Initialize Graph client with OAuth2 authentication
                var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                graphClient = new GraphServiceClient(credential);

                logger.Info($"SharePoint provider initialized for site: {siteUrl}, Drive ID: {driveId}");
            }
            catch (Exception ex)
            {
                logger.Error("Failed to initialize SharePoint provider", ex);
                throw;
            }
        }

        /// <summary>
        /// Gets the configuration table from the specified file.
        /// </summary>
        /// <param name="pluginConfDir">The plugin configuration directory.</param>
        /// <param name="confFileName">The configuration file name.</param>
        /// <returns>The configuration table.</returns>
        private TomlTable GetConfigurationTable(string pluginConfDir, string confFileName)
        {
            if (string.IsNullOrEmpty(pluginConfDir) || string.IsNullOrEmpty(confFileName))
            {
                throw new ArgumentException("Cannot get configuration table because the plugin configuration directory or the config filename are empty.");
            }
            
            var configFileLocation = Path.Combine(pluginConfDir, confFileName);
            return Toml.Parse(File.ReadAllText(configFileLocation)).ToModel();
        }

        /// <summary>
        /// Sets the Drive ID dynamically after initialization.
        /// </summary>
        /// <param name="newDriveId">The new Drive ID to set.</param>
        public void SetDriveId(string newDriveId)
        {
            if (string.IsNullOrWhiteSpace(newDriveId))
            {
                throw new ArgumentException("Drive ID cannot be null or empty.", nameof(newDriveId));
            }

            driveId = newDriveId;
            logger.Info($"Drive ID updated to: {driveId}");
        }

        /// <summary>
        /// Gets the current Drive ID.
        /// </summary>
        /// <returns>The current Drive ID.</returns>
        public string GetDriveId()
        {
            return driveId;
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<SharePointFileProviderPlugin>();
        }

        public override bool FileExists(string path)
        {
            ValidatePath(path);
            try
            {
                var item = GetDriveItemAsync(path).Result;
                return item != null && item.Folder == null;
            }
            catch
            {
                return false;
            }
        }

        public override bool DirectoryExists(string path)
        {
            ValidatePath(path);
            try
            {
                var item = GetDriveItemAsync(path).Result;
                return item != null && item.Folder != null;
            }
            catch
            {
                return false;
            }
        }

        public override string ReadAllText(string path)
        {
            ValidatePath(path);
            try
            {
                var content = GetFileContentAsync(path).Result;
                return Encoding.UTF8.GetString(content);
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to read file: {path}", ex);
                throw new FileNotFoundException($"File not found or cannot be read: {path}", ex);
            }
        }

        public override async Task<string> ReadAllTextAsync(string path)
        {
            return await Task.Run(() => ReadAllText(path));
        }

        public override List<string> ReadLines(string path)
        {
            ValidatePath(path);
            try
            {
                var content = ReadAllText(path);
                return new List<string>(content.Split(new[] { Environment.NewLine, "\n", "\r\n" }, StringSplitOptions.None));
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to read lines from file: {path}", ex);
                throw new FileNotFoundException($"File not found or cannot be read: {path}", ex);
            }
        }

        public override async Task<List<string>> ReadLinesAsync(string path)
        {
            return await Task.Run(() => ReadLines(path));
        }

        public override byte[] ReadAllBytes(string path)
        {
            ValidatePath(path);
            try
            {
                return GetFileContentAsync(path).Result;
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to read file: {path}", ex);
                throw new FileNotFoundException($"File not found or cannot be read: {path}", ex);
            }
        }

        public override async Task<byte[]> ReadAllBytesAsync(string path)
        {
            return await Task.Run(() => ReadAllBytes(path));
        }

        public override void WriteAllText(string path, string contents)
        {
            ValidatePath(path);
            ValidatePath(contents, "contents");
            
            try
            {
                var bytes = Encoding.UTF8.GetBytes(contents);
                UploadFileAsync(path, bytes).Wait();
                logger.Debug($"File written to SharePoint: {path}");
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to write file: {path}", ex);
                throw;
            }
        }

        public override async Task WriteAllTextAsync(string path, string contents)
        {
            await Task.Run(() => WriteAllText(path, contents));
        }

        public override void WriteAllBytes(string path, byte[] bytes)
        {
            ValidatePath(path);
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }
            
            try
            {
                UploadFileAsync(path, bytes).Wait();
                logger.Debug($"File written to SharePoint: {path}");
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to write file: {path}", ex);
                throw;
            }
        }

        public override async Task WriteAllBytesAsync(string path, byte[] bytes)
        {
            await Task.Run(() => WriteAllBytes(path, bytes));
        }

        public override Stream OpenRead(string path)
        {
            ValidatePath(path);
            try
            {
                var content = GetFileContentAsync(path).Result;
                return new MemoryStream(content);
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to open file for reading: {path}", ex);
                throw new FileNotFoundException($"File not found or cannot be read: {path}", ex);
            }
        }

        public override Stream OpenWrite(string path, FileMode mode = FileMode.Create)
        {
            ValidatePath(path);
            
            // For SharePoint, we'll use a memory stream and upload when disposed
            var memoryStream = new SharePointMemoryStream(path, this, mode);
            return memoryStream;
        }

        public override void CreateDirectory(string path)
        {
            ValidatePath(path);
            try
            {
                CreateFolderAsync(path).Wait();
                logger.Info($"Directory created in SharePoint: {path}");
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to create directory: {path}", ex);
                throw;
            }
        }

        public override void DeleteFile(string path)
        {
            ValidatePath(path);
            try
            {
                DeleteItemAsync(path).Wait();
                logger.Info($"File deleted from SharePoint: {path}");
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to delete file: {path}", ex);
                throw;
            }
        }

        public override void DeleteDirectory(string path, bool recursive = false)
        {
            ValidatePath(path);
            try
            {
                if (recursive)
                {
                    // Get all items in the directory and delete them
                    var itemsResponse = GetItemsInFolderAsync(path).Result;
                    if (itemsResponse?.Value != null)
                    {
                        foreach (var item in itemsResponse.Value)
                        {
                            if (item.Name != null)
                            {
                                var fullPath = Combine(path, item.Name);
                                DeleteItemAsync(fullPath).Wait();
                            }
                        }
                    }
                }
                
                DeleteItemAsync(path).Wait();
                logger.Info($"Directory deleted from SharePoint: {path}");
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to delete directory: {path}", ex);
                throw;
            }
        }

        public override string[] GetFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            ValidatePath(path);
            try
            {
                var itemsResponse = GetItemsInFolderAsync(path).Result;
                var files = itemsResponse?.Value?
                    .Where(item => item.Folder == null && item.Name != null && MatchesPattern(item.Name, searchPattern))
                    .Select(item => Combine(path, item.Name))
                    .ToArray() ?? Array.Empty<string>();

                if (searchOption == SearchOption.AllDirectories)
                {
                    var subDirs = GetDirectories(path, "*", SearchOption.TopDirectoryOnly);
                    var subFiles = subDirs.SelectMany(dir => GetFiles(dir, searchPattern, SearchOption.AllDirectories));
                    files = files.Concat(subFiles).ToArray();
                }

                return files;
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to get files: {path}", ex);
                throw;
            }
        }

        public override string[] GetDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            ValidatePath(path);
            try
            {
                var itemsResponse = GetItemsInFolderAsync(path).Result;
                var directories = itemsResponse?.Value?
                    .Where(item => item.Folder != null && item.Name != null && MatchesPattern(item.Name, searchPattern))
                    .Select(item => Combine(path, item.Name))
                    .ToArray() ?? Array.Empty<string>();

                if (searchOption == SearchOption.AllDirectories)
                {
                    var subDirs = directories.SelectMany(dir => GetDirectories(dir, searchPattern, SearchOption.AllDirectories));
                    directories = directories.Concat(subDirs).ToArray();
                }

                return directories;
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to get directories: {path}", ex);
                throw;
            }
        }

        public override void CopyFile(string sourcePath, string destinationPath, bool overwrite = false)
        {
            ValidatePath(sourcePath, "sourcePath");
            ValidatePath(destinationPath, "destinationPath");
            
            try
            {
                // Read from source and write to destination
                var content = GetFileContentAsync(sourcePath).Result;
                UploadFileAsync(destinationPath, content).Wait();
                logger.Info($"File copied in SharePoint: {sourcePath} -> {destinationPath}");
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to copy file: {sourcePath} -> {destinationPath}", ex);
                throw;
            }
        }

        public override void MoveFile(string sourcePath, string destinationPath, bool overwrite = false)
        {
            ValidatePath(sourcePath, "sourcePath");
            ValidatePath(destinationPath, "destinationPath");
            
            try
            {
                // Copy then delete
                CopyFile(sourcePath, destinationPath, overwrite);
                DeleteFile(sourcePath);
                logger.Info($"File moved in SharePoint: {sourcePath} -> {destinationPath}");
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to move file: {sourcePath} -> {destinationPath}", ex);
                throw;
            }
        }

        public override DateTime GetLastWriteTime(string path)
        {
            ValidatePath(path);
            try
            {
                var item = GetDriveItemAsync(path).Result;
                if (item?.LastModifiedDateTime?.DateTime != null)
                {
                    return item.LastModifiedDateTime.Value.DateTime;
                }
                return DateTime.MinValue;
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to get last write time: {path}", ex);
                throw;
            }
        }

        public override long GetFileSize(string path)
        {
            ValidatePath(path);
            try
            {
                var item = GetDriveItemAsync(path).Result;
                return item?.Size ?? 0;
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to get file size: {path}", ex);
                throw;
            }
        }

        public override string Combine(params string[] paths)
        {
            if (paths == null || paths.Length == 0)
            {
                throw new ArgumentException("At least one path must be provided.");
            }
            
            // For SharePoint, use forward slashes and normalize
            var combined = string.Join("/", paths.Select(p => p?.TrimStart('/').TrimEnd('/')));
            return "/" + combined;
        }

        public override string GetDirectoryName(string path)
        {
            ValidatePath(path);
            var normalizedPath = path.TrimEnd('/');
            var lastSlash = normalizedPath.LastIndexOf('/');
            return lastSlash > 0 ? normalizedPath.Substring(0, lastSlash) : "/";
        }

        public override string GetFileName(string path)
        {
            ValidatePath(path);
            var normalizedPath = path.TrimEnd('/');
            var lastSlash = normalizedPath.LastIndexOf('/');
            return lastSlash >= 0 ? normalizedPath.Substring(lastSlash + 1) : normalizedPath;
        }

        public override string GetFileNameWithoutExtension(string path)
        {
            var fileName = GetFileName(path);
            var lastDot = fileName.LastIndexOf('.');
            return lastDot > 0 ? fileName.Substring(0, lastDot) : fileName;
        }

        public override string GetExtension(string path)
        {
            var fileName = GetFileName(path);
            var lastDot = fileName.LastIndexOf('.');
            return lastDot > 0 ? fileName.Substring(lastDot) : string.Empty;
        }

        public override string GetFullPath(string path)
        {
            ValidatePath(path);
            // For SharePoint, normalize the path
            return "/" + path.TrimStart('/').TrimEnd('/');
        }

        public override bool IsPathRooted(string path)
        {
            ValidatePath(path);
            return path.StartsWith("/");
        }

        public override string GetRelativePath(string relativeTo, string path)
        {
            ValidatePath(relativeTo, "relativeTo");
            ValidatePath(path, "path");
            
            // Simple relative path calculation for SharePoint
            if (path.StartsWith(relativeTo))
            {
                var relative = path.Substring(relativeTo.Length).TrimStart('/');
                return relative.Length > 0 ? relative : ".";
            }
            
            throw new ArgumentException("Path is not relative to the base path.");
        }

        #region Private Helper Methods

        private async Task<DriveItem?> GetDriveItemAsync(string path)
        {
            try
            {
                var normalizedPath = path.TrimStart('/');
                return await graphClient.Drives[driveId].Root.ItemWithPath(normalizedPath).GetAsync();
            }
            catch (ServiceException ex) when (ex.ResponseStatusCode == (int)System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        private async Task<byte[]> GetFileContentAsync(string path)
        {
            var normalizedPath = path.TrimStart('/');
            var content = await graphClient.Drives[driveId].Root.ItemWithPath(normalizedPath).Content.GetAsync();
            using var memoryStream = new MemoryStream();
            await content.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }

        public async Task UploadFileAsync(string path, byte[] content)
        {
            var normalizedPath = path.TrimStart('/');
            using var stream = new MemoryStream(content);
            await graphClient.Drives[driveId].Root.ItemWithPath(normalizedPath).Content.PutAsync(stream);
        }

        private async Task CreateFolderAsync(string path)
        {
            var folderName = GetFileName(path);
            var parentPath = GetDirectoryName(path);
            var normalizedParentPath = parentPath.TrimStart('/');
            
            var driveItem = new DriveItem
            {
                Name = folderName,
                Folder = new Folder()
            };
            
            await graphClient.Drives[driveId].Root.ItemWithPath(normalizedParentPath).Children.PostAsync(driveItem);
        }

        private async Task DeleteItemAsync(string path)
        {
            var normalizedPath = path.TrimStart('/');
            await graphClient.Drives[driveId].Root.ItemWithPath(normalizedPath).DeleteAsync();
        }

        private async Task<DriveItemCollectionResponse?> GetItemsInFolderAsync(string path)
        {
            var normalizedPath = path.TrimStart('/');
            return await graphClient.Drives[driveId].Root.ItemWithPath(normalizedPath).Children.GetAsync();
        }

        private bool MatchesPattern(string fileName, string pattern)
        {
            if (pattern == "*") return true;
            
            // Simple wildcard matching
            var regexPattern = pattern.Replace(".", "\\.").Replace("*", ".*").Replace("?", ".");
            return System.Text.RegularExpressions.Regex.IsMatch(fileName, regexPattern);
        }

        #endregion

        #region SharePoint Memory Stream

        private class SharePointMemoryStream : MemoryStream
        {
            private readonly string path;
            private readonly SharePointFileProviderPlugin provider;
            private readonly FileMode mode;
            private bool disposed = false;

            public SharePointMemoryStream(string path, SharePointFileProviderPlugin provider, FileMode mode)
            {
                this.path = path;
                this.provider = provider;
                this.mode = mode;
            }

            protected override void Dispose(bool disposing)
            {
                if (!disposed && disposing)
                {
                    try
                    {
                        var content = ToArray();
                        provider.UploadFileAsync(path, content).Wait();
                    }
                    catch (Exception ex)
                    {
                        NLog.LogManager.GetCurrentClassLogger().Error($"Failed to upload file during stream disposal: {path}", ex);
                    }
                }
                
                disposed = true;
                base.Dispose(disposing);
            }
        }

        #endregion
    }
} 