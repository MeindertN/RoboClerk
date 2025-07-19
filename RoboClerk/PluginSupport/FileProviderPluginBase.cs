using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RoboClerk.Configuration;
using Tomlyn;
using Tomlyn.Model;

namespace RoboClerk
{
    /// <summary>
    /// Base class for file provider plugins that provides common functionality
    /// and default implementations for file operations.
    /// </summary>
    public abstract class FileProviderPluginBase : PluginBase, IFileProviderPlugin
    {
        public FileProviderPluginBase()
        {
      
        }

        /// <summary>
        /// Checks if a file exists at the specified path.
        /// </summary>
        /// <param name="path">The file path to check.</param>
        /// <returns>True if the file exists, false otherwise.</returns>
        public abstract bool FileExists(string path);

        /// <summary>
        /// Checks if a directory exists at the specified path.
        /// </summary>
        /// <param name="path">The directory path to check.</param>
        /// <returns>True if the directory exists, false otherwise.</returns>
        public abstract bool DirectoryExists(string path);

        /// <summary>
        /// Reads all text from a file.
        /// </summary>
        /// <param name="path">The path to the file to read.</param>
        /// <returns>The contents of the file as a string.</returns>
        public abstract string ReadAllText(string path);

        /// <summary>
        /// Reads all text from a file asynchronously.
        /// </summary>
        /// <param name="path">The path to the file to read.</param>
        /// <returns>A task that represents the asynchronous read operation.</returns>
        public virtual async Task<string> ReadAllTextAsync(string path)
        {
            return await Task.Run(() => ReadAllText(path));
        }

        /// <summary>
        /// Reads all lines from a text file.
        /// </summary>
        /// <param name="path">The path to the file to read.</param>
        /// <returns>A list containing each line of the file.</returns>
        public abstract List<string> ReadLines(string path);

        /// <summary>
        /// Reads all lines from a text file asynchronously.
        /// </summary>
        /// <param name="path">The path to the file to read.</param>
        /// <returns>A task that represents the asynchronous read operation.</returns>
        public virtual async Task<List<string>> ReadLinesAsync(string path)
        {
            return await Task.Run(() => ReadLines(path));
        }

        /// <summary>
        /// Reads all bytes from a file.
        /// </summary>
        /// <param name="path">The path to the file to read.</param>
        /// <returns>The contents of the file as a byte array.</returns>
        public abstract byte[] ReadAllBytes(string path);

        /// <summary>
        /// Reads all bytes from a file asynchronously.
        /// </summary>
        /// <param name="path">The path to the file to read.</param>
        /// <returns>A task that represents the asynchronous read operation.</returns>
        public virtual async Task<byte[]> ReadAllBytesAsync(string path)
        {
            return await Task.Run(() => ReadAllBytes(path));
        }

        /// <summary>
        /// Writes text to a file, creating the file if it doesn't exist.
        /// </summary>
        /// <param name="path">The path to the file to write.</param>
        /// <param name="contents">The text to write to the file.</param>
        public abstract void WriteAllText(string path, string contents);

        /// <summary>
        /// Writes text to a file asynchronously, creating the file if it doesn't exist.
        /// </summary>
        /// <param name="path">The path to the file to write.</param>
        /// <param name="contents">The text to write to the file.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        public virtual async Task WriteAllTextAsync(string path, string contents)
        {
            await Task.Run(() => WriteAllText(path, contents));
        }

        /// <summary>
        /// Writes bytes to a file, creating the file if it doesn't exist.
        /// </summary>
        /// <param name="path">The path to the file to write.</param>
        /// <param name="bytes">The bytes to write to the file.</param>
        public abstract void WriteAllBytes(string path, byte[] bytes);

        /// <summary>
        /// Writes bytes to a file asynchronously, creating the file if it doesn't exist.
        /// </summary>
        /// <param name="path">The path to the file to write.</param>
        /// <param name="bytes">The bytes to write to the file.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        public virtual async Task WriteAllBytesAsync(string path, byte[] bytes)
        {
            await Task.Run(() => WriteAllBytes(path, bytes));
        }

        /// <summary>
        /// Opens a file stream for reading.
        /// </summary>
        /// <param name="path">The path to the file to open.</param>
        /// <returns>A stream for reading the file.</returns>
        public abstract Stream OpenRead(string path);

        /// <summary>
        /// Opens a file stream for writing.
        /// </summary>
        /// <param name="path">The path to the file to open.</param>
        /// <param name="mode">The file mode.</param>
        /// <returns>A stream for writing to the file.</returns>
        public abstract Stream OpenWrite(string path, FileMode mode = FileMode.Create);

