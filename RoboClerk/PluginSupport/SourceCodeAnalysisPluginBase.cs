using RoboClerk.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using Tomlyn.Model;

namespace RoboClerk
{
    public abstract class SourceCodeAnalysisPluginBase : PluginBase, ISourceCodeAnalysisPlugin
    {
        protected bool subDir = false;
        protected List<string> directories = new List<string>();
        protected List<string> fileMasks = new List<string>();
        protected List<string> sourceFiles = new List<string>();
        protected List<UnitTestItem> unitTests = new List<UnitTestItem>();
        protected GitRepoInformation gitInfo = null;

        public List<UnitTestItem> GetUnitTests()
        {
            return unitTests;
        }

        public abstract void RefreshItems();

        public override void Initialize(IConfiguration configuration)
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
                    gitInfo = new GitRepoInformation(configuration);
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
                DirectoryInfo dir = new DirectoryInfo(testDirectory);
                try
                {
                    foreach (var fileMask in fileMasks)
                    {
                        FileInfo[] files = dir.GetFiles(fileMask, subDir ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
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
        }
    }
}
