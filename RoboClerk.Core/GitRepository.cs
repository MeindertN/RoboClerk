using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using CliWrap;
using RoboClerk.Core.Configuration;

namespace RoboClerk
{
    public class GitRepository
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private string projectRoot = string.Empty;
        private readonly Dictionary<string, GitFileInfo> fileInfoCache = new();
        private readonly IFileProviderPlugin fileSystem;

        public GitRepository(IConfiguration config, IFileProviderPlugin fileSystem) 
        {
            this.fileSystem = fileSystem;
            projectRoot = config.ProjectRoot;
            var result = RunGitCommand("--version");
            if(!result.Contains("git version"))
            {
                throw new Exception("Git support enabled in RoboClerk but Git executable not found in path. Please ensure Git command is in your path or disable Git support in RoboClerk.");
            }
            logger.Info($"Git support enabled and Git executable found in path: {result}");
            RunGitCommand($"config --global --add safe.directory {config.ProjectRoot}"); //needed to ensure we can apply git on files we don't own
        }

        /// <summary>
        /// Pre-loads git information for all files in the specified directory and subdirectories.
        /// This significantly improves performance when processing many files.
        /// </summary>
        /// <param name="directory">Directory to scan for git file information</param>
        public void PreloadDirectoryInfo(string directory)
        {
            logger.Info($"Pre-loading git information for directory: {directory}");
            
            try
            {
                // Get all tracked files in the directory and subdirectories
                var trackedFilesOutput = RunGitCommand($"ls-files \"{directory}\"");
                var trackedFiles = trackedFilesOutput
                    .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(f => Path.GetFullPath(Path.Combine(projectRoot, f.Trim())))
                    .ToList();

                if (!trackedFiles.Any())
                {
                    logger.Debug($"No tracked files found in directory: {directory}");
                    return;
                }

                // Get modification status ONLY for files we're tracking in this directory
                var modifiedFiles = GetModifiedFiles(trackedFiles);

                // Get version and date information for all files in this directory
                var fileVersionsAndDates = GetFileVersionsAndDates(trackedFiles);

                // Cache the results
                foreach (var file in trackedFiles)
                {
                    var normalizedPath = Path.GetFullPath(file);
                    fileInfoCache[normalizedPath] = new GitFileInfo
                    {
                        IsModified = modifiedFiles.Contains(normalizedPath),
                        Version = fileVersionsAndDates.TryGetValue(normalizedPath, out var info) ? info.Version : string.Empty,
                        LastUpdated = fileVersionsAndDates.TryGetValue(normalizedPath, out var dateInfo) ? dateInfo.LastUpdated : DateTime.MinValue,
                        IsCached = true
                    };
                }

                logger.Info($"Cached git information for {trackedFiles.Count} files in directory: {directory}");
            }
            catch (Exception ex)
            {
                logger.Error($"Error pre-loading directory info for {directory}: {ex.Message}");
                throw;
            }
        }

        public bool GetFileLocallyUpdated(string file)
        {
            var normalizedPath = Path.GetFullPath(file);
            
            // Check cache first
            if (fileInfoCache.TryGetValue(normalizedPath, out var cachedInfo))
            {
                return cachedInfo.IsModified;
            }

            // Fall back to individual command
            var status = RunGitCommand($"status \"{file}\"");
            var isModified = status.Contains("modified:");

            // Cache the result
            UpdateCacheEntry(normalizedPath, isModified: isModified);
            
            return isModified;
        }

        public string GetFileVersion(string file)
        {
            var normalizedPath = Path.GetFullPath(file);
            
            // Check cache first
            if (fileInfoCache.TryGetValue(normalizedPath, out var cachedInfo) && !string.IsNullOrEmpty(cachedInfo.Version))
            {
                return cachedInfo.Version;
            }

            // Fall back to individual command
            var commitSHA = RunGitCommand($"log -n 1 --pretty=format:%H -- \"{file}\"");
            if (!commitSHA.Contains("fatal:") && commitSHA.Length != 0)
            {
                var version = commitSHA.Substring(0, Math.Min(7, commitSHA.Length));
                UpdateCacheEntry(normalizedPath, version: version);
                return version;
            }
            else
            {
                // For new/untracked files, return empty version instead of throwing
                logger.Debug($"File {file} is not checked into git, returning empty version.");
                UpdateCacheEntry(normalizedPath, version: string.Empty);
                return string.Empty;
            }
        }

