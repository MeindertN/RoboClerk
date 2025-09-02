using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using NLog.Web;
using RoboClerk;
using RoboClerk.Core.Configuration;
using RoboClerk.ContentCreators;
using RoboClerk.Core;
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
    RegisterRoboClerkServices(builder.Services);

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

static void RegisterRoboClerkServices(IServiceCollection services)
{
    // Core RoboClerk services
    services.AddTransient<IFileProviderPlugin>(x => new LocalFileSystemPlugin(new FileSystem()));
    services.AddTransient<IFileSystem, FileSystem>();
    services.AddSingleton<IPluginLoader, PluginLoader>();
    services.AddSingleton<ITraceabilityAnalysis, TraceabilityAnalysis>();
    
    // Register content creators
    RegisterContentCreators(services);
    
    // Register the content creator factory
    services.AddSingleton<IContentCreatorFactory>(serviceProvider =>
        new ContentCreatorFactory(serviceProvider, serviceProvider.GetRequiredService<ITraceabilityAnalysis>()));
    
    // Register project manager service
    services.AddSingleton<IProjectManager, ProjectManager>();
    
    // Register data sources factory
    services.AddTransient<IDataSourcesFactory, DataSourcesFactory>();
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
