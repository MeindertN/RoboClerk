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
            if (args.Length < 5)
            {
                Console.WriteLine("Usage: RoboClerk.Server.TestClient <ProjectPath> <SPDriveId> <ProjectRoot> <DocumentName> <SPSiteUrl> [ServerUrl] [StartupDelayMs]");
                Console.WriteLine();
                Console.WriteLine("Parameters:");
                Console.WriteLine("  ProjectPath    - SharePoint project path (e.g., /sites/project/Shared Documents/MyRoboClerkProject)");
                Console.WriteLine("  SPDriveId      - SharePoint Drive ID (obtained from Microsoft Graph API)");
                Console.WriteLine("  ProjectRoot    - Project root directory name (e.g., MyRoboClerkProject)");
                Console.WriteLine("  DocumentName   - Name of the document to test (e.g., SRS)");
                Console.WriteLine("  SPSiteUrl      - SharePoint site URL (e.g., https://company.sharepoint.com/sites/project)");
                Console.WriteLine("  ServerUrl      - Optional: RoboClerk Server URL (default: http://localhost:5000)");
                Console.WriteLine("  StartupDelayMs - Optional: Startup delay in milliseconds (default: 2000, use 0 to disable)");
                Console.WriteLine();
                Console.WriteLine("Example:");
                Console.WriteLine("  RoboClerk.Server.TestClient \"/sites/projects/Shared Documents/MyProject\" \"b!abc123...\" \"MyProject\" \"SRS\" \"https://mycompany.sharepoint.com/sites/projects\"");
                Console.WriteLine("  RoboClerk.Server.TestClient \"/sites/projects/Shared Documents/MyProject\" \"b!abc123...\" \"MyProject\" \"SRS\" \"https://mycompany.sharepoint.com/sites/projects\" \"http://localhost:5000\" \"5000\"");
                Console.WriteLine();
                Console.WriteLine("Note: This test client simulates the Word add-in workflow using real SharePoint parameters.");
                Console.WriteLine("      The SharePoint Drive ID must be obtained through the Microsoft Graph API.");
                Console.WriteLine("      The server must be properly configured with SharePoint access credentials.");
                Console.WriteLine("      When debugging, a longer startup delay (5000ms) is recommended to allow the server to fully start.");
                return 1;
            }

            var projectPath = args[0];
            var spDriveId = args[1]; 
            var projectRoot = args[2];
            var documentName = args[3];
            var spSiteUrl = args[4];
            var serverUrl = args.Length > 5 ? args[5] : "http://localhost:5000";
            
            // Parse startup delay parameter
            var startupDelayMs = 2000; // Default delay
            if (args.Length > 6)
            {
                if (int.TryParse(args[6], out var customDelay) && customDelay >= 0)
                {
                    startupDelayMs = customDelay;
                }
                else
                {
                    Console.WriteLine($"Warning: Invalid startup delay '{args[6]}'. Using default of {startupDelayMs}ms.");
                }
            }
            else if (System.Diagnostics.Debugger.IsAttached)
            {
                // Auto-increase delay when debugging
                startupDelayMs = 5000;
            }

            // Setup dependency injection and logging
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddHttpClient();
                    services.AddTransient<IRoboClerkServerClient, RoboClerkServerClient>();
                    services.AddTransient<IWordAddInSimulator, WordAddInSimulator>();
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
                logger.LogInformation("Project Path: {ProjectPath}", projectPath);
                logger.LogInformation("SharePoint Drive ID: {SPDriveId}", spDriveId);
                logger.LogInformation("Project Root: {ProjectRoot}", projectRoot);
                logger.LogInformation("Document Name: {DocumentName}", documentName);
                logger.LogInformation("SharePoint Site URL: {SPSiteUrl}", spSiteUrl);
                logger.LogInformation("Server URL: {ServerUrl}", serverUrl);
                logger.LogInformation("Startup Delay: {StartupDelayMs}ms", startupDelayMs);
                
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    logger.LogInformation("?? Debug mode detected");
                }
                
                logger.LogInformation("" +
                    "");

                // Apply startup delay if configured
                if (startupDelayMs > 0)
                {
                    logger.LogInformation("? Applying startup delay of {DelayMs}ms to allow server to be ready...", startupDelayMs);
                    await Task.Delay(startupDelayMs);
                }

                // Run the Word add-in simulation
                var success = await simulator.SimulateWordAddInWorkflowAsync(
                    serverUrl, 
                    projectPath,
                    spDriveId,
                    projectRoot,
                    spSiteUrl,
                    documentName);

                if (success)
                {
                    logger.LogInformation("?? Word add-in workflow simulation completed successfully!");
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