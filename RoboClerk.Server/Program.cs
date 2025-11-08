using CommandLine;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using NLog.Web;
using RoboClerk;
using RoboClerk.AISystem;
using RoboClerk.ContentCreators;
using RoboClerk.Core;
using RoboClerk.Core.Configuration;
using RoboClerk.Server;
using RoboClerk.Server.Services;
using System.IO.Abstractions;
using System.Reflection;
using IConfiguration = RoboClerk.Core.Configuration.IConfiguration;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("Starting RoboClerk Server");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // NLog: Setup NLog for Dependency injection
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "RoboClerk Server API", Version = "v1" });
    });

    // Add CORS for Word add-in
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("WordAddInPolicy", builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
    });

    // Register RoboClerk dependencies
    RegisterRoboClerkServices(builder.Services,args);

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "RoboClerk Server API V1");
            c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
        });
    }

    app.UseHttpsRedirection();
    app.UseCors("WordAddInPolicy");
    app.UseAuthorization();
    app.MapControllers();

    logger.Info("RoboClerk Server starting...");
    app.Run();
}
catch (Exception exception)
{
    logger.Error(exception, "Stopped program because of exception");
    throw;
}
finally
{
    LogManager.Shutdown();
}

static Dictionary<string, string> GetConfigOptions(IEnumerable<string> commandlineOptions)
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

static void RegisterRoboClerkServices(IServiceCollection services, string[] args)
{
    try
    {
        Parser.Default.ParseArguments<CommandlineOptions>(args)
            .WithParsed<CommandlineOptions>(options =>
            {
                var assembly = Assembly.GetExecutingAssembly();
                var roboClerkConfigFile = $"{Path.GetDirectoryName(assembly.Location)}/RoboClerk_input/RoboClerkConfig/RoboClerk.toml";
                if (options.ConfigurationFile != null)
                {
                    roboClerkConfigFile = options.ConfigurationFile;
                }

                var commandlineOptions = GetConfigOptions(options.ConfigurationOptions);
                try
                {
                    var roboclerkConfig = RoboClerk.Configuration.Configuration.CreateBuilder()
                            .WithRoboClerkConfig(new LocalFileSystemPlugin(new FileSystem()), roboClerkConfigFile, commandlineOptions)
                            .Build();
                    var logger = NLog.LogManager.GetCurrentClassLogger();
                    logger.Warn($"RoboClerk Version: {Assembly.GetExecutingAssembly().GetName().Version}");

                    // Core RoboClerk services
                    services.AddTransient<IFileProviderPlugin>(x => new LocalFileSystemPlugin(new FileSystem()));
                    services.AddTransient<IFileSystem, FileSystem>();
                    services.AddSingleton<IPluginLoader, PluginLoader>();
                    services.AddSingleton<ITraceabilityAnalysis, TraceabilityAnalysis>();

                    // Register the content creator factory
                    services.AddSingleton<IContentCreatorFactory>(serviceProvider =>
                        new ContentCreatorFactory(serviceProvider, serviceProvider.GetRequiredService<ITraceabilityAnalysis>()));

                    // Register project manager service
                    services.AddSingleton<IProjectManager, ProjectManager>();

                    // Register data sources factory
                    services.AddTransient<IDataSourcesFactory, DataSourcesFactory>();

                    // Register configuration instance
                    services.AddSingleton<IConfiguration>(roboclerkConfig);
                }
                catch (Exception ex)
                {
                    var logger = LogManager.GetCurrentClassLogger();
                    logger.Error(ex, "Error initializing configuration");
                    throw;
                }
            });
    }
    catch (Exception ex)
    {
        var logger = LogManager.GetCurrentClassLogger();
        logger.Error(ex, "Error parsing command line options");
        throw;
    }    
}

static void RegisterContentCreators(IServiceCollection services)
{
    var currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
    var coreAssemblyPath = Path.Combine(currentDir, "RoboClerk.Core.dll");
    
    if (!File.Exists(coreAssemblyPath))
    {
        // Try alternative paths for different deployment scenarios
        coreAssemblyPath = Path.Combine(AppContext.BaseDirectory, "RoboClerk.Core.dll");
    }
    
    var assembly = Assembly.LoadFrom(coreAssemblyPath);

    var contentCreatorTypes = assembly.GetTypes()
        .Where(t => typeof(IContentCreator).IsAssignableFrom(t) && 
                   !t.IsInterface && 
                   !t.IsAbstract &&
                   !t.IsGenericType)
        .ToList();

    var logger = LogManager.GetCurrentClassLogger();
    logger.Debug($"Found {contentCreatorTypes.Count} content creator types to register");

    foreach (var type in contentCreatorTypes)
    {
        try
        {
            services.AddTransient(type);
            logger.Debug($"Registered content creator: {type.Name}");
        }
        catch (Exception ex)
        {
            logger.Warn($"Failed to register content creator {type.Name}: {ex.Message}");
        }
    }
}

static void RegisterAIPlugin(IServiceCollection services, IConfiguration config, IPluginLoader pluginLoader)
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

static IAISystemPlugin LoadAIPlugin(IConfiguration config, IPluginLoader pluginLoader)
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
