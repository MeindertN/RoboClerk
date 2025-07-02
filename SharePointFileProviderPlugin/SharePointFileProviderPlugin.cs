using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RoboClerk.Configuration;
using Tomlyn.Model;
using Microsoft.Extensions.DependencyInjection;
using RestSharp;

namespace RoboClerk.SharePointFileProvider
{
    public interface ISharePointClient
    {
        RestRequest CreateRequest(string resource, Method method);
        Task<RestResponse<T>> ExecuteAsync<T>(RestRequest request) where T : new();
        Task<RestResponse> ExecuteAsync(RestRequest request);
        RestResponse<T> Execute<T>(RestRequest request) where T : new();
        RestResponse Execute(RestRequest request);
        void AddDefaultHeader(string name, string value);
    }

    // Concrete implementation using RestSharp
    public class RestSharpSharePointClient : ISharePointClient
    {
        private readonly RestClient _client;
        private readonly string _graphApiBase = "https://graph.microsoft.com/v1.0";

        public RestSharpSharePointClient()
        {
            _client = new RestClient(_graphApiBase);
        }

        public RestRequest CreateRequest(string resource, Method method)
        {
            return new RestRequest(resource, method);
        }

        public RestResponse Execute(RestRequest request)
        {
            return _client.ExecuteAsync(request).GetAwaiter().GetResult();
        }

        public RestResponse<T> Execute<T>(RestRequest request) where T : new()
        {
            return _client.ExecuteAsync<T>(request).GetAwaiter().GetResult();
        }

        public async Task<RestResponse<T>> ExecuteAsync<T>(RestRequest request) where T : new()
        {
            return await _client.ExecuteAsync<T>(request);
        }

        public async Task<RestResponse> ExecuteAsync(RestRequest request)
        {
            return await _client.ExecuteAsync(request);
        }

        public void AddDefaultHeader(string name, string value)
        {
            _client.AddDefaultHeader(name, value);
        }
    }

    /// <summary>
    /// SharePoint file provider plugin that provides access to SharePoint Online document libraries
    /// using Microsoft Graph API.
    /// </summary>
    public class SharePointFileProviderPlugin : FileProviderPluginBase, IFileProviderPlugin
    {
        private string siteUrl=string.Empty;
        private string accessToken=string.Empty;
        private string driveId = string.Empty;
        private readonly ISharePointClient _client;
        private readonly string graphApiBase = "https://graph.microsoft.com/v1.0";

