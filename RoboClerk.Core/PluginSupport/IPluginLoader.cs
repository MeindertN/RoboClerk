using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO.Abstractions;

namespace RoboClerk
{
    public interface IPluginLoader
    {
        public T LoadByName<T>(string pluginDir,
            string typeName,
            Action<IServiceCollection> configureGlobals) where T : class, IPlugin;
    }
}