using NUnit.Framework;
using NSubstitute;
using RoboClerk.Configuration;
using System.Collections.Generic;
using NSubstitute.Extensions;
using Tomlyn.Model;
using System.IO.Abstractions;
using System;
using System.IO.Abstractions.TestingHelpers;
using System.IO;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("These tests test the DataSources class")]
    internal class TestDatasources
    {
        ISLMSPlugin mockSLMSPlugin = null; 
        IDependencyManagementPlugin mockDepMgmtPlugin = null;
        ISourceCodeAnalysisPlugin mockSrcCodePlugin = null;
        IPluginLoader mockPluginLoader = null; 
        IConfiguration mockConfiguration = null;
        IFileSystem mockFileSystem = null;

        [SetUp]
        public void TestSetup()
        {
            mockSLMSPlugin = Substitute.For<ISLMSPlugin>();
            mockSLMSPlugin.Name.Returns("SLMS Test Plugin");
            mockSLMSPlugin.Description.Returns("SLMS Test Plugin Description");
            mockDepMgmtPlugin = Substitute.For<IDependencyManagementPlugin>();
            mockDepMgmtPlugin.Name.Returns("Dependency Test Plugin");
            mockDepMgmtPlugin.Description.Returns("Dependency Test Plugin Description");
            mockSrcCodePlugin = Substitute.For<ISourceCodeAnalysisPlugin>();
            mockSrcCodePlugin.Name.Returns("Source Code Analysis Plugin");
            mockSrcCodePlugin.Description.Returns("Source Code Analysis Plugin Description");
            mockPluginLoader = Substitute.For<IPluginLoader>();
            mockConfiguration = Substitute.For<IConfiguration>();

            mockConfiguration.DataSourcePlugins.ReturnsForAnyArgs(new List<string> { "testPlugin1", "testPlugin2", "testDepPlugin", "testSrcPlugin" });
            mockConfiguration.PluginDirs.ReturnsForAnyArgs(new List<string> { "c:\\temp\\does_not_exist", "c:\\temp\\" });
            mockPluginLoader.LoadPlugin<IPlugin>(Arg.Any<string>(), Arg.Any<string>()).Returns<IPlugin>(l => null);
            mockPluginLoader.Configure().LoadPlugin<IPlugin>(Arg.Is("testPlugin2"), Arg.Is("c:\\temp\\does_not_exist")).Returns(mockSLMSPlugin);
            mockPluginLoader.Configure().LoadPlugin<IPlugin>(Arg.Is("testDepPlugin"), Arg.Is("c:\\temp\\does_not_exist")).Returns(mockDepMgmtPlugin);
            mockPluginLoader.Configure().LoadPlugin<IPlugin>(Arg.Is("testSrcPlugin"), Arg.Is("c:\\temp\\does_not_exist")).Returns(mockSrcCodePlugin);
        }

        [Test]
        public void Successful_Creation_DataSources_VERIFIES_DataSources_Class_Creation_No_Throw()
        {
            Assert.DoesNotThrow(() => new DataSources(mockConfiguration,mockPluginLoader,mockFileSystem));
        }

        [Test]
        public void Plugin_Search_Functionality_Works_Correctly_VERIFIES_DataSources_Traverses_All_Plugins_And_All_Directories()
        {
            var ds = new DataSources(mockConfiguration, mockPluginLoader, mockFileSystem);
            Assert.DoesNotThrow(() => mockPluginLoader.Received().LoadPlugin<IPlugin>(Arg.Is<string>("testPlugin1"), Arg.Is<string>("c:\\temp\\does_not_exist")));
            Assert.DoesNotThrow(() => mockPluginLoader.Received().LoadPlugin<IPlugin>(Arg.Is<string>("testPlugin1"), Arg.Is<string>("c:\\temp\\")));
            Assert.DoesNotThrow(() => mockPluginLoader.Received().LoadPlugin<IPlugin>(Arg.Is<string>("testPlugin2"), Arg.Is<string>("c:\\temp\\does_not_exist")));
            Assert.DoesNotThrow(() => mockPluginLoader.Received().LoadPlugin<IPlugin>(Arg.Is<string>("testPlugin2"), Arg.Is<string>("c:\\temp\\")));
        }

        private List<RequirementItem> SYSs = null;
        private List<RequirementItem> SWRs = null;
        private List<TestCaseItem> TCs = null;
        private List<AnomalyItem> ANOMALYs = null;
        private List<UnitTestItem> SLMSUTs = null;
        private List<UnitTestItem> SRCUTs = null;
        private List<ExternalDependency> DEPs = null;
        private List<SOUPItem> SOUPs = null;
        private List<RiskItem> RISKs = null;
        private void SetupSLMSPlugin()
        {
            SYSs = new List<RequirementItem> { new RequirementItem(RequirementType.SystemRequirement), new RequirementItem(RequirementType.SystemRequirement) };
            SYSs[0].RequirementTitle = "SYS_TestTitle1";
            SYSs[0].ItemID = "SYS_id1";
            SYSs[1].RequirementTitle = "SYS_TestTitle2";
            SYSs[1].ItemID = "SYS_id2";
            mockSLMSPlugin.GetSystemRequirements().Returns(SYSs);
            SWRs = new List<RequirementItem> { new RequirementItem(RequirementType.SoftwareRequirement), new RequirementItem(RequirementType.SoftwareRequirement) };
            SWRs[0].RequirementTitle = "SWR_TestTitle1";
            SWRs[0].ItemID = "SWR_id1";
            SWRs[1].RequirementTitle = "SWR_TestTitle2";
            SWRs[1].ItemID = "SWR_id2";
            mockSLMSPlugin.GetSoftwareRequirements().Returns(SWRs);
            TCs = new List<TestCaseItem> { new TestCaseItem(), new TestCaseItem() };
            TCs[0].TestCaseTitle = "TC_TestTitle1";
            TCs[0].ItemID = "TC_id1";
            TCs[1].TestCaseTitle = "TC_TestTitle2";
            TCs[1].ItemID = "TC_id2";
            mockSLMSPlugin.GetSoftwareSystemTests().Returns(TCs);
            ANOMALYs = new List<AnomalyItem> { new AnomalyItem(), new AnomalyItem() };
            ANOMALYs[0].AnomalyTitle = "ANOMALY_TestTitle1";
            ANOMALYs[0].ItemID = "ANOMALY_id1";
            ANOMALYs[1].AnomalyTitle = "ANOMALY_TestTitle2";
            ANOMALYs[1].ItemID = "ANOMALY_id2";
            mockSLMSPlugin.GetAnomalies().Returns(ANOMALYs);
            SLMSUTs = new List<UnitTestItem> { new UnitTestItem(), new UnitTestItem() };
            SLMSUTs[0].UnitTestPurpose = "UT_TestPurpose1";
            SLMSUTs[0].ItemID = "UT_id1";
            SLMSUTs[1].UnitTestPurpose = "UT_TestPurpose2";
            SLMSUTs[1].ItemID = "UT_id2";
            mockSLMSPlugin.GetUnitTests().Returns(SLMSUTs);
            SOUPs = new List<SOUPItem> { new SOUPItem(), new SOUPItem() };
            SOUPs[0].SOUPName = "SOUP_name1";
            SOUPs[0].SOUPLicense = "SOUP_license1";
            SOUPs[0].SOUPDetailedDescription = "SOUP_details1";
            SOUPs[0].ItemID = "SOUP_id1";
            SOUPs[1].SOUPName = "SOUP_name2";
            SOUPs[1].SOUPLicense = "SOUP_license2";
            SOUPs[1].SOUPDetailedDescription = "SOUP_details2";
            SOUPs[1].ItemID = "SOUP_id2";
            mockSLMSPlugin.GetSOUP().Returns(SOUPs);
            RISKs = new List<RiskItem> { new RiskItem(), new RiskItem() };
            RISKs[0].DetectabilityScore = 100;
            RISKs[0].CauseOfFailure = "RISK_cause1";
            RISKs[0].ItemID = "RISK_id1";
            RISKs[1].DetectabilityScore = 200;
            RISKs[1].CauseOfFailure = "RISK_cause2";
            RISKs[1].ItemID = "RISK_id2";
            mockSLMSPlugin.GetRisks().Returns(RISKs);
        }

        private void SetupDepPlugin()
        {
            DEPs = new List<ExternalDependency> 
                { new ExternalDependency("testname1", "testversion1", false), 
                  new ExternalDependency("testname2", "testversion2", true) };
            mockDepMgmtPlugin.GetDependencies().Returns(DEPs);
        }

        private void SetupSrcCodePlugin()
        {
            SRCUTs = new List<UnitTestItem> { new UnitTestItem(), new UnitTestItem() };
            SRCUTs[0].UnitTestPurpose = "UT_TestPurpose3";
            SRCUTs[0].ItemID = "UT_id3";
            SRCUTs[1].UnitTestPurpose = "UT_TestPurpose4";
            SRCUTs[1].ItemID = "UT_id4";
            mockSrcCodePlugin.GetUnitTests().Returns(SRCUTs);
        }

        [Test]
        public void System_Requirements_Can_Be_Retrieved_VERIFIES_Supplied_Requirements_Are_Returned()
        {
            SetupSLMSPlugin();
            var ds = new DataSources(mockConfiguration, mockPluginLoader, mockFileSystem);
            var returnedReqs = ds.GetAllSystemRequirements();
            Assert.AreSame(SYSs[0], returnedReqs[0]);
            Assert.AreSame(SYSs[1], returnedReqs[1]);
            Assert.AreSame(SYSs[1], ds.GetSystemRequirement("SYS_id2"));
            Assert.AreSame(SYSs[0], ds.GetItem("SYS_id1"));
            var returnedItems = ds.GetItems(new TraceEntity("SystemRequirement", "Requirement", "SYS", TraceEntityType.Truth));
            Assert.That(returnedItems[0].ItemID, Is.EqualTo(SYSs[0].ItemID));
            Assert.That(returnedItems[1].ItemID, Is.EqualTo(SYSs[1].ItemID));
        }

        [Test]
        public void Software_Requirements_Can_Be_Retrieved_VERIFIES_Supplied_Requirements_Are_Returned()
        {
            SetupSLMSPlugin();
            var ds = new DataSources(mockConfiguration, mockPluginLoader, mockFileSystem);
            var returnedReqs = ds.GetAllSoftwareRequirements();
            Assert.AreSame(SWRs[0], returnedReqs[0]);
            Assert.AreSame(SWRs[1], returnedReqs[1]);
            Assert.AreSame(SWRs[1], ds.GetSoftwareRequirement("SWR_id2"));
            Assert.AreSame(SWRs[0], ds.GetItem("SWR_id1"));
            var returnedItems = ds.GetItems(new TraceEntity("SoftwareRequirement", "Requirement", "SWR", TraceEntityType.Truth));
            Assert.That(returnedItems[0].ItemID, Is.EqualTo(SWRs[0].ItemID));
            Assert.That(returnedItems[1].ItemID, Is.EqualTo(SWRs[1].ItemID));
        }

        [Test]
        public void Test_Cases_Can_Be_Retrieved_VERIFIES_Supplied_Test_Cases_Are_Returned()
        {
            SetupSLMSPlugin();
            var ds = new DataSources(mockConfiguration, mockPluginLoader, mockFileSystem);
            var returnedReqs = ds.GetAllSoftwareSystemTests();
            Assert.AreSame(TCs[0], returnedReqs[0]);
            Assert.AreSame(TCs[1], returnedReqs[1]);
            Assert.AreSame(TCs[1], ds.GetSoftwareSystemTest("TC_id2"));
            Assert.AreSame(TCs[0], ds.GetItem("TC_id1"));
            var returnedItems = ds.GetItems(new TraceEntity("SoftwareSystemTest", "System Level Test", "SLT", TraceEntityType.Truth));
            Assert.That(returnedItems[0].ItemID, Is.EqualTo(TCs[0].ItemID));
            Assert.That(returnedItems[1].ItemID, Is.EqualTo(TCs[1].ItemID));
        }

        [Test]
        public void SOUP_Items_Can_Be_Retrieved_VERIFIES_Supplied_SOUP_Items_Are_Returned()
        {
            SetupSLMSPlugin();
            SetupSrcCodePlugin();
            var ds = new DataSources(mockConfiguration, mockPluginLoader, mockFileSystem);
            var returnedSOUP = ds.GetAllSOUP();
            Assert.AreSame(SOUPs[0], returnedSOUP[0]);
            Assert.AreSame(SOUPs[1], returnedSOUP[1]);
            Assert.AreSame(SOUPs[1], ds.GetSOUP("SOUP_id2"));
            Assert.AreSame(SOUPs[0], ds.GetItem("SOUP_id1"));
            var returnedItems = ds.GetItems(new TraceEntity("SOUP", "SOUP", "OTS", TraceEntityType.Truth));
            Assert.That(returnedItems[0].ItemID, Is.EqualTo(SOUPs[0].ItemID));
            Assert.That(returnedItems[1].ItemID, Is.EqualTo(SOUPs[1].ItemID));
        }

        [Test]
        public void RISKs_Can_Be_Retrieved_VERIFIES_Supplied_RISKs_Are_Returned()
        {
            SetupSLMSPlugin();
            SetupSrcCodePlugin();
            var ds = new DataSources(mockConfiguration, mockPluginLoader, mockFileSystem);
            var returnedRISKs = ds.GetAllRisks();
            Assert.AreSame(RISKs[0], returnedRISKs[0]);
            Assert.AreSame(RISKs[1], returnedRISKs[1]);
            Assert.AreSame(RISKs[1], ds.GetRisk("RISK_id2"));
            Assert.AreSame(RISKs[0], ds.GetItem("RISK_id1"));
            var returnedItems = ds.GetItems(new TraceEntity("Risk", "Risk", "RSK", TraceEntityType.Truth));
            Assert.That(returnedItems[0].ItemID, Is.EqualTo(RISKs[0].ItemID));
            Assert.That(returnedItems[1].ItemID, Is.EqualTo(RISKs[1].ItemID));
        }

        [Test]
        public void Unit_Tests_Can_Be_Retrieved_VERIFIES_Supplied_Unit_Tests_Are_Returned()
        {
            SetupSLMSPlugin();
            SetupSrcCodePlugin();
            var ds = new DataSources(mockConfiguration, mockPluginLoader, mockFileSystem);
            var returnedUTs = ds.GetAllSoftwareUnitTests();
            Assert.AreSame(SLMSUTs[0], returnedUTs[0]);
            Assert.AreSame(SLMSUTs[1], returnedUTs[1]);
            Assert.AreSame(SRCUTs[0], returnedUTs[2]);
            Assert.AreSame(SRCUTs[1], returnedUTs[3]);
            Assert.AreSame(returnedUTs[2], ds.GetSoftwareUnitTest("UT_id3"));
            Assert.AreSame(returnedUTs[0], ds.GetItem("UT_id1"));
            var returnedItems = ds.GetItems(new TraceEntity("SoftwareUnitTest", "Unit Test", "UT", TraceEntityType.Truth));
            Assert.That(returnedItems[0].ItemID, Is.EqualTo(SLMSUTs[0].ItemID));
            Assert.That(returnedItems[2].ItemID, Is.EqualTo(SRCUTs[0].ItemID));
        }

        [Test]
        public void Anomalies_Can_Be_Retrieved_VERIFIES_Supplied_Anomalies_Are_Returned()
        {
            SetupSLMSPlugin();
            SetupSrcCodePlugin();
            var ds = new DataSources(mockConfiguration, mockPluginLoader, mockFileSystem);
            var returnedAnomalies = ds.GetAllAnomalies();
            Assert.IsTrue(returnedAnomalies.Count > 0);
            Assert.AreSame(ANOMALYs[0], returnedAnomalies[0]);
            Assert.AreSame(ANOMALYs[1], returnedAnomalies[1]);
            Assert.AreSame(ANOMALYs[1], ds.GetAnomaly("ANOMALY_id2"));
            Assert.AreSame(ANOMALYs[0], ds.GetItem("ANOMALY_id1"));
            var returnedItems = ds.GetItems(new TraceEntity("Anomaly","Bug","Bug",TraceEntityType.Truth));
            Assert.That(returnedItems[0].ItemID,Is.EqualTo(ANOMALYs[0].ItemID));
            Assert.That(returnedItems[1].ItemID, Is.EqualTo(ANOMALYs[1].ItemID));
        }

        [Test]
        public void GetItems_Function_Verifies_TraceEntity_VERIFIES_Exception_Is_Thrown_When_Unknown_Trace_Entity()
        {
            SetupSLMSPlugin();
            SetupSrcCodePlugin();
            var ds = new DataSources(mockConfiguration, mockPluginLoader, mockFileSystem);
            var ex = Assert.Throws<Exception>(() => ds.GetItems(new TraceEntity("wrongID", "wrong", "wrong", TraceEntityType.Truth)));
            Assert.That(ex.Message, Is.EqualTo("No datasource available for unknown trace entity: wrongID"));
        }

        [Test]
        public void External_Dependencies_Can_Be_Retrieved_VERIFIES_Supplied_External_Dependencies_Are_Returned()
        {
            SetupSLMSPlugin();
            SetupDepPlugin();
            var ds = new DataSources(mockConfiguration, mockPluginLoader, mockFileSystem);
            var returnedDeps = ds.GetAllExternalDependencies();
            Assert.IsTrue(returnedDeps.Count > 0);
            Assert.AreSame(DEPs[0], returnedDeps[0]);
            Assert.AreSame(DEPs[1], returnedDeps[1]);
        }


        [Test]
        public void GetItem_Can_Handle_Missing_ID_VERIFIES_Null_Returned_For_Missing_ID()
        {
            SetupSLMSPlugin();
            SetupSrcCodePlugin();
            var ds = new DataSources(mockConfiguration,mockPluginLoader, mockFileSystem);
            var result = ds.GetItem("does not exist");
            Assert.AreSame(null, result);
        }

        [Test]
        public void Configuration_Data_Items_Are_Retrieved_VERIFIES_Supplied_Data_Item_Matches_Retrieved()
        {
            TomlTable table = new TomlTable();
            table["ConfigValues"] = new TomlTable();
            ((TomlTable)table["ConfigValues"])["testKey"] = "TestValue";
            ConfigurationValues vals = new ConfigurationValues();
            vals.FromToml(table);
            mockConfiguration.ConfigVals.Returns(vals);
            var ds = new DataSources(mockConfiguration, mockPluginLoader, mockFileSystem);
            Assert.AreSame("TestValue",ds.GetConfigValue("testKey"));
            Assert.AreSame("NOT FOUND", ds.GetConfigValue("estKey"));
        }

        [Test]
        public void File_Is_Retrieved_From_Disk_VERIFIES_Text_File_Matches_File_On_Disk()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"c:\myfile.txt", new MockFileData("This is a \nmultiline text file.") },
            });
            var ds = new DataSources(mockConfiguration, mockPluginLoader, fileSystem);
            Assert.That(ds.GetTemplateFile(@"C:\myfile.txt"), Is.EqualTo("This is a \nmultiline text file."));
        }

        [Test]
        public void FileStream_Is_Retrieved_From_Disk_VERIFIES_Stream_Contents_Matches_File_On_Disk()
        {
            mockConfiguration.TemplateDir.Returns(@"C:\templatedir");
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"c:\templateDir\myfile.txt", new MockFileData("This is a \nmultiline text file.") },
            });
            var ds = new DataSources(mockConfiguration, mockPluginLoader, fileSystem);
            Stream s = ds.GetFileStreamFromTemplateDir(@"myfile.txt");
            StreamReader reader = new StreamReader(s);
            Assert.That(reader.ReadToEnd(), Is.EqualTo("This is a \nmultiline text file."));
        }
    }
}