        public SharePointFileProviderPlugin(ISharePointClient client)
        {
            name = "SharePointFileProvider";
            description = "Provides access to SharePoint Online document libraries using Microsoft Graph API.";
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public override void InitializePlugin(IConfiguration configuration)
        {
            try
            {
                /*var config = GetConfigurationTable(configuration.PluginConfigDir, $"{name}.toml");
                siteUrl = GetObjectForKey<string>(config, "SiteUrl", true);
                accessToken = GetObjectForKey<string>(config, "AccessToken", true);
                driveId = GetObjectForKey<string>(config, "DriveId", false);

                // Set up HTTP client with authentication
                _client.AddDefaultHeader("Authorization", $"Bearer {accessToken}");
                _client.AddDefaultHeader("Accept", "application/json");

                // If DriveId is not provided, get the default document library
                if (string.IsNullOrEmpty(driveId))
                {
                    driveId = GetDefaultDriveId().Result;
                }*/

                logger.Info($"SharePoint provider initialized for site: {siteUrl}, Drive ID: {driveId}");
            }
            catch (Exception ex)
            {
                logger.Error("Failed to initialize SharePoint provider", ex);
                throw;
            }
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<ISharePointClient>(provider => {
                return new RestSharpSharePointClient();
            });
            

        }

        public override bool FileExists(string path)
        {
            ValidatePath(path);
            try
            {
                var item = GetDriveItem(path).Result;
                return item.HasValue && !item.Value.GetProperty("folder").ValueKind.Equals(JsonValueKind.Undefined);
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
                var item = GetDriveItem(path).Result;
                return item.HasValue && item.Value.GetProperty("folder").ValueKind.Equals(JsonValueKind.Object);
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
                var content = GetFileContent(path).Result;
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
                return GetFileContent(path).Result;
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
                UploadFile(path, bytes).Wait();
                logger.Info($"File written to SharePoint: {path}");
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
                UploadFile(path, bytes).Wait();
                logger.Info($"File written to SharePoint: {path}");
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
                var content = GetFileContent(path).Result;
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
                CreateFolder(path).Wait();
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
                DeleteItem(path).Wait();
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
                    var items = GetItemsInFolder(path).Result;
                    foreach (var item in items)
                    {
                        var itemPath = item.GetProperty("name").GetString();
                        if (itemPath != null)
                        {
                            var fullPath = Combine(path, itemPath);
                            DeleteItem(fullPath).Wait();
                        }
                    }
                }
                
                DeleteItem(path).Wait();
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
                var items = GetItemsInFolder(path).Result;
                var files = items
                    .Where(item =>
                    {
                        // Try to get the "folder" property and check if it's not an object
                        if (!item.TryGetProperty("folder", out var folderProp) || folderProp.ValueKind == JsonValueKind.Object)
                            return false;

                        // Try to get the "name" property and ensure it's a string
                        if (!item.TryGetProperty("name", out var nameProp) || nameProp.ValueKind != JsonValueKind.String)
                            return false;

                        var name = nameProp.GetString();
                        return name != null && MatchesPattern(name, searchPattern);
                    })
                    .Select(item =>
                    {
                        var name = item.GetProperty("name").GetString(); // Safe now, already checked
                        return Combine(path, name!);
                    })
                    .ToArray();

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
                var items = GetItemsInFolder(path).Result;
                var directories = items
                    .Where(item =>
                    {
                        // Ensure "folder" exists and is an object
                        if (!item.TryGetProperty("folder", out var folderProp) || folderProp.ValueKind != JsonValueKind.Object)
                            return false;

                        // Ensure "name" exists and is a string
                        if (!item.TryGetProperty("name", out var nameProp) || nameProp.ValueKind != JsonValueKind.String)
                            return false;

                        var name = nameProp.GetString();
                        return name != null && MatchesPattern(name, searchPattern);
                    })
                    .Select(item =>
                    {
                        var name = item.GetProperty("name").GetString(); // Safe because we filtered above
                        return Combine(path, name!);
                    })
                    .ToArray();


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
                var content = GetFileContent(sourcePath).Result;
                UploadFile(destinationPath, content).Wait();
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
                var item = GetDriveItem(path).Result;
                if (item.HasValue && item.Value.TryGetProperty("lastModifiedDateTime", out var lastModified))
                {
                    var str = lastModified.GetString();
                    if (!string.IsNullOrEmpty(str))
                    {
                        return DateTime.Parse(str);
                    }
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
                var item = GetDriveItem(path).Result;
                if (item.HasValue && item.Value.TryGetProperty("size", out var size))
                {
                    return size.GetInt64();
                }
                return 0;
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

        private async Task<string> GetDefaultDriveId()
        {
            var url = $"/sites/{siteUrl}/drives";
            var request = _client.CreateRequest(url, Method.Get);
            var response = await _client.ExecuteAsync<dynamic>(request);
            
            if (response.IsSuccessful && response.Content != null)
            {
                var drives = JsonDocument.Parse(response.Content);
                var defaultDrive = drives.RootElement.GetProperty("value").EnumerateArray().First();
                var id = defaultDrive.GetProperty("id").GetString();
                if (!string.IsNullOrEmpty(id))
                {
                    return id;
                }
                throw new Exception("Drive ID is null or empty.");
            }
            throw new Exception($"Failed to get default drive ID: {response.ErrorMessage}");
        }

        private async Task<JsonElement?> GetDriveItem(string path)
        {
            var normalizedPath = path.TrimStart('/');
            var url = $"/drives/{driveId}/root:/{normalizedPath}";
            var request = _client.CreateRequest(url, Method.Get);
            var response = await _client.ExecuteAsync<dynamic>(request);
            
            if (response.IsSuccessful && response.Content != null)
            {
                return JsonDocument.Parse(response.Content).RootElement;
            }
            return null;
        }

        private async Task<byte[]> GetFileContent(string path)
        {
            var normalizedPath = path.TrimStart('/');
            var url = $"/drives/{driveId}/root:/{normalizedPath}:/content";
            var request = _client.CreateRequest(url, Method.Get);
            var response = await _client.ExecuteAsync(request);
            
            if (response.IsSuccessful)
            {
                return response.RawBytes ?? Array.Empty<byte>();
            }
            
            throw new Exception($"Failed to get file content: {response.ErrorMessage}");
        }

        public async Task UploadFile(string path, byte[] content)
        {
            var normalizedPath = path.TrimStart('/');
            var url = $"/drives/{driveId}/root:/{normalizedPath}:/content";
            var request = _client.CreateRequest(url, Method.Put);
            request.AddBody(content);
            
            var response = await _client.ExecuteAsync(request);
            if (!response.IsSuccessful)
            {
                throw new Exception($"Failed to upload file: {response.ErrorMessage}");
            }
        }

        private async Task CreateFolder(string path)
        {
            var folderName = GetFileName(path);
            var parentPath = GetDirectoryName(path);
            var normalizedParentPath = parentPath.TrimStart('/');
            
            var url = $"/drives/{driveId}/root:/{normalizedParentPath}:/children";
            var request = _client.CreateRequest(url, Method.Post);
            
            var folderData = new
            {
                name = folderName,
                folder = new { },
                conflictBehavior = "rename"
            };
            
            var json = JsonSerializer.Serialize(folderData);
            request.AddJsonBody(folderData);
            
            var response = await _client.ExecuteAsync(request);
            if (!response.IsSuccessful)
            {
                throw new Exception($"Failed to create folder: {response.ErrorMessage}");
            }
        }

        private async Task DeleteItem(string path)
        {
            var normalizedPath = path.TrimStart('/');
            var url = $"/drives/{driveId}/root:/{normalizedPath}";
            var request = _client.CreateRequest(url, Method.Delete);
            var response = await _client.ExecuteAsync(request);
            
            if (!response.IsSuccessful)
            {
                throw new Exception($"Failed to delete item: {response.ErrorMessage}");
            }
        }

        private async Task<JsonElement[]> GetItemsInFolder(string path)
        {
            var normalizedPath = path.TrimStart('/');
            var url = $"/drives/{driveId}/root:/{normalizedPath}:/children";
            var request = _client.CreateRequest(url, Method.Get);
            var response = await _client.ExecuteAsync<dynamic>(request);
            var content = response.Content;
            if (response.IsSuccessful && content != null)
            {
                var items = JsonDocument.Parse(content);
                return items.RootElement.GetProperty("value").EnumerateArray().ToArray();
            }
            
            throw new Exception($"Failed to get items in folder: {response.ErrorMessage}");
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
                        provider.UploadFile(path, content).Wait();
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