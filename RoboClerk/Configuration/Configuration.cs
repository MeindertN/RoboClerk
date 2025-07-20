using RoboClerk.AISystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using Tomlyn;
using Tomlyn.Model;

namespace RoboClerk.Configuration
{
    public class Configuration : IConfiguration
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        //The information contained in the general configuration file
        private List<string> dataSourcePlugins = new List<string>();
        private string pluginDir = string.Empty;
        private string aiPlugin = string.Empty;
        private string fileProviderPlugin = string.Empty;
        private string outputDir = string.Empty;
        private string logLevel = string.Empty;
        private string pluginConfigDir = string.Empty;
        private bool clearOutput = false;
        private string outputFormat = string.Empty;

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

        //The information supplied on the commandline
        private Dictionary<string, string> commandLineOptions = new Dictionary<string, string>();

        public Configuration(string mainConfig, Dictionary<string, string> commandLineOptions)
        {
            this.commandLineOptions = commandLineOptions;
            ReadGeneralConfigFile(mainConfig);
        }

        public List<string> DataSourcePlugins => dataSourcePlugins;
        public string PluginDir => pluginDir;
        public string OutputDir => outputDir;
        public string LogLevel => logLevel;
        public string OutputFormat => outputFormat;
        public List<TraceEntity> TruthEntities => truthEntities;
        public List<TraceEntity> AICheckTraceEntities => checkTraceEntitiesAI;
        public bool AICheckTemplateContents => checkTemplateContentsAI;
        public List<DocumentConfig> Documents => documents;
        public List<TraceConfig> TraceConfig => traceConfig;
        public CheckpointConfig CheckpointConfig => checkpointConfig;
        public ConfigurationValues ConfigVals => configVals;
        public string PluginConfigDir => pluginConfigDir;
        public string TemplateDir => templateDir;
        public string MediaDir => mediaDir;
        public string ProjectRoot => projectRoot;
        public bool ClearOutputDir => clearOutput;
        public string AIPlugin => aiPlugin;
        public string FileProviderPlugin => fileProviderPlugin;

        public void AddProjectConfig(string projectConfig)
        {
            ReadProjectConfigFile(projectConfig);
        }

        private void ReadGeneralConfigFile(string configFile)
        {
            var toml = Toml.Parse(configFile).ToModel();
            foreach (var obj in (TomlArray)toml["DataSourcePlugin"])
            {
                dataSourcePlugins.Add((string)obj);
            }
            pluginDir = CommandLineOptionOrDefault("PluginDir", (string)toml["PluginDir"]);
            fileProviderPlugin = CommandLineOptionOrDefault("FileProviderPlugin", (string)toml["FileProviderPlugin"]);
            aiPlugin = CommandLineOptionOrDefault("AISystemPlugin", (string)toml["AISystemPlugin"]);
            pluginConfigDir = CommandLineOptionOrDefault("PluginConfigurationDir", (string)toml["PluginConfigurationDir"]);
            outputDir = CommandLineOptionOrDefault("OutputDirectory", (string)toml["OutputDirectory"]);
            clearOutput = CommandLineOptionOrDefault("ClearOutputDir", (string)toml["ClearOutputDir"]).ToUpper() == "TRUE";
            logLevel = CommandLineOptionOrDefault("LogLevel", (string)toml["LogLevel"]);
            outputFormat = CommandLineOptionOrDefault("OutputFormat", (string)toml["OutputFormat"]);
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
}
