using CommandLine;
using Microsoft.OpenApi.Models;
using NLog;
using NLog.Web;
using RoboClerk;
using RoboClerk.ContentCreators;
using RoboClerk.Core.FileProviders;
using RoboClerk.Server.Configuration;
using RoboClerk.Server.Services;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.IO.Abstractions;
using System.Reflection;
using IConfiguration = RoboClerk.Core.Configuration.IConfiguration;
using ServerCommandlineOptions = RoboClerk.Server.CommandlineOptions;


var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("Starting RoboClerk Server");

try
{
    // Load server configuration first
    var serverConfig = LoadServerConfiguration(args, logger);

    var builder = WebApplication.CreateBuilder(args);

    // Configure the server based on configuration
    ConfigureServer(builder, serverConfig);

    // NLog: Setup NLog for Dependency injection
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    
    // Configure Swagger based on settings
    ConfigureSwagger(builder, serverConfig);

    // Configure CORS based on settings
    ConfigureCors(builder, serverConfig);

    // Register server configuration for dependency injection FIRST
    // This must be done before RegisterRoboClerkServices because SharePointService depends on it
    builder.Services.AddSingleton(serverConfig);

    // Register RoboClerk dependencies
    RegisterRoboClerkServices(builder.Services, args);

    var app = builder.Build();

    // Configure the HTTP request pipeline
    ConfigurePipeline(app, serverConfig);

    // Configure server URLs
    var urls = BuildServerUrls(serverConfig);
    logger.Info($"RoboClerk Server starting on: {string.Join(", ", urls)}");
    
    app.Urls.Clear();
    foreach (var url in urls)
    {
        app.Urls.Add(url);
    }

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

static ServerConfiguration LoadServerConfiguration(string[] args, NLog.Logger logger)
{
    try
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyDir = Path.GetDirectoryName(assembly.Location) ?? "";
        var serverConfigFile = Path.Combine(assemblyDir, "RoboClerk.Server.toml");
        
        // Check for command line override
        Parser.Default.ParseArguments<ServerCommandlineOptions>(args)
            .WithParsed<ServerCommandlineOptions>(options =>
            {
                if (!string.IsNullOrEmpty(options.ServerConfigurationFile))
                {
                    serverConfigFile = options.ServerConfigurationFile;
                }
            });

        var commandlineOptions = GetConfigOptions(args);
        var configLoader = new ServerConfigurationLoader(new FileSystem());
        var config = configLoader.LoadConfiguration(serverConfigFile, commandlineOptions);
        
        logger.Info($"Server configuration loaded from: {serverConfigFile}");
        return config;
    }
    catch (Exception ex)
    {
        logger.Warn(ex, "Failed to load server configuration. Using defaults.");
        return new ServerConfiguration();
    }
}

static void ConfigureServer(WebApplicationBuilder builder, ServerConfiguration serverConfig)
{
    // Configure Kestrel server options
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Limits.MaxRequestBodySize = serverConfig.API.MaxRequestBodySize;
        
        // Configure timeouts
        options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(serverConfig.API.RequestTimeoutSeconds);
    });
    
    // Set environment
    builder.Environment.EnvironmentName = serverConfig.Server.Environment;
}

static void ConfigureSwagger(WebApplicationBuilder builder, ServerConfiguration serverConfig)
{
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "RoboClerk Server API", Version = "v1" });
        
        if (!string.IsNullOrEmpty(serverConfig.API.BasePath))
        {
            c.DocumentFilter<BasePathFilter>(serverConfig.API.BasePath);
        }
    });
}

static void ConfigureCors(WebApplicationBuilder builder, ServerConfiguration serverConfig)
{
    if (!serverConfig.CORS.EnableCORS) return;

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("WordAddInPolicy", corsBuilder =>
        {
            // Configure origins
            if (serverConfig.CORS.AllowedOrigins == "*")
            {
                corsBuilder.AllowAnyOrigin();
            }
            else
            {
                var origins = serverConfig.CORS.AllowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(o => o.Trim()).ToArray();
                corsBuilder.WithOrigins(origins);
            }

            // Configure methods
            if (serverConfig.CORS.AllowedMethods == "*")
            {
                corsBuilder.AllowAnyMethod();
            }
            else
            {
                var methods = serverConfig.CORS.AllowedMethods.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(m => m.Trim()).ToArray();
                corsBuilder.WithMethods(methods);
            }

            // Configure headers
            if (serverConfig.CORS.AllowedHeaders == "*")
            {
                corsBuilder.AllowAnyHeader();
            }
            else
            {
                var headers = serverConfig.CORS.AllowedHeaders.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(h => h.Trim()).ToArray();
                corsBuilder.WithHeaders(headers);
            }

            // Configure credentials
            if (serverConfig.CORS.AllowCredentials)
            {
                corsBuilder.AllowCredentials();
            }
        });
    });
}

