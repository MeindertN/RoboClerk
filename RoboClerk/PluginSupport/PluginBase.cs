using Microsoft.Extensions.DependencyInjection;
using RoboClerk.Configuration;
using System;
using System.IO.Abstractions;
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
              
        public PluginBase()
        {
        }

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

        public abstract void ConfigureServices(IServiceCollection services);

        protected T GetObjectForKey<T>(TomlTable config, string keyName, bool required)
        {
            string result = string.Empty;
            if (config.ContainsKey(keyName))
            {
                return (T)config[keyName];
            }
            else
            {
                if (required)
                {
                    throw new Exception($"Required key \"{keyName}\" missing from configuration file for {name}. Cannot continue.");
                }
                else
                {
                    logger.Warn($"Key \"{keyName}\" missing from configuration file for {name}. Attempting to continue.");
                    return default(T);
                }
            }
        }

    }
}
