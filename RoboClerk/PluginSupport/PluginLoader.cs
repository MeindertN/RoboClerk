using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace RoboClerk
{
    public class PluginLoadContext : AssemblyLoadContext
    {
        private AssemblyDependencyResolver _resolver;

        public PluginLoadContext(string pluginPath)
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            string assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }

            return IntPtr.Zero;
        }
    }

    public class PluginLoader<T> where T : class
    {
        private readonly IFileSystem _fileSystem;
        private readonly ServiceCollection _services;

        public PluginLoader(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
            _services = new ServiceCollection();
            
            // Register the file system as a singleton
            _services.AddSingleton<IFileSystem>(fileSystem);
        }

        public void RegisterGlobalService<TService>(TService service) where TService : class
        {
            _services.AddSingleton<TService>(service);
        }

        public void RegisterGlobalService<TService, TImplementation>() 
            where TService : class 
            where TImplementation : class, TService
        {
            _services.AddTransient<TService, TImplementation>();
        }

        public IServiceProvider LoadPlugins(string pluginDirectory)
        {
            if (!_fileSystem.Directory.Exists(pluginDirectory))
            {
                throw new DirectoryNotFoundException($"Plugin directory not found: {pluginDirectory}");
            }

            var pluginDlls = _fileSystem.Directory.GetFiles(pluginDirectory, "*.dll");

            foreach (var file in pluginDlls)
            {
                try
                {
                    var loadCtx = new PluginLoadContext(file);
                    var assembly = loadCtx.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(file)));

                    // 1) Let the module register its own services
                    foreach (var modType in assembly.GetTypes().Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass))
                    {
                        var module = (IPlugin)Activator.CreateInstance(modType)!;
                        module.ConfigureServices(_services);
                    } 

                    // 2) Then register the actual plugin classes
                    foreach (var type in assembly.GetTypes())
                    {
                        if (typeof(T).IsAssignableFrom(type) && !type.IsAbstract && type.IsClass)
                            _services.AddTransient(typeof(T), type);
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception but continue loading other plugins
                    Console.WriteLine($"Error loading plugin {file}: {ex.Message}");
                }
            }

            return _services.BuildServiceProvider();
        }

        public IEnumerable<T> GetPlugins(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetServices<T>();
        }
    }

    public class PluginLoader : IPluginLoader
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IFileSystem _fileSystem;

        public PluginLoader(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public T LoadPlugin<T>(string name, string pluginDir, IFileSystem fileSystem) where T : class
        {
            if (!_fileSystem.Directory.Exists(pluginDir))
            {
                logger.Warn($"Plugin directory not found: {pluginDir}");
                return null;
            }

            var pluginDlls = _fileSystem.Directory.GetFiles(pluginDir, "*.dll");
            
            foreach (var file in pluginDlls)
            {
                try
                {
                    var loadCtx = new PluginLoadContext(file);
                    var assembly = loadCtx.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(file)));

                    // Find types that implement T
                    foreach (var type in assembly.GetTypes())
                    {
                        if (typeof(T).IsAssignableFrom(type) && !type.IsAbstract && type.IsClass)
                        {
                            // Check if this class has the right name
                            if (type.Name == name)
                            {
                                // Create an instance of the plugin
                                var constructors = type.GetConstructors();
                                foreach (var constructor in constructors)
                                {
                                    var parameters = constructor.GetParameters();
                                    // Look for a constructor that takes IFileSystem
                                    if (parameters.Length == 1 && parameters[0].ParameterType == typeof(IFileSystem))
                                    {
                                        logger.Debug($"Found plugin {name} in {file}");
                                        return (T)Activator.CreateInstance(type, fileSystem);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception but continue loading other plugins
                    logger.Warn($"Error loading plugin from {file}: {ex.Message}");
                }
            }

            return null;
        }
    }
} 