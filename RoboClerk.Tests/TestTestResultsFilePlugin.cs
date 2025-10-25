using NSubstitute;
using NUnit.Framework;
using RoboClerk.Configuration;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("These tests test the TestResultsFilePlugin")]
    internal class TestTestResultsFilePlugin
    {
        private IFileSystem fileSystem = null;
        private IConfiguration configuration = null;

        [SetUp]
        public void TestSetup()
        {
            string configFile = $@"
FileLocations = [""{TestingHelpers.ConvertFileName(@"c:/temp/system_results.json")}"", ""{TestingHelpers.ConvertFileName(@"c:/temp/unit_results.json")}""]
";
            
            string systemResultsJson = JsonSerializer.Serialize(new[]
            {
                new RoboClerk.TestResultsFilePlugin.TestResultJSONObject
                {
                    ID = "SYS-001",
                    Name = "System Login Test",
                    Type = TestType.SYSTEM,
                    Status = TestResultStatus.PASS,
                    Message = "Login successful",
                    ExecutionTime = new DateTime(2023, 10, 1, 12, 30, 0)
                },
                new RoboClerk.TestResultsFilePlugin.TestResultJSONObject
                {
                    ID = "SYS-002",
                    Name = "System Security Test",
                    Type = TestType.SYSTEM,
                    Status = TestResultStatus.FAIL,
                    Message = "Security vulnerability detected",
                    ExecutionTime = new DateTime(2023, 10, 1, 12, 35, 0)
                }
            }, new JsonSerializerOptions { WriteIndented = true });

            string unitResultsJson = JsonSerializer.Serialize(new[]
            {
                new RoboClerk.TestResultsFilePlugin.TestResultJSONObject
                {
                    ID = "UNIT-001",
                    Name = "Test Login Validation",
                    Type = TestType.UNIT,
                    Status = TestResultStatus.PASS,
                    Message = "Validation successful",
                    ExecutionTime = new DateTime(2023, 10, 1, 10, 15, 0)
                },
                new RoboClerk.TestResultsFilePlugin.TestResultJSONObject
                {
                    ID = "UNIT-002",
                    Name = "Test Password Encryption",
                    Type = TestType.UNIT,
                    Status = TestResultStatus.PASS,
                    Message = "Encryption working correctly"
                }
            }, new JsonSerializerOptions { WriteIndented = true });

            fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\test\TestResultsFilePlugin.toml"), new MockFileData(configFile) },
                { TestingHelpers.ConvertFileName(@"c:\temp\system_results.json"), new MockFileData(systemResultsJson) },
                { TestingHelpers.ConvertFileName(@"c:\temp\unit_results.json"), new MockFileData(unitResultsJson) }
            });

            configuration = Substitute.For<IConfiguration>();
            configuration.PluginConfigDir.Returns(TestingHelpers.ConvertFileName(@"c:/test/"));
        }

        [UnitTestAttribute(
            Purpose = "TestResultsFilePlugin is created successfully",
            Identifier = "A8F21C45-B3D7-4E2A-9F14-C7B8E9A0D1F2", 
            PostCondition = "No exception is thrown")]
        [Test]
        public void TestResultsFilePlugin_Creation()
        {
            var plugin = new RoboClerk.TestResultsFilePlugin.TestResultsFilePlugin(fileSystem);
            Assert.That(plugin, Is.Not.Null);
        }

        [UnitTestAttribute(
            Purpose = "TestResultsFilePlugin is initialized with valid configuration",
            Identifier = "B9E32D56-C4E8-4F3B-A025-D8C9F0B1E3F4",
            PostCondition = "Plugin initializes without exception")]
        [Test]
        public void TestResultsFilePlugin_Initialize()
        {
            var plugin = new RoboClerk.TestResultsFilePlugin.TestResultsFilePlugin(fileSystem);
            Assert.DoesNotThrow(() => plugin.InitializePlugin(configuration));
        }

        [UnitTestAttribute(
            Purpose = "TestResultsFilePlugin refreshes test results and extracts data correctly",
            Identifier = "C0F43E67-D5F9-4034-B136-E9DAF1C2E4F5",
            PostCondition = "Test results are loaded from files and accessible")]
        [Test]
        public void TestResultsFilePlugin_RefreshItems()
        {
            var plugin = new RoboClerk.TestResultsFilePlugin.TestResultsFilePlugin(fileSystem);
            plugin.InitializePlugin(configuration);
            plugin.RefreshItems();

            var results = plugin.GetTestResults().ToArray();
            Assert.That(results.Length, Is.EqualTo(4));
            
            // Verify system test results
            var systemResults = results.Where(r => r.ResultType == TestType.SYSTEM).ToArray();
            Assert.That(systemResults.Length, Is.EqualTo(2));
            Assert.That(systemResults[0].TestID, Is.EqualTo("SYS-001"));
            Assert.That(systemResults[0].ResultStatus, Is.EqualTo(TestResultStatus.PASS));
            Assert.That(systemResults[1].TestID, Is.EqualTo("SYS-002"));
            Assert.That(systemResults[1].ResultStatus, Is.EqualTo(TestResultStatus.FAIL));

            // Verify unit test results
            var unitResults = results.Where(r => r.ResultType == TestType.UNIT).ToArray();
            Assert.That(unitResults.Length, Is.EqualTo(2));
            Assert.That(unitResults[0].TestID, Is.EqualTo("UNIT-001"));
            Assert.That(unitResults[0].ResultStatus, Is.EqualTo(TestResultStatus.PASS));
            Assert.That(unitResults[1].TestID, Is.EqualTo("UNIT-002"));
            Assert.That(unitResults[1].ResultStatus, Is.EqualTo(TestResultStatus.PASS));
        }

        [UnitTestAttribute(
            Purpose = "TestResultsFilePlugin handles missing configuration file gracefully",
            Identifier = "D1054F78-E60A-4145-C247-F0EBF2D3F506",
            PostCondition = "Exception is thrown with appropriate error message")]
        [Test]
        public void TestResultsFilePlugin_MissingConfigFile()
        {
            var fileSystemWithoutConfig = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\temp\results.json"), new MockFileData("[]") }
            });

            var plugin = new RoboClerk.TestResultsFilePlugin.TestResultsFilePlugin(fileSystemWithoutConfig);
            
            var ex = Assert.Throws<Exception>(() => plugin.InitializePlugin(configuration));
            Assert.That(ex.Message.Contains("could not read its configuration"));
        }

        [UnitTestAttribute(
            Purpose = "TestResultsFilePlugin handles malformed JSON file",
            Identifier = "E2165089-F71B-4256-D358-01FCF3E4F617",
            PostCondition = "JsonException is thrown when JSON is invalid")]
        [Test]
        public void TestResultsFilePlugin_MalformedJson()
        {
            string malformedJson = @"{ ""invalid"": json }";
            
            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\test\TestResultsFilePlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFileName(@"c:\test\TestResultsFilePlugin.toml")) },
                { TestingHelpers.ConvertFileName(@"c:\temp\system_results.json"), new MockFileData(malformedJson) },
                { TestingHelpers.ConvertFileName(@"c:\temp\unit_results.json"), new MockFileData("[]") }
            });

            var plugin = new RoboClerk.TestResultsFilePlugin.TestResultsFilePlugin(testFileSystem);
            plugin.InitializePlugin(configuration);
            
            Assert.Throws<JsonException>(() => plugin.RefreshItems());
        }

        [UnitTestAttribute(
            Purpose = "TestResultsFilePlugin handles missing required fields",
            Identifier = "F3276190-082C-4367-E469-12FDF4F5F728",
            PostCondition = "JsonException is thrown when required ID field is missing")]
        [Test]
        public void TestResultsFilePlugin_MissingRequiredFields()
        {
            string jsonWithMissingId = JsonSerializer.Serialize(new[]
            {
                new 
                {
                    // Missing ID field
                    name = "Test without ID",
                    type = "UNIT",
                    status = "PASS"
                }
            }, new JsonSerializerOptions { WriteIndented = true });

            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\test\TestResultsFilePlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFileName(@"c:\test\TestResultsFilePlugin.toml")) },
                { TestingHelpers.ConvertFileName(@"c:\temp\system_results.json"), new MockFileData(jsonWithMissingId) },
                { TestingHelpers.ConvertFileName(@"c:\temp\unit_results.json"), new MockFileData("[]") }
            });

            var plugin = new RoboClerk.TestResultsFilePlugin.TestResultsFilePlugin(testFileSystem);
            plugin.InitializePlugin(configuration);
            
            var ex = Assert.Throws<JsonException>(() => plugin.RefreshItems());
            Assert.That(ex.Message.Contains("missing required properties, including the following: id"));
        }

        [UnitTestAttribute(
            Purpose = "TestResultsFilePlugin clears previous results on refresh",
            Identifier = "04387201-193D-4478-F570-23FEF5F6F839",
            PostCondition = "Previous test results are cleared before loading new ones")]
        [Test]
        public void TestResultsFilePlugin_ClearsPreviousResults()
        {
            var plugin = new RoboClerk.TestResultsFilePlugin.TestResultsFilePlugin(fileSystem);
            plugin.InitializePlugin(configuration);
            
            // First refresh
            plugin.RefreshItems();
            var firstResults = plugin.GetTestResults().ToArray();
            Assert.That(firstResults.Length, Is.EqualTo(4));
            
            // Modify files with different content
            string newJson = JsonSerializer.Serialize(new[]
            {
                new RoboClerk.TestResultsFilePlugin.TestResultJSONObject
                {
                    ID = "NEW-001",
                    Name = "New Test",
                    Type = TestType.UNIT,
                    Status = TestResultStatus.PASS
                }
            }, new JsonSerializerOptions { WriteIndented = true });

            var mockFileSystem = (MockFileSystem)fileSystem;
            mockFileSystem.RemoveFile(TestingHelpers.ConvertFileName(@"c:\temp\system_results.json"));
            mockFileSystem.AddFile(TestingHelpers.ConvertFileName(@"c:\temp\system_results.json"), new MockFileData(newJson));
            mockFileSystem.RemoveFile(TestingHelpers.ConvertFileName(@"c:\temp\unit_results.json"));
            mockFileSystem.AddFile(TestingHelpers.ConvertFileName(@"c:\temp\unit_results.json"), new MockFileData("[]"));
            
            // Second refresh
            plugin.RefreshItems();
            var secondResults = plugin.GetTestResults().ToArray();
            Assert.That(secondResults.Length, Is.EqualTo(1));
            Assert.That(secondResults[0].TestID, Is.EqualTo("NEW-001"));
        }

        [UnitTestAttribute(
            Purpose = "TestResultsFilePlugin handles empty JSON array",
            Identifier = "15498312-204E-4589-0681-34FFF6F7F940",
            PostCondition = "Empty file results in no test results")]
        [Test]
        public void TestResultsFilePlugin_EmptyJsonArray()
        {
            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\test\TestResultsFilePlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFileName(@"c:\test\TestResultsFilePlugin.toml")) },
                { TestingHelpers.ConvertFileName(@"c:\temp\system_results.json"), new MockFileData("[]") },
                { TestingHelpers.ConvertFileName(@"c:\temp\unit_results.json"), new MockFileData("[]") }
            });

            var plugin = new RoboClerk.TestResultsFilePlugin.TestResultsFilePlugin(testFileSystem);
            plugin.InitializePlugin(configuration);
            plugin.RefreshItems();

            var results = plugin.GetTestResults().ToArray();
            Assert.That(results.Length, Is.EqualTo(0));
        }

        [UnitTestAttribute(
            Purpose = "TestResultsFilePlugin handles optional fields correctly",
            Identifier = "37610534-426F-4701-2803-56FFF8F9F162",
            PostCondition = "Test results with missing optional fields are processed correctly")]
        [Test]
        public void TestResultsFilePlugin_OptionalFields()
        {
            string jsonWithOptionalFields = JsonSerializer.Serialize(new object[]
            {
                new 
                {
                    id = "OPTIONAL-001",
                    type = "UNIT",
                    status = "PASS"
                    // Missing name, message, executionTime
                },
                new 
                {
                    id = "OPTIONAL-002",
                    name = "Test with partial fields",
                    type = "SYSTEM",
                    status = "FAIL",
                    message = "Test failed"
                    // Missing executionTime
                }
            }, new JsonSerializerOptions { WriteIndented = true });

            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\test\TestResultsFilePlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFileName(@"c:\test\TestResultsFilePlugin.toml")) },
                { TestingHelpers.ConvertFileName(@"c:\temp\system_results.json"), new MockFileData(jsonWithOptionalFields) },
                { TestingHelpers.ConvertFileName(@"c:\temp\unit_results.json"), new MockFileData("[]") }
            });

            var plugin = new RoboClerk.TestResultsFilePlugin.TestResultsFilePlugin(testFileSystem);
            plugin.InitializePlugin(configuration);
            plugin.RefreshItems();

            var results = plugin.GetTestResults().ToArray();
            Assert.That(results.Length, Is.EqualTo(2));
            
            Assert.That(results[0].TestID, Is.EqualTo("OPTIONAL-001"));
            Assert.That(results[0].ResultType, Is.EqualTo(TestType.UNIT));
            Assert.That(results[0].ResultStatus, Is.EqualTo(TestResultStatus.PASS));
            Assert.That(results[0].ExecutionTime, Is.EqualTo(DateTime.MinValue));
            
            Assert.That(results[1].TestID, Is.EqualTo("OPTIONAL-002"));
            Assert.That(results[1].ResultType, Is.EqualTo(TestType.SYSTEM));
            Assert.That(results[1].ResultStatus, Is.EqualTo(TestResultStatus.FAIL));
            Assert.That(results[1].Message, Is.EqualTo("Test failed"));
        }

        [UnitTestAttribute(
            Purpose = "TestResultsFilePlugin processes multiple files correctly",
            Identifier = "48721645-537F-4812-3914-67FFF9F0F273",
            PostCondition = "Results from all configured files are combined")]
        [Test]
        public void TestResultsFilePlugin_MultipleFiles()
        {
            var plugin = new RoboClerk.TestResultsFilePlugin.TestResultsFilePlugin(fileSystem);
            plugin.InitializePlugin(configuration);
            plugin.RefreshItems();

            var results = plugin.GetTestResults().ToArray();
            
            // Should have results from both files
            Assert.That(results.Length, Is.EqualTo(4));
            
            // Check that we have results from both system and unit test files
            var systemResults = results.Where(r => r.TestID.StartsWith("SYS-")).ToArray();
            var unitResults = results.Where(r => r.TestID.StartsWith("UNIT-")).ToArray();
            
            Assert.That(systemResults.Length, Is.EqualTo(2));
            Assert.That(unitResults.Length, Is.EqualTo(2));
        }

        [UnitTestAttribute(
            Purpose = "TestResultsFilePlugin validates JSON structure against expected schema",
            Identifier = "59832756-648F-4923-4025-78FFF0F1F384",
            PostCondition = "Invalid JSON structures are rejected appropriately")]
        [Test]
        public void TestResultsFilePlugin_InvalidJsonStructure()
        {
            string invalidStructureJson = JsonSerializer.Serialize(new
            {
                // Wrong structure - object instead of array
                result = new
                {
                    id = "INVALID-001",
                    type = "UNIT",
                    status = "PASS"
                }
            }, new JsonSerializerOptions { WriteIndented = true });

            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\test\TestResultsFilePlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFileName(@"c:\test\TestResultsFilePlugin.toml")) },
                { TestingHelpers.ConvertFileName(@"c:\temp\system_results.json"), new MockFileData(invalidStructureJson) },
                { TestingHelpers.ConvertFileName(@"c:\temp\unit_results.json"), new MockFileData("[]") }
            });

            var plugin = new RoboClerk.TestResultsFilePlugin.TestResultsFilePlugin(testFileSystem);
            plugin.InitializePlugin(configuration);
            
            Assert.Throws<JsonException>(() => plugin.RefreshItems());
        }

        [UnitTestAttribute(
            Purpose = "TestResultsFilePlugin handles optional project field correctly",
            Identifier = "e0c7b549-ebae-4e18-8cb0-aef7a8712f38",
            PostCondition = "Project field is properly set on test results when provided")]
        [Test]
        public void TestResultsFilePlugin_ProjectField()
        {
            string jsonWithProject = JsonSerializer.Serialize(new[]
            {
                new RoboClerk.TestResultsFilePlugin.TestResultJSONObject
                {
                    ID = "SYS-PROJ-001",
                    Name = "System test with project",
                    Type = TestType.SYSTEM,
                    Status = TestResultStatus.PASS,
                    Message = "Test passed",
                    Project = "ProjectAlpha"
                },
                new RoboClerk.TestResultsFilePlugin.TestResultJSONObject
                {
                    ID = "UNIT-PROJ-001",
                    Name = "Unit test with project", 
                    Type = TestType.UNIT,
                    Status = TestResultStatus.FAIL,
                    Message = "Test failed",
                    Project = "ProjectBeta"
                },
                new RoboClerk.TestResultsFilePlugin.TestResultJSONObject
                {
                    ID = "SYS-NO-PROJ-001",
                    Name = "System test without project",
                    Type = TestType.SYSTEM,
                    Status = TestResultStatus.PASS
                    // No project field
                }
            }, new JsonSerializerOptions { WriteIndented = true });

            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\test\TestResultsFilePlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFileName(@"c:\test\TestResultsFilePlugin.toml")) },
                { TestingHelpers.ConvertFileName(@"c:\temp\system_results.json"), new MockFileData(jsonWithProject) },
                { TestingHelpers.ConvertFileName(@"c:\temp\unit_results.json"), new MockFileData("[]") }
            });

            var plugin = new RoboClerk.TestResultsFilePlugin.TestResultsFilePlugin(testFileSystem);
            plugin.InitializePlugin(configuration);
            plugin.RefreshItems();

            var results = plugin.GetTestResults().ToArray();
            Assert.That(results.Length, Is.EqualTo(3));
            
            // Check result with ProjectAlpha
            var resultWithProjectAlpha = results.FirstOrDefault(r => r.TestID == "SYS-PROJ-001");
            Assert.That(resultWithProjectAlpha, Is.Not.Null);
            Assert.That(resultWithProjectAlpha.ItemProject, Is.EqualTo("ProjectAlpha"));
            
            // Check result with ProjectBeta
            var resultWithProjectBeta = results.FirstOrDefault(r => r.TestID == "UNIT-PROJ-001");
            Assert.That(resultWithProjectBeta, Is.Not.Null);
            Assert.That(resultWithProjectBeta.ItemProject, Is.EqualTo("ProjectBeta"));
            
            // Check result without project (should have empty string)
            var resultWithoutProject = results.FirstOrDefault(r => r.TestID == "SYS-NO-PROJ-001");
            Assert.That(resultWithoutProject, Is.Not.Null);
            Assert.That(resultWithoutProject.ItemProject, Is.EqualTo(string.Empty));
        }

        [UnitTestAttribute(
            Purpose = "TestResultsFilePlugin handles null project field gracefully",
            Identifier = "38c30256-bebf-4d3d-8a84-f7111629438a",
            PostCondition = "Null project field is converted to empty string")]
        [Test]
        public void TestResultsFilePlugin_NullProjectField()
        {
            // Create test data with explicit null project
            var testData = new[]
            {
                new Dictionary<string, object>
                {
                    ["id"] = "NULL-PROJ-001",
                    ["name"] = "Test with null project",
                    ["type"] = "UNIT",
                    ["status"] = "PASS",
                    ["project"] = (string)null
                }
            };

            string jsonWithNullProject = JsonSerializer.Serialize(testData, new JsonSerializerOptions { WriteIndented = true });

            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\test\TestResultsFilePlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFileName(@"c:\test\TestResultsFilePlugin.toml")) },
                { TestingHelpers.ConvertFileName(@"c:\temp\system_results.json"), new MockFileData(jsonWithNullProject) },
                { TestingHelpers.ConvertFileName(@"c:\temp\unit_results.json"), new MockFileData("[]") }
            });

            var plugin = new RoboClerk.TestResultsFilePlugin.TestResultsFilePlugin(testFileSystem);
            plugin.InitializePlugin(configuration);
            plugin.RefreshItems();

            var results = plugin.GetTestResults().ToArray();
            Assert.That(results.Length, Is.EqualTo(1));
            
            var resultWithNullProject = results[0];
            Assert.That(resultWithNullProject.TestID, Is.EqualTo("NULL-PROJ-001"));
            Assert.That(resultWithNullProject.ItemProject, Is.EqualTo(string.Empty));
        }

        [UnitTestAttribute(
            Purpose = "TestResultsFilePlugin maintains backward compatibility when project field is absent",
            Identifier = "aefe435e-b112-4f08-bdfb-da8413ae9dbd",
            PostCondition = "Test results without project field are processed normally with empty project")]
        [Test]
        public void TestResultsFilePlugin_BackwardCompatibility()
        {
            // Use original test setup which doesn't include project field
            var plugin = new RoboClerk.TestResultsFilePlugin.TestResultsFilePlugin(fileSystem);
            plugin.InitializePlugin(configuration);
            plugin.RefreshItems();

            var results = plugin.GetTestResults().ToArray();
            
            // All results should have empty project field
            Assert.That(results.All(r => r.ItemProject == string.Empty), Is.True);
            
            // But other functionality should work normally
            Assert.That(results.Length, Is.EqualTo(4));
            Assert.That(results.Any(r => r.TestID == "SYS-001"), Is.True);
            Assert.That(results.Any(r => r.TestID == "UNIT-001"), Is.True);
        }

        [UnitTestAttribute(
            Purpose = "TestResultsFilePlugin handles empty string project field",
            Identifier = "f6fd801a-2bc2-4248-b4d2-6ea323bc8606",
            PostCondition = "Empty string project field is preserved as empty string")]
        [Test]
        public void TestResultsFilePlugin_EmptyStringProjectField()
        {
            string jsonWithEmptyProject = JsonSerializer.Serialize(new[]
            {
                new RoboClerk.TestResultsFilePlugin.TestResultJSONObject
                {
                    ID = "EMPTY-PROJ-001",
                    Name = "Test with empty project",
                    Type = TestType.UNIT,
                    Status = TestResultStatus.PASS,
                    Project = ""
                }
            }, new JsonSerializerOptions { WriteIndented = true });

            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\test\TestResultsFilePlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFileName(@"c:\test\TestResultsFilePlugin.toml")) },
                { TestingHelpers.ConvertFileName(@"c:\temp\system_results.json"), new MockFileData(jsonWithEmptyProject) },
                { TestingHelpers.ConvertFileName(@"c:\temp\unit_results.json"), new MockFileData("[]") }
            });

            var plugin = new RoboClerk.TestResultsFilePlugin.TestResultsFilePlugin(testFileSystem);
            plugin.InitializePlugin(configuration);
            plugin.RefreshItems();

            var results = plugin.GetTestResults().ToArray();
            Assert.That(results.Length, Is.EqualTo(1));
            
            var resultWithEmptyProject = results[0];
            Assert.That(resultWithEmptyProject.TestID, Is.EqualTo("EMPTY-PROJ-001"));
            Assert.That(resultWithEmptyProject.ItemProject, Is.EqualTo(string.Empty));
        }
    }
}