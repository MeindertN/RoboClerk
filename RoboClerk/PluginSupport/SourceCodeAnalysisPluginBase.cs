using RoboClerk.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Tomlyn.Model;

namespace RoboClerk
{
    /// <summary>
    /// Represents a single test configuration with its own settings and associated files
    /// </summary>
    public class TestConfiguration
    {
        // Known/common fields with typed accessors
        public string Language { get; set; } = "csharp";
        public string TestDirectory { get; set; } = string.Empty;
        public bool SubDirs { get; set; } = true;
        public List<string> FileMasks { get; set; } = new List<string>();
        public string Project { get; set; } = string.Empty;
        
        // Dictionary for all fields (including unknown ones)
        public Dictionary<string, object> AllFields { get; private set; } = new Dictionary<string, object>();
        
        // Files associated with this configuration
        public List<string> SourceFiles { get; private set; } = new List<string>();

        public void FromToml(TomlTable toml)
        {
            // Store ALL fields from TOML first
            AllFields.Clear();
            foreach (var kvp in toml)
            {
                AllFields[kvp.Key] = kvp.Value;
            }
            
            // Then populate known typed properties
            if (AllFields.TryGetValue("Language", out var lang))
                Language = lang.ToString();
                
            if (AllFields.TryGetValue("TestDirectory", out var dir))
                TestDirectory = dir.ToString();
                
            if (AllFields.TryGetValue("SubDirs", out var sub))
                SubDirs = (bool)sub;
                
            if (AllFields.TryGetValue("FileMasks", out var masks) && masks is TomlArray masksArray)
            {
                FileMasks.Clear();
                foreach (var obj in masksArray)
                {
                    FileMasks.Add((string)obj);
                }
            }
            
            if (AllFields.TryGetValue("Project", out var proj))
                Project = proj.ToString();
        }
        
        /// <summary>
        /// Get a typed value from the configuration, with optional default
        /// </summary>
        public T GetValue<T>(string key, T defaultValue = default(T))
        {
            if (AllFields.TryGetValue(key, out var value))
            {
                if (value is T typedValue)
                    return typedValue;
                
                // Try to convert common types
                if (typeof(T) == typeof(string))
                    return (T)(object)value.ToString();
                
                if (typeof(T) == typeof(bool) && bool.TryParse(value.ToString(), out var boolVal))
                    return (T)(object)boolVal;
                    
                // Add other type conversions as needed
            }
            
            return defaultValue;
        }
        
        /// <summary>
        /// Check if a field exists in the configuration
        /// </summary>
        public bool HasField(string key) => AllFields.ContainsKey(key);
        
        /// <summary>
        /// Add a source file to this configuration
        /// </summary>
        internal void AddSourceFile(string filePath)
        {
            SourceFiles.Add(filePath);
        }
        
        /// <summary>
        /// Clear all source files for this configuration
        /// </summary>
        internal void ClearSourceFiles()
        {
            SourceFiles.Clear();
        }
    }

    public abstract class SourceCodeAnalysisPluginBase : DataSourcePluginBase
    {
        // Configuration-based approach
        protected List<TestConfiguration> testConfigurations = new List<TestConfiguration>();
        protected List<string> sourceFiles = new List<string>();
        protected GitRepository gitRepo = null;

        public SourceCodeAnalysisPluginBase(IFileSystem fileSystem)
            : base(fileSystem)
        {
        }

        public override void InitializePlugin(IConfiguration configuration)
        {
            var config = GetConfigurationTable(configuration.PluginConfigDir, $"{name}.toml");
            
            // Check if using TestConfigurations format
            if (config.ContainsKey("TestConfigurations"))
            {
                InitializeTestConfigurations(config);
            }
            else
            {
                throw new Exception($"TestConfigurations section is required in {name}.toml configuration file.");
            }

            try
            {
                if (config.ContainsKey("UseGit") && (bool)config["UseGit"])
                {
                    gitRepo = new GitRepository(configuration, fileSystem);
                }
            }
            catch (Exception)
            {
                logger.Error($"Error opening git repo at project root \"{configuration.ProjectRoot}\" even though the {name}.toml configuration file UseGit setting was set to true.");
                throw;
            }
        }

