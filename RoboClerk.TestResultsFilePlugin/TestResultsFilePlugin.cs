using RoboClerk.Configuration;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Text.Json;
using Tomlyn.Model;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace RoboClerk.TestResultsFilePlugin
{
    public class TestResultsFilePlugin : DataSourcePluginBase
    {
        private List<string> fileLocations = new List<string>();

        public TestResultsFilePlugin(IFileProviderPlugin fileSystem)
            : base(fileSystem)
        {
            name = "TestResultsFilePlugin";
            description = "A plugin that retrieves the test results via one or more files.";
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            //this plugin does not need to register services
        }

        public override void Initialize(IConfiguration configuration)
        {
            logger.Info("Initializing the Test Results File Plugin");
            try
            {
                var config = GetConfigurationTable(configuration.PluginConfigDir, $"{name}.toml");
                foreach (var item in (TomlArray)config["FileLocations"])
                {
                    if (item == null)
                    {
                        logger.Warn($"In the Test Results File Plugin configuration file (\"{name}.toml\"), there is a null valued item in \"FileLocations\".");
                        continue;
                    }
                    fileLocations.Add((string)item);
                }
            }
            catch (Exception e)
            {
                logger.Error("Error reading configuration file for Test Results File plugin.");
                logger.Error(e);
                throw new Exception("The Test Results File plugin could not read its configuration. Aborting...");
            }
        }

        public override void RefreshItems()
        {
            logger.Info("Refreshing the test results from file.");
            testResults.Clear();
            for (int i = 0; i < fileLocations.Count; i++)
            {
                string json = fileProvider.ReadAllText(fileLocations[i]);
                try
                {
                    var fileTestResults = JsonSerializer.Deserialize<List<TestResultJSONObject>>(json);

                    foreach (var result in fileTestResults)
                    {
                        if (string.IsNullOrEmpty(result.ID))
                            throw new JsonException("The 'id' field is required.");

                        testResults.Add(new TestResult(result.ID,result.Type,result.Status,result.Name,result.Message,result.ExecutionTime ?? DateTime.MinValue));
                    }
                }
                catch (JsonException)
                {
                    logger.Error($"Error parsing the file with test results: {fileLocations[i]}");
                    throw;
                }
            }
        }
    }
}
