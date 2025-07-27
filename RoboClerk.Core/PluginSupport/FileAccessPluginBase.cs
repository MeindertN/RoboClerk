using Microsoft.Extensions.DependencyInjection;
using RoboClerk.Configuration;
using System;
using System.IO.Abstractions;
using System.Reflection;
using Tomlyn;
using Tomlyn.Model;

namespace RoboClerk
{
    /// <summary>
    /// Base class for plugins that need to interact with the file system.
    /// This class provides access to file operations through IFileProviderPlugin.
    /// </summary>
    public abstract class FileAccessPluginBase : PluginBase
    {
        protected IFileProviderPlugin fileProvider = null!;
              
        public FileAccessPluginBase(IFileProviderPlugin fileProvider)
        {
            this.fileProvider = fileProvider ?? throw new ArgumentNullException(nameof(fileProvider));
        }

        protected TomlTable GetConfigurationTable(string pluginConfDir, string confFileName)
        {
            if (string.IsNullOrEmpty(pluginConfDir) || string.IsNullOrEmpty(confFileName))
            {
                throw new ArgumentException("Cannot get configuration table because the plugin configuration directory or the config filename are empty.");
            }
            
            var configFileLocation = fileProvider.Combine(pluginConfDir, confFileName);
            return Toml.Parse(fileProvider.ReadAllText(configFileLocation)).ToModel();
        }
    }
} 