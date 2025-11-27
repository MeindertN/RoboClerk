using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using RoboClerk.Core.Configuration;

namespace RoboClerk
{
    /// <summary>
    /// Interface for file provider plugins that abstract file storage operations.
    /// This allows RoboClerk to work with different file storage backends
    /// such as local filesystem, SharePoint, OneDrive, S3, etc.
    /// </summary>
    public interface IFileProviderPlugin : IPlugin
    {
        /// <summary>
        /// Gets the URI-style prefix that identifies paths handled by this provider.
        /// For example: "sp://" for SharePoint, "file://" for explicit local paths.
        /// Return null or empty string for providers that don't use a prefix (default/local).
        /// </summary>
        /// <returns>The path prefix (including ://) or null/empty for default provider.</returns>
        string GetPathPrefix();

        /// <summary>
        /// Checks if a file exists at the specified path.
        /// </summary>
        /// <param name="path">The file path to check.</param>
        /// <returns>True if the file exists, false otherwise.</returns>
        bool FileExists(string path);

        /// <summary>
        /// Checks if a directory exists at the specified path.
        /// </summary>
        /// <param name="path">The directory path to check.</param>
        /// <returns>True if the directory exists, false otherwise.</returns>
        bool DirectoryExists(string path);

        /// <summary>
        /// Reads all text from a file.
        /// </summary>
        /// <param name="path">The path to the file to read.</param>
        /// <returns>The contents of the file as a string.</returns>
        string ReadAllText(string path);

        /// <summary>
        /// Reads all text from a file asynchronously.
        /// </summary>
        /// <param name="path">The path to the file to read.</param>
        /// <returns>A task that represents the asynchronous read operation.</returns>
        Task<string> ReadAllTextAsync(string path);

        /// <summary>
        /// Reads all lines from a text file.
        /// </summary>
        /// <param name="path">The path to the file to read.</param>
        /// <returns>A list containing each line of the file.</returns>
        List<string> ReadLines(string path);

        /// <summary>
        /// Reads all lines from a text file asynchronously.
        /// </summary>
        /// <param name="path">The path to the file to read.</param>
        /// <returns>A task that represents the asynchronous read operation.</returns>
        Task<List<string>> ReadLinesAsync(string path);

        /// <summary>
        /// Reads all bytes from a file.
        /// </summary>
        /// <param name="path">The path to the file to read.</param>
        /// <returns>The contents of the file as a byte array.</returns>
        byte[] ReadAllBytes(string path);

        /// <summary>
        /// Reads all bytes from a file asynchronously.
        /// </summary>
        /// <param name="path">The path to the file to read.</param>
        /// <returns>A task that represents the asynchronous read operation.</returns>
        Task<byte[]> ReadAllBytesAsync(string path);

        /// <summary>
        /// Writes text to a file, creating the file if it doesn't exist.
        /// </summary>
        /// <param name="path">The path to the file to write.</param>
        /// <param name="contents">The text to write to the file.</param>
        void WriteAllText(string path, string contents);

        /// <summary>
        /// Writes text to a file asynchronously, creating the file if it doesn't exist.
        /// </summary>
        /// <param name="path">The path to the file to write.</param>
        /// <param name="contents">The text to write to the file.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        Task WriteAllTextAsync(string path, string contents);

        /// <summary>
        /// Writes bytes to a file, creating the file if it doesn't exist.
        /// </summary>
        /// <param name="path">The path to the file to write.</param>
        /// <param name="bytes">The bytes to write to the file.</param>
        void WriteAllBytes(string path, byte[] bytes);

        /// <summary>
        /// Writes bytes to a file asynchronously, creating the file if it doesn't exist.
        /// </summary>
        /// <param name="path">The path to the file to write.</param>
        /// <param name="bytes">The bytes to write to the file.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        Task WriteAllBytesAsync(string path, byte[] bytes);

        /// <summary>
        /// Opens a file stream for reading.
        /// </summary>
        /// <param name="path">The path to the file to open.</param>
        /// <param name="mode">The file mode.</param>
        /// <returns>A stream for reading the file.</returns>
        Stream OpenRead(string path);

