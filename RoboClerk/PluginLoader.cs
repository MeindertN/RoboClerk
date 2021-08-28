using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace RoboClerk
{
    public static class PluginLoader
    {
        public static IEnumerable<ISLMSPlugin> LoadPlugins(string pluginDir)
        {
            string[] pluginPaths = new string[1] { pluginDir };
                
            IEnumerable<ISLMSPlugin> plugins = pluginPaths.SelectMany(pluginPath =>
            {
                Assembly pluginAssembly = LoadPlugin(pluginPath);
                return CreateSLMSPlugins(pluginAssembly);
            }).ToList();
            return plugins;
        }

        private static Assembly LoadPlugin(string relativePath)
        {
            // Navigate up to the solution root
            string root = Path.GetFullPath(Path.Combine(
                Path.GetDirectoryName(
                    Path.GetDirectoryName(
                        Path.GetDirectoryName(
                            Path.GetDirectoryName(
                                Path.GetDirectoryName(typeof(Program).Assembly.Location)))))));

            string pluginLocation = Path.GetFullPath(Path.Combine(root, relativePath.Replace('\\', Path.DirectorySeparatorChar)));
            Console.WriteLine($"Loading from: {pluginLocation}");
            PluginLoadContext loadContext = new PluginLoadContext(pluginLocation);
            return loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(pluginLocation)));
        }

        private static IEnumerable<ISLMSPlugin> CreateSLMSPlugins(Assembly assembly)
        {
            int count = 0;

            foreach (Type type in assembly.GetTypes())
            {
                if (typeof(ISLMSPlugin).IsAssignableFrom(type))
                {
                    ISLMSPlugin result = Activator.CreateInstance(type) as ISLMSPlugin;
                    if (result != null)
                    {
                        count++;
                        yield return result;
                    }
                }
            }
        }
    }
}
