using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace RoboClerk
{
    public class PluginLoader : IPluginLoader
    {
        public T LoadPlugin<T>(string name, string pluginDir) where T : class
        {
            //get all the potential plugin dlls
            foreach (string file in Directory.GetFiles(pluginDir, "RoboClerk.*.dll", SearchOption.AllDirectories))
            {
                //go over all the plugins and try to load them as the appropriate type
                Console.WriteLine($"Loading from: {file}");
                PluginLoadContext loadContext = new PluginLoadContext(file);
                var assembly = loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(file)));
                //check the name of the plugin and return if found
                foreach (var plugin in CreatePlugins<T>(assembly))
                {
                    if ((plugin as IPlugin).Name == name)
                    {
                        //found the plugin we were looking for
                        return plugin;
                    }
                }
            }
            return null;
        }

        private IEnumerable<T> CreatePlugins<T>(Assembly assembly) where T : class
        {
            int count = 0;

            foreach (Type type in assembly.GetTypes())
            {
                if (typeof(T).IsAssignableFrom(type))
                {
                    T result = Activator.CreateInstance(type) as T;
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
