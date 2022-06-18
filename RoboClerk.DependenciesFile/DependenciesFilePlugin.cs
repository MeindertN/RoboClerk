using RoboClerk.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Tomlyn;
using Tomlyn.Model;

namespace RoboClerk.DependenciesFile
{
    public class DependenciesFilePlugin : IDependencyManagementPlugin
    {
        private string name = string.Empty;
        private string description = string.Empty;
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private List<string> fileLocations = new List<string>();
        private List<string> fileFormats = new List<string>();

        private List<ExternalDependency> externalDependencies = new List<ExternalDependency>();

        public DependenciesFilePlugin()
        {
            name = "DependenciesFilePlugin";
            description = "A plugin that retrieves project dependencies via one or more files.";
        }

        public string Name => name;

        public string Description => description;

        public List<ExternalDependency> GetDependencies()
        {
            return externalDependencies;
        }

        public void Initialize(IConfiguration configuration)
        {
            logger.Info("Initializing the Dependencies Files Plugin");
            var assembly = Assembly.GetAssembly(this.GetType());
            try
            {
                var configFileLocation = $"{Path.GetDirectoryName(assembly?.Location)}/Configuration/DependenciesFilePlugin.toml";
                if (configuration.PluginConfigDir != string.Empty)
                {
                    configFileLocation = Path.Combine(configuration.PluginConfigDir, "DependenciesFilePlugin.toml");
                }
                var config = Toml.Parse(File.ReadAllText(configFileLocation)).ToModel();
                foreach( var item in (TomlArray)config["FileLocations"])
                {
                    fileLocations.Add((string)item);
                }
                foreach( var item in (TomlArray)config["FileFormats"])
                {
                    fileFormats.Add((string)item);
                }

                if(fileLocations.Count != fileFormats.Count)
                {
                    throw new Exception("The number of file locations and file formats provided in the configuration file should be the same.");
                }   
            }
            catch (Exception e)
            {
                logger.Error("Error reading configuration file for Dependencies File plugin.");
                logger.Error(e);
                throw new Exception("The Dependencies File plugin could not read its configuration. Aborting...");
            }
        }

        public void RefreshItems()
        {
            externalDependencies.Clear();
            for(int i = 0; i < fileLocations.Count; i++)
            {
                switch(fileFormats[i])
                {
                    case "GRADLE":
                        ParseGradleFile(fileLocations[i]);
                        break;
                    default: 
                        throw new Exception("Unknown file format indicated in Dependencies File Plugin congfiguration file.");
                }
            }
        }

        private void ParseGradleFile(string filename)
        {
            foreach (string line in File.ReadLines(filename))
            {
                if (line.StartsWith("+---") || line.StartsWith("\\---"))
                {
                    string[] elements = line.Substring(5).Split(':');
                    string version = elements[2].Trim();
                    bool conflict = false;
                    if (version.Contains("(*)"))
                    {
                        version = version.Split("(*)")[0].Trim();
                    }
                    if (version.Contains("->"))
                    {
                        version = version.Split("->")[1].Trim();
                        conflict = true;
                    }
                    externalDependencies.Add(new ExternalDependency($"{elements[0]}:{elements[1]}",version, conflict));
                }
            }
        }
    }
}