static void ConfigurePipeline(WebApplication app, ServerConfiguration serverConfig)
{
    // Configure the HTTP request pipeline
    var isDevelopment = app.Environment.IsDevelopment();
    
    if (isDevelopment || serverConfig.API.EnableSwaggerInProduction)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "RoboClerk Server API V1");
            c.RoutePrefix = serverConfig.API.SwaggerRoutePrefix;
        });
    }

    if (serverConfig.Server.UseHttpsRedirection)
    {
        app.UseHttpsRedirection();
    }

    if (serverConfig.CORS.EnableCORS)
    {
        app.UseCors("WordAddInPolicy");
    }

    app.UseAuthorization();
    app.MapControllers();
}

static List<string> BuildServerUrls(ServerConfiguration serverConfig)
{
    var urls = new List<string>();
    var host = serverConfig.Server.HostAddress;
    
    urls.Add($"http://{host}:{serverConfig.Server.HttpPort}");
    
    if (serverConfig.Server.UseHttpsRedirection)
    {
        urls.Add($"https://{host}:{serverConfig.Server.HttpsPort}");
    }
    
    return urls;
}

static Dictionary<string, string> GetConfigOptions(IEnumerable<string> commandlineOptions)
{
    Dictionary<string, string> options = new Dictionary<string, string>();
    
    // Parse from ServerCommandlineOptions if available
    Parser.Default.ParseArguments<ServerCommandlineOptions>(commandlineOptions.ToArray())
        .WithParsed<ServerCommandlineOptions>(opts =>
        {
            foreach (var commandlineOption in opts.ConfigurationOptions)
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
        });
    
    return options;
}

static void RegisterRoboClerkServices(IServiceCollection services, string[] args)
{
    try
    {
        Parser.Default.ParseArguments<ServerCommandlineOptions>(args)
            .WithParsed<ServerCommandlineOptions>(options =>
            {
                var assembly = Assembly.GetExecutingAssembly();
                var roboClerkConfigFile = $"{Path.GetDirectoryName(assembly.Location)}/RoboClerk_input/RoboClerkConfig/RoboClerk.toml";
                if (options.ConfigurationFile != null)
                {
                    roboClerkConfigFile = options.ConfigurationFile;
                }

                var commandlineOptions = GetConfigOptionsForRoboClerk(options.ConfigurationOptions);
                
                // Get server configuration to inject SharePoint settings into RoboClerk configuration
                // Note: ServerConfiguration has already been registered as a singleton before this method is called
                var serviceProvider = services.BuildServiceProvider();
                var serverConfig = serviceProvider.GetRequiredService<ServerConfiguration>();
                
                // Add SharePoint ClientID and TenantID from server config to RoboClerk command-line options
                // These can be overridden by actual command-line options if provided
                if (!commandlineOptions.ContainsKey("SPClientId") && !string.IsNullOrEmpty(serverConfig.SharePoint.ClientId))
                {
                    commandlineOptions["SPClientId"] = serverConfig.SharePoint.ClientId;
                }
                if (!commandlineOptions.ContainsKey("SPTenantId") && !string.IsNullOrEmpty(serverConfig.SharePoint.TenantId))
                {
                    commandlineOptions["SPTenantId"] = serverConfig.SharePoint.TenantId;
                }
                
                try
                {
                    var roboclerkConfig = RoboClerk.Configuration.Configuration.CreateBuilder()
                            .WithRoboClerkConfig(new LocalFileSystemPlugin(new FileSystem()), roboClerkConfigFile, commandlineOptions)
                            .Build();
                    var logger = NLog.LogManager.GetCurrentClassLogger();
                    logger.Warn($"RoboClerk Version: {Assembly.GetExecutingAssembly().GetName().Version}");
                    logger.Info($"RoboClerk configuration loaded with SPClientID and SPTenantID from server configuration");

                    // Core RoboClerk services
                    services.AddTransient<IFileProviderPlugin>(x => new LocalFileSystemPlugin(new FileSystem()));
                    services.AddTransient<IFileSystem, FileSystem>();
                    services.AddSingleton<IPluginLoader, PluginLoader>();
                    services.AddSingleton<ITraceabilityAnalysis, TraceabilityAnalysis>();

                    // Register the content creator factory
                    services.AddSingleton<IContentCreatorFactory>(serviceProvider =>
                        new ContentCreatorFactory(serviceProvider, serviceProvider.GetRequiredService<ITraceabilityAnalysis>()));

                    // Register content creator metadata service (no dependencies needed!)
                    services.AddSingleton<IContentCreatorMetadataService, ContentCreatorMetadataService>();

                    // Register SharePoint service
                    services.AddSingleton<ISharePointService, SharePointService>();

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

static Dictionary<string, string> GetConfigOptionsForRoboClerk(IEnumerable<string> commandlineOptions)
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

// Helper class for Swagger base path filter
public class BasePathFilter : IDocumentFilter
{
    private readonly string basePath;

    public BasePathFilter(string basePath)
    {
        this.basePath = basePath;
    }

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Servers = new List<OpenApiServer>
        {
            new OpenApiServer { Url = basePath }
        };
    }
}