        /// <summary>
        /// Opens a file stream for writing.
        /// </summary>
        /// <param name="path">The path to the file to open.</param>
        /// <param name="mode">The file mode.</param>
        /// <returns>A stream for writing to the file.</returns>
        Stream OpenWrite(string path, FileMode mode = FileMode.Create);

        /// <summary>
        /// Creates a directory at the specified path.
        /// </summary>
        /// <param name="path">The path where the directory should be created.</param>
        void CreateDirectory(string path);

        /// <summary>
        /// Deletes a file at the specified path.
        /// </summary>
        /// <param name="path">The path to the file to delete.</param>
        void DeleteFile(string path);

        /// <summary>
        /// Deletes a directory at the specified path.
        /// </summary>
        /// <param name="path">The path to the directory to delete.</param>
        /// <param name="recursive">Whether to delete subdirectories and files.</param>
        void DeleteDirectory(string path, bool recursive = false);

        /// <summary>
        /// Gets all files in a directory that match a search pattern.
        /// </summary>
        /// <param name="path">The directory path to search.</param>
        /// <param name="searchPattern">The search pattern (e.g., "*.txt").</param>
        /// <param name="searchOption">Whether to search subdirectories.</param>
        /// <returns>An array of file paths.</returns>
        string[] GetFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);

        /// <summary>
        /// Gets all directories in a directory.
        /// </summary>
        /// <param name="path">The directory path to search.</param>
        /// <param name="searchPattern">The search pattern.</param>
        /// <param name="searchOption">Whether to search subdirectories.</param>
        /// <returns>An array of directory paths.</returns>
        string[] GetDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);

        /// <summary>
        /// Copies a file from source to destination.
        /// </summary>
        /// <param name="sourcePath">The source file path.</param>
        /// <param name="destinationPath">The destination file path.</param>
        /// <param name="overwrite">Whether to overwrite the destination file if it exists.</param>
        void CopyFile(string sourcePath, string destinationPath, bool overwrite = false);

        /// <summary>
        /// Moves a file from source to destination.
        /// </summary>
        /// <param name="sourcePath">The source file path.</param>
        /// <param name="destinationPath">The destination file path.</param>
        /// <param name="overwrite">Whether to overwrite the destination file if it exists.</param>
        void MoveFile(string sourcePath, string destinationPath, bool overwrite = false);

        /// <summary>
        /// Gets the last write time of a file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <returns>The last write time of the file.</returns>
        DateTime GetLastWriteTime(string path);

        /// <summary>
        /// Gets the file size in bytes.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <returns>The size of the file in bytes.</returns>
        long GetFileSize(string path);

        /// <summary>
        /// Combines path segments into a single path.
        /// </summary>
        /// <param name="paths">The path segments to combine.</param>
        /// <returns>The combined path.</returns>
        string Combine(params string[] paths);

        /// <summary>
        /// Gets the directory name from a file path.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>The directory name.</returns>
        string GetDirectoryName(string path);

        /// <summary>
        /// Gets the file name from a file path.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>The file name.</returns>
        string GetFileName(string path);

        /// <summary>
        /// Gets the file name without extension from a file path.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>The file name without extension.</returns>
        string GetFileNameWithoutExtension(string path);

        /// <summary>
        /// Gets the file extension from a file path.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>The file extension.</returns>
        string GetExtension(string path);

        /// <summary>
        /// Gets the full path from a relative path.
        /// </summary>
        /// <param name="path">The relative path.</param>
        /// <returns>The full path.</returns>
        string GetFullPath(string path);

        /// <summary>
        /// Checks if a path is absolute.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <returns>True if the path is absolute, false otherwise.</returns>
        bool IsPathRooted(string path);

        /// <summary>
        /// Gets the relative path from one path to another.
        /// </summary>
        /// <param name="relativeTo">The base path.</param>
        /// <param name="path">The target path.</param>
        /// <returns>The relative path.</returns>
        string GetRelativePath(string relativeTo, string path);
    }
}
