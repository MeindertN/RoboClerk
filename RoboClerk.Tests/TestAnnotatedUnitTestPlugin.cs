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
DecorationMarker = ""[UnitTestAttribut""
ParameterStartDelimiter = ""(""
ParameterEndDelimiter = "")""
ParameterSeparator = "",""
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
[FunctionName]
	StartString = ""public void ""
	EndString = ""(""");
            string testFile = @"
 [UnitTestAttribut(
            Identifier = ""9A3258CF-F9EE-4A1A-95E6-B49EF25FB200"",
            Purpose = """"""RoboClerk Processes the media directory including subdirs, 
output media directory exists including subdirs""
"""""",
            PostCondition = ""Media directory is deleted, recreated and files are copied (except .gitignore)"")]
        [Test]
        public void CheckMediaDirectory()
        {
	    }

[UnitTestAttribut(
        Identifier = ""5D8F1310-1D33-49C1-93C9-0072428EF215"",
        Purpose = ""SourceCodeAnalysisPlugin is created and initialized, subdirs set to false"",
        PostCondition = ""Expected files =found"")]
        [Test]
        public void TestSourceCodeAnalysisPlugin3()
        {
        }";
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

        [UnitTestAttribute(Purpose = "AnnotatedUnitTestPlugin is created",
        Identifier = "1C2B7995-DFDF-466B-96D8-B8165EDC28C8",
        PostCondition = "No exception is thrown")]
        [Test]
        public void TestUnitTestPlugin1()
        {
            var temp = new AnnotatedUnitTestPlugin(fileSystem);
            temp.InitializePlugin(configuration);
        }

        [UnitTestAttribute(Purpose = "AnnotatedUnitTestPlugin is created, refresh is called",
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
            Assert.That(tests[0].UnitTestPurpose == "RoboClerk Processes the media directory including subdirs, output media directory exists including subdirs\"");
            Assert.That(tests[1].UnitTestPurpose == "SourceCodeAnalysisPlugin is created and initialized, subdirs set to false");
            Assert.That(tests[0].UnitTestAcceptanceCriteria == "Media directory is deleted, recreated and files are copied (except .gitignore)");
            Assert.That(tests[1].UnitTestAcceptanceCriteria == "Expected files =found");
        }
    }
}
