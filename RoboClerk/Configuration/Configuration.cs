using RoboClerk.AISystem;
using RoboClerk.Core.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Tomlyn;
using Tomlyn.Model;

namespace RoboClerk.Configuration
{
    public class Configuration : IConfiguration
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        //The information contained in the general configuration file
        private List<string> dataSourcePlugins = new List<string>();
        private List<string> pluginDirs = new List<string>();
        private string aiPlugin = string.Empty;
        private string outputDir = string.Empty;
        private string logLevel = string.Empty;
        private string pluginConfigDir = string.Empty;
        private bool clearOutput = false;
        private string outputFormat = string.Empty;
        private string fileProviderPlugin = string.Empty;

        //The information contained in the project configuration file
        private List<TraceEntity> truthEntities = new List<TraceEntity>();
        private List<DocumentConfig> documents = new List<DocumentConfig>();
        private List<TraceConfig> traceConfig = new List<TraceConfig>();
        private CheckpointConfig checkpointConfig = new CheckpointConfig();
        private List<TraceEntity> checkTraceEntitiesAI = new List<TraceEntity>();
        private bool checkTemplateContentsAI = false;
        private ConfigurationValues configVals = null;
        private string templateDir = string.Empty;
        private string mediaDir = string.Empty;
        private string projectRoot = string.Empty;
        private string projectName = string.Empty;
        private string projectID = string.Empty; //assigned by the server to indicate the sharepoint project this config belongs to

        //The information supplied on the commandline
        internal Dictionary<string, string> commandLineOptions = new Dictionary<string, string>();

        internal bool projectConfigLoaded = false;

        // Internal constructor - accessible to builder
        internal Configuration() { }

        /// <summary>
        /// Creates a new configuration builder
        /// </summary>
        /// <returns>A new ConfigurationBuilder instance</returns>
        public static ConfigurationBuilder CreateBuilder()
        {
            return new ConfigurationBuilder();
        }

        /// <summary>
        /// Creates a deep clone of this configuration object
        /// </summary>
        /// <returns>A new Configuration instance that is a deep copy of this one</returns>
        public Configuration Clone()
        {
            var cloned = new Configuration();
            
            // Clone general configuration
            cloned.pluginDirs = new List<string>(pluginDirs);
            cloned.logLevel = logLevel;
            cloned.pluginConfigDir = pluginConfigDir;
            cloned.clearOutput = clearOutput;
            cloned.outputFormat = outputFormat;
            cloned.fileProviderPlugin = fileProviderPlugin;

            // Clone command line options
            cloned.commandLineOptions = new Dictionary<string, string>(commandLineOptions);
            
            // Clone project configuration if loaded
            if (projectConfigLoaded)
            {
                cloned.dataSourcePlugins = new List<string>(dataSourcePlugins);
                cloned.truthEntities = new List<TraceEntity>(truthEntities);
                cloned.documents = new List<DocumentConfig>(documents);
                cloned.traceConfig = new List<TraceConfig>(traceConfig);
                cloned.checkpointConfig = checkpointConfig; // CheckpointConfig should implement proper cloning if needed
                cloned.checkTraceEntitiesAI = new List<TraceEntity>(checkTraceEntitiesAI);
                cloned.checkTemplateContentsAI = checkTemplateContentsAI;
                cloned.configVals = configVals; // ConfigurationValues should implement proper cloning if needed
                cloned.templateDir = templateDir;
                cloned.mediaDir = mediaDir;
                cloned.projectRoot = projectRoot;
                cloned.projectConfigLoaded = projectConfigLoaded;
                cloned.projectName = projectName;
                cloned.aiPlugin = aiPlugin;
                cloned.outputDir = outputDir;
            }
            
            logger.Debug("Configuration cloned successfully");
            return cloned;
        }

        // Public properties
        public List<string> DataSourcePlugins => dataSourcePlugins;
        public List<string> PluginDirs => pluginDirs;
        public string OutputDir => outputDir;
        public string LogLevel => logLevel;
        public string OutputFormat => outputFormat;
        public string PluginConfigDir => pluginConfigDir;
        public bool ClearOutputDir => clearOutput;
        public string AIPlugin => aiPlugin;
        public string FileProviderPlugin => fileProviderPlugin;
        public string ProjectName => projectName;
        public string ProjectID 
        { 
            get => projectID; 
            set => projectID = value;
        }

        public string GetCommandLineOption(string name)
        {
            if (commandLineOptions.ContainsKey(name))
            {
                return commandLineOptions[name];
            }
            throw new KeyNotFoundException($"Command line option '{name}' not found.");
        }

        public bool HasCommandLineOption(string name)
        {
            return commandLineOptions.ContainsKey(name);
        }

