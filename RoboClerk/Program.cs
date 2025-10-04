using CommandLine;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using RoboClerk.AISystem;
using RoboClerk.Core.Configuration;
using RoboClerk.ContentCreators;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Tomlyn;

[assembly: AssemblyVersion("2.0.*")]
[assembly: InternalsVisibleTo("RoboClerk.Tests")]

namespace RoboClerk
{
    class Program
    {
        internal static Dictionary<string, string> GetConfigOptions(IEnumerable<string> commandlineOptions)
        {
            Dictionary<string, string> options = new Dictionary<string, string>();
            foreach (var commandlineOption in commandlineOptions)
            {
                if (commandlineOption != ",")
                {
                    var elements = commandlineOption.Split('=');
                    if (elements.Length != 2)
                    {                        
                        Console.WriteLine($"An error occurred parsing commandline option: {commandlineOption}. Expected syntax is <IDENTIFIER>=<VALUE>.");
                        throw new Exception("Error parsing commandline options.");
                    }
                    options[elements[0]] = elements[1];
                }
            }
            return options;
        }

        internal static void CleanOutputDirectory(IFileProviderPlugin fileSystem, string outputDir, ILogger logger)
        {
            logger.Info("Cleaning output directory.");
            string[] files = fileSystem.GetFiles(outputDir);
            foreach (string file in files)
            {
                if (!file.Contains(".gitignore"))
                {
                    fileSystem.DeleteFile(file);
                }
            }
        }

        internal static void RegisterContentCreators(IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            // Get the assembly containing the content creators            
            var currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            var otherAssemblyPath = Path.Combine(currentDir, "RoboClerk.Core.dll");
            var assembly = Assembly.LoadFrom(otherAssemblyPath);

            // Find all types that implement IContentCreator
            var contentCreatorTypes = assembly.GetTypes()
                .Where(t => typeof(IContentCreator).IsAssignableFrom(t) && 
                           !t.IsInterface && 
                           !t.IsAbstract &&
                           !t.IsGenericType)
                .ToList();

            var logger = NLog.LogManager.GetCurrentClassLogger();
            logger.Debug($"Found {contentCreatorTypes.Count} content creator types to register");

            foreach (var type in contentCreatorTypes)
            {
                try
                {
                    // Register each content creator as transient
                    services.AddTransient(type);
                    logger.Debug($"Registered content creator: {type.Name}");
                }
                catch (Exception ex)
                {
                    logger.Warn($"Failed to register content creator {type.Name}: {ex.Message}");
                }
            }
        }

        internal static void RegisterAIPlugin(IServiceCollection services, IConfiguration config, IPluginLoader pluginLoader)
        {
            // Load and register AI plugin if configured
            if (!string.IsNullOrEmpty(config.AIPlugin))
            {
                var aiPlugin = LoadAIPlugin(config, pluginLoader);
                if (aiPlugin != null)
                {
                    services.AddSingleton(aiPlugin);
                }
            }
        }

        internal static IAISystemPlugin LoadAIPlugin(IConfiguration config, IPluginLoader pluginLoader)
        {
            // Try loading plugins from each directory
            var logger = NLog.LogManager.GetCurrentClassLogger();
            foreach (var dir in config.PluginDirs)
            {
                IAISystemPlugin plugin = null;
                try
                {
                    plugin = pluginLoader.LoadByName<IAISystemPlugin>(
                        pluginDir: dir,
                        typeName: config.AIPlugin,
                        configureGlobals: sc =>
                        {
                            sc.AddSingleton(config);
                        });
                }
                catch (Exception ex)
                {
                    logger.Warn($"Error loading AI plugin from directory {dir}: {ex.Message}. Will try other directories.");
                }
                try
                { 
                    if (plugin is not null)
                    {
                        logger.Info($"Found AI plugin: {plugin.Name}");
                        plugin.InitializePlugin(config);
                        return plugin;
                    }
                }
                catch (Exception ex)
                {
                    logger.Warn($"Error initializing AI plugin from directory {dir}: {ex.Message}");
                    return null;
                }
            }
            logger.Warn($"Could not find AI plugin '{config.AIPlugin}' in any of the plugin directories.");
            return null;
        }

