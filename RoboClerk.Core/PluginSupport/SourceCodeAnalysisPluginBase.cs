using RoboClerk.Core.Configuration;
using RoboClerk.SourceProviders;
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
        
        // Git source configuration (alternative to TestDirectory)
        public GitSourceConfiguration? GitSource { get; set; }
        
        // Effective directory to scan (resolved after source preparation)
        public string EffectiveScanDirectory { get; internal set; } = string.Empty;
        
        // Dictionary for all fields (including unknown ones)
        public Dictionary<string, object> AllFields { get; private set; } = new Dictionary<string, object>();
        
        // Files associated with this configuration
        public List<string> SourceFiles { get; private set; } = new List<string>();
        
        /// <summary>
        /// Indicates if this configuration uses a Git repository as the source
        /// </summary>
        public bool UsesGitSource => GitSource != null && !string.IsNullOrEmpty(GitSource.RepositoryUrl);

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
                
            // Parse GitSource section if present
            if (AllFields.TryGetValue("GitSource", out var gitSourceObj) && gitSourceObj is TomlTable gitSourceTable)
            {
                GitSource = ParseGitSourceConfiguration(gitSourceTable);
            }
            
            // Validate that either TestDirectory or GitSource is specified, but not both
            ValidateSourceConfiguration();
        }
        
        private GitSourceConfiguration ParseGitSourceConfiguration(TomlTable table)
        {
            var config = new GitSourceConfiguration();
            
            if (table.TryGetValue("RepositoryUrl", out var repoUrl))
                config.RepositoryUrl = repoUrl.ToString();
                
            if (table.TryGetValue("Branch", out var branch))
                config.Branch = branch.ToString();
                
            if (table.TryGetValue("CommitSha", out var sha))
                config.CommitSha = sha.ToString();
                
            if (table.TryGetValue("Tag", out var tag))
                config.Tag = tag.ToString();
                
            if (table.TryGetValue("SourcePath", out var sourcePath))
                config.SourcePath = sourcePath.ToString();
                
            if (table.TryGetValue("AuthMethod", out var authMethod))
            {
                config.AuthMethod = Enum.TryParse<GitAuthMethod>(authMethod.ToString(), ignoreCase: true, out var method)
                    ? method
                    : GitAuthMethod.None;
            }
            
            if (table.TryGetValue("CredentialKey", out var credKey))
                config.CredentialKey = credKey.ToString();
                
            if (table.TryGetValue("ShallowClone", out var shallow))
                config.ShallowClone = (bool)shallow;
                
            if (table.TryGetValue("TimeoutSeconds", out var timeout))
                config.TimeoutSeconds = Convert.ToInt32(timeout);
            
            return config;
        }
        
        private void ValidateSourceConfiguration()
        {
            var hasTestDirectory = !string.IsNullOrEmpty(TestDirectory);
            var hasGitSource = UsesGitSource;
            
            if (hasTestDirectory && hasGitSource)
            {
                throw new InvalidOperationException(
                    $"Configuration for project '{Project}' specifies both TestDirectory and GitSource. " +
                    "Only one source type can be used per configuration.");
            }
            
            if (!hasTestDirectory && !hasGitSource)
            {
                throw new InvalidOperationException(
                    $"Configuration for project '{Project}' must specify either TestDirectory or GitSource.");
            }
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




    public abstract class SourceCodeAnalysisPluginBase : DataSourcePluginBase, IDisposable
    {
        // Configuration-based approach
        protected List<TestConfiguration> testConfigurations = new List<TestConfiguration>();
        protected List<string> sourceFiles = new List<string>();
        protected GitRepository gitRepo = null;
        
        // Source providers for Git repository support
        private readonly List<ISourceProvider> sourceProviders = new List<ISourceProvider>();
        private ITempDirectoryManager tempDirectoryManager;
        private IGitCloneService gitCloneService;
        private IGitCredentialManager gitCredentialManager;
        private bool disposed;

        public SourceCodeAnalysisPluginBase(IFileProviderPlugin fileSystem)
            : base(fileSystem)
        {
        }

        public override void InitializePlugin(IConfiguration configuration)
        {
            var config = GetConfigurationTable(configuration.PluginConfigDir, $"{name}.toml");
            
            // Check if using TestConfigurations format
            if (config.ContainsKey("TestConfigurations"))
            {
                InitializeTestConfigurations(config, configuration);
            }
            else
            {
                throw new Exception($"TestConfigurations section is required in {name}.toml configuration file.");
            }

            try
            {
                if (config.ContainsKey("UseGit") && (bool)config["UseGit"])
                {
                    gitRepo = new GitRepository(configuration, fileProvider);
                }
            }
            catch (Exception)
            {
                logger.Error($"Error opening git repo at project root \"{configuration.ProjectRoot}\" even though the {name}.toml configuration file UseGit setting was set to true.");
                throw;
            }
        }

        private void InitializeTestConfigurations(TomlTable config, IConfiguration appConfig)
        {
            testConfigurations.Clear();
            sourceProviders.Clear();
            
            var configurationsArray = (TomlTableArray)config["TestConfigurations"];
            
            foreach (TomlTable configTable in configurationsArray)
            {
                var testConfig = new TestConfiguration();
                testConfig.FromToml(configTable);
                testConfigurations.Add(testConfig);
                
                // Create and initialize source provider for this configuration
                var provider = CreateSourceProvider(testConfig, appConfig);
                sourceProviders.Add(provider);
            }

            if (testConfigurations.Count == 0)
            {
                throw new Exception($"No test configurations found in {name}.toml. At least one TestConfiguration is required.");
            }
            
            // Prepare all source providers (clone git repos if needed)
            PrepareAllSources();
        }
        
        /// <summary>
        /// Creates a source provider for the given test configuration
        /// </summary>
        private ISourceProvider CreateSourceProvider(TestConfiguration testConfig, IConfiguration appConfig)
        {
            if (testConfig.UsesGitSource)
            {
                // Lazy initialize Git-related services
                EnsureGitServicesInitialized();
                
                logger.Info($"Creating Git source provider for project '{testConfig.Project}': {testConfig.GitSource!.RepositoryUrl}");
                return new GitSourceProvider(
                    testConfig.GitSource,
                    gitCloneService,
                    tempDirectoryManager,
                    fileProvider);
            }
            else
            {
                logger.Debug($"Creating local source provider for project '{testConfig.Project}': {testConfig.TestDirectory}");
                return new LocalSourceProvider(
                    testConfig.TestDirectory,
                    appConfig.ProjectRoot,
                    fileProvider);
            }
        }
        
        /// <summary>
        /// Ensures Git-related services are initialized
        /// </summary>
        private void EnsureGitServicesInitialized()
        {
            if (gitCredentialManager == null)
            {
                gitCredentialManager = new GitCredentialManager();
            }
            
            if (gitCloneService == null)
            {
                gitCloneService = new GitCloneService(gitCredentialManager);
            }
            
            if (tempDirectoryManager == null)
            {
                tempDirectoryManager = new TempDirectoryManager(new FileSystem());
            }
        }
        
        /// <summary>
        /// Prepares all source providers (clones Git repos, validates local directories)
        /// </summary>
        private void PrepareAllSources()
        {
            for (int i = 0; i < testConfigurations.Count; i++)
            {
                var testConfig = testConfigurations[i];
                var provider = sourceProviders[i];
                
                try
                {
                    logger.Info($"Preparing source for configuration '{testConfig.Project}'...");
                    var scanPath = provider.PrepareSourceAsync().GetAwaiter().GetResult();
                    testConfig.EffectiveScanDirectory = scanPath;
                    logger.Info($"Source prepared for '{testConfig.Project}': {scanPath}");
                }
                catch (Exception ex)
                {
                    logger.Error($"Failed to prepare source for configuration '{testConfig.Project}': {ex.Message}");
                    throw;
                }
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
            // Note: Only for local git repos, not for cloned repos
            if (gitRepo != null && totalFiles > 0)
            {
                logger.Debug($"Preloading git information for {totalFiles} source files across {testConfigurations.Count} configurations");
                
                // Get unique directories that contain our source files (only for local sources)
                var sourceDirectories = testConfigurations
                    .Where(config => !config.UsesGitSource) // Only local sources
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
            // Use EffectiveScanDirectory which is set during source preparation
            var scanDirectory = testConfig.EffectiveScanDirectory;
            
            if (string.IsNullOrEmpty(scanDirectory))
            {
                logger.Warn($"Empty scan directory in configuration for project '{testConfig.Project}', skipping");
                return;
            }

            if (!fileProvider.DirectoryExists(scanDirectory))
            {
                logger.Error($"Directory {scanDirectory} for project '{testConfig.Project}' does not exist");
                throw new DirectoryNotFoundException($"Directory not found: {scanDirectory}");
            }

            try
            {
                foreach (var fileMask in testConfig.FileMasks)
                {
                    string[] files = fileProvider.GetFiles(scanDirectory, fileMask, testConfig.SubDirs ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                    foreach (var file in files)
                    {
                        testConfig.AddSourceFile(file);
                        sourceFiles.Add(file); // Keep for backward compatibility
                        logger.Debug($"Found source file: {file} (Project: {testConfig.Project}, Language: {testConfig.Language})");
                    }
                }
                
                logger.Info($"Configuration '{testConfig.Project}' ({testConfig.Language}): Found {testConfig.SourceFiles.Count} source files");
            }
            catch (Exception ex)
            {
                logger.Error($"Error reading directory {scanDirectory} for project '{testConfig.Project}': {ex.Message}");
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
                var scanDir = config.EffectiveScanDirectory;
                if (!string.IsNullOrEmpty(scanDir) && IsFileInDirectory(filePath, scanDir, config.SubDirs))
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
                string fileDir = fileProvider.GetDirectoryName(filePath);
                string fullDirectoryPath = fileProvider.GetFullPath(directoryPath);
                
                while (!string.IsNullOrEmpty(fileDir))
                {
                    if (string.Equals(fileProvider.GetFullPath(fileDir), fullDirectoryPath, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                    
                    if (!includeSubDirs)
                    {
                        break;
                    }
                    
                    fileDir = fileProvider.GetDirectoryName(fileDir);
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
        
        /// <summary>
        /// Disposes resources including temporary directories for cloned repositories
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Disposes resources
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Dispose all source providers
                    foreach (var provider in sourceProviders)
                    {
                        try
                        {
                            provider.Dispose();
                        }
                        catch (Exception ex)
                        {
                            logger.Warn($"Error disposing source provider: {ex.Message}");
                        }
                    }
                    sourceProviders.Clear();
                    
                    // Dispose temp directory manager
                    tempDirectoryManager?.Dispose();
                    tempDirectoryManager = null;
                }
                
                disposed = true;
            }
        }
        
        ~SourceCodeAnalysisPluginBase()
        {
            Dispose(false);
        }
    }
}
