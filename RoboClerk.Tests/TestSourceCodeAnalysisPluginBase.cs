using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using RoboClerk.Core.Configuration;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Runtime.InteropServices;

namespace RoboClerk.Tests
{
    internal class TestSourceCodeAnalysisPlugin : SourceCodeAnalysisPluginBase
    {
        public TestSourceCodeAnalysisPlugin(IFileProviderPlugin fileSystem) 
            :base(fileSystem)
        {
            name = "TestSourceCodeAnalysisPlugin";
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            //this test plugin does not need to register any services
        }

        public List<UnitTestItem> UnitTests => unitTests;
        public IReadOnlyList<RoboClerk.TestConfiguration> Configurations => TestConfigurations;
        public GitRepository GitInfo => gitRepo;
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
        private IFileProviderPlugin fileProvider = null;
        
        [SetUp]
        public void TestSetup()
        {
            string tomlFile = string.Empty;

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                tomlFile = @"
[[TestConfigurations]]
Language = ""csharp""
TestDirectory = ""/C/RoboClerkTest""
SubDirs = true
FileMasks = [""Test*.cs""]
Project = ""TestProject1""

[[TestConfigurations]]
Language = ""csharp""
TestDirectory = ""/C/RoboClerkTest2""
SubDirs = true
FileMasks = [""Test*.cs""]
Project = ""TestProject2""

UseGit = false";
            }
            else
            {
                tomlFile = @"
[[TestConfigurations]]
Language = ""csharp""
TestDirectory = ""C:\\RoboClerkTest""
SubDirs = true
FileMasks = [""Test*.cs""]
Project = ""TestProject1""

[[TestConfigurations]]
Language = ""csharp""
TestDirectory = ""C:\\RoboClerkTest2""
SubDirs = true
FileMasks = [""Test*.cs""]
Project = ""TestProject2""

UseGit = false";
            }

