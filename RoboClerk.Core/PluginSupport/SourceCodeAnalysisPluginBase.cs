using RoboClerk.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using Tomlyn.Model;

namespace RoboClerk
{
    public abstract class SourceCodeAnalysisPluginBase : DataSourcePluginBase
    {
        protected bool subDir = false;
        protected List<string> directories = new List<string>();
        protected List<string> fileMasks = new List<string>();
        protected List<string> sourceFiles = new List<string>();
        protected GitRepository? gitRepo = null;

        public SourceCodeAnalysisPluginBase(IFileProviderPlugin fileSystem)
            : base(fileSystem)
        {
        }

        public override void InitializePlugin(IConfiguration configuration)
        {
            var config = GetConfigurationTable(configuration.PluginConfigDir, $"{name}.toml");
            subDir = (bool)config["SubDirs"];
            foreach (var obj in (TomlArray)config["TestDirectories"])
            {
                if (obj is string dir && dir != null)
                {
                    directories.Add(dir);
                }
            }

            foreach (var obj in (TomlArray)config["FileMasks"])
            {
                if (obj is string mask && mask != null)
                {
                    fileMasks.Add(mask);
                }
            }

            try
            {
                if (config.ContainsKey("UseGit") && (bool)config["UseGit"])
                {
                    gitRepo = new GitRepository(configuration);
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
                try
                {
                    foreach (var fileMask in fileMasks)
                    {
                        var files = fileProvider.GetFiles(testDirectory, fileMask, subDir ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                        foreach (var file in files)
                        {
                            sourceFiles.Add(file);
                        }
                    }
                }
                catch
                {
                    logger.Error($"Error reading directory {testDirectory}");
                    throw;
                }
            }
        }
    }
}
