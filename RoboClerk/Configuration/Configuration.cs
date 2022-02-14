using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using Tomlyn;
using Tomlyn.Model;
using System.Linq;

namespace RoboClerk.Configuration
{
    internal class Configuration : IConfiguration
    {
        private readonly IFileSystem fileSystem = null;
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        
        //The information contained in the general configuration file
        private List<string> dataSourcePlugins = new List<string>();
        private List<string> pluginDirs = new List<string>();
        private string outputDir = string.Empty;
        private string logLevel = string.Empty;

        //The information contained in the project configuration file
        private List<TraceEntity> truthEntities = new List<TraceEntity>();
        private List<DocumentConfig> documents = new List<DocumentConfig>();
        private List<TraceConfig> traceConfig = new List<TraceConfig>();
        private ConfigurationValues configVals = null;

        public Configuration(IFileSystem fs, string configFileName, string projectConfigFileName)
        {
            fileSystem = fs;
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
            outputDir = (string)toml["OutputDirectory"];
            logLevel = (string)toml["LogLevel"];
        }

        private void ReadProjectConfigFile(string projectConfig)
        {
            var toml = Toml.Parse(projectConfig).ToModel();
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
                if (!doc.ContainsKey("template") || !doc.ContainsKey("title") || !doc.ContainsKey("abbreviation"))
                {
                    throw new Exception($"Error reading document configuration out of project configuration file for document {doctable.Key}");
                }
                DocumentConfig docConfig = new DocumentConfig(id, (string)doc["title"], (string)doc["abbreviation"], (string)doc["template"]);

                if (doc.ContainsKey("Command"))
                {
                    docConfig.AddCommands(new Commands((TomlTableArray)doc["Command"], outputDir, Path.GetFileName((string)doc["template"])));
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
                TraceEntity entity = new TraceEntity(entityTable.Key, (string)elements["name"], (string)elements["abbreviation"]);
                
                truthEntities.Add(entity);
            }
        }
    }
}
