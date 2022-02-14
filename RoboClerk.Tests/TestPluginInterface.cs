using NUnit.Framework;
using NSubstitute;

namespace RoboClerk.Tests
{
    internal static class GenerateConfigFiles
    {
        internal static string testConfigFile = @"# This is the RoboClerk configuration file

# Provide the names of the DataSource plugin(s) in a comma seperated list
# For example: DataSourcePlugin = [ ""AzureDevOpsSLMSPlugin"", ""RedmineCSVSLMSPlugin"" ]
DataSourcePlugin = [ ""testplugin1"", ""testplugin2"" ]

# These plugin directories are relative to the executing assembly. RoboClerk will
# search for plugins in the subdirectories of the relative plugin dirs.
# For example: RelativePluginDirs = [ ""plugins"" ]
        RelativePluginDirs = [ ""testdir1"", ""testdir2"" ]

# The output directory is where the output files and logs are placed
        OutputDirectory = ""C:/temp/roboclerk""

# Logging level (DEBUG or INFO)
LogLevel = ""INFO""";

        internal static string testProjectConfig = @"# Truth items are those items retrieved from the source of truth (e.g. the SLMS). Specify how the these truth trace entities
# should be called. This will ensure that the trace output makes sense in your context.

[Truth.SystemRequirement]
	name = ""Requirement""
	abbreviation = ""SYS""

[Truth.SoftwareRequirement]
        name = ""Specification""
	abbreviation = ""SWR""

[Truth.SoftwareSystemTest]
        name = ""Test Case""
	abbreviation = ""TC""

[Truth.SoftwareUnitTest]
        name = ""Unit Test""
	abbreviation = ""UT""

[Truth.Anomaly]
        name = ""Bug""
	abbreviation = ""BG""

[Document.SoftwareRequirementsSpecification]
	title = ""Software Requirements Specification""
	abbreviation = ""SRS""
	template = ""I:/temp/roboclerk_input/SoftwareRequirementSpecification.md""
	[[Document.SoftwareRequirementsSpecification.Command]]
		executable = ""pandoc""
		arguments = ""-s %OUTPUTDIR%/SoftwareRequirementSpecification.md --reference-doc=I:/code/RoboClerk/DocxProcessingScripts/General_DOCX_template.docx -o %OUTPUTDIR%/SoftwareRequirementSpecification.docx""
		workingDirectory = "" ""
		ignoreErrors = ""False""

[TraceConfig]
	
    [TraceConfig.SoftwareRequirement]
        SoftwareRequirementsSpecification = [""ALL""]
        SystemRequirement = [""ALL""]
        RiskAssessmentRecord = [""Risk Control Measure""]
        SystemLevelTestPlan = [""ALL""]

[ConfigValues]

CompanyName = ""Test Inc.""
SoftwareName = ""RoboClerk Tests""
SoftwareVersion = ""0.1b""
";

    };

    public class DataSourcesTest : DataSources
    {
        public DataSourcesTest(string configFile, string projectConfig, IPluginLoader loader)
        {
            pluginLoader = loader;
            base(configFile, projectConfig);
        }
    }

    [TestFixture]
    [Description("These tests test the plugin interface")]
    internal class TestPluginInterface
    {
        ISLMSPlugin testPlugin = Substitute.For<ISLMSPlugin>();
        IPluginLoader mockPluginLoader = Substitute.For<IPluginLoader>();
        DataSources data = null;

        [SetUp]
        public void TestSetup()
        {
            
        }

        [Test]
        public void Successful_Creation_DataSources()
        {
            Assert.DoesNotThrow(() => new DataSourcesTest(GenerateConfigFiles.testConfigFile, GenerateConfigFiles.testProjectConfig, mockPluginLoader));
        }

        [Test]
        public void DataSources_Traverses_All_Plugins_And_All_Directories()
        {
            Assert.DoesNotThrow(() => new DataSourcesTest(GenerateConfigFiles.testConfigFile, GenerateConfigFiles.testProjectConfig, mockPluginLoader));
            mockPluginLoader.Received().LoadPlugin<ISLMSPlugin>(Arg.Is<string>("testplugin1"), Arg.Is<string>("testdir1"));
        }


    }
}
