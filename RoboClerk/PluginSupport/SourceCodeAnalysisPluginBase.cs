using RoboClerk.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Tomlyn.Model;

namespace RoboClerk
{
    public abstract class SourceCodeAnalysisPluginBase : DataSourcePluginBase
    {
        protected bool subDir = false;
        protected List<string> directories = new List<string>();
        protected List<string> fileMasks = new List<string>();
        protected List<string> sourceFiles = new List<string>();
        protected GitRepository gitRepo = null;

        public SourceCodeAnalysisPluginBase(IFileSystem fileSystem)
            : base(fileSystem)
        {
        }

        public override void InitializePlugin(IConfiguration configuration)
        {
            var config = GetConfigurationTable(configuration.PluginConfigDir, $"{name}.toml");
            subDir = (bool)config["SubDirs"];
            foreach (var obj in (TomlArray)config["TestDirectories"])
            {
                directories.Add((string)obj);
            }

            foreach (var obj in (TomlArray)config["FileMasks"])
            {
                fileMasks.Add((string)obj);
            }

            try
            {
                if (config.ContainsKey("UseGit") && (bool)config["UseGit"])
                {
                    gitRepo = new GitRepository(configuration,fileSystem);
                }
            }
            catch (Exception)
            {
                logger.Error($"Error opening git repo at project root \"{configuration.ProjectRoot}\" even though the {name}.toml configuration file UseGit setting was set to true.");
                throw;
            }
        }

        protected void ScanDirectoriesForSourceFiles()
        {
            sourceFiles.Clear();
            foreach (var testDirectory in directories)
            {
                IDirectoryInfo dir = fileSystem.DirectoryInfo.New(testDirectory);
                try
                {
                    foreach (var fileMask in fileMasks)
                    {
                        IFileInfo[] files = dir.GetFiles(fileMask, subDir ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                        foreach (var file in files)
                        {
                            sourceFiles.Add(file.FullName);
                        }
                    }
                }
                catch
                {
                    logger.Error($"Error reading directory {testDirectory}");
                    throw;
                }
            }

            // Preload git information for performance if git is enabled and we have files
            if (gitRepo != null && sourceFiles.Any())
            {
                logger.Debug($"Preloading git information for {sourceFiles.Count} source files");
                
                // Get unique directories that contain our source files
                var sourceDirectories = sourceFiles
                    .Select(f => Path.GetDirectoryName(f))
                    .Where(d => !string.IsNullOrEmpty(d))
                    .Distinct()
                    .ToList();

                // Preload git information for each directory
                foreach (var directory in sourceDirectories)
                {
                    try
                    {
                        gitRepo.PreloadDirectoryInfo(directory);
                    }
                    catch (Exception ex)
                    {
                        logger.Warn($"Could not preload git information for directory {directory}: {ex.Message}");
                        // Continue processing other directories even if one fails
                    }
                }
            }
        }
    }
}
