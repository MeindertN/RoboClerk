using RoboClerk.Configuration;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using Tomlyn.Model;

namespace RoboClerk.DependenciesFile
{
    public class DependenciesFilePlugin : DataSourcePluginBase
    {
        private List<string> fileLocations = new List<string>();
        private List<string> fileFormats = new List<string>();

        private List<ExternalDependency> externalDependencies = new List<ExternalDependency>();

        public DependenciesFilePlugin(IFileSystem fileSystem)
            : base(fileSystem)
        {
            name = "DependenciesFilePlugin";
            description = "A plugin that retrieves project dependencies via one or more files.";
        }

        public override void Initialize(IConfiguration configuration)
        {
            logger.Info("Initializing the Dependencies Files Plugin");
            try
            {
                var config = GetConfigurationTable(configuration.PluginConfigDir, $"{name}.toml");
                foreach (var item in (TomlArray)config["FileLocations"])
                {
                    fileLocations.Add((string)item);
                }
                foreach (var item in (TomlArray)config["FileFormats"])
                {
                    fileFormats.Add((string)item);
                }

                if (fileLocations.Count != fileFormats.Count)
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

        public override void RefreshItems()
        {
            logger.Info("Refreshing the external dependencies from file.");
            externalDependencies.Clear();
            for (int i = 0; i < fileLocations.Count; i++)
            {
                switch (fileFormats[i])
                {
                    case "GRADLE":
                        ParseGradleFile(fileLocations[i]);
                        break;
                    case "DOTNET":
                        ParseDotnetFile(fileLocations[i]);
                        break;
                    default:
                        throw new Exception("Unknown file format indicated in Dependencies File Plugin congfiguration file.");
                }
            }
        }

        private void ParseDotnetFile(string filename)
        {
            if (!fileSystem.File.Exists(filename))
            {
                logger.Warn($"Cannot find Gradle file \"{filename}\" no dependencies will be loaded from this file.");
                return;
            }
            foreach (string line in fileSystem.File.ReadLines(filename))
            {
                string trimmedLine = line.Trim();
                if(trimmedLine.StartsWith("> "))
                {
                    string[] elements = trimmedLine.Split(' ',StringSplitOptions.RemoveEmptyEntries);
                    if(elements.Length < 4)
                    {
                        throw new Exception($"DOTNET dependencies file does not match expected format. Error in line \"{trimmedLine}\". Cannot continue.");
                    }
                    externalDependencies.Add(new ExternalDependency(elements[1], elements[3], elements[2] != elements[3]));
                }
            }
        }

        private void ParseGradleFile(string filename)
        {
            if (!fileSystem.File.Exists(filename))
            {
                logger.Warn($"Cannot find Gradle file \"{filename}\" no dependencies will be loaded from this file.");
                return;
            }
            foreach (string line in fileSystem.File.ReadLines(filename))
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
                        version = version.Split("->")[0].Trim();
                        conflict = true;
                    }
                    externalDependencies.Add(new ExternalDependency($"{elements[0]}:{elements[1]}", version, conflict));
                }
            }
        }
    }
}