using NSubstitute;
using NUnit.Framework;
using RoboClerk.Core.Configuration;
using RoboClerk.Core.FileProviders;
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
            configFile.Append(@"
[[TestConfigurations]]
Language = ""csharp""
TestDirectory = """ + TestingHelpers.ConvertFilePath(@"c:/temp") + @"""
SubDirs = true
FileMasks = [""Test*.cs""]
Project = ""RoboClerk""
FunctionMask = ""<PURPOSE>_VERIFIES_<POSTCONDITION>""
SectionSeparator = ""_""

UseGit = false
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
                { TestingHelpers.ConvertFilePath(@"c:\test\UnitTestFNPlugin.toml"), new MockFileData(configFile.ToString()) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\TestDummy.cs"), new MockFileData(testFile) },
            });
            configuration = Substitute.For<IConfiguration>();
            configuration.PluginConfigDir.Returns(TestingHelpers.ConvertFilePath(@"c:\test\"));
            configuration.ProjectRoot.Returns(TestingHelpers.ConvertFilePath(@"c:\temp\"));
            configuration.CommandLineOptionOrDefault(Arg.Any<string>(), Arg.Any<string>())
                .ReturnsForAnyArgs(callInfo => callInfo.ArgAt<string>(1));
        }

        #region Basic Tests

        [UnitTestAttribute(
        Purpose = "UnitTestFNPlugin is created using TreeSitter",
        Identifier = "4d74ff03-bc34-40ac-ad23-f5150d8b71bc",
        PostCondition = "No exception is thrown")]
        [Test]
        public void TestUnitTestFNPlugin_Creation()
        {
            var temp = new UnitTestFNPlugin(new LocalFileSystemPlugin(fileSystem));
            temp.InitializePlugin(configuration);
        }

        [UnitTestAttribute(
        Purpose = "UnitTestFNPlugin extracts unit test information using TreeSitter and function mask parsing",
        Identifier = "abcf064f-7078-41a0-a38e-ddd1d235d208",
        PostCondition = "Unit test information is correctly extracted using function name parsing")]
        [Test]
        public void TestUnitTestFNPlugin_ExtractTests()
        {
            var temp = new UnitTestFNPlugin(new LocalFileSystemPlugin(fileSystem));
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
        Identifier = "afd65daa-bd37-4e5a-964a-558cf428f8ec",
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
                { TestingHelpers.ConvertFilePath(@"c:\test\UnitTestFNPlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFilePath(@"c:\test\UnitTestFNPlugin.toml")) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\TestNoMatches.cs"), new MockFileData(testFileWithoutMatches) },
            });

            var temp = new UnitTestFNPlugin(new LocalFileSystemPlugin(testFileSystem));
            temp.InitializePlugin(configuration);
            temp.RefreshItems();

            var tests = temp.GetUnitTests().ToArray();
            Assert.That(tests.Length == 0);
        }

        [UnitTestAttribute(
        Purpose = "UnitTestFNPlugin handles camelCase section separator correctly",
        Identifier = "89065686-4147-46a3-b6ed-dd70ff94751e",
        PostCondition = "CamelCase function names are correctly parsed")]
        [Test]
        public void TestUnitTestFNPlugin_CamelCaseSeparator()
        {
            string configFile = ($@"
UseGit = false
[[TestConfigurations]]
Language = ""csharp""
TestDirectory = ""{TestingHelpers.ConvertFilePath(@"c:/temp")}""
SubDirs = true
FileMasks = [""Test*.cs""]
Project = ""RoboClerk""
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
                { TestingHelpers.ConvertFilePath(@"c:\test\UnitTestFNPlugin.toml"), new MockFileData(configFile) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\TestCamelCase.cs"), new MockFileData(testFile) },
            });

            var testConfiguration = Substitute.For<IConfiguration>();
            testConfiguration.PluginConfigDir.Returns(TestingHelpers.ConvertFilePath(@"c:\test\"));
            testConfiguration.ProjectRoot.Returns(TestingHelpers.ConvertFilePath(@"c:\temp\"));
            testConfiguration.CommandLineOptionOrDefault(Arg.Any<string>(), Arg.Any<string>())
                .ReturnsForAnyArgs(callInfo => callInfo.ArgAt<string>(1));

            var temp = new UnitTestFNPlugin(new LocalFileSystemPlugin(testFileSystem));
            temp.InitializePlugin(testConfiguration);
            temp.RefreshItems();

            var tests = temp.GetUnitTests().ToArray();

            Assert.That(tests.Length == 1);
            Assert.That(tests[0].UnitTestPurpose == "This Is The Purpose");
            Assert.That(tests[0].UnitTestAcceptanceCriteria == "That The Test Is Good");
        }

        [UnitTestAttribute(
        Purpose = "UnitTestFNPlugin handles missing configuration file gracefully",
        Identifier = "5c6ba4dc-ab2d-4071-84bc-095e877c75e3",
        PostCondition = "Exception is thrown with appropriate error message")]
        [Test]
        public void TestUnitTestFNPlugin_MissingConfigFile()
        {
            var fileSystemWithoutConfig = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFilePath(@"c:\temp\TestDummy.cs"), new MockFileData("test content") },
            });

            var temp = new UnitTestFNPlugin(new LocalFileSystemPlugin(fileSystemWithoutConfig));

            var ex = Assert.Throws<Exception>(() => temp.InitializePlugin(configuration));
            Assert.That(ex.Message.Contains("could not read its configuration"));
        }

        [UnitTestAttribute(
        Purpose = "UnitTestFNPlugin handles invalid function mask correctly",
        Identifier = "4ed831a6-687a-4b27-b27c-510bc6abc614",
        PostCondition = "Exception is thrown for invalid function mask")]
        [Test]
        public void TestUnitTestFNPlugin_InvalidFunctionMask()
        {
            StringBuilder configFile = new StringBuilder();
            configFile.Append(@"
[[TestConfigurations]]
Language = ""csharp""
TestDirectory = """ + TestingHelpers.ConvertFilePath(@"c:\temp") + @"""
SubDirs = true
FileMasks = [""Test*.cs""]
Project = ""RoboClerk""
FunctionMask = ""INVALID_MASK_WITHOUT_ELEMENT_IDENTIFIER""
SectionSeparator = ""_""

UseGit = false
");

            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFilePath(@"c:\test\UnitTestFNPlugin.toml"), new MockFileData(configFile.ToString()) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\TestDummy.cs"), new MockFileData("test content") },
            });

            var temp = new UnitTestFNPlugin(new LocalFileSystemPlugin(testFileSystem));

            var ex = Assert.Throws<Exception>(() => temp.InitializePlugin(configuration));
            Assert.That(ex.Message.Contains("could not read"));
        }

        #endregion Basic Tests

        #region Language-Specific Tests

        [UnitTestAttribute(
        Purpose = "UnitTestFNPlugin processes Java methods correctly with TreeSitter",
        Identifier = "d3573677-c17d-4623-9172-dbndsff2abe9b",
        PostCondition = "Java unit test methods are correctly extracted and parsed")]
        [Test]
        public void TestUnitTestFNPlugin_Java_ExtractTests()
        {
            StringBuilder configFile = new StringBuilder();
            configFile.Append($@"
[[TestConfigurations]]
Language = ""java""
TestDirectory = ""{TestingHelpers.ConvertFilePath(@"c:/temp")}""
SubDirs = true
FileMasks = [""Test*.java""]
Project = ""RoboClerk""
FunctionMask = ""<PURPOSE>_VERIFIES_<POSTCONDITION>""
SectionSeparator = ""_""

UseGit = false
");

            string javaTestFile = @"
public class TestClass {
    public void This_is_java_test_VERIFIES_that_java_parsing_works() {
        // Java test implementation
    }

    public void Another_java_test_VERIFIES_java_method_extraction() {
        // Another Java test
    }

    // This should be ignored - doesn't match pattern
    public void regularJavaMethod() {
        // Not matching
    }
}";
            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFilePath(@"c:\test\UnitTestFNPlugin.toml"), new MockFileData(configFile.ToString()) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\TestJava.java"), new MockFileData(javaTestFile) },
            });

            var testConfiguration = Substitute.For<IConfiguration>();
            testConfiguration.PluginConfigDir.Returns(TestingHelpers.ConvertFilePath(@"c:\test\"));
            testConfiguration.ProjectRoot.Returns(TestingHelpers.ConvertFilePath(@"c:\temp\"));
            testConfiguration.CommandLineOptionOrDefault(Arg.Any<string>(), Arg.Any<string>())
                .ReturnsForAnyArgs(callInfo => callInfo.ArgAt<string>(1));

            var temp = new UnitTestFNPlugin(new LocalFileSystemPlugin(testFileSystem));
            temp.InitializePlugin(testConfiguration);
            temp.RefreshItems();

            var tests = temp.GetUnitTests().ToArray();

            Assert.That(tests.Length == 2);
            Assert.That(tests[0].UnitTestPurpose == "This is java test");
            Assert.That(tests[0].UnitTestAcceptanceCriteria == "that java parsing works");
            Assert.That(tests[0].UnitTestFileName == "TestJava.java");
            Assert.That(tests[1].UnitTestPurpose == "Another java test");
            Assert.That(tests[1].UnitTestAcceptanceCriteria == "java method extraction");
        }

        [UnitTestAttribute(
        Purpose = "UnitTestFNPlugin processes Python functions correctly with TreeSitter",
        Identifier = "eb8c82b5-67c9-41d5-a34e-69bceea47e81",
        PostCondition = "Python unit test functions are correctly extracted and parsed")]
        [Test]
        public void TestUnitTestFNPlugin_Python_ExtractTests()
        {
            StringBuilder configFile = new StringBuilder();
            configFile.Append($@"
[[TestConfigurations]]
Language = ""python""
TestDirectory = ""{TestingHelpers.ConvertFilePath(@"c:/temp")}""
SubDirs = true
FileMasks = [""test_*.py""]
Project = ""RoboClerk""
FunctionMask = ""<PURPOSE>_VERIFIES_<POSTCONDITION>""
SectionSeparator = ""_""

UseGit = false
");

            string pythonTestFile = @"
import unittest

class TestClass(unittest.TestCase):
    def this_is_python_test_VERIFIES_that_python_parsing_works(self):
        # Python test implementation
        pass

    def another_python_test_VERIFIES_python_function_extraction(self):
        # Another Python test
        pass

    # This should be ignored - doesn't match pattern
    def regular_python_method(self):
        # Not matching
        pass

def standalone_test_function_VERIFIES_standalone_functions_work():
    # Standalone function test
    pass
";

            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFilePath(@"c:\test\UnitTestFNPlugin.toml"), new MockFileData(configFile.ToString()) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\test_python.py"), new MockFileData(pythonTestFile) },
            });

            var testConfiguration = Substitute.For<IConfiguration>();
            testConfiguration.PluginConfigDir.Returns(TestingHelpers.ConvertFilePath(@"c:\test\"));
            testConfiguration.ProjectRoot.Returns(TestingHelpers.ConvertFilePath(@"c:\temp\"));
            testConfiguration.CommandLineOptionOrDefault(Arg.Any<string>(), Arg.Any<string>())
                .ReturnsForAnyArgs(callInfo => callInfo.ArgAt<string>(1));

            var temp = new UnitTestFNPlugin(new LocalFileSystemPlugin(testFileSystem));
            temp.InitializePlugin(testConfiguration);
            temp.RefreshItems();

            var tests = temp.GetUnitTests().ToArray();

            Assert.That(tests.Length == 3);
            Assert.That(tests[0].UnitTestPurpose == "this is python test");
            Assert.That(tests[0].UnitTestAcceptanceCriteria == "that python parsing works");
            Assert.That(tests[0].UnitTestFileName == "test_python.py");
            Assert.That(tests[1].UnitTestPurpose == "another python test");
            Assert.That(tests[1].UnitTestAcceptanceCriteria == "python function extraction");
            Assert.That(tests[2].UnitTestPurpose == "standalone test function");
            Assert.That(tests[2].UnitTestAcceptanceCriteria == "standalone functions work");
        }

        [UnitTestAttribute(
        Purpose = "UnitTestFNPlugin processes TypeScript methods correctly with TreeSitter",
        Identifier = "ba48f80c-dbb9-4089-87a4-99382bf75a44",
        PostCondition = "TypeScript unit test methods are correctly extracted and parsed")]
        [Test]
        public void TestUnitTestFNPlugin_TypeScript_ExtractTests()
        {
            StringBuilder configFile = new StringBuilder();
            configFile.Append($@"
[[TestConfigurations]]
Language = ""typescript""
TestDirectory = ""{TestingHelpers.ConvertFilePath(@"c:/temp")}""
SubDirs = true
FileMasks = [""*.test.ts""]
Project = ""RoboClerk""
FunctionMask = ""<PURPOSE>_VERIFIES_<POSTCONDITION>""
SectionSeparator = ""_""

UseGit = false
");

            string tsTestFile = @"
import { describe, test, expect } from '@jest/globals';

class TestClass {
    this_is_typescript_test_VERIFIES_that_typescript_parsing_works(): void {
        // TypeScript test implementation
        expect(true).toBe(true);
    }

    another_typescript_test_VERIFIES_typescript_method_extraction(): boolean {
        // Another TypeScript test
        return true;
    }

    // This should be ignored - doesn't match pattern
    regularTypescriptMethod(): void {
        // Not matching
    }
}

function standalone_ts_function_VERIFIES_standalone_ts_functions_work(): void {
    // Standalone TypeScript function test
}
";

            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFilePath(@"c:\test\UnitTestFNPlugin.toml"), new MockFileData(configFile.ToString()) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\typescript.test.ts"), new MockFileData(tsTestFile) },
            });

            var testConfiguration = Substitute.For<IConfiguration>();
            testConfiguration.PluginConfigDir.Returns(TestingHelpers.ConvertFilePath(@"c:\test\"));
            testConfiguration.ProjectRoot.Returns(TestingHelpers.ConvertFilePath(@"c:\temp\"));
            testConfiguration.CommandLineOptionOrDefault(Arg.Any<string>(), Arg.Any<string>())
                .ReturnsForAnyArgs(callInfo => callInfo.ArgAt<string>(1));

            var temp = new UnitTestFNPlugin(new LocalFileSystemPlugin(testFileSystem));
            temp.InitializePlugin(testConfiguration);
            temp.RefreshItems();

            var tests = temp.GetUnitTests().ToArray();

            Assert.That(tests.Length == 3);
            Assert.That(tests[0].UnitTestPurpose == "this is typescript test");
            Assert.That(tests[0].UnitTestAcceptanceCriteria == "that typescript parsing works");
            Assert.That(tests[0].UnitTestFileName == "typescript.test.ts");
            Assert.That(tests[1].UnitTestPurpose == "another typescript test");
            Assert.That(tests[1].UnitTestAcceptanceCriteria == "typescript method extraction");
            Assert.That(tests[2].UnitTestPurpose == "standalone ts function");
            Assert.That(tests[2].UnitTestAcceptanceCriteria == "standalone ts functions work");
        }

        [UnitTestAttribute(
        Purpose = "UnitTestFNPlugin processes JavaScript methods correctly with TreeSitter",
        Identifier = "c0e075b4-6408-4445-9842-036de8e3d430",
        PostCondition = "JavaScript unit test methods are correctly extracted and parsed")]
        [Test]
        public void TestUnitTestFNPlugin_JavaScript_ExtractTests()
        {
            StringBuilder configFile = new StringBuilder();
            configFile.Append($@"
[[TestConfigurations]]
Language = ""javascript""
TestDirectory = ""{TestingHelpers.ConvertFilePath(@"c:/temp")}""
SubDirs = true
FileMasks = [""*.test.js""]
Project = ""RoboClerk""
FunctionMask = ""<PURPOSE>_VERIFIES_<POSTCONDITION>""
SectionSeparator = ""_""

UseGit = false
");

            string jsTestFile = @"
const { describe, test, expect } = require('@jest/globals');

class TestClass {
    this_is_javascript_test_VERIFIES_that_javascript_parsing_works() {
        // JavaScript test implementation
        expect(true).toBe(true);
    }

    another_javascript_test_VERIFIES_javascript_method_extraction() {
        // Another JavaScript test
        return true;
    }

    // This should be ignored - doesn't match pattern
    regularJavascriptMethod() {
        // Not matching
    }
}

function standalone_js_function_VERIFIES_standalone_js_functions_work() {
    // Standalone JavaScript function test
}
";

            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFilePath(@"c:\test\UnitTestFNPlugin.toml"), new MockFileData(configFile.ToString()) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\javascript.test.js"), new MockFileData(jsTestFile) },
            });

            var testConfiguration = Substitute.For<IConfiguration>();
            testConfiguration.PluginConfigDir.Returns(TestingHelpers.ConvertFilePath(@"c:\test\"));
            testConfiguration.ProjectRoot.Returns(TestingHelpers.ConvertFilePath(@"c:\temp\"));
            testConfiguration.CommandLineOptionOrDefault(Arg.Any<string>(), Arg.Any<string>())
                .ReturnsForAnyArgs(callInfo => callInfo.ArgAt<string>(1));

            var temp = new UnitTestFNPlugin(new LocalFileSystemPlugin(testFileSystem));
            temp.InitializePlugin(testConfiguration);
            temp.RefreshItems();

            var tests = temp.GetUnitTests().ToArray();

            Assert.That(tests.Length == 3);
            Assert.That(tests[0].UnitTestPurpose == "this is javascript test");
            Assert.That(tests[0].UnitTestAcceptanceCriteria == "that javascript parsing works");
            Assert.That(tests[0].UnitTestFileName == "javascript.test.js");
            Assert.That(tests[1].UnitTestPurpose == "another javascript test");
            Assert.That(tests[1].UnitTestAcceptanceCriteria == "javascript method extraction");
            Assert.That(tests[2].UnitTestPurpose == "standalone js function");
            Assert.That(tests[2].UnitTestAcceptanceCriteria == "standalone js functions work");
        }

        [UnitTestAttribute(
        Purpose = "UnitTestFNPlugin handles complex function masks with IDENTIFIER and TRACEID elements",
        Identifier = "61d4f6a4-4691-404b-ac54-944299cd7924",
        PostCondition = "Complex function masks with all element types are correctly parsed")]
        [Test]
        public void TestUnitTestFNPlugin_ComplexFunctionMask()
        {
            StringBuilder configFile = new StringBuilder();
            configFile.Append($@"
[[TestConfigurations]]
Language = ""csharp""
TestDirectory = ""{TestingHelpers.ConvertFilePath(@"c:/temp")}""
SubDirs = true
FileMasks = [""Test*.cs""]
Project = ""RoboClerk""
FunctionMask = ""<IDENTIFIER>_<PURPOSE>_VERIFIES_<POSTCONDITION>_TRACE_<TRACEID>""
SectionSeparator = ""_""

UseGit = false
");

            string testFile = @"
class TestClass {
    public void ID123_This_is_complex_test_VERIFIES_that_complex_parsing_works_TRACE_REQ001()
    {
        // Complex test implementation
    }

    public void ID456_Another_complex_test_VERIFIES_another_complex_result_TRACE_REQ002()
    {
        // Another complex test
    }
}";
            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFilePath(@"c:\test\UnitTestFNPlugin.toml"), new MockFileData(configFile.ToString()) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\TestComplex.cs"), new MockFileData(testFile) },
            });

            var testConfiguration = Substitute.For<IConfiguration>();
            testConfiguration.PluginConfigDir.Returns(TestingHelpers.ConvertFilePath(@"c:\test\"));
            testConfiguration.ProjectRoot.Returns(TestingHelpers.ConvertFilePath(@"c:\temp\"));
            testConfiguration.CommandLineOptionOrDefault(Arg.Any<string>(), Arg.Any<string>())
                .ReturnsForAnyArgs(callInfo => callInfo.ArgAt<string>(1));

            var temp = new UnitTestFNPlugin(new LocalFileSystemPlugin(testFileSystem));
            temp.InitializePlugin(testConfiguration);
            temp.RefreshItems();

            var tests = temp.GetUnitTests().ToArray();

            Assert.That(tests.Length == 2);
            
            // First test
            Assert.That(tests[0].ItemID == "ID123");
            Assert.That(tests[0].UnitTestPurpose == "This is complex test");
            Assert.That(tests[0].UnitTestAcceptanceCriteria == "that complex parsing works");
            Assert.That(tests[0].UnitTestFileName == "TestComplex.cs");
            
            // Second test
            Assert.That(tests[1].ItemID == "ID456");
            Assert.That(tests[1].UnitTestPurpose == "Another complex test");
            Assert.That(tests[1].UnitTestAcceptanceCriteria == "another complex result");
        }

        [UnitTestAttribute(
        Purpose = "UnitTestFNPlugin handles function masks with IGNORE elements correctly",
        Identifier = "9e2a5e54-a646-409e-8216-891ec32fa9fc",
        PostCondition = "Function masks with IGNORE elements skip specified parts correctly")]
        [Test]
        public void TestUnitTestFNPlugin_IgnoreElements()
        {
            StringBuilder configFile = new StringBuilder();
            configFile.Append($@"
[[TestConfigurations]]
Language = ""csharp""
TestDirectory = ""{TestingHelpers.ConvertFilePath(@"c:/temp")}""
SubDirs = true
FileMasks = [""Test*.cs""]
Project = ""RoboClerk""
FunctionMask = ""<PURPOSE>_IGNORE_<IGNORE>_VERIFIES_<POSTCONDITION>""
SectionSeparator = ""_""

UseGit = false
");

            string testFile = @"
class TestClass {
    public void This_is_test_purpose_IGNORE_IGNORED_PART_VERIFIES_that_ignore_works()
    {
        // Test with ignored part
    }

    public void Another_test_purpose_IGNORE_DIFFERENT_IGNORED_VERIFIES_another_result()
    {
        // Another test with different ignored part
    }
}";
            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFilePath(@"c:\test\UnitTestFNPlugin.toml"), new MockFileData(configFile.ToString()) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\TestIgnore.cs"), new MockFileData(testFile) },
            });

            var testConfiguration = Substitute.For<IConfiguration>();
            testConfiguration.PluginConfigDir.Returns(TestingHelpers.ConvertFilePath(@"c:\test\"));
            testConfiguration.ProjectRoot.Returns(TestingHelpers.ConvertFilePath(@"c:\temp\"));
            testConfiguration.CommandLineOptionOrDefault(Arg.Any<string>(), Arg.Any<string>())
                .ReturnsForAnyArgs(callInfo => callInfo.ArgAt<string>(1));

            var temp = new UnitTestFNPlugin(new LocalFileSystemPlugin(testFileSystem));
            temp.InitializePlugin(testConfiguration);
            temp.RefreshItems();

            var tests = temp.GetUnitTests().ToArray();

            Assert.That(tests.Length == 2);
            
            // First test
            Assert.That(tests[0].UnitTestPurpose == "This is test purpose");
            Assert.That(tests[0].UnitTestAcceptanceCriteria == "that ignore works");
            
            // Second test
            Assert.That(tests[1].UnitTestPurpose == "Another test purpose");
            Assert.That(tests[1].UnitTestAcceptanceCriteria == "another result");
        }

        [UnitTestAttribute(
        Purpose = "UnitTestFNPlugin handles different section separators for various languages",
        Identifier = "291cddda-281c-4a15-ad82-c989092906ff",
        PostCondition = "Different section separators work correctly for each language")]
        [Test]
        public void TestUnitTestFNPlugin_DifferentSeparators()
        {
            StringBuilder configFile = new StringBuilder();
            configFile.Append($@"
[[TestConfigurations]]
Language = ""python""
TestDirectory = ""{TestingHelpers.ConvertFilePath(@"c:/temp")}""
SubDirs = true
FileMasks = [""test_*.py""]
Project = ""RoboClerk""
FunctionMask = ""<PURPOSE>__VERIFIES__<POSTCONDITION>""
SectionSeparator = ""__""

UseGit = false
");

            string pythonTestFile = @"
def this__is__python__test__VERIFIES__that__double__underscore__separator__works():
    # Python test with double underscore separator
    pass

def another__python__test__VERIFIES__python__separator__handling():
    # Another Python test with custom separator
    pass
";

            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFilePath(@"c:\test\UnitTestFNPlugin.toml"), new MockFileData(configFile.ToString()) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\test_separator.py"), new MockFileData(pythonTestFile) },
            });

            var testConfiguration = Substitute.For<IConfiguration>();
            testConfiguration.PluginConfigDir.Returns(TestingHelpers.ConvertFilePath(@"c:\test\"));
            testConfiguration.ProjectRoot.Returns(TestingHelpers.ConvertFilePath(@"c:\temp\"));
            testConfiguration.CommandLineOptionOrDefault(Arg.Any<string>(), Arg.Any<string>())
                .ReturnsForAnyArgs(callInfo => callInfo.ArgAt<string>(1));

            var temp = new UnitTestFNPlugin(new LocalFileSystemPlugin(testFileSystem));
            temp.InitializePlugin(testConfiguration);
            temp.RefreshItems();

            var tests = temp.GetUnitTests().ToArray();

            Assert.That(tests.Length == 2);
            Assert.That(tests[0].UnitTestPurpose == "this is python test");
            Assert.That(tests[0].UnitTestAcceptanceCriteria == "that double underscore separator works");
            Assert.That(tests[1].UnitTestPurpose == "another python test");
            Assert.That(tests[1].UnitTestAcceptanceCriteria == "python separator handling");
        }

        #endregion Language-Specific Tests
    }
}