using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using RoboClerk.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RoboClerk
{
    /// <summary>
    /// Local filesystem implementation of the file provider plugin.
    /// This provides access to the local file system using System.IO.Abstractions for better testability.
    /// </summary>
    public class LocalFileSystemPlugin : FileProviderPluginBase, IFileProviderPlugin
    {
        private readonly IFileSystem _fileSystem;

        public LocalFileSystemPlugin(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            name = "LocalFileSystemPlugin";
            description = "Provides access to the local file system using standard .NET IO operations.";
        }

        public override void InitializePlugin(IConfiguration configuration)
        {
            LogInfo("Local file system plugin initialized");
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            // Register IFileSystem if not already registered
            if (!services.Any(s => s.ServiceType == typeof(IFileSystem)))
            {
                services.AddSingleton<IFileSystem>(new FileSystem());
            }
        }

        public override bool FileExists(string path)
        {
            ValidatePath(path);
            return _fileSystem.File.Exists(path);
        }

        public override bool DirectoryExists(string path)
        {
            ValidatePath(path);
            return _fileSystem.Directory.Exists(path);
        }

        public override string ReadAllText(string path)
        {
            ValidatePath(path);
            if (!FileExists(path))
            {
                throw new FileNotFoundException($"File not found: {path}");
            }
            return _fileSystem.File.ReadAllText(path);
        }

        public override List<string> ReadLines(string path)
        {
            ValidatePath(path);
            if (!FileExists(path))
            {
                throw new FileNotFoundException($"File not found: {path}");
            }
            return new List<string>(_fileSystem.File.ReadAllLines(path));
        }

        public override async Task<List<string>> ReadLinesAsync(string path)
        {
            return await Task.Run(() => ReadLines(path));
        }

        public override byte[] ReadAllBytes(string path)
        {
            ValidatePath(path);
            if (!FileExists(path))
            {
                throw new FileNotFoundException($"File not found: {path}");
            }
            return _fileSystem.File.ReadAllBytes(path);
        }

        public override void WriteAllText(string path, string contents)
        {
            ValidatePath(path);
            ValidatePath(contents, "contents");
            
            // Ensure directory exists
            string directory = GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !DirectoryExists(directory))
            {
                CreateDirectory(directory);
            }
            
            _fileSystem.File.WriteAllText(path, contents);
        }

        public override void WriteAllBytes(string path, byte[] bytes)
        {
            ValidatePath(path);
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }
            
            // Ensure directory exists
            string directory = GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !DirectoryExists(directory))
            {
                CreateDirectory(directory);
            }
            
            _fileSystem.File.WriteAllBytes(path, bytes);
        }

        public override Stream OpenRead(string path)
        {
            ValidatePath(path);
            if (!FileExists(path))
            {
                throw new FileNotFoundException($"File not found: {path}");
            }
            return _fileSystem.File.OpenRead(path);
        }

        public override Stream OpenWrite(string path, FileMode mode = FileMode.Create)
        {
            ValidatePath(path);
            
            // Ensure directory exists
            string directory = GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !DirectoryExists(directory))
            {
                CreateDirectory(directory);
            }
            
            return _fileSystem.File.Open(path, mode);
        }

        public override void CreateDirectory(string path)
        {
            ValidatePath(path);
            _fileSystem.Directory.CreateDirectory(path);
        }

        public override void DeleteFile(string path)
        {
            ValidatePath(path);
            if (!FileExists(path))
            {
                throw new FileNotFoundException($"File not found: {path}");
            }
            _fileSystem.File.Delete(path);
        }

        public override void DeleteDirectory(string path, bool recursive = false)
        {
            ValidatePath(path);
            if (!DirectoryExists(path))
            {
                throw new DirectoryNotFoundException($"Directory not found: {path}");
            }
            _fileSystem.Directory.Delete(path, recursive);
        }

        public override string[] GetFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            ValidatePath(path);
            if (!DirectoryExists(path))
            {
                throw new DirectoryNotFoundException($"Directory not found: {path}");
            }
            return _fileSystem.Directory.GetFiles(path, searchPattern, searchOption);
        }

        public override string[] GetDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            ValidatePath(path);
            if (!DirectoryExists(path))
            {
                throw new DirectoryNotFoundException($"Directory not found: {path}");
            }
            return _fileSystem.Directory.GetDirectories(path, searchPattern, searchOption);
        }

        public override void CopyFile(string sourcePath, string destinationPath, bool overwrite = false)
        {
            ValidatePath(sourcePath, "sourcePath");
            ValidatePath(destinationPath, "destinationPath");
            
            if (!FileExists(sourcePath))
            {
                throw new FileNotFoundException($"Source file not found: {sourcePath}");
            }
            
            // Ensure destination directory exists
            string destinationDirectory = GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destinationDirectory) && !DirectoryExists(destinationDirectory))
            {
                CreateDirectory(destinationDirectory);
            }
            
            _fileSystem.File.Copy(sourcePath, destinationPath, overwrite);
        }

        public override void MoveFile(string sourcePath, string destinationPath, bool overwrite = false)
        {
            ValidatePath(sourcePath, "sourcePath");
            ValidatePath(destinationPath, "destinationPath");
            
            if (!FileExists(sourcePath))
            {
                throw new FileNotFoundException($"Source file not found: {sourcePath}");
            }
            
            if (FileExists(destinationPath) && !overwrite)
            {
                throw new IOException($"Destination file already exists: {destinationPath}");
            }
            
            // Ensure destination directory exists
            string destinationDirectory = GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destinationDirectory) && !DirectoryExists(destinationDirectory))
            {
                CreateDirectory(destinationDirectory);
            }
            
            _fileSystem.File.Move(sourcePath, destinationPath, overwrite);
        }

        public override DateTime GetLastWriteTime(string path)
        {
            ValidatePath(path);
            if (!FileExists(path))
            {
                throw new FileNotFoundException($"File not found: {path}");
            }
            return _fileSystem.File.GetLastWriteTime(path);
        }

        public override long GetFileSize(string path)
        {
            ValidatePath(path);
            if (!FileExists(path))
            {
                throw new FileNotFoundException($"File not found: {path}");
            }
            return _fileSystem.FileInfo.New(path).Length;
        }

        public override string Combine(params string[] paths)
        {
            if (paths == null || paths.Length == 0)
            {
                throw new ArgumentException("At least one path must be provided.");
            }
            return _fileSystem.Path.Combine(paths);
        }

        public override string GetDirectoryName(string path)
        {
            ValidatePath(path);
            return _fileSystem.Path.GetDirectoryName(path);
        }

        public override string GetFileName(string path)
        {
            ValidatePath(path);
            return _fileSystem.Path.GetFileName(path);
        }

        public override string GetFileNameWithoutExtension(string path)
        {
            ValidatePath(path);
            return _fileSystem.Path.GetFileNameWithoutExtension(path);
        }

        public override string GetExtension(string path)
        {
            ValidatePath(path);
            return _fileSystem.Path.GetExtension(path);
        }

        public override string GetFullPath(string path)
        {
            ValidatePath(path);
            return _fileSystem.Path.GetFullPath(path);
        }

        public override bool IsPathRooted(string path)
        {
            ValidatePath(path);
            return _fileSystem.Path.IsPathRooted(path);
        }

        public override string GetRelativePath(string relativeTo, string path)
        {
            ValidatePath(relativeTo, "relativeTo");
            ValidatePath(path, "path");
            return _fileSystem.Path.GetRelativePath(relativeTo, path);
        }
    }
} 