        public void AddOrUpdateCommandLineOption(string name, string value)
        {
            commandLineOptions[name] = value;
            logger.Debug($"Command line option '{name}' set to '{value}'");
        }

        // Project-specific properties with guards
        public List<TraceEntity> TruthEntities 
        { 
            get 
            { 
                EnsureProjectConfigLoaded();
                return truthEntities; 
            } 
        }
        
        public List<TraceEntity> AICheckTraceEntities 
        { 
            get 
            { 
                EnsureProjectConfigLoaded();
                return checkTraceEntitiesAI; 
            } 
        }
        
        public bool AICheckTemplateContents 
        { 
            get 
            { 
                EnsureProjectConfigLoaded();
                return checkTemplateContentsAI; 
            } 
        }
        
        public List<DocumentConfig> Documents 
        { 
            get 
            { 
                EnsureProjectConfigLoaded();
                return documents; 
            } 
        }
        
        public List<TraceConfig> TraceConfig 
        { 
            get 
            { 
                EnsureProjectConfigLoaded();
                return traceConfig; 
            } 
        }
        
        public CheckpointConfig CheckpointConfig 
        { 
            get 
            { 
                EnsureProjectConfigLoaded();
                return checkpointConfig; 
            } 
        }
        
        public ConfigurationValues ConfigVals 
        { 
            get 
            { 
                EnsureProjectConfigLoaded();
                return configVals; 
            } 
        }
        
        public string TemplateDir 
        { 
            get 
            { 
                EnsureProjectConfigLoaded();
                return templateDir; 
            } 
        }
        
        public string MediaDir 
        { 
            get 
            { 
                EnsureProjectConfigLoaded();
                return mediaDir; 
            } 
        }
        
        public string ProjectRoot 
        { 
            get 
            { 
                EnsureProjectConfigLoaded();
                return projectRoot; 
            } 
        }

        // Internal method to load project config after RoboClerk config is loaded
        internal void LoadProjectConfiguration(string projectConfigContent)
        {
            if (projectConfigLoaded)
            {
                throw new InvalidOperationException("Project configuration already loaded");
            }

            ReadProjectConfigFile(projectConfigContent);
            projectConfigLoaded = true;
            logger.Debug("Project configuration loaded successfully");
        }

        private void EnsureProjectConfigLoaded()
        {
            if (!projectConfigLoaded)
            {
                throw new InvalidOperationException("Project configuration not loaded. Use ConfigurationBuilder to load project configuration first.");
            }
        }

        internal void ReadGeneralConfigFile(string configFile)
        {
            var toml = Toml.Parse(configFile).ToModel();
            foreach (var obj in (TomlArray)toml["PluginDirs"])
            {
                pluginDirs.Add((string)obj);
            }
            pluginConfigDir = CommandLineOptionOrDefault("PluginConfigurationDir", (string)toml["PluginConfigurationDir"]);
            clearOutput = CommandLineOptionOrDefault("ClearOutputDir", (string)toml["ClearOutputDir"]).ToUpper() == "TRUE";
            logLevel = CommandLineOptionOrDefault("LogLevel", (string)toml["LogLevel"]);
            outputFormat = CommandLineOptionOrDefault("OutputFormat", (string)toml["OutputFormat"]);
            fileProviderPlugin = CommandLineOptionOrDefault("FileProviderPlugin", (string)toml["FileProviderPlugin"]);
        }

        private void ReadProjectConfigFile(string projectConfig)
        {
            var toml = Toml.Parse(projectConfig).ToModel();
            templateDir = CommandLineOptionOrDefault("TemplateDirectory", (string)toml["TemplateDirectory"]);
            outputDir = CommandLineOptionOrDefault("OutputDirectory", (string)toml["OutputDirectory"]);
            projectRoot = CommandLineOptionOrDefault("ProjectRoot", (string)toml["ProjectRoot"]);
            aiPlugin = CommandLineOptionOrDefault("AISystemPlugin", (string)toml["AISystemPlugin"]);
            projectName = CommandLineOptionOrDefault("ProjectName", (string)toml["ProjectName"]);
            foreach (var obj in (TomlArray)toml["DataSourcePlugin"])
            {
                dataSourcePlugins.Add((string)obj);
            }
            if (toml.ContainsKey("MediaDirectory"))
            {
                mediaDir = CommandLineOptionOrDefault("MediaDirectory", (string)toml["MediaDirectory"]);
            }
            else
            {
                logger.Warn("MediaDirectory entry is missing from project configuration file, some images will not be shown in output documents.");
            }
            if(toml.ContainsKey("PluginConfigurationDir") && ((string)toml["PluginConfigurationDir"]) != string.Empty)
            {
                pluginConfigDir = (string)toml["PluginConfigurationDir"];
            }
            ReadTruthTraceItems(toml);
            ReadDocuments(toml);
            ReadTraceConfiguration(toml);
            ReadCheckpointConfiguration(toml);
            ReadConfigurationValues(toml);
            ReadAISystemConfigurationValues(toml); //this step depends on the fact the truth trace items have already been read from the config file
            AddEliminatedTraceEntity(); //this is a trace entity that is used for all eliminated (i.e. ignored) items. Trace to eliminated items is not supported
        }

