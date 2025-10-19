using RoboClerk.Configuration;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Text.Json;
using Tomlyn.Model;
using System;

namespace RoboClerk.TestDescriptionFilePlugin
{
    public class TestDescriptionFilePlugin : DataSourcePluginBase
    {
        private List<string> fileLocations = new List<string>();

        public TestDescriptionFilePlugin(IFileSystem fileSystem)
            : base(fileSystem)
        {
            SetBaseParam();
        }

        private void SetBaseParam()
        {
            name = "TestDescriptionFilePlugin";
            description = "A plugin that retrieves test descriptions (system/unit) via one or more files.";
        }

        public override void InitializePlugin(IConfiguration configuration)
        {
            logger.Info("Initializing the Test Description File Plugin");
            try
            {
                var config = GetConfigurationTable(configuration.PluginConfigDir, $"{name}.toml");
                foreach (var item in (TomlArray)config["FileLocations"])
                {
                    if (item == null)
                    {
                        logger.Warn($"In the Test Description File Plugin configuration file (\"{name}.toml\"), there is a null valued item in \"FileLocations\".");
                        continue;
                    }
                    fileLocations.Add((string)item);
                }
            }
            catch (Exception e)
            {
                logger.Error("Error reading configuration file for Test Description File plugin.");
                logger.Error(e);
                throw new Exception("The Test Description File plugin could not read its configuration. Aborting...");
            }
        }

        public override void RefreshItems()
        {
            logger.Info("Refreshing the test descriptions from file.");
            
            for (int i = 0; i < fileLocations.Count; i++)
            {
                string json = fileSystem.File.ReadAllText(fileLocations[i]);
                try
                {
                    var fileTestDescriptions = JsonSerializer.Deserialize<List<TestDescriptionJSONObject>>(json);

                    foreach (var description in fileTestDescriptions)
                    {
                        ValidateTestDescription(description, fileLocations[i]);

                        if (description.Type == TestType.UNIT)
                        {
                            var test = new UnitTestItem();
                            test.ItemID = description.ID;
                            test.UnitTestFunctionName = description.Name;
                            test.UnitTestPurpose = description.Purpose;
                            test.UnitTestAcceptanceCriteria = description.Acceptance;   
                            test.UnitTestFileName = description.Filename;
                            foreach (var trace in description.Trace)
                            {
                                test.AddLinkedItem(new ItemLink(trace,ItemLinkType.Tests));
                            }
                            unitTests.Add(test);
                        }
                        if (description.Type == TestType.SYSTEM)
                        {
                            var test = new SoftwareSystemTestItem();
                            test.ItemID = description.ID;
                            var testDescription = description.Description.ToUpper();
                            if (testDescription.Contains("GIVEN:") && testDescription.Contains("WHEN:") && testDescription.Contains("THEN:"))
                            {
                                var steps = SoftwareSystemTestItem.GetTestSteps(description.Description);
                                foreach (var step in steps)
                                {
                                    test.AddTestCaseStep(step);
                                }
                                test.TestCaseDescription = description.Name;
                            }
                            else
                            {
                                test.TestCaseDescription = description.Description;
                            }                                
                            test.TestCaseAutomated = true;
                            foreach (var trace in description.Trace)
                            {
                                test.AddLinkedItem(new ItemLink(trace, ItemLinkType.Tests));
                            }
                            testCases.Add(test);
                        }
                    }
                }
                catch (JsonException ex)
                {
                    logger.Error($"Error parsing or validating the test description file: {fileLocations[i]}");
                    logger.Error($"Validation error: {ex.Message}");
                    throw;
                }
                catch (Exception ex)
                {
                    logger.Error($"Unexpected error processing test description file: {fileLocations[i]}");
                    logger.Error(ex);
                    throw;
                }
            }
        }

        private void ValidateTestDescription(TestDescriptionJSONObject description, string fileName)
        {
            // Basic validation
            if (string.IsNullOrEmpty(description.ID))
                throw new JsonException("The 'id' field is required for all test descriptions.");
            
            if (string.IsNullOrEmpty(description.Name))
                throw new JsonException($"The 'name' field is required for all test descriptions. Test ID: {description.ID}");
            
            if (string.IsNullOrEmpty(description.Description))
                throw new JsonException($"The 'description' field is required for all test descriptions. Test ID: {description.ID}");
            
            if (description.Trace == null || description.Trace.Count == 0)
                throw new JsonException($"The 'trace' field is required and must contain at least one item. Test ID: {description.ID}");

            // Conditional validation based on test type
            if (description.Type == TestType.UNIT)
            {
                if (string.IsNullOrEmpty(description.Purpose))
                    throw new JsonException($"The 'purpose' field is required for UNIT test descriptions. Test ID: {description.ID}, Name: {description.Name}");
                
                if (string.IsNullOrEmpty(description.Acceptance))
                    throw new JsonException($"The 'acceptance' field is required for UNIT test descriptions. Test ID: {description.ID}, Name: {description.Name}");
            }
        }
    }
}
