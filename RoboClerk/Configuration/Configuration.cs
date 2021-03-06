using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using Tomlyn;
using Tomlyn.Model;
using System.Linq;

namespace RoboClerk.Configuration
{
    public class Configuration : IConfiguration
    {
        private readonly IFileSystem fileSystem = null;
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        
        //The information contained in the general configuration file
        private List<string> dataSourcePlugins = new List<string>();
        private List<string> pluginDirs = new List<string>();
        private string outputDir = string.Empty;
        private string logLevel = string.Empty;
        private string pluginConfigDir = string.Empty;
        private bool clearOutput = false;

        //The information contained in the project configuration file
        private List<TraceEntity> truthEntities = new List<TraceEntity>();
        private List<DocumentConfig> documents = new List<DocumentConfig>();
        private List<TraceConfig> traceConfig = new List<TraceConfig>();
        private ConfigurationValues configVals = null;
        private string templateDir = string.Empty;
        private string mediaDir = string.Empty;
        private string projectRoot = string.Empty;

        //The information supplied on the commandline
        private Dictionary<string, string> commandLineOptions = new Dictionary<string,string>();

        public Configuration(IFileSystem fileSystem, string configFileName, string projectConfigFileName, Dictionary<string,string> commandLineOptions)
        {
            this.commandLineOptions = commandLineOptions;
            this.fileSystem = fileSystem;
            logger.Debug($"Loading configuration files into RoboClerk: {configFileName} and {projectConfigFileName}");
            (configFileName, projectConfigFileName) = LoadConfigFiles(configFileName, projectConfigFileName);
            ProcessConfigs(configFileName, projectConfigFileName);
        }

        public List<string> DataSourcePlugins => dataSourcePlugins;
        public List<string> PluginDirs => pluginDirs;
        public string OutputDir => outputDir;
        public string LogLevel => logLevel;
        public List<TraceEntity> TruthEntities => truthEntities;
        public List<DocumentConfig> Documents => documents;
        public List<TraceConfig > TraceConfig => traceConfig;
        public ConfigurationValues ConfigVals => configVals;
        public string PluginConfigDir => pluginConfigDir;
        public string TemplateDir => templateDir;
        public string MediaDir => mediaDir;
        public string ProjectRoot => projectRoot;
        public bool ClearOutputDir => clearOutput;

        private (string, string) LoadConfigFiles(string configFile, string projectConfigFile)
        {
            string config;
            string projectConfig;
            try
            {
                config = fileSystem.File.ReadAllText(configFile);
            }
            catch (IOException)
            {
                throw new Exception($"Unable to read config file: {configFile}");
            }
            try
            {
                projectConfig = fileSystem.File.ReadAllText(projectConfigFile);
            }
            catch (IOException)
            {
                throw new Exception($"Unable to read project config file {projectConfigFile}");
            }
            return (config, projectConfig);
        }

        private void ProcessConfigs(string config, string projectConfig)
        {
            ReadGeneralConfigFile(config);
            ReadProjectConfigFile(projectConfig);
        }

        private void ReadGeneralConfigFile(string configFile)
        {
            var toml = Toml.Parse(configFile).ToModel();
            foreach( var obj in (TomlArray)toml["DataSourcePlugin"])
            {
                dataSourcePlugins.Add((string)obj);
            }
            foreach( var obj in (TomlArray)toml["PluginDirs"])
            {
                pluginDirs.Add((string)obj);    
            }
            pluginConfigDir = CommandLineOptionOrDefault("PluginConfigurationDir", (string)toml["PluginConfigurationDir"]);
            outputDir = CommandLineOptionOrDefault("OutputDirectory",(string)toml["OutputDirectory"]);
            clearOutput = CommandLineOptionOrDefault("ClearOutputDir", (string)toml["ClearOutputDir"]).ToUpper() == "TRUE";
            logLevel = CommandLineOptionOrDefault("LogLevel",(string)toml["LogLevel"]);
        }

        private void ReadProjectConfigFile(string projectConfig)
        {
            var toml = Toml.Parse(projectConfig).ToModel();
            templateDir = CommandLineOptionOrDefault("TemplateDirectory", (string)toml["TemplateDirectory"]);
            projectRoot = CommandLineOptionOrDefault("ProjectRoot", (string)toml["ProjectRoot"]);
            if (toml.ContainsKey("MediaDirectory"))
            {
                mediaDir = CommandLineOptionOrDefault("MediaDirectory", (string)toml["MediaDirectory"]);
            }
            else
            {
                logger.Warn("MediaDirectory entry is missing from project configuration file, some images will not be shown in output documents.");
            }
            ReadTruthTraceItems(toml);
            ReadDocuments(toml);
            ReadTraceConfiguration(toml);
            ReadConfigurationValues(toml);
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
            if(commandLineOptions.ContainsKey(name))
            {
                return commandLineOptions[name];
            }
            return defaultValue;
        }
    }
}
