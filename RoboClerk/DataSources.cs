using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Tomlyn;
using Tomlyn.Model;

namespace RoboClerk
{
    public class DataSources
    {
        private ConfigurationValues configVals = null;
        private List<ISLMSPlugin> slmsPlugins = new List<ISLMSPlugin>();

        public DataSources(string configFile, string projectConfigFile)
        {
            configVals = new ConfigurationValues();
            configVals.FromToml(projectConfigFile);
            Configure(configFile);
        }

        private void Configure(string configFile)
        {
            string config = File.ReadAllText(configFile);
            var toml = Toml.Parse(config).ToModel();

            foreach (var val in (TomlArray)toml["SLMSPlugin"])
            {
                foreach (var dir in (TomlArray)toml["RelativePluginDirs"])
                {
                    string path = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/{dir}";
                    path = Path.GetFullPath(path);
                    var plugin = PluginLoader.LoadPlugin<ISLMSPlugin>((string)val,path);
                    if (plugin != null)
                    {
                        plugin.Initialize();
                        slmsPlugins.Add(plugin);
                    }
                }
            }
        }

        public List<RequirementItem> getAllSoftwareRequirements()
        {
            List<RequirementItem> items = new List<RequirementItem>();
            foreach(var plugin in slmsPlugins)
            {
                plugin.RefreshItems();
                items.AddRange(plugin.GetSoftwareRequirements());
            }
            return items;
        }

        public List<RequirementItem> getAllProductRequirements()
        {
            List<RequirementItem> items = new List<RequirementItem>();
            foreach (var plugin in slmsPlugins)
            {
                plugin.RefreshItems();
                items.AddRange(plugin.GetProductRequirements());
            }
            return items;
        }

        public string GetConfigValue(string key)
        {
            return configVals.GetValue(key);
        }
    }
}
