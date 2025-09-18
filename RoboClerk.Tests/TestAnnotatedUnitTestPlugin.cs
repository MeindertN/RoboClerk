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

        #region C# Language Support Tests

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
        #endregion C# Language Support Tests

        #region Java Language Support Tests

        private IFileSystem CreateJavaTestFileSystem()
        {
            StringBuilder javaConfigFile = new StringBuilder();
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                javaConfigFile.Append(@"TestDirectories = [""/c/temp""]");
            }
            else
            {
                javaConfigFile.Append(@"TestDirectories = [""c:/temp""]");
            }
            javaConfigFile.Append(@"
SubDirs = true
FileMasks = [""Test*.java""]
UseGit = false
AnnotationName = ""TestAnnotation""
Language = ""java""
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
                { TestingHelpers.ConvertFileName(@"c:\test\AnnotatedUnitTestPlugin.toml"), new MockFileData(javaConfigFile.ToString()) }
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
            mockFileSystem.AddFile(TestingHelpers.ConvertFileName(@"c:\temp\TestJavaBasic.java"), new MockFileData(javaCode));

            var temp = new AnnotatedUnitTestPlugin(testFileSystem);
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
            mockFileSystem.AddFile(TestingHelpers.ConvertFileName(@"c:\temp\TestJavaMarker.java"), new MockFileData(javaCode));

            var temp = new AnnotatedUnitTestPlugin(testFileSystem);
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
        purpose = ""Test with \""escaped quotes\"" and special chars\n\t"",
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
            mockFileSystem.AddFile(TestingHelpers.ConvertFileName(@"c:\temp\TestJavaStrings.java"), new MockFileData(javaCode));

            var temp = new AnnotatedUnitTestPlugin(testFileSystem);
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
            mockFileSystem.AddFile(TestingHelpers.ConvertFileName(@"c:\temp\TestJavaContexts.java"), new MockFileData(javaCode));

            var temp = new AnnotatedUnitTestPlugin(testFileSystem);
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
            mockFileSystem.AddFile(TestingHelpers.ConvertFileName(@"c:\temp\TestJavaOptional.java"), new MockFileData(javaCode));

            var temp = new AnnotatedUnitTestPlugin(testFileSystem);
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
        purpose = ""Test generic method with complex signature"",
        expected = ""Generic method should be parsed correctly""
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
        Runnable r = new Runnable() {
            @Override
            public void run() {
                // Anonymous inner class
            }
        };
    }

    @Override
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
            mockFileSystem.AddFile(TestingHelpers.ConvertFileName(@"c:\temp\TestJavaComplex.java"), new MockFileData(javaCode));

            var temp = new AnnotatedUnitTestPlugin(testFileSystem);
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
    }
}
