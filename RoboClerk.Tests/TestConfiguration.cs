using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("These tests test the RoboClerk Configuration object")]
    internal class TestConfiguration
    {
        private IFileSystem fs = null;
        private Dictionary<string, string> cmdOptions = null;
        [SetUp]
        public void TestSetup()
        {
            cmdOptions = new Dictionary<string, string>();
            string roboConf = @"
DataSourcePlugin = [ ""RedmineSLMSPlugin"", ""DependenciesFilePlugin"" ]
PluginDirs = [ ""I:/test/plugindir"" ]
PluginConfigurationDir = ""testdir""
OutputDirectory = ""I:/temp/Roboclerk_output""
ClearOutputDir = ""True""
LogLevel = ""INFO""";

            string roboProjectConf = @"
TemplateDirectory = ""I:/temp/roboclerk_input/""
ProjectRoot = ""I:/code/RoboClerk""
MediaDirectory = ""I:/temp/Roboclerk_input/media""
[Truth.SystemRequirement]
	name = ""Requirement""
	abbreviation = ""SYS""
[Truth.SoftwareRequirement]
	name = ""SWRRequirement""
	abbreviation = ""SWR""
[Document.SystemRequirementsSpecification]
	title = ""System Requirements Specification""
	abbreviation = ""PRS""
	identifier = ""DOC001""
	template = ""SystemRequirementSpecification.adoc""
	[[Document.SystemRequirementsSpecification.Command]]
		executable = ""docker""
		arguments = ""run -a stdout -a stderr -v \""I:/Temp/\"":/mnt --rm roboclerk asciidoctor -r asciidoctor-kroki /mnt/Roboclerk_output/%OUTPUTFILE% --backend docbook""
		workingDirectory = """"
        ignoreErrors = ""False""
[TraceConfig]
    [TraceConfig.SystemRequirement]
        SystemRequirementsSpecification.forward = [""ALL1""]
        SystemRequirementsSpecification.backward = [""ALL2""]
        SystemRequirementsSpecification.forwardLink = ""DOC""
        SystemRequirementsSpecification.backwardLink = ""DOC""
        SoftwareRequirement.forward = [""ALL3""]
        SoftwareRequirement.backward = [""ALL4""]
        SoftwareRequirement.forwardLink = ""Child""
        SoftwareRequirement.backwardLink = ""Parent""
[CheckpointConfiguration]
CheckpointFile = ""C:/chkpt.json""
UpdatedSystemRequirementIDs = [""test1""]
UpdatedSoftwareRequirementIDs = [""test2""]
UpdatedSoftwareSystemTestIDs = [""test3""]
UpdatedSoftwareUnitTestIDs = [""test4""]
UpdatedRiskIDs = [""test5""]
UpdatedAnomalyIDs = [""test6"",""test8""]
UpdatedSOUPIDs = [""test7""]
UpdatedDocumentationRequirementIDs = [""test9""]
UpdatedDocContentIDs = [""test10""]
[ConfigValues]
CompanyName = ""Acme Inc.""
";
            fs = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"c:\temp\roboConf.toml", new MockFileData(roboConf) },
                { @"c:\temp\roboProjectConf.toml", new MockFileData(roboProjectConf) }
            });
        }

        [UnitTestAttribute(
        Identifier = "4FD605FF-34C8-4DE1-9372-C717F9665EAE",
        Purpose = "Configuration object is created",
        PostCondition = "No exception is thrown")]
        [Test]
        public void CreateConfiguration()
        {
            var conf = new RoboClerk.Configuration.Configuration(fs, @"C:\temp\roboConf.toml", @"C:\temp\roboProjectConf.toml", cmdOptions);
        }

        [UnitTestAttribute(
        Identifier = "87A3EEB5-67F2-42DA-8674-3FDAA2930897",
        Purpose = "Configuration object is created",
        PostCondition = "All properties have the expected values")]
        [Test]
        public void TestConfiguration1()
        {
            var conf = new RoboClerk.Configuration.Configuration(fs, @"C:\temp\roboConf.toml", @"C:\temp\roboProjectConf.toml", cmdOptions);

            Assert.That(conf.DataSourcePlugins.Count,Is.EqualTo(2));
            Assert.That(conf.DataSourcePlugins[1], Is.EqualTo("DependenciesFilePlugin"));

            Assert.That(conf.PluginDirs.Count, Is.EqualTo(1));
            Assert.That(conf.PluginDirs[0], Is.EqualTo("I:/test/plugindir"));

            Assert.That(conf.OutputDir, Is.EqualTo("I:/temp/Roboclerk_output"));

            Assert.That(conf.LogLevel, Is.EqualTo("INFO"));

            Assert.That(conf.TruthEntities.Count, Is.EqualTo(2));
            Assert.That(conf.TruthEntities[0].ID, Is.EqualTo("SystemRequirement"));
            Assert.That(conf.TruthEntities[1].ID, Is.EqualTo("SoftwareRequirement"));
            Assert.That(conf.TruthEntities[0].Name, Is.EqualTo("Requirement"));
            Assert.That(conf.TruthEntities[1].Name, Is.EqualTo("SWRRequirement"));
            Assert.That(conf.TruthEntities[0].Abbreviation, Is.EqualTo("SYS"));
            Assert.That(conf.TruthEntities[1].Abbreviation, Is.EqualTo("SWR"));

            Assert.That(conf.Documents.Count,Is.EqualTo(1));
            Assert.That(conf.Documents[0].DocumentID, Is.EqualTo("DOC001"));
            Assert.That(conf.Documents[0].DocumentTitle, Is.EqualTo("System Requirements Specification"));
            Assert.That(conf.Documents[0].DocumentTemplate, Is.EqualTo("SystemRequirementSpecification.adoc"));

            Assert.That(conf.TraceConfig.Count, Is.EqualTo(1));
            Assert.That(conf.TraceConfig[0].ID, Is.EqualTo("SystemRequirement"));
            Assert.That(conf.TraceConfig[0].Traces.Count, Is.EqualTo(2));
            Assert.That(conf.TraceConfig[0].Traces["SystemRequirementsSpecification"].ForwardFilters.Count, Is.EqualTo(1));
            Assert.That(conf.TraceConfig[0].Traces["SystemRequirementsSpecification"].ForwardFilters[0], Is.EqualTo("ALL1"));
            Assert.That(conf.TraceConfig[0].Traces["SystemRequirementsSpecification"].BackwardFilters.Count, Is.EqualTo(1));
            Assert.That(conf.TraceConfig[0].Traces["SystemRequirementsSpecification"].BackwardFilters[0], Is.EqualTo("ALL2"));
            Assert.That(conf.TraceConfig[0].Traces["SystemRequirementsSpecification"].ForwardLinkType, Is.EqualTo("DOC"));
            Assert.That(conf.TraceConfig[0].Traces["SystemRequirementsSpecification"].BackwardLinkType, Is.EqualTo("DOC"));

            Assert.That(conf.TraceConfig[0].Traces["SoftwareRequirement"].ForwardFilters.Count, Is.EqualTo(1));
            Assert.That(conf.TraceConfig[0].Traces["SoftwareRequirement"].ForwardFilters[0], Is.EqualTo("ALL3"));
            Assert.That(conf.TraceConfig[0].Traces["SoftwareRequirement"].BackwardFilters.Count, Is.EqualTo(1));
            Assert.That(conf.TraceConfig[0].Traces["SoftwareRequirement"].BackwardFilters[0], Is.EqualTo("ALL4"));
            Assert.That(conf.TraceConfig[0].Traces["SoftwareRequirement"].ForwardLinkType, Is.EqualTo("Child"));
            Assert.That(conf.TraceConfig[0].Traces["SoftwareRequirement"].BackwardLinkType, Is.EqualTo("Parent"));

            var checkPointCfg = conf.CheckpointConfig;
            Assert.That(checkPointCfg.CheckpointFile, Is.EqualTo("C:/chkpt.json"));
            Assert.That(checkPointCfg.UpdatedSystemRequirementIDs.Count, Is.EqualTo(1));
            Assert.That(checkPointCfg.UpdatedSystemRequirementIDs[0], Is.EqualTo("test1"));
            Assert.That(checkPointCfg.UpdatedSoftwareRequirementIDs.Count, Is.EqualTo(1));
            Assert.That(checkPointCfg.UpdatedSoftwareRequirementIDs[0], Is.EqualTo("test2"));
            Assert.That(checkPointCfg.UpdatedSoftwareSystemTestIDs.Count, Is.EqualTo(1));
            Assert.That(checkPointCfg.UpdatedSoftwareSystemTestIDs[0], Is.EqualTo("test3"));
            Assert.That(checkPointCfg.UpdatedSoftwareUnitTestIDs.Count, Is.EqualTo(1));
            Assert.That(checkPointCfg.UpdatedSoftwareUnitTestIDs[0], Is.EqualTo("test4"));
            Assert.That(checkPointCfg.UpdatedRiskIDs.Count, Is.EqualTo(1));
            Assert.That(checkPointCfg.UpdatedRiskIDs[0], Is.EqualTo("test5"));
            Assert.That(checkPointCfg.UpdatedAnomalyIDs.Count, Is.EqualTo(2));
            Assert.That(checkPointCfg.UpdatedAnomalyIDs[0], Is.EqualTo("test6"));
            Assert.That(checkPointCfg.UpdatedAnomalyIDs[1], Is.EqualTo("test8"));
            Assert.That(checkPointCfg.UpdatedSOUPIDs.Count, Is.EqualTo(1));
            Assert.That(checkPointCfg.UpdatedSOUPIDs[0], Is.EqualTo("test7"));
            Assert.That(checkPointCfg.UpdatedDocumentationRequirementIDs.Count, Is.EqualTo(1));
            Assert.That(checkPointCfg.UpdatedDocumentationRequirementIDs[0], Is.EqualTo("test9"));
            Assert.That(checkPointCfg.UpdatedDocContentIDs.Count, Is.EqualTo(1));
            Assert.That(checkPointCfg.UpdatedDocContentIDs[0], Is.EqualTo("test10"));

            Assert.That(conf.ConfigVals.HasKey("CompanyName"));
            Assert.That(conf.ConfigVals.GetValue("CompanyName"), Is.EqualTo("Acme Inc."));

            Assert.That(conf.PluginConfigDir, Is.EqualTo("testdir"));
            Assert.That(conf.TemplateDir, Is.EqualTo("I:/temp/roboclerk_input/"));
            Assert.That(conf.MediaDir, Is.EqualTo("I:/temp/Roboclerk_input/media"));
            Assert.That(conf.ProjectRoot, Is.EqualTo("I:/code/RoboClerk"));
            Assert.That(conf.ClearOutputDir, Is.EqualTo(true));
        }

        [UnitTestAttribute(
        Identifier = "8D7DE00F-FD64-406D-BF7B-27FB61890C7A",
        Purpose = "Configuration object is created, checkpoint config is missing",
        PostCondition = "No exception is thrown.")]
        [Test]
        public void TestConfiguration5()
        {
            string roboProjectConf = @"
TemplateDirectory = ""I:/temp/roboclerk_input/""
ProjectRoot = ""I:/code/RoboClerk""
MediaDirectory = ""I:/temp/Roboclerk_input/media""
[Truth.SystemRequirement]
	name = ""Requirement""
	abbreviation = ""SYS""
[Truth.SoftwareRequirement]
	name = ""SWRRequirement""
	abbreviation = ""SWR""
[Document.SystemRequirementsSpecification]
	title = ""System Requirements Specification""
	abbreviation = ""PRS""
	identifier = ""DOC001""
	template = ""SystemRequirementSpecification.adoc""
	[[Document.SystemRequirementsSpecification.Command]]
		executable = ""docker""
		arguments = ""run -a stdout -a stderr -v \""I:/Temp/\"":/mnt --rm roboclerk asciidoctor -r asciidoctor-kroki /mnt/Roboclerk_output/%OUTPUTFILE% --backend docbook""
		workingDirectory = """"
        ignoreErrors = ""False""
[TraceConfig]
    [TraceConfig.SystemRequirement]
        SystemRequirementsSpecification.forward = [""ALL1""]
        SystemRequirementsSpecification.backward = [""ALL2""]
        SystemRequirementsSpecification.forwardLink = ""DOC""
        SystemRequirementsSpecification.backwardLink = ""DOC""
        SoftwareRequirement.forward = [""ALL3""]
        SoftwareRequirement.backward = [""ALL4""]
        SoftwareRequirement.forwardLink = ""Child""
        SoftwareRequirement.backwardLink = ""Parent""
[ConfigValues]
CompanyName = ""Acme Inc.""
";
            fs.File.WriteAllText(@"C:\temp\confMissing.toml", roboProjectConf);
            Assert.DoesNotThrow(() => new RoboClerk.Configuration.Configuration(fs, @"C:\temp\roboConf.toml", @"C:\temp\confMissing.toml", cmdOptions));
        }

        [UnitTestAttribute(
        Identifier = "E7EECA82-EE1A-4F5C-8234-5D750666A24B",
        Purpose = "Configuration object is created using invalid general config file location",
        PostCondition = "Exception is thrown")]
        [Test]
        public void TestConfiguration2()
        {
            Assert.Throws<FileNotFoundException>(()=> new RoboClerk.Configuration.Configuration(fs, @"C:\temp\roboConf_invalid.toml", @"C:\temp\roboProjectConf.toml", cmdOptions));
        }

        [UnitTestAttribute(
        Identifier = "DAC2CC5A-06B7-4D73-9EC6-49984E6AC48C",
        Purpose = "Configuration object is created using invalid project config file location",
        PostCondition = "Exception is thrown")]
        [Test]
        public void TestConfiguration3()
        {
            Assert.Throws<FileNotFoundException>(() => new RoboClerk.Configuration.Configuration(fs, @"C:\temp\roboConf.toml", @"C:\temp\roboProjectConf_invalid.toml", cmdOptions));
        }

        [UnitTestAttribute(
        Identifier = "DE35615D-7778-4B37-ADDE-3A50BE16C0F0",
        Purpose = "Configuration object is created using with a supplied commanline option",
        PostCondition = "Commandline option overrides config option")]
        [Test]
        public void TestConfiguration4()
        {
            cmdOptions.Add("CheckpointFile", "testvalue");
            var conf = new RoboClerk.Configuration.Configuration(fs, @"C:\temp\roboConf.toml", @"C:\temp\roboProjectConf.toml", cmdOptions);
            Assert.That(conf.CommandLineOptionOrDefault("CheckpointFile", "error"), Is.EqualTo("testvalue"));
        }
    }
}