        /// <summary>
        /// Creates a directory at the specified path.
        /// </summary>
        /// <param name="path">The path where the directory should be created.</param>
        public abstract void CreateDirectory(string path);

        /// <summary>
        /// Deletes a file at the specified path.
        /// </summary>
        /// <param name="path">The path to the file to delete.</param>
        public abstract void DeleteFile(string path);

        /// <summary>
        /// Deletes a directory at the specified path.
        /// </summary>
        /// <param name="path">The path to the directory to delete.</param>
        /// <param name="recursive">Whether to delete subdirectories and files.</param>
        public abstract void DeleteDirectory(string path, bool recursive = false);

        /// <summary>
        /// Gets all files in a directory that match a search pattern.
        /// </summary>
        /// <param name="path">The directory path to search.</param>
        /// <param name="searchPattern">The search pattern (e.g., "*.txt").</param>
        /// <param name="searchOption">Whether to search subdirectories.</param>
        /// <returns>An array of file paths.</returns>
        public abstract string[] GetFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);

        /// <summary>
        /// Gets all directories in a directory.
        /// </summary>
        /// <param name="path">The directory path to search.</param>
        /// <param name="searchPattern">The search pattern.</param>
        /// <param name="searchOption">Whether to search subdirectories.</param>
        /// <returns>An array of directory paths.</returns>
        public abstract string[] GetDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);

        /// <summary>
        /// Copies a file from source to destination.
        /// </summary>
        /// <param name="sourcePath">The source file path.</param>
        /// <param name="destinationPath">The destination file path.</param>
        /// <param name="overwrite">Whether to overwrite the destination file if it exists.</param>
        public abstract void CopyFile(string sourcePath, string destinationPath, bool overwrite = false);

        /// <summary>
        /// Moves a file from source to destination.
        /// </summary>
        /// <param name="sourcePath">The source file path.</param>
        /// <param name="destinationPath">The destination file path.</param>
        /// <param name="overwrite">Whether to overwrite the destination file if it exists.</param>
        public abstract void MoveFile(string sourcePath, string destinationPath, bool overwrite = false);

        /// <summary>
        /// Gets the last write time of a file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <returns>The last write time of the file.</returns>
        public abstract DateTime GetLastWriteTime(string path);

        /// <summary>
        /// Gets the size of a file in bytes.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <returns>The size of the file in bytes.</returns>
        public abstract long GetFileSize(string path);

        /// <summary>
        /// Combines multiple path segments into a single path.
        /// </summary>
        /// <param name="paths">The path segments to combine.</param>
        /// <returns>The combined path.</returns>
        public abstract string Combine(params string[] paths);

        /// <summary>
        /// Gets the directory name from a file path.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>The directory name.</returns>
        public abstract string GetDirectoryName(string path);

        /// <summary>
        /// Gets the file name from a file path.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>The file name.</returns>
        public abstract string GetFileName(string path);

        /// <summary>
        /// Gets the file name without extension from a file path.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>The file name without extension.</returns>
        public abstract string GetFileNameWithoutExtension(string path);

        /// <summary>
        /// Gets the file extension from a file path.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>The file extension.</returns>
        public abstract string GetExtension(string path);

        /// <summary>
        /// Gets the full path from a relative path.
        /// </summary>
        /// <param name="path">The relative path.</param>
        /// <returns>The full path.</returns>
        public abstract string GetFullPath(string path);

        /// <summary>
        /// Checks if a path is rooted.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <returns>True if the path is rooted, false otherwise.</returns>
        public abstract bool IsPathRooted(string path);

        /// <summary>
        /// Gets the relative path from one path to another.
        /// </summary>
        /// <param name="relativeTo">The base path.</param>
        /// <param name="path">The target path.</param>
        /// <returns>The relative path.</returns>
        public abstract string GetRelativePath(string relativeTo, string path);

        /// <summary>
        /// Validates that a path is not null or empty.
        /// </summary>
        /// <param name="path">The path to validate.</param>
        /// <param name="paramName">The parameter name for error messages.</param>
        protected void ValidatePath(string path, string paramName = "path")
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException($"Path cannot be null or empty.", paramName);
            }
        }

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        protected void LogDebug(string message)
        {
            logger.Debug($"[{name}] {message}");
        }

        /// <summary>
        /// Logs an info message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        protected void LogInfo(string message)
        {
            logger.Info($"[{name}] {message}");
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        protected void LogWarn(string message)
        {
            logger.Warn($"[{name}] {message}");
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        protected void LogError(string message)
        {
            logger.Error($"[{name}] {message}");
        }

        /// <summary>
        /// Logs an error message with an exception.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        protected void LogError(string message, Exception exception)
        {
            logger.Error($"[{name}] {message}", exception);
        }
    }
}
