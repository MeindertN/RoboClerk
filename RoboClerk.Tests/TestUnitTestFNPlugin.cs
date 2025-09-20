using NSubstitute;
using NUnit.Framework;
using RoboClerk.Configuration;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("These tests test the UnitTestFNPlugin with TreeSitter")]
    internal class TestUnitTestFNPlugin
    {
        private IFileSystem fileSystem = null;
        private IConfiguration configuration = null;

        [SetUp]
        public void TestSetup()
        {
            StringBuilder configFile = new StringBuilder();
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                configFile.Append(@"TestDirectories = [""/c/temp""]");
            }
            else
            {
                configFile.Append(@"TestDirectories = [""c:/temp""]");
            }
            configFile.Append(@"
SubDirs = true
FileMasks = [""Test*.cs""]
UseGit = false
Language = ""csharp""
FunctionMask = ""<PURPOSE>_VERIFIES_<POSTCONDITION>""
SectionSeparator = ""_""
");

            string testFile = @"
class TestClass {
    public void This_is_the_purpose_VERIFIES_that_the_test_is_good(int i)
    {
        // Test implementation
    }

    public void Another_purpose_VERIFIES_another_postcondition_here()
    {
        // Another test
    }

    // This method should be ignored - doesn't match mask pattern
    public void Not_matching_pattern()
    {
        // Not matching the function mask
    }
}";
            fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\test\UnitTestFNPlugin.toml"), new MockFileData(configFile.ToString()) },
                { TestingHelpers.ConvertFileName(@"c:\temp\TestDummy.cs"), new MockFileData(testFile) },
            });
            configuration = Substitute.For<IConfiguration>();
            configuration.PluginConfigDir.Returns(TestingHelpers.ConvertFileName(@"c:/test/"));
            configuration.ProjectRoot.Returns(TestingHelpers.ConvertFileName(@"c:/temp/"));
            configuration.CommandLineOptionOrDefault(Arg.Any<string>(), Arg.Any<string>())
                .ReturnsForAnyArgs(callInfo => callInfo.ArgAt<string>(1));
        }

        [UnitTestAttribute(
        Purpose = "UnitTestFNPlugin is created using TreeSitter",
        Identifier = "F1A2B3C4-D5E6-7890-AB12-CD34EF567890",
        PostCondition = "No exception is thrown")]
        [Test]
        public void TestUnitTestFNPlugin_Creation()
        {
            var temp = new UnitTestFNPlugin(fileSystem);
            temp.InitializePlugin(configuration);
        }

        [UnitTestAttribute(
        Purpose = "UnitTestFNPlugin extracts unit test information using TreeSitter and function mask parsing",
        Identifier = "A1B2C3D4-E5F6-7890-1234-567890ABCDEF",
        PostCondition = "Unit test information is correctly extracted using function name parsing")]
        [Test]
        public void TestUnitTestFNPlugin_ExtractTests()
        {
            var temp = new UnitTestFNPlugin(fileSystem);
            temp.InitializePlugin(configuration);
            temp.RefreshItems();

            var tests = temp.GetUnitTests().ToArray();

            Assert.That(tests.Length == 2);
            
            // First test
            Assert.That(tests[0].UnitTestPurpose == "This is the purpose");
            Assert.That(tests[0].UnitTestAcceptanceCriteria == "that the test is good");
            Assert.That(tests[0].UnitTestFunctionName == "This_is_the_purpose_VERIFIES_that_the_test_is_good");
            Assert.That(tests[0].UnitTestFileName == "TestDummy.cs");
            
            // Second test
            Assert.That(tests[1].UnitTestPurpose == "Another purpose");
            Assert.That(tests[1].UnitTestAcceptanceCriteria == "another postcondition here");
            Assert.That(tests[1].UnitTestFunctionName == "Another_purpose_VERIFIES_another_postcondition_here");
            Assert.That(tests[1].UnitTestFileName == "TestDummy.cs");
        }

        [UnitTestAttribute(
        Purpose = "UnitTestFNPlugin handles methods that don't match function mask correctly",
        Identifier = "B2C3D4E5-F6A7-8901-2345-6789ABCDEF01",
        PostCondition = "Methods that don't match the function mask pattern are ignored")]
        [Test]
        public void TestUnitTestFNPlugin_IgnoreNonMatchingMethods()
        {
            string testFileWithoutMatches = @"
class TestClass {
    // This method should be ignored - doesn't match pattern
    public void This_is_not_matching_the_pattern()
    {
        // Not matching
    }
    
    // This should also be ignored
    public void AnotherMethodNotMatching()
    {
        // Not matching
    }
}";

            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\test\UnitTestFNPlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFileName(@"c:\test\UnitTestFNPlugin.toml")) },
                { TestingHelpers.ConvertFileName(@"c:\temp\TestNoMatches.cs"), new MockFileData(testFileWithoutMatches) },
            });

            var temp = new UnitTestFNPlugin(testFileSystem);
            temp.InitializePlugin(configuration);
            temp.RefreshItems();

            var tests = temp.GetUnitTests().ToArray();
            Assert.That(tests.Length == 0);
        }

        [UnitTestAttribute(
        Purpose = "UnitTestFNPlugin handles camelCase section separator correctly",
        Identifier = "C3D4E5F6-A7B8-9012-3456-789ABCDEF012",
        PostCondition = "CamelCase function names are correctly parsed")]
        [Test]
        public void TestUnitTestFNPlugin_CamelCaseSeparator()
        {
            StringBuilder configFile = new StringBuilder();
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                configFile.Append(@"TestDirectories = [""/c/temp""]");
            }
            else
            {
                configFile.Append(@"TestDirectories = [""c:/temp""]");
            }
            configFile.Append(@"
SubDirs = true
FileMasks = [""Test*.cs""]
UseGit = false
Language = ""csharp""
FunctionMask = ""<PURPOSE>VERIFIES<POSTCONDITION>""
SectionSeparator = ""CAMELCASE""
");

            string testFile = @"
class TestClass {
    public void ThisIsThePurposeVERIFIESThatTheTestIsGood(int i)
    {
        // Test implementation
    }
}";

            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\test\UnitTestFNPlugin.toml"), new MockFileData(configFile.ToString()) },
                { TestingHelpers.ConvertFileName(@"c:\temp\TestCamelCase.cs"), new MockFileData(testFile) },
            });

            var testConfiguration = Substitute.For<IConfiguration>();
            testConfiguration.PluginConfigDir.Returns(TestingHelpers.ConvertFileName(@"c:/test/"));
            testConfiguration.ProjectRoot.Returns(TestingHelpers.ConvertFileName(@"c:/temp/"));
            testConfiguration.CommandLineOptionOrDefault(Arg.Any<string>(), Arg.Any<string>())
                .ReturnsForAnyArgs(callInfo => callInfo.ArgAt<string>(1));

            var temp = new UnitTestFNPlugin(testFileSystem);
            temp.InitializePlugin(testConfiguration);
            temp.RefreshItems();

            var tests = temp.GetUnitTests().ToArray();

            Assert.That(tests.Length == 1);
            Assert.That(tests[0].UnitTestPurpose == "This Is The Purpose");
            Assert.That(tests[0].UnitTestAcceptanceCriteria == "That The Test Is Good");
        }

        [UnitTestAttribute(
        Purpose = "UnitTestFNPlugin handles missing configuration file gracefully",
        Identifier = "D4E5F6A7-B8C9-0123-4567-89ABCDEF0123",
        PostCondition = "Exception is thrown with appropriate error message")]
        [Test]
        public void TestUnitTestFNPlugin_MissingConfigFile()
        {
            var fileSystemWithoutConfig = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\temp\TestDummy.cs"), new MockFileData("test content") },
            });

            var temp = new UnitTestFNPlugin(fileSystemWithoutConfig);
            
            var ex = Assert.Throws<Exception>(() => temp.InitializePlugin(configuration));
            Assert.That(ex.Message.Contains("could not read its configuration"));
        }

        [UnitTestAttribute(
        Purpose = "UnitTestFNPlugin handles invalid function mask correctly",
        Identifier = "E5F6A7B8-C9D0-1234-5678-9ABCDEF01234",
        PostCondition = "Exception is thrown for invalid function mask")]
        [Test]
        public void TestUnitTestFNPlugin_InvalidFunctionMask()
        {
            StringBuilder configFile = new StringBuilder();
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                configFile.Append(@"TestDirectories = [""/c/temp""]");
            }
            else
            {
                configFile.Append(@"TestDirectories = [""c:/temp""]");
            }
            configFile.Append(@"
SubDirs = true
FileMasks = [""Test*.cs""]
UseGit = false
Language = ""csharp""
FunctionMask = ""INVALID_MASK_WITHOUT_ELEMENT_IDENTIFIER""
SectionSeparator = ""_""
");

            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\test\UnitTestFNPlugin.toml"), new MockFileData(configFile.ToString()) },
                { TestingHelpers.ConvertFileName(@"c:\temp\TestDummy.cs"), new MockFileData("test content") },
            });

            var temp = new UnitTestFNPlugin(testFileSystem);
            
            var ex = Assert.Throws<Exception>(() => temp.InitializePlugin(configuration));
            Assert.That(ex.Message.Contains("could not read"));
        }
    }
}