        private void InitializeTestConfigurations(TomlTable config)
        {
            testConfigurations.Clear();
            var configurationsArray = (TomlTableArray)config["TestConfigurations"];
            
            foreach (TomlTable configTable in configurationsArray)
            {
                var testConfig = new TestConfiguration();
                testConfig.FromToml(configTable);
                testConfigurations.Add(testConfig);
            }

            if (testConfigurations.Count == 0)
            {
                throw new Exception($"No test configurations found in {name}.toml. At least one TestConfiguration is required.");
            }
        }

        protected void ScanDirectoriesForSourceFiles()
        {
            // Clear all files from configurations first
            foreach (var testConfig in testConfigurations)
            {
                testConfig.ClearSourceFiles();
            }
            
            // Clear the legacy sourceFiles list (keep for backward compatibility)
            sourceFiles.Clear();
            
            foreach (var testConfig in testConfigurations)
            {
                ScanDirectoryForConfiguration(testConfig);
            }

            // Count total files across all configurations
            var totalFiles = testConfigurations.Sum(config => config.SourceFiles.Count);
            logger.Info($"Found {totalFiles} source files across {testConfigurations.Count} configurations");
            
            // Preload git information for performance if git is enabled and we have files
            if (gitRepo != null && totalFiles > 0)
            {
                logger.Debug($"Preloading git information for {totalFiles} source files across {testConfigurations.Count} configurations");
                
                // Get unique directories that contain our source files
                var sourceDirectories = testConfigurations
                    .SelectMany(config => config.SourceFiles)
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

        private void ScanDirectoryForConfiguration(TestConfiguration testConfig)
        {
            if (string.IsNullOrEmpty(testConfig.TestDirectory))
            {
                logger.Warn($"Empty TestDirectory in configuration for project '{testConfig.Project}', skipping");
                return;
            }

            IDirectoryInfo dir = fileSystem.DirectoryInfo.New(testConfig.TestDirectory);
            try
            {
                foreach (var fileMask in testConfig.FileMasks)
                {
                    IFileInfo[] files = dir.GetFiles(fileMask, testConfig.SubDirs ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                    foreach (var file in files)
                    {
                        testConfig.AddSourceFile(file.FullName);
                        sourceFiles.Add(file.FullName); // Keep for backward compatibility
                        logger.Debug($"Found source file: {file.FullName} (Project: {testConfig.Project}, Language: {testConfig.Language})");
                    }
                }
                
                logger.Info($"Configuration '{testConfig.Project}' ({testConfig.Language}): Found {testConfig.SourceFiles.Count} source files");
            }
            catch (Exception ex)
            {
                logger.Error($"Error reading directory {testConfig.TestDirectory} for project '{testConfig.Project}': {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get the test configuration that applies to a specific source file path
        /// </summary>
        /// <param name="filePath">The path to the source file</param>
        /// <returns>The TestConfiguration that matches this file, or null if not found</returns>
        protected TestConfiguration GetConfigurationForFile(string filePath)
        {
            foreach (var config in testConfigurations)
            {
                if (IsFileInDirectory(filePath, config.TestDirectory, config.SubDirs))
                {
                    return config;
                }
            }
            return null;
        }

        private bool IsFileInDirectory(string filePath, string directoryPath, bool includeSubDirs)
        {
            try
            {
                var fileInfo = fileSystem.FileInfo.New(filePath);
                var dirInfo = fileSystem.DirectoryInfo.New(directoryPath);
                
                var fileDir = fileInfo.Directory;
                while (fileDir != null)
                {
                    if (string.Equals(fileDir.FullName, dirInfo.FullName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                    
                    if (!includeSubDirs)
                    {
                        break;
                    }
                    
                    fileDir = fileDir.Parent;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets all test configurations
        /// </summary>
        protected IReadOnlyList<TestConfiguration> TestConfigurations => testConfigurations.AsReadOnly();
    }
}