            fs = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFilePath(@"C:\TestSourceCodeAnalysisPlugin.toml"), new MockFileData(tomlFile) },
                { TestingHelpers.ConvertFilePath(@"C:\RoboClerkTest\TestStuff1.cs"), new MockFileData("this is a mock file1") },
                { TestingHelpers.ConvertFilePath(@"C:\RoboClerkTest\TestStuff2.txt"), new MockFileData("this is a mock file2") },
                { TestingHelpers.ConvertFilePath(@"C:\RoboClerkTest\TestStuff3.cs"), new MockFileData("this is a mock file3") },
                { TestingHelpers.ConvertFilePath(@"C:\RoboClerkTest\SubDir\TestStuff4.cs"), new MockFileData("this is a mock file4") },
                { TestingHelpers.ConvertFilePath(@"C:\RoboClerkTest2\TestStuff5.cs"), new MockFileData("this is a mock file5") },
            });
            fileProvider = new LocalFileSystemPlugin(fs);
            config = Substitute.For<IConfiguration>();
            config.PluginConfigDir.Returns(TestingHelpers.ConvertFilePath(@"C:\"));
            config.ProjectRoot.Returns(TestingHelpers.ConvertFilePath(@"C:\RoboClerkTest"));
        }

        [UnitTestAttribute(
        Identifier = "FEDA10DA-717C-47C2-9933-9F304C935B93",
        Purpose = "SourceCodeAnalysisPlugin is created",
        PostCondition = "No exception is thrown")]
        [Test]
        public void CreateSourceCodeAnalysisPlugin()
        {
            var testPlugin = new TestSourceCodeAnalysisPlugin(fileProvider);
        }

        [UnitTestAttribute(
        Identifier = "B73B72B4-628A-42B9-A7F6-8AA24775CD9C",
        Purpose = "SourceCodeAnalysisPlugin is created and initialized",
        PostCondition = "Expected values are set")]
        [Test]
        public void TestSourceCodeAnalysisPlugin1()
        {

            var testPlugin = new TestSourceCodeAnalysisPlugin(fileProvider);
            testPlugin.InitializePlugin(config);

            Assert.That(testPlugin.Configurations.Count, Is.EqualTo(2));
            
            var config1 = testPlugin.Configurations[0];
            Assert.That(config1.FileMasks.Count, Is.EqualTo(1));
            Assert.That(config1.FileMasks[0], Is.EqualTo("Test*.cs"));
            Assert.That(config1.TestDirectory, Is.EqualTo(TestingHelpers.ConvertFilePath("C:\\RoboClerkTest")));
            Assert.That(config1.SubDirs);
            Assert.That(config1.Project, Is.EqualTo("TestProject1"));
            
            var config2 = testPlugin.Configurations[1];
            Assert.That(config2.TestDirectory, Is.EqualTo(TestingHelpers.ConvertFilePath("C:\\RoboClerkTest2")));
            Assert.That(config2.Project, Is.EqualTo("TestProject2"));
        }

        [UnitTestAttribute(
        Identifier = "5D8F1310-1D33-49C1-93C9-0072428EF215",
        Purpose = "SourceCodeAnalysisPlugin is created and initialized, subdirs set to false",
        PostCondition = "Expected files found")]
        [Test]
        public void TestSourceCodeAnalysisPlugin3()
        {
            string tomlFile = string.Empty;
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                tomlFile = @"
[[TestConfigurations]]
Language = ""csharp""
TestDirectory = ""/C/RoboClerkTest""
SubDirs = false
FileMasks = [""Test*.cs""]
Project = ""TestProject1""

[[TestConfigurations]]
Language = ""csharp""
TestDirectory = ""/C/RoboClerkTest2""
SubDirs = false
FileMasks = [""Test*.cs""]
Project = ""TestProject2""

UseGit = false";
            }
            else
            {
                tomlFile = @"
[[TestConfigurations]]
Language = ""csharp""
TestDirectory = ""C:\\RoboClerkTest""
SubDirs = false
FileMasks = [""Test*.cs""]
Project = ""TestProject1""

[[TestConfigurations]]
Language = ""csharp""
TestDirectory = ""C:\\RoboClerkTest2""
SubDirs = false
FileMasks = [""Test*.cs""]
Project = ""TestProject2""

UseGit = false";
            }

            fs.File.WriteAllText(TestingHelpers.ConvertFilePath(@"C:\TestSourceCodeAnalysisPlugin.toml"), tomlFile);

            var testPlugin = new TestSourceCodeAnalysisPlugin(fileProvider);
            testPlugin.InitializePlugin(config);
          
            testPlugin.RefreshItems();
            Assert.That(testPlugin.SourceFiles.Count, Is.EqualTo(3));
            Assert.That(testPlugin.SourceFiles[0], Is.EqualTo(TestingHelpers.ConvertFilePath(@"C:\RoboClerkTest\TestStuff1.cs")));
            Assert.That(testPlugin.SourceFiles[1], Is.EqualTo(TestingHelpers.ConvertFilePath(@"C:\RoboClerkTest\TestStuff3.cs")));
            Assert.That(testPlugin.SourceFiles[2], Is.EqualTo(TestingHelpers.ConvertFilePath(@"C:\RoboClerkTest2\TestStuff5.cs")));
        }

        [UnitTestAttribute(
        Identifier = "DA02C319-2C9E-4FA0-BCAB-B9DDA56A1E8B",
        Purpose = "SourceCodeAnalysisPlugin is created and initialized, subdirs set to true",
        PostCondition = "Expected files found")]
        [Test]
        public void TestSourceCodeAnalysisPlugin4()
        {
            string tomlFile = string.Empty;
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                tomlFile = @"
[[TestConfigurations]]
Language = ""csharp""
TestDirectory = ""/C/RoboClerkTest""
SubDirs = true
FileMasks = [""Test*.cs""]
Project = ""TestProject1""

[[TestConfigurations]]
Language = ""csharp""
TestDirectory = ""/C/RoboClerkTest2""
SubDirs = true
FileMasks = [""Test*.cs""]
Project = ""TestProject2""

UseGit = false";
            }
            else
            {
                tomlFile = @"
[[TestConfigurations]]
Language = ""csharp""
TestDirectory = ""C:\\RoboClerkTest""
SubDirs = true
FileMasks = [""Test*.cs""]
Project = ""TestProject1""

[[TestConfigurations]]
Language = ""csharp""
TestDirectory = ""C:\\RoboClerkTest2""
SubDirs = true
FileMasks = [""Test*.cs""]
Project = ""TestProject2""

UseGit = false";
            }

            fs.File.WriteAllText(@"c:\TestSourceCodeAnalysisPlugin.toml", tomlFile);

            var testPlugin = new TestSourceCodeAnalysisPlugin(fileProvider);
            testPlugin.InitializePlugin(config);

            testPlugin.RefreshItems();
            Assert.That(testPlugin.SourceFiles.Count, Is.EqualTo(4));
            Assert.That(testPlugin.SourceFiles[0], Is.EqualTo(TestingHelpers.ConvertFilePath(@"C:\RoboClerkTest\TestStuff1.cs")));
            Assert.That(testPlugin.SourceFiles[1], Is.EqualTo(TestingHelpers.ConvertFilePath(@"C:\RoboClerkTest\TestStuff3.cs")));
            Assert.That(testPlugin.SourceFiles[2], Is.EqualTo(TestingHelpers.ConvertFilePath(@"C:\RoboClerkTest\SubDir\TestStuff4.cs")));
            Assert.That(testPlugin.SourceFiles[3], Is.EqualTo(TestingHelpers.ConvertFilePath(@"C:\RoboClerkTest2\TestStuff5.cs")));
        }

        [UnitTestAttribute(
        Identifier = "8C3E62E3-8B03-4765-8B97-26F5B79E6CFA",
        Purpose = "SourceCodeAnalysisPlugin is created and initialized, subdirs set to false, two filemasks are provided",
        PostCondition = "Expected files found")]
        [Test]
        public void TestSourceCodeAnalysisPlugin5()
        {
            string tomlFile = string.Empty;
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                tomlFile = @"
[[TestConfigurations]]
Language = ""csharp""
TestDirectory = ""/C/RoboClerkTest""
SubDirs = false
FileMasks = [""Test*.cs"",""Test*.txt""]
Project = ""TestProject1""

[[TestConfigurations]]
Language = ""csharp""
TestDirectory = ""/C/RoboClerkTest2""
SubDirs = false
FileMasks = [""Test*.cs""]
Project = ""TestProject2""

UseGit = false";
            }
            else
            {
                tomlFile = @"
[[TestConfigurations]]
Language = ""csharp""
TestDirectory = ""C:\\RoboClerkTest""
SubDirs = false
FileMasks = [""Test*.cs"",""Test*.txt""]
Project = ""TestProject1""

[[TestConfigurations]]
Language = ""csharp""
TestDirectory = ""C:\\RoboClerkTest2""
SubDirs = false
FileMasks = [""Test*.cs""]
Project = ""TestProject2""

UseGit = false";
            }

            fs.File.WriteAllText(TestingHelpers.ConvertFilePath(@"C:\TestSourceCodeAnalysisPlugin.toml"), tomlFile);

            var testPlugin = new TestSourceCodeAnalysisPlugin(fileProvider);
            testPlugin.InitializePlugin(config);

            testPlugin.RefreshItems();
            Assert.That(testPlugin.SourceFiles.Count, Is.EqualTo(4));
            Assert.That(testPlugin.SourceFiles[0], Is.EqualTo(TestingHelpers.ConvertFilePath(@"C:\RoboClerkTest\TestStuff1.cs")));
            Assert.That(testPlugin.SourceFiles[1], Is.EqualTo(TestingHelpers.ConvertFilePath(@"C:\RoboClerkTest\TestStuff3.cs")));
            Assert.That(testPlugin.SourceFiles[2], Is.EqualTo(TestingHelpers.ConvertFilePath(@"C:\RoboClerkTest\TestStuff2.txt")));
            Assert.That(testPlugin.SourceFiles[3], Is.EqualTo(TestingHelpers.ConvertFilePath(@"C:\RoboClerkTest2\TestStuff5.cs")));
        }

        [UnitTestAttribute(
        Identifier = "AAE8D0FE-61C4-4E89-A042-BDECEE4D4EAE",
        Purpose = "SourceCodeAnalysisPlugin is created and initialized, subdirs set to false, one of the provided directories does not exist",
        PostCondition = "Exception thrown")]
        [Test]
        public void TestSourceCodeAnalysisPlugin6()
        {
            string tomlFile = string.Empty;
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                tomlFile = @"
[[TestConfigurations]]
Language = ""csharp""
TestDirectory = ""/C/RoboClerkTest""
SubDirs = false
FileMasks = [""Test*.cs"",""Test*.txt""]
Project = ""TestProject1""

[[TestConfigurations]]
Language = ""csharp""
TestDirectory = ""/C/RoboClerkTest3""
SubDirs = false
FileMasks = [""Test*.cs""]
Project = ""TestProject2""

UseGit = false";
            }
            else
            {
                tomlFile = @"
[[TestConfigurations]]
Language = ""csharp""
TestDirectory = ""C:\\RoboClerkTest""
SubDirs = false
FileMasks = [""Test*.cs"",""Test*.txt""]
Project = ""TestProject1""

[[TestConfigurations]]
Language = ""csharp""
TestDirectory = ""C:\\RoboClerkTest3""
SubDirs = false
FileMasks = [""Test*.cs""]
Project = ""TestProject2""

UseGit = false";
            }

            fs.File.WriteAllText(TestingHelpers.ConvertFilePath(@"C:\TestSourceCodeAnalysisPlugin.toml"), tomlFile);

            var testPlugin = new TestSourceCodeAnalysisPlugin(fileProvider);
            testPlugin.InitializePlugin(config);

            Assert.Throws<System.IO.DirectoryNotFoundException>(()=>testPlugin.RefreshItems());
        }

    }
}
