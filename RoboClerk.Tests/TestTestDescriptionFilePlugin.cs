using NSubstitute;
using NUnit.Framework;
using RoboClerk.Core.Configuration;
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
    [Description("These tests test the TestDescriptionFilePlugin")]
    internal class TestTestDescriptionFilePlugin
    {
        private IFileSystem fileSystem = null;
        private IConfiguration configuration = null;

        [SetUp]
        public void TestSetup()
        {
            string configFile = @$"
FileLocations = [""{TestingHelpers.ConvertFilePath(@"c:/temp/system_descriptions.json")}"", ""{TestingHelpers.ConvertFilePath(@"c:/temp/unit_descriptions.json")}""]
";
            
            string systemDescriptionsJson = JsonSerializer.Serialize(new[]
            {
                new RoboClerk.TestDescriptionFilePlugin.TestDescriptionJSONObject
                {
                    ID = "SYS-DESC-001",
                    Name = "User Authentication System Test",
                    Type = TestType.SYSTEM,
                    Description = "GIVEN: A user with valid credentials\nWHEN: The user attempts to log into the system\nTHEN: The user is successfully authenticated and redirected to the dashboard",
                    Trace = new List<string> { "REQ-001", "REQ-002" },
                    Filename = "AuthenticationTests.feature"
                },
                new RoboClerk.TestDescriptionFilePlugin.TestDescriptionJSONObject
                {
                    ID = "SYS-DESC-002",
                    Name = "Password Security System Test",
                    Type = TestType.SYSTEM,
                    Description = "Test system-level password security and encryption",
                    Trace = new List<string> { "REQ-003" }
                }
            }, new JsonSerializerOptions { WriteIndented = true });

            string unitDescriptionsJson = JsonSerializer.Serialize(new[]
            {
                new RoboClerk.TestDescriptionFilePlugin.TestDescriptionJSONObject
                {
                    ID = "UNIT-DESC-001",
                    Name = "testValidateUserCredentials",
                    Type = TestType.UNIT,
                    Description = "Unit test to verify that user credentials are properly validated against the authentication system",
                    Trace = new List<string> { "REQ-001" },
                    Filename = "AuthenticationTests.cs",
                    Purpose = "Verify that the validateCredentials method correctly identifies valid and invalid user credentials",
                    Acceptance = "Method returns true for valid credentials and false for invalid credentials, with appropriate logging"
                },
                new RoboClerk.TestDescriptionFilePlugin.TestDescriptionJSONObject
                {
                    ID = "UNIT-DESC-002",
                    Name = "testPasswordEncryption",
                    Type = TestType.UNIT,
                    Description = "Unit test to ensure passwords are encrypted using the correct algorithm and salt",
                    Trace = new List<string> { "REQ-003", "REQ-004" },
                    Filename = "SecurityTests.cs",
                    Purpose = "Validate that password encryption follows security standards and produces consistent results",
                    Acceptance = "Encrypted password matches expected hash format and can be verified against original password"
                }
            }, new JsonSerializerOptions { WriteIndented = true });

            fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFilePath(@"c:\test\TestDescriptionFilePlugin.toml"), new MockFileData(configFile) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\system_descriptions.json"), new MockFileData(systemDescriptionsJson) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\unit_descriptions.json"), new MockFileData(unitDescriptionsJson) }
            });

            configuration = Substitute.For<IConfiguration>();
            configuration.PluginConfigDir.Returns(TestingHelpers.ConvertFilePath(@"c:/test/"));
        }

        [UnitTestAttribute(
            Purpose = "TestDescriptionFilePlugin is created successfully",
            Identifier = "A1B2C3D4-E5F6-4789-A012-B3C4D5E6F789",
            PostCondition = "No exception is thrown")]
        [Test]
        public void TestDescriptionFilePlugin_Creation()
        {
            var plugin = new RoboClerk.TestDescriptionFilePlugin.TestDescriptionFilePlugin(new LocalFileSystemPlugin(fileSystem));
            Assert.That(plugin, Is.Not.Null);
        }

        [UnitTestAttribute(
            Purpose = "TestDescriptionFilePlugin is initialized with valid configuration",
            Identifier = "B2C3D4E5-F6A7-4890-B123-C4D5E6F7A890",
            PostCondition = "Plugin initializes without exception")]
        [Test]
        public void TestDescriptionFilePlugin_Initialize()
        {
            var plugin = new RoboClerk.TestDescriptionFilePlugin.TestDescriptionFilePlugin(new LocalFileSystemPlugin(fileSystem));
            Assert.DoesNotThrow(() => plugin.InitializePlugin(configuration));
        }

        [UnitTestAttribute(
            Purpose = "TestDescriptionFilePlugin refreshes test descriptions and extracts data correctly",
            Identifier = "C3D4E5F6-A7B8-4901-C234-D5E6F7A8B901",
            PostCondition = "Test descriptions are loaded from files and accessible as unit tests and system tests")]
        [Test]
        public void TestDescriptionFilePlugin_RefreshItems()
        {
            var plugin = new RoboClerk.TestDescriptionFilePlugin.TestDescriptionFilePlugin(new LocalFileSystemPlugin(fileSystem));
            plugin.InitializePlugin(configuration);
            plugin.RefreshItems();

            var unitTests = plugin.GetUnitTests().ToArray();
            var systemTests = plugin.GetSoftwareSystemTests().ToArray();
            
            // Verify unit tests
            Assert.That(unitTests.Length, Is.EqualTo(2));
            
            var unitTest1 = unitTests.FirstOrDefault(ut => ut.ItemID == "UNIT-DESC-001");
            Assert.That(unitTest1, Is.Not.Null);
            Assert.That(unitTest1.UnitTestFunctionName, Is.EqualTo("testValidateUserCredentials"));
            Assert.That(unitTest1.UnitTestPurpose, Is.EqualTo("Verify that the validateCredentials method correctly identifies valid and invalid user credentials"));
            Assert.That(unitTest1.UnitTestAcceptanceCriteria, Is.EqualTo("Method returns true for valid credentials and false for invalid credentials, with appropriate logging"));
            Assert.That(unitTest1.UnitTestFileName, Is.EqualTo("AuthenticationTests.cs"));
            
            var unitTest2 = unitTests.FirstOrDefault(ut => ut.ItemID == "UNIT-DESC-002");
            Assert.That(unitTest2, Is.Not.Null);
            Assert.That(unitTest2.UnitTestFunctionName, Is.EqualTo("testPasswordEncryption"));
            Assert.That(unitTest2.UnitTestFileName, Is.EqualTo("SecurityTests.cs"));

            // Verify system tests
            Assert.That(systemTests.Length, Is.EqualTo(2));
            
            var systemTest1 = systemTests.FirstOrDefault(st => st.ItemID == "SYS-DESC-001");
            Assert.That(systemTest1, Is.Not.Null);
            Assert.That(systemTest1.TestCaseDescription, Is.EqualTo("User Authentication System Test"));
            Assert.That(systemTest1.TestCaseAutomated, Is.True);
            Assert.That(systemTest1.TestCaseSteps.Count(), Is.GreaterThan(0)); // Should have parsed Given-When-Then
            
            var systemTest2 = systemTests.FirstOrDefault(st => st.ItemID == "SYS-DESC-002");
            Assert.That(systemTest2, Is.Not.Null);
            Assert.That(systemTest2.TestCaseDescription, Is.EqualTo("Test system-level password security and encryption"));
        }

        [UnitTestAttribute(
            Purpose = "TestDescriptionFilePlugin handles missing configuration file gracefully",
            Identifier = "D4E5F6A7-B8C9-4012-D345-E6F7A8B9C012",
            PostCondition = "Exception is thrown with appropriate error message")]
        [Test]
        public void TestDescriptionFilePlugin_MissingConfigFile()
        {
            var fileSystemWithoutConfig = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFilePath(@"c:\temp\descriptions.json"), new MockFileData("[]") }
            });

            var plugin = new RoboClerk.TestDescriptionFilePlugin.TestDescriptionFilePlugin(new LocalFileSystemPlugin(fileSystemWithoutConfig));
            
            var ex = Assert.Throws<Exception>(() => plugin.InitializePlugin(configuration));
            Assert.That(ex.Message.Contains("could not read its configuration"));
        }

        [UnitTestAttribute(
            Purpose = "TestDescriptionFilePlugin handles malformed JSON file",
            Identifier = "E5F6A7B8-C9D0-4123-E456-F7A8B9C0D123",
            PostCondition = "JsonException is thrown when JSON is invalid")]
        [Test]
        public void TestDescriptionFilePlugin_MalformedJson()
        {
            string malformedJson = @"{ ""invalid"": json }";
            
            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFilePath(@"c:\test\TestDescriptionFilePlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFilePath(@"c:\test\TestDescriptionFilePlugin.toml")) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\system_descriptions.json"), new MockFileData(malformedJson) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\unit_descriptions.json"), new MockFileData("[]") }
            });

            var plugin = new RoboClerk.TestDescriptionFilePlugin.TestDescriptionFilePlugin(new LocalFileSystemPlugin(testFileSystem));
            plugin.InitializePlugin(configuration);
            
            Assert.Throws<JsonException>(() => plugin.RefreshItems());
        }

        [UnitTestAttribute(
            Purpose = "TestDescriptionFilePlugin validates required fields for all test types",
            Identifier = "F6A7B8C9-D0E1-4234-F567-A8B9C0D1E234",
            PostCondition = "JsonException is thrown when required fields are missing")]
        [Test]
        public void TestDescriptionFilePlugin_MissingRequiredFields()
        {
            string jsonWithMissingFields = JsonSerializer.Serialize(new[]
            {
                new 
                {
                    // Missing ID field
                    name = "Test without ID",
                    type = "UNIT",
                    description = "Test description",
                    trace = new[] { "REQ-001" }
                }
            }, new JsonSerializerOptions { WriteIndented = true });

            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFilePath(@"c:\test\TestDescriptionFilePlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFilePath(@"c:\test\TestDescriptionFilePlugin.toml")) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\system_descriptions.json"), new MockFileData(jsonWithMissingFields) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\unit_descriptions.json"), new MockFileData("[]") }
            });

            var plugin = new RoboClerk.TestDescriptionFilePlugin.TestDescriptionFilePlugin(new LocalFileSystemPlugin(testFileSystem));
            plugin.InitializePlugin(configuration);
            
            var ex = Assert.Throws<JsonException>(() => plugin.RefreshItems());
            Assert.That(ex.Message.Contains("missing required properties, including the following: id"));
        }

        [UnitTestAttribute(
            Purpose = "TestDescriptionFilePlugin validates conditional required fields for UNIT tests",
            Identifier = "A7B8C9D0-E1F2-4345-A678-B9C0D1E2F345",
            PostCondition = "JsonException is thrown when purpose or acceptance fields are missing for UNIT tests")]
        [Test]
        public void TestDescriptionFilePlugin_MissingUnitTestRequiredFields()
        {
            string jsonWithMissingUnitFields = JsonSerializer.Serialize(new[]
            {
                new 
                {
                    id = "UNIT-MISSING-PURPOSE",
                    name = "Test missing purpose",
                    type = "UNIT",
                    description = "Test description",
                    trace = new[] { "REQ-001" }
                    // Missing purpose and acceptance
                }
            }, new JsonSerializerOptions { WriteIndented = true });

            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFilePath(@"c:\test\TestDescriptionFilePlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFilePath(@"c:\test\TestDescriptionFilePlugin.toml")) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\system_descriptions.json"), new MockFileData("[]") },
                { TestingHelpers.ConvertFilePath(@"c:\temp\unit_descriptions.json"), new MockFileData(jsonWithMissingUnitFields) }
            });

            var plugin = new RoboClerk.TestDescriptionFilePlugin.TestDescriptionFilePlugin(new LocalFileSystemPlugin(testFileSystem));
            plugin.InitializePlugin(configuration);
            
            var ex = Assert.Throws<JsonException>(() => plugin.RefreshItems());
            Assert.That(ex.Message.Contains("'purpose' field is required for UNIT"));
        }

        [UnitTestAttribute(
            Purpose = "TestDescriptionFilePlugin allows optional fields for SYSTEM tests",
            Identifier = "B8C9D0E1-F2A3-4456-B789-C0D1E2F3A456",
            PostCondition = "SYSTEM tests are processed correctly without purpose and acceptance fields")]
        [Test]
        public void TestDescriptionFilePlugin_SystemTestOptionalFields()
        {
            string systemTestWithoutOptional = JsonSerializer.Serialize(new[]
            {
                new 
                {
                    id = "SYS-WITHOUT-OPTIONAL",
                    name = "System test without optional fields",
                    type = "SYSTEM",
                    description = "System test description",
                    trace = new[] { "REQ-001" }
                    // No purpose, acceptance, or filename
                }
            }, new JsonSerializerOptions { WriteIndented = true });

            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFilePath(@"c:\test\TestDescriptionFilePlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFilePath(@"c:\test\TestDescriptionFilePlugin.toml")) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\system_descriptions.json"), new MockFileData(systemTestWithoutOptional) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\unit_descriptions.json"), new MockFileData("[]") }
            });

            var plugin = new RoboClerk.TestDescriptionFilePlugin.TestDescriptionFilePlugin(new LocalFileSystemPlugin(testFileSystem));
            plugin.InitializePlugin(configuration);
            
            Assert.DoesNotThrow(() => plugin.RefreshItems());
            
            var systemTests = plugin.GetSoftwareSystemTests().ToArray();
            Assert.That(systemTests.Length, Is.EqualTo(1));
            Assert.That(systemTests[0].ItemID, Is.EqualTo("SYS-WITHOUT-OPTIONAL"));
        }

        [UnitTestAttribute(
            Purpose = "TestDescriptionFilePlugin processes Given-When-Then format for system tests",
            Identifier = "C9D0E1F2-A3B4-4567-C890-D1E2F3A4B567",
            PostCondition = "Given-When-Then format is parsed into test steps")]
        [Test]
        public void TestDescriptionFilePlugin_GivenWhenThenFormat()
        {
            var plugin = new RoboClerk.TestDescriptionFilePlugin.TestDescriptionFilePlugin(new LocalFileSystemPlugin(fileSystem));
            plugin.InitializePlugin(configuration);
            plugin.RefreshItems();

            var systemTests = plugin.GetSoftwareSystemTests().ToArray();
            var gwtTest = systemTests.FirstOrDefault(st => st.ItemID == "SYS-DESC-001");
            
            Assert.That(gwtTest, Is.Not.Null);
            Assert.That(gwtTest.TestCaseSteps.Count(), Is.GreaterThan(0));
            
            // The test case description should be the name when Given-When-Then is detected
            Assert.That(gwtTest.TestCaseDescription, Is.EqualTo("User Authentication System Test"));
        }

        [UnitTestAttribute(
            Purpose = "TestDescriptionFilePlugin handles trace links correctly",
            Identifier = "D0E1F2A3-B4C5-4678-D901-E2F3A4B5C678",
            PostCondition = "Trace links are properly added to test items")]
        [Test]
        public void TestDescriptionFilePlugin_TraceLinks()
        {
            var plugin = new RoboClerk.TestDescriptionFilePlugin.TestDescriptionFilePlugin(new LocalFileSystemPlugin(fileSystem));
            plugin.InitializePlugin(configuration);
            plugin.RefreshItems();

            var unitTests = plugin.GetUnitTests().ToArray();
            var unitTest = unitTests.FirstOrDefault(ut => ut.ItemID == "UNIT-DESC-001");
            
            Assert.That(unitTest, Is.Not.Null);
            Assert.That(unitTest.LinkedItems.Count(), Is.EqualTo(1));
            
            var link = unitTest.LinkedItems.First();
            Assert.That(link.TargetID, Is.EqualTo("REQ-001"));
            Assert.That(link.LinkType, Is.EqualTo(ItemLinkType.Tests));
        }

        [UnitTestAttribute(
            Purpose = "TestDescriptionFilePlugin handles multiple files correctly",
            Identifier = "E1F2A3B4-C5D6-4789-E012-F3A4B5C6D789",
            PostCondition = "Test descriptions from all configured files are combined")]
        [Test]
        public void TestDescriptionFilePlugin_MultipleFiles()
        {
            var plugin = new RoboClerk.TestDescriptionFilePlugin.TestDescriptionFilePlugin(new LocalFileSystemPlugin(fileSystem));
            plugin.InitializePlugin(configuration);
            plugin.RefreshItems();

            var unitTests = plugin.GetUnitTests().ToArray();
            var systemTests = plugin.GetSoftwareSystemTests().ToArray();
            
            // Should have 2 unit tests from unit_descriptions.json
            Assert.That(unitTests.Length, Is.EqualTo(2));
            
            // Should have 2 system tests from system_descriptions.json
            Assert.That(systemTests.Length, Is.EqualTo(2));
            
            // Verify we have tests from both files
            Assert.That(unitTests.Any(ut => ut.ItemID == "UNIT-DESC-001"), Is.True);
            Assert.That(unitTests.Any(ut => ut.ItemID == "UNIT-DESC-002"), Is.True);
            Assert.That(systemTests.Any(st => st.ItemID == "SYS-DESC-001"), Is.True);
            Assert.That(systemTests.Any(st => st.ItemID == "SYS-DESC-002"), Is.True);
        }

        [UnitTestAttribute(
            Purpose = "TestDescriptionFilePlugin handles empty JSON array",
            Identifier = "F2A3B4C5-D6E7-4890-F123-A4B5C6D7E890",
            PostCondition = "Empty files result in no test descriptions")]
        [Test]
        public void TestDescriptionFilePlugin_EmptyJsonArray()
        {
            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFilePath(@"c:\test\TestDescriptionFilePlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFilePath(@"c:\test\TestDescriptionFilePlugin.toml")) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\system_descriptions.json"), new MockFileData("[]") },
                { TestingHelpers.ConvertFilePath(@"c:\temp\unit_descriptions.json"), new MockFileData("[]") }
            });

            var plugin = new RoboClerk.TestDescriptionFilePlugin.TestDescriptionFilePlugin(new LocalFileSystemPlugin(testFileSystem));
            plugin.InitializePlugin(configuration);
            plugin.RefreshItems();

            var unitTests = plugin.GetUnitTests().ToArray();
            var systemTests = plugin.GetSoftwareSystemTests().ToArray();
            
            Assert.That(unitTests.Length, Is.EqualTo(0));
            Assert.That(systemTests.Length, Is.EqualTo(0));
        }

        [UnitTestAttribute(
            Purpose = "TestDescriptionFilePlugin validates trace array is not empty",
            Identifier = "A3B4C5D6-E7F8-4901-A234-B5C6D7E8F901",
            PostCondition = "JsonException is thrown when trace array is empty")]
        [Test]
        public void TestDescriptionFilePlugin_EmptyTraceArray()
        {
            string jsonWithEmptyTrace = JsonSerializer.Serialize(new[]
            {
                new 
                {
                    id = "TEST-EMPTY-TRACE",
                    name = "Test with empty trace",
                    type = "SYSTEM",
                    description = "Test description",
                    trace = new string[] { } // Empty trace array
                }
            }, new JsonSerializerOptions { WriteIndented = true });

            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFilePath(@"c:\test\TestDescriptionFilePlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFilePath(@"c:\test\TestDescriptionFilePlugin.toml")) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\system_descriptions.json"), new MockFileData(jsonWithEmptyTrace) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\unit_descriptions.json"), new MockFileData("[]") }
            });

            var plugin = new RoboClerk.TestDescriptionFilePlugin.TestDescriptionFilePlugin(new LocalFileSystemPlugin(testFileSystem));
            plugin.InitializePlugin(configuration);
            
            var ex = Assert.Throws<JsonException>(() => plugin.RefreshItems());
            Assert.That(ex.Message.Contains("'trace' field is required and must contain at least one item"));
        }

        [UnitTestAttribute(
            Purpose = "TestDescriptionFilePlugin provides detailed error messages with context",
            Identifier = "C5D6E7F8-A9B0-4123-C456-D7E8F9A0B123",
            PostCondition = "Error messages include test ID and name for better debugging")]
        [Test]
        public void TestDescriptionFilePlugin_DetailedErrorMessages()
        {
            string jsonWithMissingAcceptance = JsonSerializer.Serialize(new[]
            {
                new 
                {
                    id = "UNIT-MISSING-ACCEPTANCE",
                    name = "testMissingAcceptance",
                    type = "UNIT",
                    description = "Test description",
                    trace = new[] { "REQ-001" },
                    purpose = "Test purpose"
                    // Missing acceptance
                }
            }, new JsonSerializerOptions { WriteIndented = true });

            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFilePath(@"c:\test\TestDescriptionFilePlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFilePath(@"c:\test\TestDescriptionFilePlugin.toml")) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\system_descriptions.json"), new MockFileData("[]") },
                { TestingHelpers.ConvertFilePath(@"c:\temp\unit_descriptions.json"), new MockFileData(jsonWithMissingAcceptance) }
            });

            var plugin = new RoboClerk.TestDescriptionFilePlugin.TestDescriptionFilePlugin(new LocalFileSystemPlugin(testFileSystem));
            plugin.InitializePlugin(configuration);
            
            var ex = Assert.Throws<JsonException>(() => plugin.RefreshItems());
            Assert.That(ex.Message.Contains("UNIT-MISSING-ACCEPTANCE"), "Error should include test ID");
            Assert.That(ex.Message.Contains("testMissingAcceptance"), "Error should include test name");
            Assert.That(ex.Message.Contains("'acceptance' field is required for UNIT"), "Error should specify the missing field");
        }

        [UnitTestAttribute(
            Purpose = "TestDescriptionFilePlugin validates name field is required",
            Identifier = "D6E7F8A9-B0C1-4234-D567-E8F9A0B1C234",
            PostCondition = "JsonException is thrown when name field is missing")]
        [Test]
        public void TestDescriptionFilePlugin_MissingNameField()
        {
            string jsonWithMissingName = JsonSerializer.Serialize(new[]
            {
                new 
                {
                    id = "TEST-MISSING-NAME",
                    // Missing name field
                    type = "SYSTEM",
                    description = "Test description",
                    trace = new[] { "REQ-001" }
                }
            }, new JsonSerializerOptions { WriteIndented = true });

            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFilePath(@"c:\test\TestDescriptionFilePlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFilePath(@"c:\test\TestDescriptionFilePlugin.toml")) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\system_descriptions.json"), new MockFileData(jsonWithMissingName) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\unit_descriptions.json"), new MockFileData("[]") }
            });

            var plugin = new RoboClerk.TestDescriptionFilePlugin.TestDescriptionFilePlugin(new LocalFileSystemPlugin(testFileSystem));
            plugin.InitializePlugin(configuration);
            
            var ex = Assert.Throws<JsonException>(() => plugin.RefreshItems());
            Assert.That(ex.Message.Contains("missing required properties, including the following: name"));
        }

        [UnitTestAttribute(
            Purpose = "TestDescriptionFilePlugin validates description field is required",
            Identifier = "E7F8A9B0-C1D2-4345-E678-F9A0B1C2D345",
            PostCondition = "JsonException is thrown when description field is missing")]
        [Test]
        public void TestDescriptionFilePlugin_MissingDescriptionField()
        {
            string jsonWithMissingDescription = JsonSerializer.Serialize(new[]
            {
                new 
                {
                    id = "TEST-MISSING-DESC",
                    name = "Test missing description",
                    type = "SYSTEM",
                    // Missing description field
                    trace = new[] { "REQ-001" }
                }
            }, new JsonSerializerOptions { WriteIndented = true });

            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFilePath(@"c:\test\TestDescriptionFilePlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFilePath(@"c:\test\TestDescriptionFilePlugin.toml")) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\system_descriptions.json"), new MockFileData(jsonWithMissingDescription) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\unit_descriptions.json"), new MockFileData("[]") }
            });

            var plugin = new RoboClerk.TestDescriptionFilePlugin.TestDescriptionFilePlugin(new LocalFileSystemPlugin(testFileSystem));
            plugin.InitializePlugin(configuration);
            
            var ex = Assert.Throws<JsonException>(() => plugin.RefreshItems());
            Assert.That(ex.Message.Contains("missing required properties, including the following: description"));
        }

        [UnitTestAttribute(
            Purpose = "TestDescriptionFilePlugin handles complex Given-When-Then scenarios",
            Identifier = "F8A9B0C1-D2E3-4456-F789-A0B1C2D3E456",
            PostCondition = "Complex Given-When-Then structures are correctly parsed into test steps")]
        [Test]
        public void TestDescriptionFilePlugin_ComplexGivenWhenThen()
        {
            string complexGwtJson = JsonSerializer.Serialize(new[]
            {
                new RoboClerk.TestDescriptionFilePlugin.TestDescriptionJSONObject
                {
                    ID = "COMPLEX-GWT-001",
                    Name = "Complex Given-When-Then Test",
                    Type = TestType.SYSTEM,
                    Description = @"GIVEN: The user is on the login page
AND: The user has valid credentials
WHEN: The user enters the username
AND: The user enters the password
AND: The user clicks the login button
THEN: The user should be redirected to the dashboard
AND: The user should see a welcome message",
                    Trace = new List<string> { "REQ-001" }
                }
            }, new JsonSerializerOptions { WriteIndented = true });

            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFilePath(@"c:\test\TestDescriptionFilePlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFilePath(@"c:\test\TestDescriptionFilePlugin.toml")) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\system_descriptions.json"), new MockFileData(complexGwtJson) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\unit_descriptions.json"), new MockFileData("[]") }
            });

            var plugin = new RoboClerk.TestDescriptionFilePlugin.TestDescriptionFilePlugin(new LocalFileSystemPlugin(testFileSystem));
            plugin.InitializePlugin(configuration);
            plugin.RefreshItems();

            var systemTests = plugin.GetSoftwareSystemTests().ToArray();
            Assert.That(systemTests.Length, Is.EqualTo(1));
            
            var complexTest = systemTests[0];
            Assert.That(complexTest.TestCaseSteps.Count(), Is.EqualTo(6));
            Assert.That(complexTest.TestCaseDescription, Is.EqualTo("Complex Given-When-Then Test"));
        }

        [UnitTestAttribute(
            Purpose = "TestDescriptionFilePlugin handles optional project field correctly",
            Identifier = "45e80ecc-cd7e-44e6-8519-37e2b4b277a8",
            PostCondition = "Project field is properly set on test items when provided")]
        [Test]
        public void TestDescriptionFilePlugin_ProjectField()
        {
            string jsonWithProject = JsonSerializer.Serialize(new[]
            {
                new RoboClerk.TestDescriptionFilePlugin.TestDescriptionJSONObject
                {
                    ID = "SYS-PROJ-001",
                    Name = "System test with project",
                    Type = TestType.SYSTEM,
                    Description = "System test description",
                    Trace = new List<string> { "REQ-001" },
                    Project = "ProjectAlpha"
                },
                new RoboClerk.TestDescriptionFilePlugin.TestDescriptionJSONObject
                {
                    ID = "UNIT-PROJ-001",
                    Name = "testWithProject",
                    Type = TestType.UNIT,
                    Description = "Unit test with project",
                    Trace = new List<string> { "REQ-002" },
                    Purpose = "Test with project field",
                    Acceptance = "Project field is set correctly",
                    Project = "ProjectBeta"
                },
                new RoboClerk.TestDescriptionFilePlugin.TestDescriptionJSONObject
                {
                    ID = "SYS-NO-PROJ-001",
                    Name = "System test without project",
                    Type = TestType.SYSTEM,
                    Description = "System test without project field",
                    Trace = new List<string> { "REQ-003" }
                    // No project field
                }
            }, new JsonSerializerOptions { WriteIndented = true });

            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFilePath(@"c:\test\TestDescriptionFilePlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFilePath(@"c:\test\TestDescriptionFilePlugin.toml")) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\system_descriptions.json"), new MockFileData(jsonWithProject) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\unit_descriptions.json"), new MockFileData("[]") }
            });

            var plugin = new RoboClerk.TestDescriptionFilePlugin.TestDescriptionFilePlugin(new LocalFileSystemPlugin(testFileSystem));
            plugin.InitializePlugin(configuration);
            plugin.RefreshItems();

            var unitTests = plugin.GetUnitTests().ToArray();
            var systemTests = plugin.GetSoftwareSystemTests().ToArray();
            
            // Check unit test project
            var unitTestWithProject = unitTests.FirstOrDefault(ut => ut.ItemID == "UNIT-PROJ-001");
            Assert.That(unitTestWithProject, Is.Not.Null);
            Assert.That(unitTestWithProject.ItemProject, Is.EqualTo("ProjectBeta"));
            
            // Check system test project
            var systemTestWithProject = systemTests.FirstOrDefault(st => st.ItemID == "SYS-PROJ-001");
            Assert.That(systemTestWithProject, Is.Not.Null);
            Assert.That(systemTestWithProject.ItemProject, Is.EqualTo("ProjectAlpha"));
            
            // Check system test without project (should have empty string)
            var systemTestWithoutProject = systemTests.FirstOrDefault(st => st.ItemID == "SYS-NO-PROJ-001");
            Assert.That(systemTestWithoutProject, Is.Not.Null);
            Assert.That(systemTestWithoutProject.ItemProject, Is.EqualTo(string.Empty));
        }

        [UnitTestAttribute(
            Purpose = "TestDescriptionFilePlugin handles null project field gracefully",
            Identifier = "aefd177b-1a52-4e39-9d9a-728d6977841a",
            PostCondition = "Null project field is converted to empty string")]
        [Test]
        public void TestDescriptionFilePlugin_NullProjectField()
        {
            // Create test data with explicit null project
            var testData = new[]
            {
                new Dictionary<string, object>
                {
                    ["id"] = "NULL-PROJ-001",
                    ["name"] = "Test with null project",
                    ["type"] = "SYSTEM",
                    ["description"] = "Test description",
                    ["trace"] = new[] { "REQ-001" },
                    ["project"] = (string)null
                }
            };

            string jsonWithNullProject = JsonSerializer.Serialize(testData, new JsonSerializerOptions { WriteIndented = true });

            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFilePath(@"c:\test\TestDescriptionFilePlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFilePath(@"c:\test\TestDescriptionFilePlugin.toml")) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\system_descriptions.json"), new MockFileData(jsonWithNullProject) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\unit_descriptions.json"), new MockFileData("[]") }
            });

            var plugin = new RoboClerk.TestDescriptionFilePlugin.TestDescriptionFilePlugin(new LocalFileSystemPlugin(testFileSystem));
            plugin.InitializePlugin(configuration);
            plugin.RefreshItems();

            var systemTests = plugin.GetSoftwareSystemTests().ToArray();
            Assert.That(systemTests.Length, Is.EqualTo(1));
            
            var testWithNullProject = systemTests[0];
            Assert.That(testWithNullProject.ItemID, Is.EqualTo("NULL-PROJ-001"));
            Assert.That(testWithNullProject.ItemProject, Is.EqualTo(string.Empty));
        }

        [UnitTestAttribute(
            Purpose = "TestDescriptionFilePlugin maintains backward compatibility when project field is absent",
            Identifier = "434720b2-b4dd-47e6-9829-b2cfe578b197",
            PostCondition = "Tests without project field are processed normally with empty project")]
        [Test]
        public void TestDescriptionFilePlugin_BackwardCompatibility()
        {
            // Use original test setup which doesn't include project field
            var plugin = new RoboClerk.TestDescriptionFilePlugin.TestDescriptionFilePlugin(new LocalFileSystemPlugin(fileSystem));
            plugin.InitializePlugin(configuration);
            plugin.RefreshItems();

            var unitTests = plugin.GetUnitTests().ToArray();
            var systemTests = plugin.GetSoftwareSystemTests().ToArray();
            
            // All tests should have empty project field
            Assert.That(unitTests.All(ut => ut.ItemProject == string.Empty), Is.True);
            Assert.That(systemTests.All(st => st.ItemProject == string.Empty), Is.True);
            
            // But other functionality should work normally
            Assert.That(unitTests.Length, Is.EqualTo(2));
            Assert.That(systemTests.Length, Is.EqualTo(2));
        }
    }
}