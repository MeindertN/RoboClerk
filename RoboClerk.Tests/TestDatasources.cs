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
using System.Text.Json;
using NUnit.Framework.Internal;
using System.Text;
using System.Linq;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("These tests test the DataSources class")]
    internal class TestDatasources
    {
        IPlugin mockSLMSPlugin = null; 
        IPlugin mockDepMgmtPlugin = null;
        IPlugin mockSrcCodePlugin = null;
        IPluginLoader mockPluginLoader = null; 
        IConfiguration mockConfiguration = null;
        IFileSystem mockFileSystem = null;

        [SetUp]
        public void TestSetup()
        {
            mockFileSystem = Substitute.For<IFileSystem>();
            mockSLMSPlugin = Substitute.For<IPlugin>();
            mockSLMSPlugin.Name.Returns("SLMS Test Plugin");
            mockSLMSPlugin.Description.Returns("SLMS Test Plugin Description");
            mockSLMSPlugin.GetDependencies().Returns(new List<ExternalDependency>());
            mockDepMgmtPlugin = Substitute.For<IPlugin>();
            mockDepMgmtPlugin.Name.Returns("Dependency Test Plugin");
            mockDepMgmtPlugin.Description.Returns("Dependency Test Plugin Description");
            mockDepMgmtPlugin.GetSystemRequirements().Returns(new List<RequirementItem>());
            mockDepMgmtPlugin.GetSoftwareRequirements().Returns(new List<RequirementItem>());
            mockDepMgmtPlugin.GetDocumentationRequirements().Returns(new List<RequirementItem>());
            mockDepMgmtPlugin.GetDocContents().Returns(new List<DocContentItem>());
            mockDepMgmtPlugin.GetAnomalies().Returns(new List<AnomalyItem>());
            mockDepMgmtPlugin.GetUnitTests().Returns(new List<UnitTestItem>());
            mockDepMgmtPlugin.GetSoftwareSystemTests().Returns(new List<SoftwareSystemTestItem>());
            mockDepMgmtPlugin.GetSOUP().Returns(new List<SOUPItem>());
            mockDepMgmtPlugin.GetRisks().Returns(new List<RiskItem>());
            mockDepMgmtPlugin.GetUnitTests().Returns(new List<UnitTestItem>());
            mockSrcCodePlugin = Substitute.For<IPlugin>();
            mockSrcCodePlugin.Name.Returns("Source Code Analysis Plugin");
            mockSrcCodePlugin.Description.Returns("Source Code Analysis Plugin Description");
            mockSrcCodePlugin.GetSystemRequirements().Returns(new List<RequirementItem>());
            mockSrcCodePlugin.GetSoftwareRequirements().Returns(new List<RequirementItem>());
            mockSrcCodePlugin.GetDocumentationRequirements().Returns(new List<RequirementItem>());
            mockSrcCodePlugin.GetDocContents().Returns(new List<DocContentItem>());
            mockSrcCodePlugin.GetAnomalies().Returns(new List<AnomalyItem>());
            mockSrcCodePlugin.GetUnitTests().Returns(new List<UnitTestItem>());
            mockSrcCodePlugin.GetSoftwareSystemTests().Returns(new List<SoftwareSystemTestItem>());
            mockSrcCodePlugin.GetSOUP().Returns(new List<SOUPItem>());
            mockSrcCodePlugin.GetRisks().Returns(new List<RiskItem>());
            mockSrcCodePlugin.GetDependencies().Returns(new List<ExternalDependency>());
            mockPluginLoader = Substitute.For<IPluginLoader>();
            mockConfiguration = Substitute.For<IConfiguration>();

            mockConfiguration.DataSourcePlugins.ReturnsForAnyArgs(new List<string> { "testPlugin1", "testPlugin2", "testDepPlugin", "testSrcPlugin" });
            mockConfiguration.PluginDirs.ReturnsForAnyArgs(new List<string> { TestingHelpers.ConvertFileName("c:\\temp\\does_not_exist"), TestingHelpers.ConvertFileName("c:\\temp\\") });
            mockPluginLoader.LoadPlugin<IPlugin>(Arg.Any<string>(), Arg.Any<string>(),mockFileSystem).Returns<IPlugin>(l => null);
            mockPluginLoader.Configure().LoadPlugin<IPlugin>(Arg.Is("testPlugin2"), Arg.Is(TestingHelpers.ConvertFileName("c:\\temp\\does_not_exist")),mockFileSystem).Returns(mockSLMSPlugin);
            mockPluginLoader.Configure().LoadPlugin<IPlugin>(Arg.Is("testDepPlugin"), Arg.Is(TestingHelpers.ConvertFileName("c:\\temp\\does_not_exist")),mockFileSystem).Returns(mockDepMgmtPlugin);
            mockPluginLoader.Configure().LoadPlugin<IPlugin>(Arg.Is("testSrcPlugin"), Arg.Is(TestingHelpers.ConvertFileName("c:\\temp\\does_not_exist")),mockFileSystem).Returns(mockSrcCodePlugin);
        }

        [Test]
        public void Successful_Creation_DataSources_VERIFIES_DataSources_Class_Creation_No_Throw()
        {
            Assert.DoesNotThrow(() => new PluginDataSources(mockConfiguration,mockPluginLoader,mockFileSystem));
        }

        [Test]
        public void Plugin_Search_Functionality_Works_Correctly_VERIFIES_DataSources_Traverses_All_Plugins_And_All_Directories()
        {
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileSystem);
            Assert.DoesNotThrow(() => mockPluginLoader.Received().LoadPlugin<IPlugin>(Arg.Is<string>("testPlugin1"), 
                Arg.Is<string>(TestingHelpers.ConvertFileName("c:\\temp\\does_not_exist")), mockFileSystem));
            Assert.DoesNotThrow(() => mockPluginLoader.Received().LoadPlugin<IPlugin>(Arg.Is<string>("testPlugin1"), 
                Arg.Is<string>(TestingHelpers.ConvertFileName("c:\\temp\\")),mockFileSystem));
            Assert.DoesNotThrow(() => mockPluginLoader.Received().LoadPlugin<IPlugin>(Arg.Is<string>("testPlugin2"), 
                Arg.Is<string>(TestingHelpers.ConvertFileName("c:\\temp\\does_not_exist")),mockFileSystem));
            Assert.DoesNotThrow(() => mockPluginLoader.Received().LoadPlugin<IPlugin>(Arg.Is<string>("testPlugin2"), 
                Arg.Is<string>(TestingHelpers.ConvertFileName("c:\\temp\\")),mockFileSystem));
        }

        private List<RequirementItem> SYSs = null;
        private List<RequirementItem> SWRs = null;
        private List<RequirementItem> DOCs = null;
        private List<SoftwareSystemTestItem> TCs = null;
        private List<AnomalyItem> ANOMALYs = null;
        private List<UnitTestItem> SLMSUTs = null;
        private List<UnitTestItem> SRCUTs = null;
        private List<ExternalDependency> DEPs = null;
        private List<DocContentItem> DOCCTs = null;
        private List<SOUPItem> SOUPs = null;
        private List<RiskItem> RISKs = null;
        private void SetupSLMSPlugin()
        {
            SYSs = new List<RequirementItem> { new RequirementItem(RequirementType.SystemRequirement), new RequirementItem(RequirementType.SystemRequirement) };
            SYSs[0].ItemTitle = "SYS_TestTitle1";
            SYSs[0].ItemID = "SYS_id1";
            SYSs[1].ItemTitle = "SYS_TestTitle2";
            SYSs[1].ItemID = "SYS_id2";
            mockSLMSPlugin.GetSystemRequirements().Returns(SYSs);
            SWRs = new List<RequirementItem> { new RequirementItem(RequirementType.SoftwareRequirement), new RequirementItem(RequirementType.SoftwareRequirement) };
            SWRs[0].ItemTitle = "SWR_TestTitle1";
            SWRs[0].ItemID = "SWR_id1";
            SWRs[1].ItemTitle = "SWR_TestTitle2";
            SWRs[1].ItemID = "SWR_id2";
            mockSLMSPlugin.GetSoftwareRequirements().Returns(SWRs);
            DOCs = new List<RequirementItem> { new RequirementItem(RequirementType.DocumentationRequirement), new RequirementItem(RequirementType.DocumentationRequirement) };
            DOCs[0].ItemTitle = "DOC_TestTitle1";
            DOCs[0].ItemID = "DOC_id1";
            DOCs[1].ItemTitle = "DOC_TestTitle2";
            DOCs[1].ItemID = "DOC_id2";
            mockSLMSPlugin.GetDocumentationRequirements().Returns(DOCs);
            TCs = new List<SoftwareSystemTestItem> { new SoftwareSystemTestItem(), new SoftwareSystemTestItem() };
            TCs[0].ItemTitle = "TC_TestTitle1";
            TCs[0].ItemID = "TC_id1";
            TCs[1].ItemTitle = "TC_TestTitle2";
            TCs[1].ItemID = "TC_id2";
            mockSLMSPlugin.GetSoftwareSystemTests().Returns(TCs);
            DOCCTs = new List<DocContentItem> { new DocContentItem(), new DocContentItem() };
            DOCCTs[0].DocContent = "DOCCT_TestContents";
            DOCCTs[0].ItemID = "DOCCT_id1";
            DOCCTs[1].DocContent = "DOCCT_TestContents2";
            DOCCTs[1].ItemID = "DOCCT_id2";
            mockSLMSPlugin.GetDocContents().Returns(DOCCTs);
            ANOMALYs = new List<AnomalyItem> { new AnomalyItem(), new AnomalyItem() };
            ANOMALYs[0].ItemTitle = "ANOMALY_TestTitle1";
            ANOMALYs[0].ItemID = "ANOMALY_id1";
            ANOMALYs[1].ItemTitle = "ANOMALY_TestTitle2";
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
            RISKs[0].RiskDetectabilityScore = 100;
            RISKs[0].RiskCauseOfFailure = "RISK_cause1";
            RISKs[0].ItemID = "RISK_id1";
            RISKs[1].RiskDetectabilityScore = 200;
            RISKs[1].RiskCauseOfFailure = "RISK_cause2";
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
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileSystem);
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
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileSystem);
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
        public void Documentation_Requirements_Can_Be_Retrieved_VERIFIES_Supplied_Requirements_Are_Returned()
        {
            SetupSLMSPlugin();
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileSystem);
            var returnedReqs = ds.GetAllDocumentationRequirements();
            Assert.AreSame(DOCs[0], returnedReqs[0]);
            Assert.AreSame(DOCs[1], returnedReqs[1]);
            Assert.AreSame(DOCs[1], ds.GetDocumentationRequirement("DOC_id2"));
            Assert.AreSame(DOCs[0], ds.GetItem("DOC_id1"));
            var returnedItems = ds.GetItems(new TraceEntity("DocumentationRequirement", "Requirement", "DOC", TraceEntityType.Truth));
            Assert.That(returnedItems[0].ItemID, Is.EqualTo(DOCs[0].ItemID));
            Assert.That(returnedItems[1].ItemID, Is.EqualTo(DOCs[1].ItemID));
        }

        [Test]
        public void DocContents_Can_Be_Retrieved_VERIFIES_Supplied_DocContents_Are_Returned()
        {
            SetupSLMSPlugin();
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileSystem);
            var returnedDCTs = ds.GetAllDocContents();
            Assert.AreSame(DOCCTs[0], returnedDCTs[0]);
            Assert.AreSame(DOCCTs[1], returnedDCTs[1]);
            Assert.AreSame(DOCCTs[1], ds.GetDocContent("DOCCT_id2"));
            Assert.AreSame(DOCCTs[0], ds.GetItem("DOCCT_id1"));
            var returnedItems = ds.GetItems(new TraceEntity("DocContent", "Content", "DOCCT", TraceEntityType.Truth));
            Assert.That(returnedItems[0].ItemID, Is.EqualTo(DOCCTs[0].ItemID));
            Assert.That(returnedItems[1].ItemID, Is.EqualTo(DOCCTs[1].ItemID));
        }

        [Test]
        public void Test_Cases_Can_Be_Retrieved_VERIFIES_Supplied_Test_Cases_Are_Returned()
        {
            SetupSLMSPlugin();
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileSystem);
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
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileSystem);
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
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileSystem);
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
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileSystem);
            var returnedUTs = ds.GetAllUnitTests();
            Assert.AreSame(SLMSUTs[0], returnedUTs[0]);
            Assert.AreSame(SLMSUTs[1], returnedUTs[1]);
            Assert.AreSame(SRCUTs[0], returnedUTs[2]);
            Assert.AreSame(SRCUTs[1], returnedUTs[3]);
            Assert.AreSame(returnedUTs[2], ds.GetUnitTest("UT_id3"));
            Assert.AreSame(returnedUTs[0], ds.GetItem("UT_id1"));
            var returnedItems = ds.GetItems(new TraceEntity("UnitTest", "Unit Test", "UT", TraceEntityType.Truth));
            Assert.That(returnedItems[0].ItemID, Is.EqualTo(SLMSUTs[0].ItemID));
            Assert.That(returnedItems[2].ItemID, Is.EqualTo(SRCUTs[0].ItemID));
        }

        [Test]
        public void Anomalies_Can_Be_Retrieved_VERIFIES_Supplied_Anomalies_Are_Returned()
        {
            SetupSLMSPlugin();
            SetupSrcCodePlugin();
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileSystem);
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
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileSystem);
            var ex = Assert.Throws<Exception>(() => ds.GetItems(new TraceEntity("wrongID", "wrong", "wrong", TraceEntityType.Truth)));
            Assert.That(ex.Message, Is.EqualTo("No datasource available for unknown trace entity: wrongID"));
        }

        [Test]
        public void External_Dependencies_Can_Be_Retrieved_VERIFIES_Supplied_External_Dependencies_Are_Returned()
        {
            SetupSLMSPlugin();
            SetupDepPlugin();
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileSystem);
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
            var ds = new PluginDataSources(mockConfiguration,mockPluginLoader, mockFileSystem);
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
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileSystem);
            Assert.AreSame("TestValue",ds.GetConfigValue("testKey"));
            Assert.AreSame("NOT FOUND", ds.GetConfigValue("estKey"));
        }

        [Test]
        public void Configuration_Data_Contains_ConfigValues_VERIFIES_Exception_Thrown_When_ConfigValues_Missing()
        {
            TomlTable table = new TomlTable();
            table["Incorrect"] = new TomlTable();
            ((TomlTable)table["Incorrect"])["testKey"] = "TestValue";
            ConfigurationValues vals = new ConfigurationValues();
            Assert.Throws<Exception>(() => vals.FromToml(table), "Required configuration element \"ConfigValues\" is missing from project configuration file. Cannot continue.");
        }

        [Test]
        public void File_Is_Retrieved_From_Disk_VERIFIES_Text_File_Matches_File_On_Disk()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"C:\myfile.txt"), new MockFileData("This is a \nmultiline text file.") },
            });
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, fileSystem);
            Assert.That(ds.GetTemplateFile(TestingHelpers.ConvertFileName(@"C:\myfile.txt")), Is.EqualTo("This is a \nmultiline text file."));
        }

        [Test]
        public void FileStream_Is_Retrieved_From_Disk_VERIFIES_Stream_Contents_Matches_File_On_Disk()
        {
            mockConfiguration.TemplateDir.Returns(TestingHelpers.ConvertFileName(@"C:\templateDir"));
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(TestingHelpers.ConvertFileName(@"C:\templateDir\myfile.txt")), 
                    new MockFileData("This is a \nmultiline text file.") },
            });
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, fileSystem);
            Stream s = ds.GetFileStreamFromTemplateDir(@"myfile.txt");
            StreamReader reader = new StreamReader(s);
            Assert.That(reader.ReadToEnd(), Is.EqualTo("This is a \nmultiline text file."));
        }

        private bool CompareObjects<T>(List<T> obj1, List<T> obj2)
        {
            var properties = typeof(T).GetProperties();
            if(obj1.Count != obj2.Count)
            {
                return false;
            }
            for (int i = 0; i < obj1.Count; i++)
            {
                foreach (var prop in properties)
                {
                    if (prop.GetValue(obj1[i]) as IEnumerable<dynamic> == null) 
                    {
                        if (prop.GetValue(obj1[i]) != null && prop.GetValue(obj2[i]) != null) //skip items that are null
                            if (!prop.GetValue(obj1[i]).Equals(prop.GetValue(obj2[i])))
                            {
                                return false;
                            }
                    }
                    else
                    {
                        if((prop.GetValue(obj1[i]) as IEnumerable<dynamic>).Count() != (prop.GetValue(obj2[i]) as IEnumerable<dynamic>).Count())
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        [Test]
        public void Datasources_Are_Converted_To_JSON_VERIFIES_The_Correct_JSON_Is_Generated()
        {
            SetupSLMSPlugin();
            SetupSrcCodePlugin();
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileSystem);
            string jsonString = ds.ToJSON();
            byte[] byteArray = Encoding.ASCII.GetBytes(jsonString);
            MemoryStream stream = new MemoryStream(byteArray);
            var dataStorage = JsonSerializer.Deserialize<CheckpointDataStorage>(stream);

            Assert.That(CompareObjects(dataStorage.SystemRequirements, ds.GetAllSystemRequirements()));
            Assert.That(CompareObjects(dataStorage.SoftwareRequirements, ds.GetAllSoftwareRequirements()));
            Assert.That(CompareObjects(dataStorage.DocumentationRequirements, ds.GetAllDocumentationRequirements()));
            Assert.That(CompareObjects(dataStorage.DocContents, ds.GetAllDocContents()));
            Assert.That(CompareObjects(dataStorage.Risks, ds.GetAllRisks()));
            Assert.That(CompareObjects(dataStorage.Anomalies, ds.GetAllAnomalies()));
            Assert.That(CompareObjects(dataStorage.SOUPs, ds.GetAllSOUP()));
            Assert.That(CompareObjects(dataStorage.SoftwareSystemTests, ds.GetAllSoftwareSystemTests()));
            Assert.That(CompareObjects(dataStorage.UnitTests, ds.GetAllUnitTests()));
        }
    }
}