        public DateTime GetFileLastUpdated(string file)
        {
            var normalizedPath = Path.GetFullPath(file);
            
            // Check cache first
            if (fileInfoCache.TryGetValue(normalizedPath, out var cachedInfo) && cachedInfo.LastUpdated != DateTime.MinValue)
            {
                return cachedInfo.LastUpdated;
            }

            // Try to get date from git first
            var dateTime = RunGitCommand($"log -n 1 --format=\"%ai\" -- \"{file}\"");
            if (!dateTime.Contains("fatal:") && dateTime.Length != 0)
            {
                if(DateTime.TryParse(dateTime, out DateTime output))
                {
                    UpdateCacheEntry(normalizedPath, lastUpdated: output);
                    return output;
                }
                else
                {
                    logger.Warn($"Could not parse git date for file \"{file}\", falling back to file system date. Git output: \"{dateTime}\"");
                }
            }
            
            // Fall back to file system modified date for new or untracked files
            try
            {
                if (fileSystem.FileExists(normalizedPath))
                {
                    var fileModifiedDate = fileSystem.GetLastWriteTime(normalizedPath);
                    logger.Debug($"Using file system modified date for file \"{file}\": {fileModifiedDate}");
                    UpdateCacheEntry(normalizedPath, lastUpdated: fileModifiedDate);
                    return fileModifiedDate;
                }
                else
                {
                    throw new Exception($"File \"{file}\" does not exist.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error trying to get DateTime for file \"{file}\": {ex.Message}");
            }
        }

        private HashSet<string> GetModifiedFiles(List<string> trackedFiles)
        {
            var modifiedFiles = new HashSet<string>();
            var trackedFileSet = new HashSet<string>(trackedFiles);
            
            // Use porcelain format for easier parsing
            var statusOutput = RunGitCommand("status --porcelain");
            var lines = statusOutput.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                if (line.Length >= 3 && (line[0] == 'M' || line[1] == 'M'))
                {
                    var filePath = line.Substring(3).Trim();
                    var fullPath = Path.GetFullPath(Path.Combine(projectRoot, filePath));
                    
                    // Only include files that are in our tracked files list
                    if (trackedFileSet.Contains(fullPath))
                    {
                        modifiedFiles.Add(fullPath);
                    }
                }
            }
            
            return modifiedFiles;
        }

        private Dictionary<string, (string Version, DateTime LastUpdated)> GetFileVersionsAndDates(List<string> files)
        {
            var result = new Dictionary<string, (string Version, DateTime LastUpdated)>();
            
            if (!files.Any())
                return result;

            // Build a single command to get version and date for all files
            var fileArgs = string.Join(" ", files.Select(f => $"\"{Path.GetRelativePath(projectRoot, f)}\""));
            var logOutput = RunGitCommand($"log --name-only --pretty=format:\"%H|%ai\" -- {fileArgs}");
            
            // Parse the git output for tracked files
            if (!string.IsNullOrWhiteSpace(logOutput))
            {
                var lines = logOutput.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                string currentHash = null;
                DateTime currentDate = DateTime.MinValue;
                
                foreach (var line in lines)
                {
                    if (line.Contains("|")) // This is a commit info line
                    {
                        var parts = line.Split('|');
                        if (parts.Length >= 2)
                        {
                            currentHash = parts[0].Substring(0, Math.Min(7, parts[0].Length));
                            DateTime.TryParse(parts[1], out currentDate);
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(line) && currentHash != null) // This is a file name
                    {
                        var fullPath = Path.GetFullPath(Path.Combine(projectRoot, line.Trim()));
                        if (!result.ContainsKey(fullPath)) // Only store the most recent commit info
                        {
                            result[fullPath] = (currentHash, currentDate);
                        }
                    }
                }
            }

            // For files not found in git (new/untracked files), use file system date
            foreach (var file in files)
            {
                var normalizedPath = Path.GetFullPath(file);
                if (!result.ContainsKey(normalizedPath))
                {
                    try
                    {
                        if (fileSystem.FileExists(normalizedPath))
                        {
                            var fileModifiedDate = fileSystem.GetLastWriteTime(normalizedPath);
                            result[normalizedPath] = (string.Empty, fileModifiedDate);
                            logger.Debug($"Using file system date for untracked file: {normalizedPath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Warn($"Could not get file system date for {normalizedPath}: {ex.Message}");
                        result[normalizedPath] = (string.Empty, DateTime.MinValue);
                    }
                }
            }
            
            return result;
        }

        private void UpdateCacheEntry(string normalizedPath, bool? isModified = null, string version = null, DateTime? lastUpdated = null)
        {
            if (!fileInfoCache.TryGetValue(normalizedPath, out var info))
            {
                info = new GitFileInfo();
                fileInfoCache[normalizedPath] = info;
            }

            if (isModified.HasValue) info.IsModified = isModified.Value;
            if (!string.IsNullOrEmpty(version)) info.Version = version;
            if (lastUpdated.HasValue) info.LastUpdated = lastUpdated.Value;
        }

        private string RunGitCommand(string arguments) 
        {
            var stdOutBuffer = new StringBuilder();
            var stdErrBuffer = new StringBuilder();
            CommandResultValidation validation = (CommandResultValidation.None);
            try
            {
                var cmd = Cli.Wrap("git")
                    .WithArguments(arguments)
                    .WithWorkingDirectory(projectRoot)
                    .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                    .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                    .WithValidation(validation);
                cmd.ExecuteAsync().Task.Wait();
                if (stdErrBuffer.Length > 0)
                    return stdErrBuffer.ToString();
                else
                    return stdOutBuffer.ToString();
            }
            catch (Exception ex)
            {
                logger.Error($"{ex.Message}\n");
                if (stdOutBuffer.Length > 0)
                {
                    logger.Error("Standard git command output:");
                    logger.Error(stdOutBuffer.ToString());
                }
                if (stdErrBuffer.Length > 0)
                {
                    logger.Error("Standard git error command output:");
                    logger.Error(stdErrBuffer.ToString());
                }
                throw new Exception("Git command execution failed. Ensure git is in your path. Aborting...");
            }
        }

        private class GitFileInfo
        {
            public bool IsModified { get; set; }
            public string Version { get; set; } = string.Empty;
            public DateTime LastUpdated { get; set; } = DateTime.MinValue;
            public bool IsCached { get; set; }
        }
    }
}