        private void AddEliminatedTraceEntity()
        {
            TraceEntity entity = new TraceEntity("Eliminated", "Eliminated Item", "EI", TraceEntityType.Eliminated);

            truthEntities.Add(entity);
        }

        private void ReadAISystemConfigurationValues(TomlTable toml)
        {
            if (!toml.ContainsKey("AIFeedback"))
            {
                logger.Warn("AIFeedback table missing from the project configuration file. RoboClerk will not be able to provide any AI feedback.");
                return;
            }
            foreach (var val in (TomlTable)toml["AIFeedback"])
            {
                switch (val.Key)
                {
                    case "ProvideTemplateContentFeedback":
                        checkTemplateContentsAI = ((string)val.Value).ToUpper() == "TRUE";
                        break;
                    case "TruthItemsAIFeedback":
                        {
                            foreach (var truthitemString in (TomlArray)val.Value)
                            {
                                foreach(var truthitem in truthEntities)
                                {
                                    if(truthitem.ID.ToUpper() == truthitemString.ToString().ToUpper())
                                    {
                                        checkTraceEntitiesAI.Add(truthitem);
                                    }
                                }                                
                            }
                            break;
                        }
                    default:
                        throw new Exception($"Unknown AIFeedback item \"{val.Key}\" found, please check project configuration file.");
                }
            }
        }

        private void ReadCheckpointConfiguration(TomlTable toml)
        {
            checkpointConfig = new CheckpointConfig();
            checkpointConfig.FromToml(toml);
        }

        private void ReadConfigurationValues(TomlTable toml)
        {
            configVals = new ConfigurationValues();
            configVals.FromToml(toml);
        }

        private void ReadTraceConfiguration(TomlTable toml)
        {
            foreach (var table in (TomlTable)toml["TraceConfig"])
            {
                TraceConfig conf = new TraceConfig(table.Key);
                conf.AddTraces((TomlTable)table.Value);
                traceConfig.Add(conf);
            }
        }

        private void ReadDocuments(TomlTable toml)
        {
            foreach (var doctable in (TomlTable)toml["Document"])
            {
                string id = (string)doctable.Key;

                TomlTable doc = (TomlTable)doctable.Value;
                if (!doc.ContainsKey("title") || !doc.ContainsKey("abbreviation") || !doc.ContainsKey("identifier"))
                {
                    throw new Exception($"Error reading document configuration out of project configuration file for document {doctable.Key}");
                }
                DocumentConfig docConfig = new DocumentConfig(id, (string)doc["identifier"], (string)doc["title"], (string)doc["abbreviation"],
                    doc.ContainsKey("template") ? (string)doc["template"] : string.Empty);

                if (doc.ContainsKey("template") && doc.ContainsKey("Command"))
                {
                    docConfig.AddCommands(new Commands((TomlTableArray)doc["Command"], outputDir, Path.GetFileName((string)doc["template"]), templateDir));
                }

                documents.Add(docConfig);
            }
        }

        private void ReadTruthTraceItems(TomlTable toml)
        {
            var truth = (TomlTable)toml["Truth"];
            foreach (var entityTable in truth)
            {
                TomlTable elements = (TomlTable)entityTable.Value;
                if (!elements.ContainsKey("name") || !elements.ContainsKey("abbreviation"))
                {
                    throw new Exception($"Error while reading {entityTable.Key} truth entity from project config file. Check if all required elements (\"name\" and \"abbreviation\") are present.");
                }
                TraceEntity entity = new TraceEntity(entityTable.Key, (string)elements["name"], (string)elements["abbreviation"], TraceEntityType.Truth);

                truthEntities.Add(entity);
            }
        }

