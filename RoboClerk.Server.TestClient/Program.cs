using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RoboClerk.Server.TestClient.Services;

namespace RoboClerk.Server.TestClient
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            // Parse command line arguments
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: RoboClerk.Server.TestClient <SharePointProjectUrl> <DocumentName> [ServerUrl]");
                Console.WriteLine();
                Console.WriteLine("Parameters:");
                Console.WriteLine("  SharePointProjectUrl - URL to SharePoint project (e.g., https://company.sharepoint.com/sites/project/MyRoboClerkProject)");
                Console.WriteLine("  DocumentName         - Name of the document to test (e.g., SRS)");
                Console.WriteLine("  ServerUrl           - Optional: RoboClerk Server URL (default: http://localhost:5000)");
                Console.WriteLine();
                Console.WriteLine("Example:");
                Console.WriteLine("  RoboClerk.Server.TestClient \"https://mycompany.sharepoint.com/sites/projects/MyProject\" \"SRS\"");
                return 1;
            }

            var sharePointProjectUrl = args[0];
            var documentName = args[1];
            var serverUrl = args.Length > 2 ? args[2] : "http://localhost:5000";

            // Setup dependency injection and logging
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddHttpClient();
                    services.AddSingleton<IRoboClerkServerClient, RoboClerkServerClient>();
                    services.AddSingleton<IWordAddInSimulator, WordAddInSimulator>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Information);
                })
                .Build();

            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            var simulator = host.Services.GetRequiredService<IWordAddInSimulator>();

            try
            {
                logger.LogInformation("=== RoboClerk Server Test Client ===");
                logger.LogInformation("SharePoint Project: {ProjectUrl}", sharePointProjectUrl);
                logger.LogInformation("Document Name: {DocumentName}", documentName);
                logger.LogInformation("Server URL: {ServerUrl}", serverUrl);
                logger.LogInformation("");

                // Run the Word add-in simulation
                var success = await simulator.SimulateWordAddInWorkflowAsync(
                    serverUrl, 
                    sharePointProjectUrl, 
                    documentName);

                if (success)
                {
                    logger.LogInformation("? Word add-in workflow simulation completed successfully!");
                    return 0;
                }
                else
                {
                    logger.LogError("? Word add-in workflow simulation failed!");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "?? Unhandled exception occurred during simulation");
                return 1;
            }
        }
    }
}