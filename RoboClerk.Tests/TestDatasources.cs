using NUnit.Framework;
using NSubstitute;
using RoboClerk.Configuration;
using System.Collections.Generic;
using NSubstitute.Extensions;
using Tomlyn.Model;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("These tests test the DataSources class")]
    internal class TestDatasources
    {
        ISLMSPlugin mockPlugin = null; 
        IPluginLoader mockPluginLoader = null; 
        IConfiguration mockConfiguration = null; 

        [SetUp]
        public void TestSetup()
        {
            mockPlugin = Substitute.For<ISLMSPlugin>();
            mockPluginLoader = Substitute.For<IPluginLoader>();
            mockConfiguration = Substitute.For<IConfiguration>();

            mockConfiguration.DataSourcePlugins.ReturnsForAnyArgs(new List<string> { "testPlugin1", "testPlugin2" });
            mockConfiguration.PluginDirs.ReturnsForAnyArgs(new List<string> { "c:\\temp\\does_not_exist", "c:\\temp\\" });
            mockPluginLoader.LoadPlugin<ISLMSPlugin>(Arg.Any<string>(), Arg.Any<string>()).Returns<ISLMSPlugin>(l => null);
            mockPluginLoader.Configure().LoadPlugin<ISLMSPlugin>(Arg.Is("testPlugin2"), Arg.Is("c:\\temp\\does_not_exist")).Returns(mockPlugin);
        }

        [Test]
        public void Successful_Creation_DataSources_VERIFIES_DataSources_Class_Creation_No_Throw()
        {
            Assert.DoesNotThrow(() => new DataSources(mockConfiguration,mockPluginLoader));
        }

        [Test]
        public void Plugin_Search_Functionality_Works_Correctly_VERIFIES_DataSources_Traverses_All_Plugins_And_All_Directories()
        {
            var ds = new DataSources(mockConfiguration, mockPluginLoader);
            Assert.DoesNotThrow(() => mockPluginLoader.Received().LoadPlugin<ISLMSPlugin>(Arg.Is<string>("testPlugin1"), Arg.Is<string>("c:\\temp\\does_not_exist")));
            Assert.DoesNotThrow(() => mockPluginLoader.Received().LoadPlugin<ISLMSPlugin>(Arg.Is<string>("testPlugin1"), Arg.Is<string>("c:\\temp\\")));
            Assert.DoesNotThrow(() => mockPluginLoader.Received().LoadPlugin<ISLMSPlugin>(Arg.Is<string>("testPlugin2"), Arg.Is<string>("c:\\temp\\does_not_exist")));
            Assert.DoesNotThrow(() => mockPluginLoader.Received().LoadPlugin<ISLMSPlugin>(Arg.Is<string>("testPlugin2"), Arg.Is<string>("c:\\temp\\")));
        }

        private List<RequirementItem> SYSs = null;
        private List<RequirementItem> SWRs = null;
        private List<TestCaseItem> TCs = null;
        private List<AnomalyItem> ANOMALYs = null;
        private void SetupPlugin()
        {
            SYSs = new List<RequirementItem> { new RequirementItem(RequirementType.SystemRequirement), new RequirementItem(RequirementType.SystemRequirement) };
            SYSs[0].RequirementTitle = "SYS_TestTitle1";
            SYSs[0].ItemID = "SYS_id1";
            SYSs[1].RequirementTitle = "SYS_TestTitle2";
            SYSs[1].ItemID = "SYS_id2";
            mockPlugin.GetSystemRequirements().Returns(SYSs);
            SWRs = new List<RequirementItem> { new RequirementItem(RequirementType.SoftwareRequirement), new RequirementItem(RequirementType.SoftwareRequirement) };
            SWRs[0].RequirementTitle = "SWR_TestTitle1";
            SWRs[0].ItemID = "SWR_id1";
            SWRs[1].RequirementTitle = "SWR_TestTitle2";
            SWRs[1].ItemID = "SWR_id2";
            mockPlugin.GetSoftwareRequirements().Returns(SWRs);
            TCs = new List<TestCaseItem> { new TestCaseItem(), new TestCaseItem() };
            TCs[0].TestCaseTitle = "TC_TestTitle1";
            TCs[0].ItemID = "TC_id1";
            TCs[1].TestCaseTitle = "TC_TestTitle2";
            TCs[1].ItemID = "TC_id2";
            mockPlugin.GetSoftwareSystemTests().Returns(TCs);
            ANOMALYs = new List<AnomalyItem> { new AnomalyItem(), new AnomalyItem() };
            ANOMALYs[0].AnomalyTitle = "ANOMALY_TestTitle1";
            ANOMALYs[0].ItemID = "ANOMALY_id1";
            ANOMALYs[1].AnomalyTitle = "ANOMALY_TestTitle2";
            ANOMALYs[1].ItemID = "ANOMALY_id2";
            mockPlugin.GetAnomalies().Returns(ANOMALYs);
        }

        [Test]
        public void System_Requirements_Can_Be_Retrieved_VERIFIES_Supplied_Requirements_Are_Returned()
        {
            SetupPlugin();
            var ds = new DataSources(mockConfiguration, mockPluginLoader);
            var returnedReqs = ds.GetAllSystemRequirements();
            Assert.AreSame(SYSs[0], returnedReqs[0]);
            Assert.AreSame(SYSs[1], returnedReqs[1]);
            Assert.AreSame(SYSs[1], ds.GetSystemRequirement("SYS_id2"));
            Assert.AreSame(SYSs[0], ds.GetItem("SYS_id1"));
        }

        [Test]
        public void Software_Requirements_Can_Be_Retrieved_VERIFIES_Supplied_Requirements_Are_Returned()
        {
            SetupPlugin();
            var ds = new DataSources(mockConfiguration, mockPluginLoader);
            var returnedReqs = ds.GetAllSoftwareRequirements();
            Assert.AreSame(SWRs[0], returnedReqs[0]);
            Assert.AreSame(SWRs[1], returnedReqs[1]);
            Assert.AreSame(SWRs[1], ds.GetSoftwareRequirement("SWR_id2"));
            Assert.AreSame(SWRs[0], ds.GetItem("SWR_id1"));
        }

        [Test]
        public void Test_Cases_Can_Be_Retrieved_VERIFIES_Supplied_Test_Cases_Are_Returned()
        {
            SetupPlugin();
            var ds = new DataSources(mockConfiguration, mockPluginLoader);
            var returnedReqs = ds.GetAllSystemLevelTests();
            Assert.AreSame(TCs[0], returnedReqs[0]);
            Assert.AreSame(TCs[1], returnedReqs[1]);
            Assert.AreSame(TCs[1], ds.GetSystemLevelTest("TC_id2"));
            Assert.AreSame(TCs[0], ds.GetItem("TC_id1"));
        }

        [Test]
        public void Anomalies_Can_Be_Retrieved_VERIFIES_Supplied_Anomalies_Are_Returned()
        {
            SetupPlugin();
            var ds = new DataSources(mockConfiguration, mockPluginLoader);
            var returnedReqs = ds.GetAllAnomalies();
            Assert.AreSame(ANOMALYs[0], returnedReqs[0]);
            Assert.AreSame(ANOMALYs[1], returnedReqs[1]);
            Assert.AreSame(ANOMALYs[1], ds.GetAnomaly("ANOMALY_id2"));
            Assert.AreSame(ANOMALYs[0], ds.GetItem("ANOMALY_id1"));
        }

        [Test]
        public void GetItem_Can_Handle_Missing_ID_VERIFIES_Null_Returned_For_Missing_ID()
        {
            SetupPlugin();
            var ds = new DataSources(mockConfiguration,mockPluginLoader);
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
            var ds = new DataSources(mockConfiguration, mockPluginLoader);
            Assert.AreSame("TestValue",ds.GetConfigValue("testKey"));
            Assert.AreSame("NOT FOUND", ds.GetConfigValue("estKey"));
        }
    }
}
