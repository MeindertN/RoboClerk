using LibGit2Sharp;
using RoboClerk.Configuration;
using System;
using System.IO;
using System.Reflection;
using Tomlyn;
using Tomlyn.Model;

namespace RoboClerk
{
    public abstract class PluginBase : IPlugin
    {
        protected string name = string.Empty;
        protected string description = string.Empty;
        protected static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public string Name 
        {
            get 
            { 
                return name; 
            } 
        }

        public string Description
        {
            get 
            { 
                return description; 
            }
        }

        public abstract void Initialize(IConfiguration config);

        protected string GetStringForKey(TomlTable config, string keyName, bool required)
        {
            string result = string.Empty;
            if (config.ContainsKey(keyName))
            {
                return (string)config[keyName];
            }
            else
            {
                if (required)
                {
                    throw new Exception($"Required key \"{keyName}\" missing from configuration file for {name}. Cannot continue.");
                }
                else
                {
                    logger.Warn($"Key \\\"{keyName}\\\" missing from configuration file for {name}. Attempting to continue.");
                    return string.Empty;
                }
            }
        }

        protected TomlTable GetConfigurationTable(string pluginConfDir, string confFileName)
        {
            var assembly = Assembly.GetAssembly(this.GetType());
            var configFileLocation = $"{Path.GetDirectoryName(assembly?.Location)}/Configuration/{confFileName}";
            if (pluginConfDir != string.Empty)
            {
                configFileLocation = Path.Combine(pluginConfDir, confFileName);
            }
            return Toml.Parse(File.ReadAllText(configFileLocation)).ToModel();
        }
    }
}
