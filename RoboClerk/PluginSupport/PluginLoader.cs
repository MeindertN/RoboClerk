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

    public class PluginLoader : IPluginLoader
    {
        private readonly IFileSystem _fileSystem;
        private readonly PluginAssemblyLoader _assemblyLoader;

        public PluginLoader(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
            _assemblyLoader = new PluginAssemblyLoader(fileSystem);
        }

        // -------------------------
        // PUBLIC: load *all* plugins
        // -------------------------
        public IServiceProvider LoadAll<TPluginInterface>(
            string pluginDir,
            Action<IServiceCollection>? configureGlobals = null
        ) where TPluginInterface : class, IPlugin
        {
            var (services, _) = BuildContainer<TPluginInterface>(pluginDir, configureGlobals);
            return services.BuildServiceProvider();
        }

        // ----------------------------------------
        // PUBLIC: load one plugin by its class-name
        // ----------------------------------------
        public TPluginInterface? LoadByName<TPluginInterface>(
            string pluginDir,
            string typeName,
            Action<IServiceCollection>? configureGlobals = null
        ) where TPluginInterface : class, IPlugin
        {
            // 1) Build the container and capture the list of discovered impl types:
            var (services, implTypes) = BuildContainer<TPluginInterface>(pluginDir, configureGlobals);
            var provider = services.BuildServiceProvider();

            // 2) Find the one whose class name matches
            var match = implTypes
                .FirstOrDefault(t => t.Name.Equals(typeName, StringComparison.Ordinal));
            if (match is null)
                return null;

            // 3) Resolve via DI (honors ctor injection, modules� registrations, etc.)
            return provider.GetService(match) as TPluginInterface;
        }

        // -------------------------------------------------
        // INTERNAL: assemble IServiceCollection + impl types
        // -------------------------------------------------
        private (IServiceCollection services, List<Type> implTypes) BuildContainer<TPluginInterface>
            (string pluginDir,Action<IServiceCollection>? configureGlobals) where TPluginInterface : class, IPlugin
        {
            if (!_fileSystem.Directory.Exists(pluginDir))
                throw new DirectoryNotFoundException($"Plugin directory not found: {pluginDir}");

            var services = new ServiceCollection();
            var implTypes = new List<Type>();

            // 1) globals
            services.AddSingleton<IFileSystem>(_fileSystem);
            configureGlobals?.Invoke(services);

            // 2) per‐assembly scan
            foreach (var asm in _assemblyLoader.LoadFromDirectory(pluginDir))
            {
                var pluginTypes = asm
                    .GetTypes()
                    .Where(t => typeof(TPluginInterface).IsAssignableFrom(t)
                             && !t.IsAbstract
                             && t.GetConstructor(new[] { typeof(IFileSystem) }) != null);

                foreach (var type in pluginTypes)
                {
                    implTypes.Add(type);

                    // 3) find the single‐arg ctor
                    var ctor = type.GetConstructor(new[] { typeof(IFileSystem) })!;
                    // 4) invoke it to get the PluginBase/IPluginRegistrar
                    var metadataInstance = (TPluginInterface)ctor.Invoke(new object[] { _fileSystem });

                    // 5) let the plugin register everything it needs,
                    //    including services.AddTransient<IPlugin, ThisType>()
                    metadataInstance.ConfigureServices(services);
                }
            }

            return (services, implTypes);
        }
    }

    // --- helper for loading raw assemblies ---
    public class PluginAssemblyLoader
    {
        private readonly IFileSystem _fs;

        public PluginAssemblyLoader(IFileSystem fs) => _fs = fs;

        public IEnumerable<Assembly> LoadFromDirectory(string pluginDir)
        {
            var dlls = _fs.Directory.GetFiles(pluginDir, "RoboClerk.*.dll");
            foreach (var dll in dlls)
            {
                var ctx = new PluginLoadContext(dll);
                var asmName = new AssemblyName(_fs.Path.GetFileNameWithoutExtension(dll));
                Assembly? asm = null;
                try
                {
                    asm = ctx.LoadFromAssemblyName(asmName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Skipping plugin {dll}: {ex.Message}");
                }
                if (asm != null)
                    yield return asm;
            }
        }
    }
}