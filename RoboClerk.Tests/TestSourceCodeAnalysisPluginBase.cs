using LibGit2Sharp;
using NSubstitute;
using NUnit.Framework;
using RoboClerk.Configuration;
using RoboClerk.ContentCreators;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboClerk.Tests
{
    internal class TestSourceCodeAnalysisPlugin : SourceCodeAnalysisPluginBase, ISourceCodeAnalysisPlugin
    {
        public TestSourceCodeAnalysisPlugin(IFileSystem fileSystem) 
            :base(fileSystem)
        {
            name = "TestSourceCodeAnalysisPlugin";
        }

        public List<UnitTestItem> UnitTests => unitTests;
        public List<string> Directories => directories;
        public List<string> FileMasks => fileMasks;
        public GitRepoInformation GitInfo => gitInfo;
        public bool SubDir => subDir;
        public List<string> SourceFiles => sourceFiles;

        public override void RefreshItems()
        {
            ScanDirectoriesForSourceFiles();
        }
    }


    [TestFixture]
    [Description("These tests test the Sourcecode Analysis Plugin Base")]
    internal class TestSourceCodeAnalysisPluginBase
    {
        private IConfiguration config = null;
        private IFileSystem fs = null;
        
        [SetUp]
        public void TestSetup()
        {
            string tomlFile = @"
TestDirectories = [""C:\\RoboClerkTest"",""C:\\RoboClerkTest2""]
SubDirs = true
FileMasks = [""Test*.cs""]
UseGit = false";

            fs = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"c:\TestSourceCodeAnalysisPlugin.toml", new MockFileData(tomlFile) },
                { @"c:\RoboClerkTest\TestStuff1.cs", new MockFileData("this is a mock file1") },
                { @"c:\RoboClerkTest\TestStuff2.txt", new MockFileData("this is a mock file2") },
                { @"c:\RoboClerkTest\TestStuff3.cs", new MockFileData("this is a mock file3") },
                { @"c:\RoboClerkTest\SubDir\TestStuff4.cs", new MockFileData("this is a mock file4") },
                { @"c:\RoboClerkTest2\TestStuff5.cs", new MockFileData("this is a mock file5") },
            });

            config = Substitute.For<IConfiguration>();
            config.PluginConfigDir.Returns(@"c:\");
            config.ProjectRoot.Returns(@"c:\RoboClerkTest");
        }

        [UnitTestAttribute(
        Identifier = "FEDA10DA-717C-47C2-9933-9F304C935B93",
        Purpose = "SourceCodeAnalysisPlugin is created",
        PostCondition = "No exception is thrown")]
        [Test]
        public void CreateSourceCodeAnalysisPlugin()
        {
            var testPlugin = new TestSourceCodeAnalysisPlugin(fs);
        }

        [UnitTestAttribute(
        Identifier = "B73B72B4-628A-42B9-A7F6-8AA24775CD9C",
        Purpose = "SourceCodeAnalysisPlugin is created and initialized",
        PostCondition = "Expected values are set")]
        [Test]
        public void TestSourceCodeAnalysisPlugin1()
        {
            var testPlugin = new TestSourceCodeAnalysisPlugin(fs);
            testPlugin.Initialize(config);

            Assert.That(testPlugin.FileMasks.Count, Is.EqualTo(1));
            Assert.That(testPlugin.FileMasks[0], Is.EqualTo("Test*.cs"));
            Assert.That(testPlugin.Directories.Count, Is.EqualTo(2));
            Assert.That(testPlugin.Directories[0], Is.EqualTo("C:\\RoboClerkTest"));
            Assert.That(testPlugin.Directories[1], Is.EqualTo("C:\\RoboClerkTest2"));
            Assert.That(testPlugin.SubDir);
        }

        [UnitTestAttribute(
        Identifier = "B6889217-B4F1-49C0-A13D-544A09FF6E32",
        Purpose = "SourceCodeAnalysisPlugin is created and initialized, git support is set to true but fails",
        PostCondition = "Exception is thrown")]
        [Test]
        public void TestSourceCodeAnalysisPlugin2()
        {
            string tomlFile = @"
TestDirectories = [""C:\\RoboClerkTest"",""C:\\RoboClerkTest2""]
SubDirs = true
FileMasks = [""Test*.cs""]
UseGit = true";

            fs = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"c:\TestSourceCodeAnalysisPlugin.toml", new MockFileData(tomlFile) },
            });

            var testPlugin = new TestSourceCodeAnalysisPlugin(fs);
            Assert.Throws<RepositoryNotFoundException>(()=>testPlugin.Initialize(config));
        }

        [UnitTestAttribute(
        Identifier = "5D8F1310-1D33-49C1-93C9-0072428EF215",
        Purpose = "SourceCodeAnalysisPlugin is created and initialized, subdirs set to false",
        PostCondition = "Expected files found")]
        [Test]
        public void TestSourceCodeAnalysisPlugin3()
        {
            string tomlFile = @"
TestDirectories = [""C:\\RoboClerkTest"",""C:\\RoboClerkTest2""]
SubDirs = false
FileMasks = [""Test*.cs""]
UseGit = false";

            fs.File.WriteAllText(@"c:\TestSourceCodeAnalysisPlugin.toml", tomlFile);

            var testPlugin = new TestSourceCodeAnalysisPlugin(fs);
            testPlugin.Initialize(config);
            testPlugin.RefreshItems();
            Assert.That(testPlugin.SourceFiles.Count, Is.EqualTo(3));
            Assert.That(testPlugin.SourceFiles[0], Is.EqualTo(@"c:\RoboClerkTest\TestStuff1.cs"));
            Assert.That(testPlugin.SourceFiles[1], Is.EqualTo(@"c:\RoboClerkTest\TestStuff3.cs"));
            Assert.That(testPlugin.SourceFiles[2], Is.EqualTo(@"c:\RoboClerkTest2\TestStuff5.cs"));
        }

        [UnitTestAttribute(
        Identifier = "DA02C319-2C9E-4FA0-BCAB-B9DDA56A1E8B",
        Purpose = "SourceCodeAnalysisPlugin is created and initialized, subdirs set to true",
        PostCondition = "Expected files found")]
        [Test]
        public void TestSourceCodeAnalysisPlugin4()
        {
            string tomlFile = @"
TestDirectories = [""C:\\RoboClerkTest"",""C:\\RoboClerkTest2""]
SubDirs = true
FileMasks = [""Test*.cs""]
UseGit = false";

            fs.File.WriteAllText(@"c:\TestSourceCodeAnalysisPlugin.toml", tomlFile);

            var testPlugin = new TestSourceCodeAnalysisPlugin(fs);
            testPlugin.Initialize(config);
            testPlugin.RefreshItems();
            Assert.That(testPlugin.SourceFiles.Count, Is.EqualTo(4));
            Assert.That(testPlugin.SourceFiles[0], Is.EqualTo(@"c:\RoboClerkTest\TestStuff1.cs"));
            Assert.That(testPlugin.SourceFiles[1], Is.EqualTo(@"c:\RoboClerkTest\TestStuff3.cs"));
            Assert.That(testPlugin.SourceFiles[2], Is.EqualTo(@"c:\RoboClerkTest\SubDir\TestStuff4.cs"));
            Assert.That(testPlugin.SourceFiles[3], Is.EqualTo(@"c:\RoboClerkTest2\TestStuff5.cs"));
        }

        [UnitTestAttribute(
        Identifier = "8C3E62E3-8B03-4765-8B97-26F5B79E6CFA",
        Purpose = "SourceCodeAnalysisPlugin is created and initialized, subdirs set to false, two filemasks are provided",
        PostCondition = "Expected files found")]
        [Test]
        public void TestSourceCodeAnalysisPlugin5()
        {
            string tomlFile = @"
TestDirectories = [""C:\\RoboClerkTest"",""C:\\RoboClerkTest2""]
SubDirs = false
FileMasks = [""Test*.cs"",""Test*.txt""]
UseGit = false";

            fs.File.WriteAllText(@"c:\TestSourceCodeAnalysisPlugin.toml", tomlFile);

            var testPlugin = new TestSourceCodeAnalysisPlugin(fs);
            testPlugin.Initialize(config);
            testPlugin.RefreshItems();
            Assert.That(testPlugin.SourceFiles.Count, Is.EqualTo(4));
            Assert.That(testPlugin.SourceFiles[0], Is.EqualTo(@"c:\RoboClerkTest\TestStuff1.cs"));
            Assert.That(testPlugin.SourceFiles[1], Is.EqualTo(@"c:\RoboClerkTest\TestStuff3.cs"));
            Assert.That(testPlugin.SourceFiles[2], Is.EqualTo(@"c:\RoboClerkTest\TestStuff2.txt"));
            Assert.That(testPlugin.SourceFiles[3], Is.EqualTo(@"c:\RoboClerkTest2\TestStuff5.cs"));
        }

        [UnitTestAttribute(
        Identifier = "AAE8D0FE-61C4-4E89-A042-BDECEE4D4EAE",
        Purpose = "SourceCodeAnalysisPlugin is created and initialized, subdirs set to false, one of the provided directories does not exist",
        PostCondition = "Exception thrown")]
        [Test]
        public void TestSourceCodeAnalysisPlugin6()
        {
            string tomlFile = @"
TestDirectories = [""C:\\RoboClerkTest"",""C:\\RoboClerkTest3""]
SubDirs = false
FileMasks = [""Test*.cs"",""Test*.txt""]
UseGit = false";

            fs.File.WriteAllText(@"c:\TestSourceCodeAnalysisPlugin.toml", tomlFile);

            var testPlugin = new TestSourceCodeAnalysisPlugin(fs);
            testPlugin.Initialize(config);
            Assert.Throws<System.IO.DirectoryNotFoundException>(()=>testPlugin.RefreshItems());
        }

    }
}