        /// <summary>
        /// Creates a SharePoint file provider plugin based on command line options.
        /// </summary>
        internal static IFileProviderPlugin CreateSharePointProvider(IConfiguration config, IPluginLoader pluginLoader, ILogger logger)
        {
            try
            {
                logger.Info("Initializing SharePoint file provider plugin");
                
                // Load SharePoint plugin from plugin directories
                var pluginsDir = config.PluginDirs;
                IFileProviderPlugin sharePointPlugin = null;
                foreach (var dir in pluginsDir)
                {

                    sharePointPlugin = pluginLoader.LoadByName<IFileProviderPlugin>(
                        pluginDir: dir,
                        typeName: "SharePointFileProviderPlugin",
                        configureGlobals: sc =>
                        {
                            sc.AddTransient<IFileSystem, FileSystem>();
                        });
                    if (sharePointPlugin != null)
                    {
                        sharePointPlugin.InitializePlugin(config);
                        break;
                    }
                }
                if (sharePointPlugin == null)
                {
                    throw new InvalidOperationException("SharePointFileProviderPlugin not found in plugins directory");
                }

                logger.Info($"Loaded SharePoint provider: {sharePointPlugin.Name}");
                return sharePointPlugin;
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to create SharePoint provider: {ex.Message}");
                throw new InvalidOperationException($"Failed to initialize SharePoint provider: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Creates the configuration with appropriate file providers.
        /// </summary>
        internal static IConfiguration CreateConfiguration(
            string roboClerkConfigFile,
            string projectConfigFile,
            Dictionary<string, string> commandLineOptions,
            IServiceProvider serviceProvider,
            ILogger logger)
        {
            var localFileProvider = new LocalFileSystemPlugin(new FileSystem());
            var projectFileProvider = serviceProvider.GetRequiredService<IFileProviderPlugin>();
            
            try
            {
                var finalConfig = RoboClerk.Configuration.Configuration.CreateBuilder()
                    .WithRoboClerkConfig(localFileProvider, roboClerkConfigFile, commandLineOptions)
                    .WithProjectConfig(projectFileProvider, projectConfigFile)
                    .Build();

                logger.Info("Configuration loaded successfully.");
                return finalConfig;
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to create configuration: {ex.Message}");
                throw new InvalidOperationException($"Configuration loading failed: {ex.Message}", ex);
            }
        }

        static int Main(string[] args)
        {
            Logging roboClerkLogger = new Logging();

            try
            {
                Parser.Default.ParseArguments<CommandlineOptions>(args)
                    .WithParsed<CommandlineOptions>(options =>
                   {
                       var assembly = Assembly.GetExecutingAssembly(); 
                       var projectConfigFile = $"{Path.GetDirectoryName(assembly.Location)}/RoboClerk_input/RoboClerkConfig/projectConfig.toml";
                       var roboClerkConfigFile = $"{Path.GetDirectoryName(assembly.Location)}/RoboClerk_input/RoboClerkConfig/RoboClerk.toml";
                       if (options.ConfigurationFile != null)
                       {
                           roboClerkConfigFile = options.ConfigurationFile;
                       }
                       if (options.ProjectConfigurationFile != null)
                       {
                           projectConfigFile = options.ProjectConfigurationFile;
                       }

                       var commandlineOptions = GetConfigOptions(options.ConfigurationOptions);
                       try
                       {
                           // build the roboclerk configuration first
                           var roboclerkConfig = RoboClerk.Configuration.Configuration.CreateBuilder()
                            .WithRoboClerkConfig(new LocalFileSystemPlugin(new FileSystem()), roboClerkConfigFile)
                            .Build();

                           try
                           {
                               roboClerkLogger.ConfigureLogging(roboclerkConfig.LogLevel);
                           }
                           catch (Exception e)
                           {
                               Console.WriteLine($"An error occurred configuring Roboclerk logging: \n{e.Message}");
                               throw;
                           }

                           var logger = NLog.LogManager.GetCurrentClassLogger();
                           logger.Warn($"RoboClerk Version: {Assembly.GetExecutingAssembly().GetName().Version}");

                           var serviceCollection = new ServiceCollection();
                           if (roboclerkConfig.FileProviderPlugin == "SharePointFileProviderPlugin")
                           {
                               serviceCollection.AddSingleton(x => CreateSharePointProvider(roboclerkConfig,new PluginLoader(new FileSystem(), new LocalFileSystemPlugin(new FileSystem())), logger));
                           }
                           else
                           {
                               serviceCollection.AddTransient<IFileProviderPlugin>(x => new LocalFileSystemPlugin(new FileSystem()));
                           }
                           serviceCollection.AddTransient<IFileSystem, FileSystem>();
                           serviceCollection.AddSingleton<IPluginLoader, PluginLoader>();
                           serviceCollection.AddSingleton<ITraceabilityAnalysis, TraceabilityAnalysis>();
                           serviceCollection.AddSingleton<IRoboClerkCore, RoboClerkTextCore>();

                           // Build service provider first so we can access services for configuration
                           var tempServiceProvider = serviceCollection.BuildServiceProvider();

                           // Create configuration using the new builder pattern
                           var configuration = CreateConfiguration(
                               roboClerkConfigFile, 
                               projectConfigFile, 
                               commandlineOptions, 
                               tempServiceProvider, 
                               logger);

                           // Register the configuration
                           serviceCollection.AddSingleton<IConfiguration>(configuration);

                           // Register all content creators
                           RegisterContentCreators(serviceCollection);

                           // Register the content creator factory with service provider injection
                           serviceCollection.AddSingleton<IContentCreatorFactory>(serviceProvider =>
                               new ContentCreatorFactory(serviceProvider, serviceProvider.GetRequiredService<ITraceabilityAnalysis>()));

                           if (configuration.CheckpointConfig.CheckpointFile == string.Empty) //check if we are not using a checkpoint
                           {
                               serviceCollection.AddSingleton<IDataSources, PluginDataSources>();
                           }
                           else
                           {
                               serviceCollection.AddSingleton<IDataSources>(x => new CheckpointDataSources(x.GetRequiredService<IConfiguration>(), x.GetRequiredService<IPluginLoader>(), x.GetRequiredService<IFileProviderPlugin>(), configuration.CheckpointConfig.CheckpointFile));
                           }

                           // Register AI plugin after service provider is built
                           RegisterAIPlugin(serviceCollection, configuration, tempServiceProvider.GetService<IPluginLoader>());

                           var serviceProvider = serviceCollection.BuildServiceProvider();

                           // Set log file destination now that we have configuration and file provider
                           roboClerkLogger.SetLogDestination(serviceProvider.GetService<IFileProviderPlugin>(), configuration.OutputDir);

                           //clean the output directory before we start working
                           if (configuration.ClearOutputDir)
                           {
                               CleanOutputDirectory(serviceProvider.GetService<IFileProviderPlugin>(),configuration.OutputDir, logger);
                           }

                           var core = serviceProvider.GetService<IRoboClerkCore>();
                           core.GenerateDocs();
                           core.SaveDocumentsToDisk();
                       }
                       catch (Exception e)
                       {
                           if (roboClerkLogger.Configured)
                           {
                               var logger = NLog.LogManager.GetCurrentClassLogger();
                               logger.Error("An unhandled exception has occurred. RoboClerk failed to complete:\n\n");
                               logger.Error(e);
                           }
                           else
                           {
                               Console.WriteLine($"An unhandled exception has occurred. RoboClerk failed to complete:\n\n{e.Message}");
                           }
                           throw;
                       }
                   });
            }
            catch
            {
                return 1;
            }
            finally
            {
                roboClerkLogger.WriteLogToFile();
            }
            return 0;
        }
    }
}