        public string CommandLineOptionOrDefault(string name, string defaultValue)
        {
            if (commandLineOptions.ContainsKey(name))
            {
                return commandLineOptions[name];
            }
            return defaultValue;
        }
    }

    /// <summary>
    /// Builder for Configuration class that allows flexible loading of RoboClerk and project configurations
    /// from different file providers and at different times.
    /// </summary>
    public class ConfigurationBuilder
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private Configuration config = new Configuration();
        private bool roboClerkConfigLoaded = false;

        internal ConfigurationBuilder() { }

        /// <summary>
        /// Creates a builder from an existing configuration object.
        /// This is useful when you want to modify or reload parts of an existing configuration.
        /// </summary>
        /// <param name="existingConfig">The existing configuration to base this builder on</param>
        /// <returns>A new ConfigurationBuilder instance initialized with the existing configuration</returns>
        public static ConfigurationBuilder FromExisting(Configuration existingConfig)
        {
            if (existingConfig == null)
                throw new ArgumentNullException(nameof(existingConfig));

            var builder = new ConfigurationBuilder();
            builder.config = existingConfig;
            
            // Determine if RoboClerk config was already loaded based on the presence of general config data
            builder.roboClerkConfigLoaded = !string.IsNullOrEmpty(existingConfig.LogLevel) || 
                                           existingConfig.PluginDirs.Any() || 
                                           !string.IsNullOrEmpty(existingConfig.OutputFormat);
            
            logger.Debug("ConfigurationBuilder created from existing configuration");
            return builder;
        }

        /// <summary>
        /// Loads the RoboClerk configuration from the specified file provider and path.
        /// This should be called first as it contains information needed to determine
        /// how to load the project configuration.
        /// </summary>
        /// <param name="fileProvider">The file provider to use for reading the config file</param>
        /// <param name="configPath">The path to the RoboClerk configuration file</param>
        /// <param name="commandLineOptions">Optional command line options that can override config values</param>
        /// <returns>This builder instance for chaining</returns>
        public ConfigurationBuilder WithRoboClerkConfig(IFileProviderPlugin fileProvider, string configPath, Dictionary<string, string> commandLineOptions = null)
        {
            if (roboClerkConfigLoaded)
            {
                throw new InvalidOperationException("RoboClerk configuration already loaded");
            }

            config.commandLineOptions = commandLineOptions ?? new Dictionary<string, string>();
            
            try
            {
                string configContent = fileProvider.ReadAllText(configPath);
                config.ReadGeneralConfigFile(configContent);
                roboClerkConfigLoaded = true;
                logger.Debug($"RoboClerk configuration loaded from: {configPath}");
            }
            catch (IOException ex)
            {
                throw new FileNotFoundException($"Unable to read RoboClerk config file: {configPath}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error parsing RoboClerk config file: {configPath}", ex);
            }
            
            return this;
        }

        /// <summary>
        /// Loads the project configuration from the specified file provider and path.
        /// This should be called after WithRoboClerkConfig().
        /// </summary>
        /// <param name="fileProvider">The file provider to use for reading the project config file</param>
        /// <param name="projectConfigPath">The path to the project configuration file</param>
        /// <returns>This builder instance for chaining</returns>
        public ConfigurationBuilder WithProjectConfig(IFileProviderPlugin fileProvider, string projectConfigPath)
        {
            if (!roboClerkConfigLoaded)
            {
                throw new InvalidOperationException("RoboClerk configuration must be loaded before project configuration. Call WithRoboClerkConfig() first.");
            }

            try
            {
                string projectConfigContent = fileProvider.ReadAllText(projectConfigPath);
                config.LoadProjectConfiguration(projectConfigContent);
                logger.Debug($"Project configuration loaded from: {projectConfigPath}");
            }
            catch (IOException ex)
            {
                throw new FileNotFoundException($"Unable to read project config file: {projectConfigPath}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error parsing project config file: {projectConfigPath}", ex);
            }
            
            return this;
        }

        /// <summary>
        /// Loads the project configuration from a string content.
        /// This is useful when the project config content is already available as a string.
        /// </summary>
        /// <param name="projectConfigContent">The project configuration content as a string</param>
        /// <returns>This builder instance for chaining</returns>
        public ConfigurationBuilder WithProjectConfigContent(string projectConfigContent)
        {
            if (!roboClerkConfigLoaded)
            {
                throw new InvalidOperationException("RoboClerk configuration must be loaded before project configuration. Call WithRoboClerkConfig() first.");
            }

            try
            {
                config.LoadProjectConfiguration(projectConfigContent);
                logger.Debug("Project configuration loaded from content string");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error parsing project config content", ex);
            }
            
            return this;
        }

        /// <summary>
        /// Builds and returns the final Configuration instance.
        /// </summary>
        /// <returns>A fully configured Configuration instance</returns>
        public Configuration Build()
        {
            if (!roboClerkConfigLoaded)
            {
                throw new InvalidOperationException("RoboClerk configuration must be loaded before building. Call WithRoboClerkConfig() first.");
            }

            // Validate that all required configs are loaded
            if (!config.projectConfigLoaded)
            {
                logger.Warn("Configuration built without project configuration - some features may not be available");
            }
            
            logger.Debug("Configuration build completed successfully");
            return config;
        }
    }
}
