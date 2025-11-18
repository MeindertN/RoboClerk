using NSubstitute;
using NUnit.Framework;
using RoboClerk.AnnotatedUnitTests;
using RoboClerk.Core.Configuration;
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
        private LocalFileSystemPlugin fileProviderPlugin = null;
        private IConfiguration configuration = null;

        [SetUp]
        public void TestSetup()
        {
            string configFile = ($@"
UseGit = false
[[TestConfigurations]]
SubDirs = true
FileMasks = [""Test*.cs""]
TestDirectory = ""{TestingHelpers.ConvertFilePath(@"c:/temp")}""
AnnotationName = ""TestingAttribute""
Language = ""csharp""
Project = ""RoboClerk""

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
                { TestingHelpers.ConvertFilePath(@"c:\test\AnnotatedUnitTestPlugin.toml"), new MockFileData(configFile.ToString()) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\TestDummy.cs"), new MockFileData(testFile) },
            });
            fileProviderPlugin = new LocalFileSystemPlugin(fileSystem);
            configuration = Substitute.For<IConfiguration>();
            configuration.PluginConfigDir.Returns(TestingHelpers.ConvertFilePath(@"c:/test/"));
            configuration.ProjectRoot.Returns(TestingHelpers.ConvertFilePath(@"c:/temp/"));
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
            var temp = new AnnotatedUnitTestPlugin(fileProviderPlugin);
            temp.InitializePlugin(configuration);
        }

        #region C# Language Support Tests

        [UnitTestAttribute(
        Purpose = "AnnotatedUnitTestPlugin is created, refresh is called",
        Identifier = "1C2B7995-DFDF-466B-96D8-B8165VFC28C8",
        PostCondition = "The appropriate unit test information is extracted")]
        [Test]
        public void TestUnitTestPlugin2()
        {
            var temp = new AnnotatedUnitTestPlugin(fileProviderPlugin);
          
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
                { TestingHelpers.ConvertFilePath(@"c:\temp\TestDummy.cs"), new MockFileData("test content") },
            });

            var temp = new AnnotatedUnitTestPlugin(new LocalFileSystemPlugin(fileSystemWithoutConfig));

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
                { TestingHelpers.ConvertFilePath(@"c:\test\AnnotatedUnitTestPlugin.toml"), new MockFileData(malformedConfig) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\TestDummy.cs"), new MockFileData("test content") },
            });

            var temp = new AnnotatedUnitTestPlugin(new LocalFileSystemPlugin(fileSystemWithMalformedConfig));

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
                { TestingHelpers.ConvertFilePath(@"c:\test\AnnotatedUnitTestPlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFilePath(@"c:\test\AnnotatedUnitTestPlugin.toml")) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\TestEmpty.cs"), new MockFileData(fileWithoutAttributes) },
            });

            var temp = new AnnotatedUnitTestPlugin(new LocalFileSystemPlugin(testFileSystem));
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
                { TestingHelpers.ConvertFilePath(@"c:\test\AnnotatedUnitTestPlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFilePath(@"c:\test\AnnotatedUnitTestPlugin.toml")) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\TestMixed.cs"), new MockFileData(fileWithMixedAttributes) },
            });

            var temp = new AnnotatedUnitTestPlugin(new LocalFileSystemPlugin(testFileSystem));
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
                { TestingHelpers.ConvertFilePath(@"c:\test\AnnotatedUnitTestPlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFilePath(@"c:\test\AnnotatedUnitTestPlugin.toml")) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\TestOptionalFields.cs"), new MockFileData(fileWithMissingOptionalFields) },
            });

            var temp = new AnnotatedUnitTestPlugin(new LocalFileSystemPlugin(testFileSystem));
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
                { TestingHelpers.ConvertFilePath(@"c:\test\AnnotatedUnitTestPlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFilePath(@"c:\test\AnnotatedUnitTestPlugin.toml")) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\TestSpecialChars.cs"), new MockFileData(fileWithSpecialChars) },
            });

            var temp = new AnnotatedUnitTestPlugin(new LocalFileSystemPlugin(testFileSystem));
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
                { TestingHelpers.ConvertFilePath(@"c:\test\AnnotatedUnitTestPlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFilePath(@"c:\test\AnnotatedUnitTestPlugin.toml")) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\TestDuplicates.cs"), new MockFileData(fileWithDuplicates) },
            });

            var temp = new AnnotatedUnitTestPlugin(new LocalFileSystemPlugin(testFileSystem));
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
                { TestingHelpers.ConvertFilePath(@"c:\test\AnnotatedUnitTestPlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFilePath(@"c:\test\AnnotatedUnitTestPlugin.toml")) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\TestSingleMissing.cs"), new MockFileData(fileWithSingleMissingField) },
            });

            var temp = new AnnotatedUnitTestPlugin(new LocalFileSystemPlugin(testFileSystem));
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
                { TestingHelpers.ConvertFilePath(@"c:\test\AnnotatedUnitTestPlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFilePath(@"c:\test\AnnotatedUnitTestPlugin.toml")) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\TestSyntax.cs"), new MockFileData(fileWithSyntaxVariations) },
            });

            var temp = new AnnotatedUnitTestPlugin(new LocalFileSystemPlugin(testFileSystem));
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
                { TestingHelpers.ConvertFilePath(@"c:\test\AnnotatedUnitTestPlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFilePath(@"c:\test\AnnotatedUnitTestPlugin.toml")) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\TestMissingRequired.cs"), new MockFileData(fileWithMissingRequiredFields) },
            });

            var temp = new AnnotatedUnitTestPlugin(new LocalFileSystemPlugin(testFileSystem));
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
                { TestingHelpers.ConvertFilePath(@"c:\test\AnnotatedUnitTestPlugin.toml"), fileSystem.File.ReadAllText(TestingHelpers.ConvertFilePath(@"c:\test\AnnotatedUnitTestPlugin.toml")) },
                { TestingHelpers.ConvertFilePath(@"c:\temp\TestSpecificFile.cs"), new MockFileData(testContent) },
            });

            var temp = new AnnotatedUnitTestPlugin(new LocalFileSystemPlugin(testFileSystem));
            temp.InitializePlugin(configuration);
            temp.RefreshItems();

            var tests = temp.GetUnitTests().ToArray();
            Assert.That(tests.Length == 1);
            Assert.That(tests[0].UnitTestFunctionName == "MySpecificTestMethodName");
            Assert.That(tests[0].UnitTestFileName == "TestSpecificFile.cs");
        }
        #endregion C# Language Support Tests

        #region Java Language Support Tests

        private IFileSystem CreateJavaTestFileSystem()
        {
            string javaConfigFile = ($@"
UseGit = false
[[TestConfigurations]]
SubDirs = true
FileMasks = [""Test*.java""]
TestDirectory = ""{TestingHelpers.ConvertFilePath(@"c:/temp")}""
AnnotationName = ""TestAnnotation""
Language = ""java""
Project = ""RoboClerk""

[Purpose]
	Keyword = ""purpose""
	Optional = false
[PostCondition]
	Keyword = ""expected""
	Optional = false
[Identifier]
	Keyword = ""id""
	Optional = true
[TraceID]
	Keyword = ""traceId""
	Optional = true
");

            return new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFilePath(@"c:\test\AnnotatedUnitTestPlugin.toml"), new MockFileData(javaConfigFile) }
            });
        }

        [UnitTestAttribute(
            Identifier = "c67911a0-07d3-41af-bdca-0426059af4da",
            Purpose = "AnnotatedUnitTestPlugin processes basic Java annotations with named parameters",
            PostCondition = "Java unit test annotations are correctly parsed and extracted")]
        [Test]
        public void TestUnitTestPlugin_Java_BasicAnnotations()
        {
            string javaCode = @"
public class TestClass {
    @TestAnnotation(
        id = ""JAVA-TEST-001"",
        purpose = ""Test basic Java annotation parsing"",
        expected = ""Java annotations should be parsed correctly""
    )
    @Test
    public void testBasicJavaMethod() {
        // Test implementation
    }

    @TestAnnotation(
        id = ""JAVA-TEST-002"",
        purpose = ""Test another Java method"",
        expected = ""Second test should also work""
    )
    @Test
    public void testAnotherJavaMethod() {
        // Another test
    }
}";

            var testFileSystem = CreateJavaTestFileSystem();
            var mockFileSystem = testFileSystem as MockFileSystem;
            mockFileSystem.AddFile(TestingHelpers.ConvertFilePath(@"c:\temp\TestJavaBasic.java"), new MockFileData(javaCode));

            var temp = new AnnotatedUnitTestPlugin(new LocalFileSystemPlugin(testFileSystem));
            temp.InitializePlugin(configuration);
            temp.RefreshItems();

            var tests = temp.GetUnitTests().ToArray();
            Assert.That(tests.Length == 2);
            Assert.That(tests[0].ItemID == "JAVA-TEST-001");
            Assert.That(tests[1].ItemID == "JAVA-TEST-002");
            Assert.That(tests[0].UnitTestPurpose == "Test basic Java annotation parsing");
            Assert.That(tests[1].UnitTestPurpose == "Test another Java method");
            Assert.That(tests[0].UnitTestAcceptanceCriteria == "Java annotations should be parsed correctly");
            Assert.That(tests[1].UnitTestAcceptanceCriteria == "Second test should also work");
        }

        [UnitTestAttribute(
            Identifier = "464b8b4f-cd7e-40b5-8220-c0c769ad9bf9",
            Purpose = "AnnotatedUnitTestPlugin handles Java marker annotations without parameters",
            PostCondition = "Marker annotations are processed but cause validation errors for missing required fields")]
        [Test]
        public void TestUnitTestPlugin_Java_MarkerAnnotations()
        {
            string javaCode = @"
public class TestClass {
    @TestAnnotation
    @Test
    public void testMarkerAnnotation() {
        // Test with marker annotation
    }

    @TestAnnotation(
        purpose = ""Test with partial annotation"",
        expected = ""Should work with required fields""
    )
    @Test
    public void testPartialAnnotation() {
        // Test with some fields
    }
}";

            var testFileSystem = CreateJavaTestFileSystem();
            var mockFileSystem = testFileSystem as MockFileSystem;
            mockFileSystem.AddFile(TestingHelpers.ConvertFilePath(@"c:\temp\TestJavaMarker.java"), new MockFileData(javaCode));

            var temp = new AnnotatedUnitTestPlugin(new LocalFileSystemPlugin(testFileSystem));
            temp.InitializePlugin(configuration);

            // Should throw exception due to missing required fields in marker annotation
            var ex = Assert.Throws<Exception>(() => temp.RefreshItems());
            Assert.That(ex.Message.Contains("Required field(s) missing"));
        }

        [UnitTestAttribute(
            Identifier = "a185e1a9-1c05-4fd3-b39f-d8cc74cb7a38",
            Purpose = "AnnotatedUnitTestPlugin handles Java annotations with various string literal formats",
            PostCondition = "Different Java string formats are correctly processed")]
        [Test]
        public void TestUnitTestPlugin_Java_StringLiterals()
        {
            string javaCode = @"
public class TestClass {
    @TestAnnotation(
        id = ""STRING-LITERAL-TEST"",
        purpose = ""Test with \""escaped quotes\"" and newlines\n\t"",
        expected = ""Should handle various string formats""
    )
    @Test
    public void testStringLiterals() {
        // Test string literal handling
    }

    @TestAnnotation(
        id = ""NO-SPACES-TEST"",
        purpose = ""Test without spaces"",
        expected = ""Should work without spaces""
    )
    @Test
    public void testNoSpaces() {
        // Test without spaces
    }
}";

            var testFileSystem = CreateJavaTestFileSystem();
            var mockFileSystem = testFileSystem as MockFileSystem;
            mockFileSystem.AddFile(TestingHelpers.ConvertFilePath(@"c:\temp\TestJavaStrings.java"), new MockFileData(javaCode));

            var temp = new AnnotatedUnitTestPlugin(new LocalFileSystemPlugin(testFileSystem));
            temp.InitializePlugin(configuration);
            temp.RefreshItems();

            var tests = temp.GetUnitTests().ToArray();
            Assert.That(tests.Length == 2);
            Assert.That(tests[0].ItemID == "STRING-LITERAL-TEST");
            Assert.That(tests[1].ItemID == "NO-SPACES-TEST");
            Assert.That(tests[0].UnitTestPurpose.Contains("escaped quotes"));
            Assert.That(tests[0].UnitTestPurpose.Contains("\n"));
            Assert.That(tests[0].UnitTestPurpose.Contains("\t"));
        }

        [UnitTestAttribute(
            Identifier = "cacbe83e-5e82-49d3-b23d-0bde93a3fbd7",
            Purpose = "AnnotatedUnitTestPlugin handles Java annotations in different method contexts",
            PostCondition = "Annotations on various Java method types are correctly processed")]
        [Test]
        public void TestUnitTestPlugin_Java_MethodContexts()
        {
            string javaCode = @"
public class TestClass {
    @TestAnnotation(
        id = ""PUBLIC-METHOD-TEST"",
        purpose = ""Test public method annotation"",
        expected = ""Public method should be processed""
    )
    @Test
    public void testPublicMethod() {
        // Public test method
    }

    @TestAnnotation(
        id = ""PRIVATE-METHOD-TEST"",
        purpose = ""Test private method annotation"",
        expected = ""Private method should be processed""
    )
    @Test
    private void testPrivateMethod() {
        // Private test method
    }

    @TestAnnotation(
        id = ""STATIC-METHOD-TEST"",
        purpose = ""Test static method annotation"",
        expected = ""Static method should be processed""
    )
    @Test
    public static void testStaticMethod() {
        // Static test method
    }

    @TestAnnotation(
        id = ""THROWS-METHOD-TEST"",
        purpose = ""Test method with throws clause"",
        expected = ""Method with throws should be processed""
    )
    @Test
    public void testMethodWithThrows() throws Exception {
        // Method with throws clause
    }
}";

            var testFileSystem = CreateJavaTestFileSystem();
            var mockFileSystem = testFileSystem as MockFileSystem;
            mockFileSystem.AddFile(TestingHelpers.ConvertFilePath(@"c:\temp\TestJavaContexts.java"), new MockFileData(javaCode));

            var temp = new AnnotatedUnitTestPlugin(new LocalFileSystemPlugin(testFileSystem));
            temp.InitializePlugin(configuration);
            temp.RefreshItems();

            var tests = temp.GetUnitTests().ToArray();
            Assert.That(tests.Length == 4);
            
            var testIds = tests.Select(t => t.ItemID).ToArray();
            Assert.That(testIds.Contains("PUBLIC-METHOD-TEST"));
            Assert.That(testIds.Contains("PRIVATE-METHOD-TEST"));
            Assert.That(testIds.Contains("STATIC-METHOD-TEST"));
            Assert.That(testIds.Contains("THROWS-METHOD-TEST"));
        }

        [UnitTestAttribute(
            Identifier = "a9e5c20e-e8c1-4916-a132-ad846314d5ca",
            Purpose = "AnnotatedUnitTestPlugin handles Java annotations with missing optional fields",
            PostCondition = "Java tests with missing optional fields are processed correctly")]
        [Test]
        public void TestUnitTestPlugin_Java_MissingOptionalFields()
        {
            string javaCode = @"
public class TestClass {
    @TestAnnotation(
        purpose = ""Test without ID field"",
        expected = ""Should work without optional ID""
    )
    @Test
    public void testWithoutId() {
        // Test without ID
    }

    @TestAnnotation(
        id = ""WITH-TRACE-TEST"",
        purpose = ""Test with trace ID"",
        expected = ""Should include trace information"",
        traceId = ""TRACE-456""
    )
    @Test
    public void testWithTraceId() {
        // Test with trace ID
    }
}";

            var testFileSystem = CreateJavaTestFileSystem();
            var mockFileSystem = testFileSystem as MockFileSystem;
            mockFileSystem.AddFile(TestingHelpers.ConvertFilePath(@"c:\temp\TestJavaOptional.java"), new MockFileData(javaCode));

            var temp = new AnnotatedUnitTestPlugin(new LocalFileSystemPlugin(testFileSystem));
            temp.InitializePlugin(configuration);
            temp.RefreshItems();

            var tests = temp.GetUnitTests().ToArray();
            Assert.That(tests.Length == 2);
            
            // First test should have auto-generated ID
            Assert.That(!string.IsNullOrEmpty(tests[0].ItemID));
            Assert.That(tests[0].UnitTestPurpose == "Test without ID field");
            
            // Second test should have provided ID
            Assert.That(tests[1].ItemID == "WITH-TRACE-TEST");
            Assert.That(tests[1].UnitTestPurpose == "Test with trace ID");
        }

        [UnitTestAttribute(
            Identifier = "4fa9d0f3-cd74-4983-9b94-ba64820b185b",
            Purpose = "AnnotatedUnitTestPlugin handles complex Java annotation scenarios",
            PostCondition = "Complex Java code structures with annotations are correctly processed")]
        [Test]
        public void TestUnitTestPlugin_Java_ComplexScenarios()
        {
            string javaCode = @"
package com.example.test;

import org.junit.Test;

/**
 * Complex test class with various scenarios
 */
public class ComplexTestClass extends BaseTestClass implements TestInterface {
    
    @TestAnnotation(
        id = ""COMPLEX-GENERIC-TEST"",
        purpose = ""Test method with complex type hints"",
        expected = ""Type hints should not interfere with parsing""
    )
    @Test
    public <T extends Comparable<T>> void testGenericMethod(List<T> items) {
        // Generic method test
    }

    @TestAnnotation(
        id = ""COMPLEX-NESTED-TEST"",
        purpose = ""Test method in nested class context"",
        expected = ""Nested class method should work""
    )
    @Test
    public void testInNestedContext() {
        // Method in complex context
        class NestedClass {
            void nestedMethod() {
                // Nested class method
            }
        }
    }

    @TestAnnotation(
        id = ""COMPLEX-OVERRIDE-TEST"",
        purpose = ""Test overridden method with annotation"",
        expected = ""Override annotation should not interfere""
    )
    @Test
    public void testOverriddenMethod() {
        super.testOverriddenMethod();
    }

    @TestAnnotation(
        id = ""COMPLEX-MULTILINE-TEST"",
        purpose = ""Test with multiline string in annotation and concatenation"",
        expected = ""Multiline strings should be handled""
    )
    @Test
    public void testMultilineAnnotation() {
        // Test multiline annotation
    }
}";

            var testFileSystem = CreateJavaTestFileSystem();
            var mockFileSystem = testFileSystem as MockFileSystem;
            mockFileSystem.AddFile(TestingHelpers.ConvertFilePath(@"c:\temp\TestJavaComplex.java"), new MockFileData(javaCode));

            var temp = new AnnotatedUnitTestPlugin(new LocalFileSystemPlugin(testFileSystem));
            temp.InitializePlugin(configuration);
            temp.RefreshItems();

            var tests = temp.GetUnitTests().ToArray();
            Assert.That(tests.Length == 4);
            
            var testIds = tests.Select(t => t.ItemID).ToArray();
            Assert.That(testIds.Contains("COMPLEX-GENERIC-TEST"));
            Assert.That(testIds.Contains("COMPLEX-NESTED-TEST"));
            Assert.That(testIds.Contains("COMPLEX-OVERRIDE-TEST"));
            Assert.That(testIds.Contains("COMPLEX-MULTILINE-TEST"));
            
            // Verify method names are correctly extracted
            var methodNames = tests.Select(t => t.UnitTestFunctionName).ToArray();
            Assert.That(methodNames.Contains("testGenericMethod"));
            Assert.That(methodNames.Contains("testInNestedContext"));
            Assert.That(methodNames.Contains("testOverriddenMethod"));
            Assert.That(methodNames.Contains("testMultilineAnnotation"));
        }

        #endregion

        #region Python Language Support Tests

        private IFileSystem CreatePythonTestFileSystem()
        {
            string pythonConfigFile = ($@"
UseGit = false
[[TestConfigurations]]
SubDirs = true
TestDirectory = ""{TestingHelpers.ConvertFilePath(@"c:/temp")}""
FileMasks = [""test_*.py""]
AnnotationName = ""test_decorator""
Language = ""python""
Project = ""RoboClerk""

[Purpose]
	Keyword = ""purpose""
	Optional = false
[PostCondition]
	Keyword = ""expected""
	Optional = false
[Identifier]
	Keyword = ""test_id""
	Optional = true
[TraceID]
	Keyword = ""trace_id""
	Optional = true
");

            return new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFilePath(@"c:\test\AnnotatedUnitTestPlugin.toml"), new MockFileData(pythonConfigFile) }
            });
        }

        [UnitTestAttribute(
            Identifier = "b08132ca-5bda-4196-864a-a3ad297c062b",
            Purpose = "AnnotatedUnitTestPlugin processes basic Python decorators with named parameters",
            PostCondition = "Python unit test decorators are correctly parsed and extracted")]
        [Test]
        public void TestUnitTestPlugin_Python_BasicDecorators()
        {
            string pythonCode = @"
import unittest

class TestClass(unittest.TestCase):
    @test_decorator(
        test_id='PYTHON-TEST-001',
        purpose='Test basic Python decorator parsing',
        expected='Python decorators should be parsed correctly'
    )
    def test_basic_python_method(self):
        # Test implementation
        pass

    @test_decorator(
        test_id='PYTHON-TEST-002',
        purpose='Test another Python method',
        expected='Second test should also work'
    )
    def test_another_python_method(self):
        # Another test
        pass";

            var testFileSystem = CreatePythonTestFileSystem();
            var mockFileSystem = testFileSystem as MockFileSystem;
            mockFileSystem.AddFile(TestingHelpers.ConvertFilePath(@"c:\temp\test_python_basic.py"), new MockFileData(pythonCode));

            var temp = new AnnotatedUnitTestPlugin(new LocalFileSystemPlugin(testFileSystem));
            temp.InitializePlugin(configuration);
            temp.RefreshItems();

            var tests = temp.GetUnitTests().ToArray();
            Assert.That(tests.Length == 2);
            Assert.That(tests[0].ItemID == "PYTHON-TEST-001");
            Assert.That(tests[1].ItemID == "PYTHON-TEST-002");
            Assert.That(tests[0].UnitTestPurpose == "Test basic Python decorator parsing");
            Assert.That(tests[1].UnitTestPurpose == "Test another Python method");
            Assert.That(tests[0].UnitTestAcceptanceCriteria == "Python decorators should be parsed correctly");
            Assert.That(tests[1].UnitTestAcceptanceCriteria == "Second test should also work");
        }

        [UnitTestAttribute(
            Identifier = "0e6ebbd0-4fd8-413d-84d3-faa3bae62153",
            Purpose = "AnnotatedUnitTestPlugin handles Python bare decorators without parameters",
            PostCondition = "Bare decorators are processed but cause validation errors for missing required fields")]
        [Test]
        public void TestUnitTestPlugin_Python_BareDecorators()
        {
            string pythonCode = @"
import unittest

class TestClass(unittest.TestCase):
    @test_decorator
    def test_bare_decorator(self):
        '''Test with bare decorator'''
        pass

    @test_decorator(
        purpose='Test with partial decorator',
        expected='Should work with required fields'
    )
    def test_partial_decorator(self):
        '''Test with some fields'''
        pass";

            var testFileSystem = CreatePythonTestFileSystem();
            var mockFileSystem = testFileSystem as MockFileSystem;
            mockFileSystem.AddFile(TestingHelpers.ConvertFilePath(@"c:\temp\test_python_bare.py"), new MockFileData(pythonCode));

            var temp = new AnnotatedUnitTestPlugin(new LocalFileSystemPlugin(testFileSystem));
            temp.InitializePlugin(configuration);

            // Should throw exception due to missing required fields in bare decorator
            var ex = Assert.Throws<Exception>(() => temp.RefreshItems());
            Assert.That(ex.Message.Contains("Required field(s) missing"));
        }

        [UnitTestAttribute(
            Identifier = "198fd1f6-c78a-4872-b67d-278af0d48b46",
            Purpose = "AnnotatedUnitTestPlugin handles Python decorators with various string literal formats",
            PostCondition = "Different Python string formats are correctly processed")]
        [Test]
        public void TestUnitTestPlugin_Python_StringLiterals()
        {
            string pythonCode = @"
import unittest

class TestClass(unittest.TestCase):
    @test_decorator(
        test_id='STRING-LITERAL-TEST',
        purpose='Test with ""escaped quotes"" and special chars\n\t',
        expected='Should handle various string formats'
    )
    def test_string_literals(self):
        '''Test string literal handling'''
        pass

    @test_decorator(
        test_id=""DOUBLE-QUOTE-TEST"",
        purpose=""Test with double quotes"",
        expected=""Should work with double quotes"",
    )
    def test_double_quotes(self):
        '''Test with double quotes'''
        pass

    @test_decorator(
        test_id='''TRIPLE-QUOTE-TEST''',
        purpose='''Test with triple quotes and
        multiline content''',
        expected='''Should handle triple quotes'''
    )
    def test_triple_quotes(self):
        '''Test docstring format handling'''
        pass

    @test_decorator(
        test_id='DOCSTRING-TEST',
        purpose='Test with docstring format',
        expected='Should handle docstring quotes'
    )
    def test_docstring_format(self):
        '''Test docstring format handling'''
        pass";

            var testFileSystem = CreatePythonTestFileSystem();
            var mockFileSystem = testFileSystem as MockFileSystem;
            mockFileSystem.AddFile(TestingHelpers.ConvertFilePath(@"c:\temp\test_python_strings.py"), new MockFileData(pythonCode));

            var temp = new AnnotatedUnitTestPlugin(new LocalFileSystemPlugin(testFileSystem));
            temp.InitializePlugin(configuration);
            temp.RefreshItems();

            var tests = temp.GetUnitTests().ToArray();
            Assert.That(tests.Length == 4);
            Assert.That(tests[0].ItemID == "STRING-LITERAL-TEST");
            Assert.That(tests[1].ItemID == "DOUBLE-QUOTE-TEST");
            Assert.That(tests[2].ItemID == "TRIPLE-QUOTE-TEST");
            Assert.That(tests[3].ItemID == "DOCSTRING-TEST");
            Assert.That(tests[0].UnitTestPurpose.Contains("escaped quotes"));
            Assert.That(tests[0].UnitTestPurpose.Contains("\n"));
            Assert.That(tests[0].UnitTestPurpose.Contains("\t"));
            Assert.That(tests[2].UnitTestPurpose.Contains("multiline"));
        }

        [UnitTestAttribute(
            Identifier = "35f05439-62d0-4b84-9319-6e11d6e28faa",
            Purpose = "AnnotatedUnitTestPlugin handles Python decorators in different function contexts",
            PostCondition = "Decorators on various Python function types are correctly processed")]
        [Test]
        public void TestUnitTestPlugin_Python_FunctionContexts()
        {
            string pythonCode = @"
import unittest

class TestClass(unittest.TestCase):
    @test_decorator(
        test_id='METHOD-TEST',
        purpose='Test instance method decorator',
        expected='Instance method should be processed'
    )
    def test_instance_method(self):
        '''Test instance method'''
        pass

    @classmethod
    @test_decorator(
        test_id='CLASSMETHOD-TEST',
        purpose='Test class method decorator',
        expected='Class method should be processed'
    )
    def test_class_method(cls):
        '''Test class method'''
        pass

    @staticmethod
    @test_decorator(
        test_id='STATICMETHOD-TEST',
        purpose='Test static method decorator',
        expected='Static method should be processed'
    )
    def test_static_method():
        '''Test static method'''
        pass

    @test_decorator(
        test_id='ASYNC-METHOD-TEST',
        purpose='Test async method decorator',
        expected='Async method should be processed'
    )
    async def test_async_method(self):
        '''Test async method'''
        pass

    @test_decorator(
        test_id='GENERATOR-TEST',
        purpose='Test generator function decorator',
        expected='Generator function should be processed'
    )
    def test_generator_function(self):
        '''Test generator function'''
        yield 1

def standalone_function():
    '''Standalone function outside class'''
    pass

@test_decorator(
    test_id='STANDALONE-TEST',
    purpose='Test standalone function decorator',
    expected='Standalone function should be processed'
)
def test_standalone_function():
    '''Test standalone function'''
    pass";

            var testFileSystem = CreatePythonTestFileSystem();
            var mockFileSystem = testFileSystem as MockFileSystem;
            mockFileSystem.AddFile(TestingHelpers.ConvertFilePath(@"c:\temp\test_python_contexts.py"), new MockFileData(pythonCode));

            var temp = new AnnotatedUnitTestPlugin(new LocalFileSystemPlugin(testFileSystem));
            temp.InitializePlugin(configuration);
            temp.RefreshItems();

            var tests = temp.GetUnitTests().ToArray();
            Assert.That(tests.Length == 6);
            
            var testIds = tests.Select(t => t.ItemID).ToArray();
            Assert.That(testIds.Contains("METHOD-TEST"));
            Assert.That(testIds.Contains("CLASSMETHOD-TEST"));
            Assert.That(testIds.Contains("STATICMETHOD-TEST"));
            Assert.That(testIds.Contains("ASYNC-METHOD-TEST"));
            Assert.That(testIds.Contains("GENERATOR-TEST"));
            Assert.That(testIds.Contains("STANDALONE-TEST"));
        }

        [UnitTestAttribute(
            Identifier = "4d1a7239-432b-4732-9779-0cb189fbb0a2",
            Purpose = "AnnotatedUnitTestPlugin handles Python decorators with missing optional fields",
            PostCondition = "Python tests with missing optional fields are processed correctly")]
        [Test]
        public void TestUnitTestPlugin_Python_MissingOptionalFields()
        {
            string pythonCode = @"
import unittest

class TestClass(unittest.TestCase):
    @test_decorator(
        purpose='Test without ID field',
        expected='Should work without optional ID'
    )
    def test_without_id(self):
        '''Test without ID'''
        pass

    @test_decorator(
        test_id='WITH-TRACE-TEST',
        purpose='Test with trace ID',
        expected='Should include trace information',
        trace_id='TRACE-789'
    )
    def test_with_trace_id(self):
        '''Test with trace ID'''
        pass";

            var testFileSystem = CreatePythonTestFileSystem();
            var mockFileSystem = testFileSystem as MockFileSystem;
            mockFileSystem.AddFile(TestingHelpers.ConvertFilePath(@"c:\temp\test_python_optional.py"), new MockFileData(pythonCode));

            var temp = new AnnotatedUnitTestPlugin(new LocalFileSystemPlugin(testFileSystem));
            temp.InitializePlugin(configuration);
            temp.RefreshItems();

            var tests = temp.GetUnitTests().ToArray();
            Assert.That(tests.Length == 2);
            
            // First test should have auto-generated ID
            Assert.That(!string.IsNullOrEmpty(tests[0].ItemID));
            Assert.That(tests[0].UnitTestPurpose == "Test without ID field");
            
            // Second test should have provided ID
            Assert.That(tests[1].ItemID == "WITH-TRACE-TEST");
            Assert.That(tests[1].UnitTestPurpose == "Test with trace ID");
        }

        [UnitTestAttribute(
            Identifier = "53d8f228-0030-458d-a888-9c71ede3f1ba",
            Purpose = "AnnotatedUnitTestPlugin handles complex Python decorator scenarios",
            PostCondition = "Complex Python code structures with decorators are correctly processed")]
        [Test]
        public void TestUnitTestPlugin_Python_ComplexScenarios()
        {
            string pythonCode = @"
'''
Complex test module with various scenarios
'''

import unittest
import asyncio
from typing import List, Dict, Any
from abc import ABC, abstractmethod

class BaseTestClass(unittest.TestCase, ABC):
    '''Base test class'''
    pass

class ComplexTestClass(BaseTestClass):
    '''Complex test class with various scenarios'''
    
    @test_decorator(
        test_id='COMPLEX-GENERIC-TEST',
        purpose='Test method with complex type hints',
        expected='Type hints should not interfere with parsing'
    )
    def test_generic_method(self, items: List[Dict[str, Any]]) -> bool:
        '''Test generic method with type hints'''
        return True

    @test_decorator(
        test_id='COMPLEX-NESTED-TEST',
        purpose='Test method in nested class context',
        expected='Nested class method should work'
    )
    def test_in_nested_context(self):
        '''Method in complex context'''
        class NestedClass:
            def nested_method(self):
                return 'nested'
        
        nested = NestedClass()
        return nested.nested_method()

    @property
    @test_decorator(
        test_id='COMPLEX-PROPERTY-TEST',
        purpose='Test property with decorator',
        expected='Property decorator should not interfere'
    )
    def test_property_method(self):
        '''Test property method'''
        return self._value

    @test_decorator(
        test_id='COMPLEX-MULTILINE-TEST',
        purpose=(
            'Test with multiline string using parentheses '
            'and concatenation across lines'
        ),
        expected='Multiline strings should be handled correctly'
    )
    def test_multiline_decorator(self):
        '''Test multiline decorator'''
        pass

    @test_decorator(
        test_id='COMPLEX-LAMBDA-TEST',
        purpose='Test with lambda and complex expressions',
        expected=str(lambda x: f'processed {x}')
    )
    def test_lambda_expressions(self):
        '''Test with lambda expressions'''
        func = lambda x: x * 2
        return func(5)

# Test with module-level function
@test_decorator(
    test_id='MODULE-LEVEL-TEST',
    purpose='Test module-level function',
    expected='Module functions should be processed'
)
def module_level_test_function():
    '''Module-level test function'''
    pass

if __name__ == '__main__':
    unittest.main()";

            var testFileSystem = CreatePythonTestFileSystem();
            var mockFileSystem = testFileSystem as MockFileSystem;
            mockFileSystem.AddFile(TestingHelpers.ConvertFilePath(@"c:\temp\test_python_complex.py"), new MockFileData(pythonCode));

            var temp = new AnnotatedUnitTestPlugin(new LocalFileSystemPlugin(testFileSystem));
            temp.InitializePlugin(configuration);
            temp.RefreshItems();

            var tests = temp.GetUnitTests().ToArray();
            Assert.That(tests.Length == 6);
            
            var testIds = tests.Select(t => t.ItemID).ToArray();
            Assert.That(testIds.Contains("COMPLEX-GENERIC-TEST"));
            Assert.That(testIds.Contains("COMPLEX-NESTED-TEST"));
            Assert.That(testIds.Contains("COMPLEX-PROPERTY-TEST"));
            Assert.That(testIds.Contains("COMPLEX-MULTILINE-TEST"));
            Assert.That(testIds.Contains("COMPLEX-LAMBDA-TEST"));
            Assert.That(testIds.Contains("MODULE-LEVEL-TEST"));
            
            // Verify method names are correctly extracted
            var methodNames = tests.Select(t => t.UnitTestFunctionName).ToArray();
            Assert.That(methodNames.Contains("test_generic_method"));
            Assert.That(methodNames.Contains("test_in_nested_context"));
            Assert.That(methodNames.Contains("test_property_method"));
            Assert.That(methodNames.Contains("test_multiline_decorator"));
            Assert.That(methodNames.Contains("test_lambda_expressions"));
            Assert.That(methodNames.Contains("module_level_test_function"));
        }

        #endregion

        #region TypeScript/JavaScript Language Support Tests

        private IFileSystem CreateTypeScriptTestFileSystem()
        {
            string tsConfigFile = ($@"
UseGit = false
[[TestConfigurations]]
SubDirs = true
FileMasks = [""*.test.ts"", ""*.spec.ts""]
TestDirectory = ""{TestingHelpers.ConvertFilePath(@"c:/temp")}""
AnnotationName = ""TestDecorator""
Language = ""typescript""
Project = ""RoboClerk""

[Purpose]
	Keyword = ""description""
	Optional = false
[PostCondition]
	Keyword = ""expected""
	Optional = false
[Identifier]
	Keyword = ""testId""
	Optional = true
[TraceID]
	Keyword = ""traceId""
	Optional = true
");

            return new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFilePath(@"c:\test\AnnotatedUnitTestPlugin.toml"), new MockFileData(tsConfigFile.ToString()) }
            });
        }

        private IFileSystem CreateJavaScriptTestFileSystem()
        {
            string jsConfigFile = ($@"
UseGit = false
[[TestConfigurations]]
SubDirs = true
FileMasks = [""*.test.js"", ""*.spec.js""]
AnnotationName = ""TestDecorator""
TestDirectory = ""{TestingHelpers.ConvertFilePath(@"c:/temp")}""
Language = ""javascript""
Project = ""RoboClerk""

[Purpose]
	Keyword = ""description""
	Optional = false
[PostCondition]
	Keyword = ""expected""
	Optional = false
[Identifier]
	Keyword = ""testId""
	Optional = true
[TraceID]
	Keyword = ""traceId""
	Optional = true
");

            return new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFilePath(@"c:\test\AnnotatedUnitTestPlugin.toml"), new MockFileData(jsConfigFile.ToString()) }
            });
        }

        [UnitTestAttribute(
            Identifier = "e5754768-466f-496e-9755-3e6531884fc8",
            Purpose = "AnnotatedUnitTestPlugin processes basic TypeScript decorators with named parameters",
            PostCondition = "TypeScript unit test decorators are correctly parsed and extracted")]
        [Test]
        public void TestUnitTestPlugin_TypeScript_BasicDecorators()
        {
            string typeScriptCode = @"
import { describe, test, expect } from '@jest/globals';

class TestClass {
    @TestDecorator({
        testId: 'TS-TEST-001',
        description: 'Test basic TypeScript decorator parsing',
        expected: 'TypeScript decorators should be parsed correctly'
    })
    testBasicTypeScriptMethod(): void {
        // Test implementation
        expect(true).toBe(true);
    }

    @TestDecorator({
        testId: 'TS-TEST-002',
        description: 'Test another TypeScript method',
        expected: 'Second test should also work'
    })
    testAnotherTypeScriptMethod(): boolean {
        // Another test
        return true;
    }
}";

            var testFileSystem = CreateTypeScriptTestFileSystem();
            var mockFileSystem = testFileSystem as MockFileSystem;
            mockFileSystem.AddFile(TestingHelpers.ConvertFilePath(@"c:\temp\basic.test.ts"), new MockFileData(typeScriptCode));

            var temp = new AnnotatedUnitTestPlugin( new LocalFileSystemPlugin(testFileSystem));
            temp.InitializePlugin(configuration);
            temp.RefreshItems();

            var tests = temp.GetUnitTests().ToArray();
            Assert.That(tests.Length == 2);
            Assert.That(tests[0].ItemID == "TS-TEST-001");
            Assert.That(tests[1].ItemID == "TS-TEST-002");
            Assert.That(tests[0].UnitTestPurpose == "Test basic TypeScript decorator parsing");
            Assert.That(tests[1].UnitTestPurpose == "Test another TypeScript method");
            Assert.That(tests[0].UnitTestAcceptanceCriteria == "TypeScript decorators should be parsed correctly");
            Assert.That(tests[1].UnitTestAcceptanceCriteria == "Second test should also work");
        }

        [UnitTestAttribute(
            Identifier = "be3f875b-d5e1-4673-adb3-e46ca5b92ee0",
            Purpose = "AnnotatedUnitTestPlugin processes basic JavaScript decorators with named parameters",
            PostCondition = "JavaScript unit test decorators are correctly parsed and extracted")]
        [Test]
        public void TestUnitTestPlugin_JavaScript_BasicDecorators()
        {
            string javaScriptCode = @"
const { describe, test, expect } = require('@jest/globals');

class TestClass {
    @TestDecorator({
        testId: 'JS-TEST-001',
        description: 'Test basic JavaScript decorator parsing',
        expected: 'JavaScript decorators should be parsed correctly'
    })
    testBasicJavaScriptMethod() {
        // Test implementation
        expect(true).toBe(true);
    }

    @TestDecorator({
        testId: 'JS-TEST-002',
        description: 'Test another JavaScript method',
        expected: 'Second test should also work'
    })
    testAnotherJavaScriptMethod() {
        // Another test
        return true;
    }
}";

            var testFileSystem = CreateJavaScriptTestFileSystem();
            var mockFileSystem = testFileSystem as MockFileSystem;
            mockFileSystem.AddFile(TestingHelpers.ConvertFilePath(@"c:\temp\basic.test.js"), new MockFileData(javaScriptCode));

            var temp = new AnnotatedUnitTestPlugin(new LocalFileSystemPlugin(testFileSystem));
            temp.InitializePlugin(configuration);
            temp.RefreshItems();

            var tests = temp.GetUnitTests().ToArray();
            Assert.That(tests.Length == 2);
            Assert.That(tests[0].ItemID == "JS-TEST-001");
            Assert.That(tests[1].ItemID == "JS-TEST-002");
            Assert.That(tests[0].UnitTestPurpose == "Test basic JavaScript decorator parsing");
            Assert.That(tests[1].UnitTestPurpose == "Test another JavaScript method");
            Assert.That(tests[0].UnitTestAcceptanceCriteria == "JavaScript decorators should be parsed correctly");
            Assert.That(tests[1].UnitTestAcceptanceCriteria == "Second test should also work");
        }

        [UnitTestAttribute(
            Identifier = "9df80f8e-9257-48af-8ddc-be360e4157bf",
            Purpose = "AnnotatedUnitTestPlugin handles TypeScript decorators with various string literal formats",
            PostCondition = "Different TypeScript string formats are correctly processed")]
        [Test]
        public void TestUnitTestPlugin_TypeScript_StringLiterals()
        {
            string typeScriptCode = @"
class TestClass {
    @TestDecorator({
        testId: 'STRING-LITERAL-TEST',
        description: 'Test with ""escaped quotes"" and special chars\n\t',
        expected: 'Should handle various string formats'
    })
    testStringLiterals(): void {
        // Test string literal handling
    }

    @TestDecorator({
        testId: ""DOUBLE-QUOTE-TEST"",
        description: ""Test with double quotes"",
        expected: ""Should work with double quotes""
    })
    testDoubleQuotes(): void {
        // Test with double quotes
    }

    @TestDecorator({
        testId: `TEMPLATE-LITERAL-TEST`,
        description: `Test with template literals and ${`nested`} expressions`,
        expected: `Should handle template literals`
    })
    testTemplateLiterals(): void {
        // Test template literal handling
    }

    @TestDecorator({
        testId: 'MULTILINE-TEST',
        description: 'Test with ' +
                    'string concatenation',
        expected: 'Should handle concatenated strings'
    })
    testMultilineStrings(): void {
        // Test multiline string handling
    }
}";

            var testFileSystem = CreateTypeScriptTestFileSystem();
            var mockFileSystem = testFileSystem as MockFileSystem;
            mockFileSystem.AddFile(TestingHelpers.ConvertFilePath(@"c:\temp\strings.test.ts"), new MockFileData(typeScriptCode));

            var temp = new AnnotatedUnitTestPlugin(new LocalFileSystemPlugin(testFileSystem));
            temp.InitializePlugin(configuration);
            temp.RefreshItems();

            var tests = temp.GetUnitTests().ToArray();
            Assert.That(tests.Length == 4);
            Assert.That(tests[0].ItemID == "STRING-LITERAL-TEST");
            Assert.That(tests[1].ItemID == "DOUBLE-QUOTE-TEST");
            Assert.That(tests[2].ItemID == "TEMPLATE-LITERAL-TEST");
            Assert.That(tests[3].ItemID == "MULTILINE-TEST");
            Assert.That(tests[0].UnitTestPurpose.Contains("escaped quotes"));
            Assert.That(tests[0].UnitTestPurpose.Contains("\n"));
            Assert.That(tests[0].UnitTestPurpose.Contains("\t"));
        }

        [UnitTestAttribute(
            Identifier = "27a0c4f4-2190-4237-a7a0-234d8c549168",
            Purpose = "AnnotatedUnitTestPlugin handles TypeScript decorators in different method contexts",
            PostCondition = "Decorators on various TypeScript method types are correctly processed")]
        [Test]
        public void TestUnitTestPlugin_TypeScript_MethodContexts()
        {
            string typeScriptCode = @"
interface ITestInterface {
    interfaceMethod(): void;
}

abstract class BaseTestClass {
    abstract abstractMethod(): void;
}

class TestClass extends BaseTestClass implements ITestInterface {
    
    @TestDecorator({
        testId: 'PUBLIC-METHOD-TEST',
        description: 'Test public method decorator',
        expected: 'Public method should be processed'
    })
    public testPublicMethod(): void {
        // Public test method
    }

    @TestDecorator({
        testId: 'PRIVATE-METHOD-TEST',
        description: 'Test private method decorator',
        expected: 'Private method should be processed'
    })
    private testPrivateMethod(): void {
        // Private test method
    }

    @TestDecorator({
        testId: 'STATIC-METHOD-TEST',
        description: 'Test static method decorator',
        expected: 'Static method should be processed'
    })
    static testStaticMethod(): void {
        // Static test method
    }

    @TestDecorator({
        testId: 'ASYNC-METHOD-TEST',
        description: 'Test async method decorator',
        expected: 'Async method should be processed'
    })
    async testAsyncMethod(): Promise<void> {
        // Async test method
        await new Promise(resolve => setTimeout(resolve, 100));
    }

    @TestDecorator({
        testId: 'GENERIC-METHOD-TEST',
        description: 'Test generic method decorator',
        expected: 'Generic method should be processed'
    })
    testGenericMethod<T>(item: T): T {
        // Generic test method
        return item;
    }

    @TestDecorator({
        testId: 'ABSTRACT-IMPL-TEST',
        description: 'Test abstract method implementation',
        expected: 'Abstract implementation should be processed'
    })
    abstractMethod(): void {
        // Abstract method implementation
    }

    @TestDecorator({
        testId: 'INTERFACE-IMPL-TEST',
        description: 'Test interface method implementation',
        expected: 'Interface implementation should be processed'
    })
    interfaceMethod(): void {
        // Interface method implementation
    }
}";

            var testFileSystem = CreateTypeScriptTestFileSystem();
            var mockFileSystem = testFileSystem as MockFileSystem;
            mockFileSystem.AddFile(TestingHelpers.ConvertFilePath(@"c:\temp\contexts.test.ts"), new MockFileData(typeScriptCode));

            var temp = new AnnotatedUnitTestPlugin(new LocalFileSystemPlugin(testFileSystem));
            temp.InitializePlugin(configuration);
            temp.RefreshItems();

            var tests = temp.GetUnitTests().ToArray();
            Assert.That(tests.Length == 7);
            
            var testIds = tests.Select(t => t.ItemID).ToArray();
            Assert.That(testIds.Contains("PUBLIC-METHOD-TEST"));
            Assert.That(testIds.Contains("PRIVATE-METHOD-TEST"));
            Assert.That(testIds.Contains("STATIC-METHOD-TEST"));
            Assert.That(testIds.Contains("ASYNC-METHOD-TEST"));
            Assert.That(testIds.Contains("GENERIC-METHOD-TEST"));
            Assert.That(testIds.Contains("ABSTRACT-IMPL-TEST"));
            Assert.That(testIds.Contains("INTERFACE-IMPL-TEST"));
        }

        [UnitTestAttribute(
            Identifier = "ffbac816-12d1-4dde-8e86-460b0428a79e",
            Purpose = "AnnotatedUnitTestPlugin handles JavaScript bare decorators without parameters",
            PostCondition = "Bare decorators are processed but cause validation errors for missing required fields")]
        [Test]
        public void TestUnitTestPlugin_JavaScript_BareDecorators()
        {
            string javaScriptCode = @"
class TestClass {
    @TestDecorator()
    testBareDecorator() {
        // Test with bare decorator
    }

    @TestDecorator({
        description: 'Test with partial decorator',
        expected: 'Should work with required fields'
    })
    testPartialDecorator() {
        // Test with some fields
    }
}";

            var testFileSystem = CreateJavaScriptTestFileSystem();
            var mockFileSystem = testFileSystem as MockFileSystem;
            mockFileSystem.AddFile(TestingHelpers.ConvertFilePath(@"c:\temp\bare.test.js"), new MockFileData(javaScriptCode));

            var temp = new AnnotatedUnitTestPlugin(new LocalFileSystemPlugin(testFileSystem));
            temp.InitializePlugin(configuration);

            // Should throw exception due to missing required fields in bare decorator
            var ex = Assert.Throws<Exception>(() => temp.RefreshItems());
            Assert.That(ex.Message.Contains("Required field(s) missing"));
        }

        [UnitTestAttribute(
            Identifier = "de4a45e7-ce59-410f-8720-ef4a3326ec7c",
            Purpose = "AnnotatedUnitTestPlugin handles TypeScript decorators with missing optional fields",
            PostCondition = "TypeScript tests with missing optional fields are processed correctly")]
        [Test]
        public void TestUnitTestPlugin_TypeScript_MissingOptionalFields()
        {
            string typeScriptCode = @"
class TestClass {
    @TestDecorator({
        description: 'Test without ID field',
        expected: 'Should work without optional ID'
    })
    testWithoutId(): void {
        // Test without ID
    }

    @TestDecorator({
        testId: 'WITH-TRACE-TEST',
        description: 'Test with trace ID',
        expected: 'Should include trace information',
        traceId: 'TRACE-999'
    })
    testWithTraceId(): void {
        // Test with trace ID
    }
}";

            var testFileSystem = CreateTypeScriptTestFileSystem();
            var mockFileSystem = testFileSystem as MockFileSystem;
            mockFileSystem.AddFile(TestingHelpers.ConvertFilePath(@"c:\temp\optional.test.ts"), new MockFileData(typeScriptCode));

            var temp = new AnnotatedUnitTestPlugin(new LocalFileSystemPlugin(testFileSystem));
            temp.InitializePlugin(configuration);
            temp.RefreshItems();

            var tests = temp.GetUnitTests().ToArray();
            Assert.That(tests.Length == 2);
            
            // First test should have auto-generated ID
            Assert.That(!string.IsNullOrEmpty(tests[0].ItemID));
            Assert.That(tests[0].UnitTestPurpose == "Test without ID field");
            
            // Second test should have provided ID
            Assert.That(tests[1].ItemID == "WITH-TRACE-TEST");
            Assert.That(tests[1].UnitTestPurpose == "Test with trace ID");
        }

        [UnitTestAttribute(
            Identifier = "8968846e-762a-49b2-b7ca-1e928594594e",
            Purpose = "AnnotatedUnitTestPlugin handles complex TypeScript decorator scenarios",
            PostCondition = "Complex TypeScript code structures with decorators are correctly processed")]
        [Test]
        public void TestUnitTestPlugin_TypeScript_ComplexScenarios()
        {
            string typeScriptCode = @"
import { describe, test, expect } from '@jest/globals';

// Type definitions and interfaces
interface TestResult<T> {
    success: boolean;
    data: T;
    error?: string;
}

type AsyncFunction<T> = () => Promise<T>;

// Generic constraints and utility types
type NonNullable<T> = T extends null | undefined ? never : T;

/**
 * Complex test class with various TypeScript scenarios
 */
class ComplexTestClass {
    
    @TestDecorator({
        testId: 'COMPLEX-GENERIC-TEST',
        description: 'Test method with complex generic constraints',
        expected: 'Generic constraints should not interfere with parsing'
    })
    testGenericConstraints<K extends keyof T, V extends T[K]>(
        obj: T,
        key: K,
        value: V
    ): TestResult<V> {
        // Complex generic method test
        return { success: true, data: value };
    }

    @TestDecorator({
        testId: 'COMPLEX-UNION-TEST',
        description: 'Test method with union and intersection types',
        expected: 'Complex type annotations should work'
    })
    testUnionTypes(
        param: string | number | boolean,
        options?: { strict?: boolean } & { verbose?: boolean }
    ): param is string {
        // Union and intersection types test
        return typeof param === 'string';
    }

    @TestDecorator({
        testId: 'COMPLEX-ASYNC-TEST',
        description: 'Test async method with complex Promise handling',
        expected: 'Async/await with complex types should be processed'
    })
    async testComplexAsync(): Promise<TestResult<T[]>> {
        // Complex async method
        const results = await Promise.all([
            this.helperMethod(),
            new Promise<T[]>(resolve => resolve([]))
        ]);
        
        return {
            success: true,
            data: results[1]
        };
    }

    @TestDecorator({
        testId: 'COMPLEX-DECORATOR-COMPOSITION',
        description: 'Test method with multiple decorators and complex annotations',
        expected: 'Decorator composition should work correctly'
    })
    @deprecated('Use newMethod instead')
    @performance.mark('test-method')
    testDecoratorComposition<R>(
        callback: AsyncFunction<R>
    ): Promise<R> {
        // Method with multiple decorators
        return callback();
    }

    @TestDecorator({
        testId: 'COMPLEX-CONDITIONAL-TYPES',
        description: 'Test with conditional types and mapped types',
        expected: 'Advanced TypeScript features should not break parsing'
    })
    testConditionalTypes<
        U extends T,
        K extends keyof U = keyof U
    >(
        input: U,
        keys: K[]
    ): { [P in K]: U[P] } {
        // Conditional and mapped types
        const result = {} as { [P in K]: U[P] };
        keys.forEach(key => {
            result[key] = input[key];
        });
        return result;
    }

    private async helperMethod(): Promise<void> {
        // Private helper method
    }
}

class ModuleFns {
  @TestDecorator({
    testId: 'MODULE-LEVEL-TS-TEST',
    description: 'Test module-level function with TypeScript types',
    expected: 'Module functions should be processed'
  })
  static moduleLevel<T>(items: T[]): T[] {
    return items.filter(Boolean);
  }
}
export const moduleLevel = ModuleFns.moduleLevel;

// Export for testing
export { ComplexTestClass, moduleLevel };";

            var testFileSystem = CreateTypeScriptTestFileSystem();
            var mockFileSystem = testFileSystem as MockFileSystem;
            mockFileSystem.AddFile(TestingHelpers.ConvertFilePath(@"c:\temp\complex.spec.ts"), new MockFileData(typeScriptCode));

            var temp = new AnnotatedUnitTestPlugin(new LocalFileSystemPlugin(testFileSystem));
            temp.InitializePlugin(configuration);
            temp.RefreshItems();

            var tests = temp.GetUnitTests().ToArray();
            Assert.That(tests.Length == 6);
            
            var testIds = tests.Select(t => t.ItemID).ToArray();
            Assert.That(testIds.Contains("COMPLEX-GENERIC-TEST"));
            Assert.That(testIds.Contains("COMPLEX-UNION-TEST"));
            Assert.That(testIds.Contains("COMPLEX-ASYNC-TEST"));
            Assert.That(testIds.Contains("COMPLEX-DECORATOR-COMPOSITION"));
            Assert.That(testIds.Contains("COMPLEX-CONDITIONAL-TYPES"));
            Assert.That(testIds.Contains("MODULE-LEVEL-TS-TEST"));
            
            // Verify method names are correctly extracted
            var methodNames = tests.Select(t => t.UnitTestFunctionName).ToArray();
            Assert.That(methodNames.Contains("testGenericConstraints"));
            Assert.That(methodNames.Contains("testUnionTypes"));
            Assert.That(methodNames.Contains("testComplexAsync"));
            Assert.That(methodNames.Contains("testDecoratorComposition"));
            Assert.That(methodNames.Contains("testConditionalTypes"));
            Assert.That(methodNames.Contains("moduleLevel"));
        }

        #endregion
    }
}
