using NSubstitute;
using NUnit.Framework;
using RoboClerk.AnnotatedUnitTests;
using RoboClerk.Configuration;
using RoboClerk.ContentCreators;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("These tests test the annotated unit test plugin")]
    internal class TestAnnotatedUnitTestPlugin
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
AnnotationName = ""TestingAttribute""
Language = ""csharp""
[Purpose]
	Keyword = ""Purpose""
	Optional = false
[PostCondition]
	Keyword = ""PostCondition""
	Optional = false
[Identifier]
	Keyword = ""Identifier""
	Optional = true
[TraceID]
	Keyword = ""TraceID""
	Optional = true
");
            string testFile = @"
class TestClass {
 [TestingAttribute(
            Identifier = ""9A3258CF-F9EE-4A1A-95E6-B49EF25FB200"",
            Purpose = ""RoboClerk Processes the media directory including subdirs, output media directory exists including subdirs"",
            PostCondition = ""Media directory is deleted, recreated and files are copied (except .gitignore)"")]
        [Test]
        public void CheckMediaDirectory()
        {
	    }

[TestingAttribute(
        Identifier = ""5D8F1310-1D33-49C1-93C9-0072428EF215"",
        Purpose = ""SourceCodeAnalysisPlugin is created and initialized, subdirs set to false"",
        PostCondition = ""Expected files found"")]
        [Test]
        public void TestSourceCodeAnalysisPlugin3()
        {
        }}";
            fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\test\AnnotatedUnitTestPlugin.toml"), new MockFileData(configFile.ToString()) },
                { TestingHelpers.ConvertFileName(@"c:\temp\TestDummy.cs"), new MockFileData(testFile) },
            });
            configuration = Substitute.For<IConfiguration>();
            configuration.PluginConfigDir.Returns(TestingHelpers.ConvertFileName(@"c:/test/"));
            configuration.ProjectRoot.Returns(TestingHelpers.ConvertFileName(@"c:/temp/"));
            configuration.CommandLineOptionOrDefault(Arg.Any<string>(), Arg.Any<string>())
                .ReturnsForAnyArgs(callInfo => callInfo.ArgAt<string>(1));
        }

        [UnitTestAttribute(
        Purpose = "AnnotatedUnitTestPlugin is created",
        Identifier = "1C2B7995-ABAB-466B-96D8-B8165EDC28C8",
        PostCondition = "No exception is thrown")]
        [Test]
        public void TestUnitTestPlugin1()
        {
            var temp = new AnnotatedUnitTestPlugin(fileSystem);
            temp.InitializePlugin(configuration);
        }

        [UnitTestAttribute(
        Purpose = "AnnotatedUnitTestPlugin is created, refresh is called",
        Identifier = "1C2B7995-DFDF-466B-96D8-B8165VFC28C8",
        PostCondition = "The appropriate unit test information is extracted")]
        [Test]
        public void TestUnitTestPlugin2()
        {
            var temp = new AnnotatedUnitTestPlugin(fileSystem);
            temp.InitializePlugin(configuration);
            temp.RefreshItems();

            var tests = temp.GetUnitTests().ToArray();

            Assert.That(tests.Length == 2);
            Assert.That(tests[0].ItemID == "9A3258CF-F9EE-4A1A-95E6-B49EF25FB200");
            Assert.That(tests[1].ItemID == "5D8F1310-1D33-49C1-93C9-0072428EF215");
            Assert.That(tests[0].UnitTestPurpose == "RoboClerk Processes the media directory including subdirs, output media directory exists including subdirs");
            Assert.That(tests[1].UnitTestPurpose == "SourceCodeAnalysisPlugin is created and initialized, subdirs set to false");
            Assert.That(tests[0].UnitTestAcceptanceCriteria == "Media directory is deleted, recreated and files are copied (except .gitignore)");
            Assert.That(tests[1].UnitTestAcceptanceCriteria == "Expected files found");
        }

        [UnitTestAttribute(
            Identifier = "1e746ccd-db14-4b0d-b3f6-00109385f3d2",
            Purpose = "AnnotatedUnitTestPlugin handles missing configuration file gracefully",
            PostCondition = "Exception is thrown with appropriate error message")]
        [Test]
        public void TestUnitTestPlugin_MissingConfigFile()
        {
            var fileSystemWithoutConfig = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\temp\TestDummy.cs"), new MockFileData("test content") },
            });

            var temp = new AnnotatedUnitTestPlugin(fileSystemWithoutConfig);
            
            var ex = Assert.Throws<Exception>(() => temp.InitializePlugin(configuration));
            Assert.That(ex.Message.Contains("could not read its configuration"));
        }

        [UnitTestAttribute(
            Identifier = "a04fac6d-e67d-47a7-a024-a69cb926386d",
            Purpose = "AnnotatedUnitTestPlugin handles malformed configuration file",
            PostCondition = "Exception is thrown when required configuration sections are missing")]
        [Test]
        public void TestUnitTestPlugin_MalformedConfig()
        {
            string malformedConfig = @"
TestDirectories = [""c:/temp""]
SubDirs = true
FileMasks = [""Test*.cs""]
UseGit = false
AnnotationName = ""TestingAttribute""
Language = ""csharp""
# Missing Purpose section deliberately
[PostCondition]
	Keyword = ""PostCondition""
	Optional = false
";
            var fileSystemWithMalformedConfig = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\test\AnnotatedUnitTestPlugin.toml"), new MockFileData(malformedConfig) },
                { TestingHelpers.ConvertFileName(@"c:\temp\TestDummy.cs"), new MockFileData("test content") },
            });

            var temp = new AnnotatedUnitTestPlugin(fileSystemWithMalformedConfig);
            
            var ex = Assert.Throws<Exception>(() => temp.InitializePlugin(configuration));
            Assert.That(ex.Message.Contains("could not read its"));
        }

        [UnitTestAttribute(
            Identifier = "6d06e760-3f8d-482a-a7a5-4b2bc502eb39",
            Purpose = "AnnotatedUnitTestPlugin processes file with no unit test attributes",
            PostCondition = "No unit tests are extracted from files without proper attributes")]
        [Test]
        public void TestUnitTestPlugin_NoAttributesInFile()
        {
            string fileWithoutAttributes = @"
        [Test]
        public void SomeTestMethod()
        {
            // No TestingAttribute here
        }
        
        public void NotATestMethod()
        {
            // Regular method
        }";

            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\test\AnnotatedUnitTestPlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFileName(@"c:\test\AnnotatedUnitTestPlugin.toml")) },
                { TestingHelpers.ConvertFileName(@"c:\temp\TestEmpty.cs"), new MockFileData(fileWithoutAttributes) },
            });

            var temp = new AnnotatedUnitTestPlugin(testFileSystem);
            temp.InitializePlugin(configuration);
            temp.RefreshItems();

            var tests = temp.GetUnitTests().ToArray();
            Assert.That(tests.Length == 0);
        }

        [UnitTestAttribute(
            Identifier = "f2ff4458-b8f7-4773-bdba-26f97c217327",
            Purpose = "AnnotatedUnitTestPlugin processes files with mixed valid and invalid attributes",
            PostCondition = "Only valid unit test attributes are extracted")]
        [Test]
        public void TestUnitTestPlugin_MixedValidInvalidAttributes()
        {
            string fileWithMixedAttributes = @"
class TestClass {
[TestingAttribute(
    Identifier = ""VALID-TEST-ID-1"",
    Purpose = ""This is a valid test"",
    PostCondition = ""Valid result expected"")]
[Test]
public void ValidTestMethod()
{
}

[SomeOtherAttribute(
    Identifier = ""INVALID-TEST-ID-1"",
    Purpose = ""This should be ignored"")]
[Test]
public void InvalidTestMethod()
{
}

[TestingAttribute(
    Identifier = ""VALID-TEST-ID-2"",
    Purpose = ""Another valid test"",
    PostCondition = ""Another valid result"")]
[Test]
public void AnotherValidTestMethod()
{
}}";

            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\test\AnnotatedUnitTestPlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFileName(@"c:\test\AnnotatedUnitTestPlugin.toml")) },
                { TestingHelpers.ConvertFileName(@"c:\temp\TestMixed.cs"), new MockFileData(fileWithMixedAttributes) },
            });

            var temp = new AnnotatedUnitTestPlugin(testFileSystem);
            temp.InitializePlugin(configuration);
            temp.RefreshItems();

            var tests = temp.GetUnitTests().ToArray();
            Assert.That(tests.Length == 2);
            Assert.That(tests[0].ItemID == "VALID-TEST-ID-1");
            Assert.That(tests[1].ItemID == "VALID-TEST-ID-2");
            Assert.That(tests[0].UnitTestPurpose == "This is a valid test");
            Assert.That(tests[1].UnitTestPurpose == "Another valid test");
        }

        [UnitTestAttribute(
            Identifier = "8b4425cc-e83a-433d-bfca-54d70ec1145b",
            Purpose = "AnnotatedUnitTestPlugin handles attributes with missing optional fields",
            PostCondition = "Unit tests are extracted even when optional fields are missing")]
        [Test]
        public void TestUnitTestPlugin_MissingOptionalFields()
        {
            string fileWithMissingOptionalFields = @"
class TestClass {
[TestingAttribute(
    Purpose = ""Test with missing identifier"",
    PostCondition = ""Should still work"")]
[Test]
public void TestWithoutIdentifier()
{
}

[TestingAttribute(
    Identifier = ""TEST-WITH-TRACE-ID"",
    Purpose = ""Test with trace ID"",
    PostCondition = ""Should work with trace"",
    TraceID = ""TRACE-123"")]
[Test]
public void TestWithTraceID()
{
}}";

            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\test\AnnotatedUnitTestPlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFileName(@"c:\test\AnnotatedUnitTestPlugin.toml")) },
                { TestingHelpers.ConvertFileName(@"c:\temp\TestOptionalFields.cs"), new MockFileData(fileWithMissingOptionalFields) },
            });

            var temp = new AnnotatedUnitTestPlugin(testFileSystem);
            temp.InitializePlugin(configuration);
            temp.RefreshItems();

            var tests = temp.GetUnitTests().ToArray();
            Assert.That(tests.Length == 2);
            
            // Test without identifier should get auto-generated ID
            Assert.That(!string.IsNullOrEmpty(tests[0].ItemID));
            Assert.That(tests[0].UnitTestPurpose == "Test with missing identifier");
            
            // Test with identifier should use provided ID
            Assert.That(tests[1].ItemID == "TEST-WITH-TRACE-ID");
            Assert.That(tests[1].UnitTestPurpose == "Test with trace ID");
        }

        [UnitTestAttribute(
            Identifier = "fbd68b90-f467-4781-a8e9-f162b2dd28c8",
            Purpose = "AnnotatedUnitTestPlugin handles attributes with quoted strings containing special characters",
            PostCondition = "Special characters in quoted strings are properly handled")]
        [Test]
        public void TestUnitTestPlugin_QuotedStringsWithSpecialCharacters()
        {
            string fileWithSpecialChars = @"
class TestClass {
[TestingAttribute(
    Identifier = ""SPECIAL-CHARS-TEST"",
    Purpose = ""Test with \""quotes\"" and newlines\nand tabs\t"",
    PostCondition = ""Should handle special chars properly"")]
[Test]
public void TestWithSpecialCharacters()
{
}}";

            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\test\AnnotatedUnitTestPlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFileName(@"c:\test\AnnotatedUnitTestPlugin.toml")) },
                { TestingHelpers.ConvertFileName(@"c:\temp\TestSpecialChars.cs"), new MockFileData(fileWithSpecialChars) },
            });

            var temp = new AnnotatedUnitTestPlugin(testFileSystem);
            temp.InitializePlugin(configuration);
            temp.RefreshItems();

            var tests = temp.GetUnitTests().ToArray();
            Assert.That(tests.Length == 1);
            Assert.That(tests[0].ItemID == "SPECIAL-CHARS-TEST");
            Assert.That(tests[0].UnitTestPurpose.Contains("quotes"));
            Assert.That(tests[0].UnitTestPurpose.Contains("\n"));
            Assert.That(tests[0].UnitTestPurpose.Contains("\t"));
        }

        [UnitTestAttribute(
            Identifier = "39c20bca-06ee-47ac-b2d1-c05da1537f99",
            Purpose = "AnnotatedUnitTestPlugin detects duplicate unit test identifiers",
            PostCondition = "Exception is thrown when duplicate identifiers are found")]
        [Test]
        public void TestUnitTestPlugin_DuplicateIdentifiers()
        {
            string fileWithDuplicates = @"
class TestClass {
[TestingAttribute(
    Identifier = ""DUPLICATE-ID"",
    Purpose = ""First test with duplicate ID"",
    PostCondition = ""Should cause error"")]
[Test]
public void FirstTestWithDuplicateId()
{
}

[TestingAttribute(
    Identifier = ""DUPLICATE-ID"",
    Purpose = ""Second test with same ID"",
    PostCondition = ""Should cause error"")]
[Test]
public void SecondTestWithDuplicateId()
{
}}";

            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\test\AnnotatedUnitTestPlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFileName(@"c:\test\AnnotatedUnitTestPlugin.toml")) },
                { TestingHelpers.ConvertFileName(@"c:\temp\TestDuplicates.cs"), new MockFileData(fileWithDuplicates) },
            });

            var temp = new AnnotatedUnitTestPlugin(testFileSystem);
            temp.InitializePlugin(configuration);
            
            var ex = Assert.Throws<Exception>(() => temp.RefreshItems());
            Assert.That(ex.Message.Contains("Duplicate unit test identifier"));
        }

        [UnitTestAttribute(
            Identifier = "7c23f883-ee8a-4b86-926e-22c9e0565ca5",
            Purpose = "AnnotatedUnitTestPlugin throws exception when a single required field is missing",
            PostCondition = "Exception is thrown with specific field name when only Purpose field is missing")]
        [Test]
        public void TestUnitTestPlugin_SingleMissingRequiredField()
        {
            string fileWithSingleMissingField = @"
class TestClass {
[TestingAttribute(
    Identifier = ""SINGLE-MISSING-FIELD-TEST"",
    PostCondition = ""PostCondition is present but Purpose is missing"")]
[Test]
public void TestWithoutPurpose()
{
}}";

            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\test\AnnotatedUnitTestPlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFileName(@"c:\test\AnnotatedUnitTestPlugin.toml")) },
                { TestingHelpers.ConvertFileName(@"c:\temp\TestSingleMissing.cs"), new MockFileData(fileWithSingleMissingField) },
            });

            var temp = new AnnotatedUnitTestPlugin(testFileSystem);
            temp.InitializePlugin(configuration);
            
            // The plugin should throw an exception specifically mentioning the missing Purpose field
            var ex = Assert.Throws<Exception>(() => temp.RefreshItems());
            Assert.That(ex.Message.Contains("Required field(s) missing"));
            Assert.That(ex.Message.Contains("Purpose"), "Exception should specifically mention 'Purpose' as the missing field");
            Assert.That(ex.Message.Contains("TestWithoutPurpose"), "Exception should mention the method name");
            Assert.That(!ex.Message.Contains("PostCondition"), "Exception should not mention PostCondition since it's provided");
        }

        [UnitTestAttribute(
            Identifier = "d782c973-135d-4120-a83f-841a9cdada8b",
            Purpose = "AnnotatedUnitTestPlugin supports different C# attribute syntax variations",
            PostCondition = "Various C# attribute syntax forms are correctly parsed")]
        [Test]
        public void TestUnitTestPlugin_AttributeSyntaxVariations()
        {
            string fileWithSyntaxVariations = @"
class TestClass {
// Different attribute syntax variations
[TestingAttribute(Identifier = ""SYNTAX-1"", Purpose = ""Single line"", PostCondition = ""Works"")]
[Test]
public void SingleLineAttribute() { }

[TestingAttribute(
    Identifier = ""SYNTAX-2"",
    Purpose = ""Multi-line with indentation"",
    PostCondition = ""Also works""
)]
[Test]
public void MultiLineAttribute() { }

[TestingAttribute(
Identifier=""SYNTAX-3"",Purpose=""No spaces"",PostCondition=""Still works""
)]
[Test]
public void NoSpacesAttribute() { }

[TestingAttribute(
    Identifier = ""SYNTAX-4"", 
    Purpose = @""Verbatim string literal"", 
    PostCondition = @""With @ symbol""
)]
[Test]
public void VerbatimStringAttribute() { }}";

            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\test\AnnotatedUnitTestPlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFileName(@"c:\test\AnnotatedUnitTestPlugin.toml")) },
                { TestingHelpers.ConvertFileName(@"c:\temp\TestSyntax.cs"), new MockFileData(fileWithSyntaxVariations) },
            });

            var temp = new AnnotatedUnitTestPlugin(testFileSystem);
            temp.InitializePlugin(configuration);
            temp.RefreshItems();

            var tests = temp.GetUnitTests().ToArray();
            Assert.That(tests.Length == 4);
            
            var sortedTests = tests.OrderBy(t => t.ItemID).ToArray();
            Assert.That(sortedTests[0].ItemID == "SYNTAX-1");
            Assert.That(sortedTests[1].ItemID == "SYNTAX-2");
            Assert.That(sortedTests[2].ItemID == "SYNTAX-3");
            Assert.That(sortedTests[3].ItemID == "SYNTAX-4");
            
            Assert.That(sortedTests[0].UnitTestPurpose == "Single line");
            Assert.That(sortedTests[1].UnitTestPurpose == "Multi-line with indentation");
            Assert.That(sortedTests[2].UnitTestPurpose == "No spaces");
            Assert.That(sortedTests[3].UnitTestPurpose == "Verbatim string literal");
        }

        [UnitTestAttribute(
            Identifier = "80b27518-fb4d-4e1f-9d29-7fb4bae015ed",
            Purpose = "AnnotatedUnitTestPlugin throws exception when required fields are missing from attributes",
            PostCondition = "Exception is thrown when Purpose or PostCondition fields are missing")]
        [Test]
        public void TestUnitTestPlugin_MissingRequiredFields()
        {
            string fileWithMissingRequiredFields = @"
class TestClass {
[TestingAttribute(
    Identifier = ""MISSING-PURPOSE-TEST"")]
[Test]
public void TestWithoutPurpose()
{
}

[TestingAttribute(
    Identifier = ""MISSING-POSTCONDITION-TEST"",
    Purpose = ""Test with missing PostCondition"")]
[Test]
public void TestWithoutPostCondition()
{
}}";

            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\test\AnnotatedUnitTestPlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFileName(@"c:\test\AnnotatedUnitTestPlugin.toml")) },
                { TestingHelpers.ConvertFileName(@"c:\temp\TestMissingRequired.cs"), new MockFileData(fileWithMissingRequiredFields) },
            });

            var temp = new AnnotatedUnitTestPlugin(testFileSystem);
            temp.InitializePlugin(configuration);
            
            // The plugin should now throw an exception when processing attributes with missing required fields
            var ex = Assert.Throws<Exception>(() => temp.RefreshItems());
            Assert.That(ex.Message.Contains("Required field(s) missing"));
            Assert.That(ex.Message.Contains("TestingAttribute"));
            Assert.That(ex.Message.Contains("Purpose") || ex.Message.Contains("PostCondition"), 
                "Exception message should mention the missing required field(s)");
        }

        [UnitTestAttribute(
            Identifier = "3f70f6d7-621a-4837-aac3-b8c46218a466",
            Purpose = "AnnotatedUnitTestPlugin correctly sets file names and function names",
            PostCondition = "Unit test items contain correct file and function names")]
        [Test]
        public void TestUnitTestPlugin_FileAndFunctionNames()
        {
            string testContent = @"
class TestClass {
[TestingAttribute(
    Identifier = ""FILE-FUNC-TEST"",
    Purpose = ""Test file and function name extraction"",
    PostCondition = ""Names are correctly captured"")]
[Test]
public void MySpecificTestMethodName()
{
}}";

            var testFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\test\AnnotatedUnitTestPlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFileName(@"c:\test\AnnotatedUnitTestPlugin.toml")) },
                { TestingHelpers.ConvertFileName(@"c:\temp\TestSpecificFile.cs"), new MockFileData(testContent) },
            });

            var temp = new AnnotatedUnitTestPlugin(testFileSystem);
            temp.InitializePlugin(configuration);
            temp.RefreshItems();

            var tests = temp.GetUnitTests().ToArray();
            Assert.That(tests.Length == 1);
            Assert.That(tests[0].UnitTestFunctionName == "MySpecificTestMethodName");
            Assert.That(tests[0].UnitTestFileName == "TestSpecificFile.cs");
        }
    }
}
