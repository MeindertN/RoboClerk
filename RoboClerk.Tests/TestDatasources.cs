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
using NUnit.Framework.Legacy;
using Microsoft.Extensions.DependencyInjection;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("These tests test the DataSources class")]
    internal class TestDatasources
    {
        IDataSourcePlugin mockSLMSPlugin = null; 
        IDataSourcePlugin mockDepMgmtPlugin = null;
        IDataSourcePlugin mockSrcCodePlugin = null;
        IPluginLoader mockPluginLoader = null; 
        IConfiguration mockConfiguration = null;
        IFileSystem mockFileSystem = null;
        IFileProviderPlugin mockFileProvider = null;

        [SetUp]
        public void TestSetup()
        {
            mockFileSystem = Substitute.For<IFileSystem>();
            mockFileProvider = Substitute.For<IFileProviderPlugin>();
            mockSLMSPlugin = (IDataSourcePlugin)Substitute.For<IPlugin,IDataSourcePlugin>();
            ((IPlugin)mockSLMSPlugin).Name.Returns("SLMS Test Plugin");
            ((IPlugin)mockSLMSPlugin).Description.Returns("SLMS Test Plugin Description");
            mockSLMSPlugin.GetDependencies().Returns(new List<ExternalDependency>());
            mockDepMgmtPlugin = (IDataSourcePlugin)Substitute.For<IPlugin,IDataSourcePlugin>();
            ((IPlugin)mockDepMgmtPlugin).Name.Returns("Dependency Test Plugin");
            ((IPlugin)mockDepMgmtPlugin).Description.Returns("Dependency Test Plugin Description");
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
            mockSrcCodePlugin = (IDataSourcePlugin)Substitute.For<IPlugin, IDataSourcePlugin>();
            ((IPlugin)mockSrcCodePlugin).Name.Returns("Source Code Analysis Plugin");
            ((IPlugin)mockSrcCodePlugin).Description.Returns("Source Code Analysis Plugin Description");
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

            mockConfiguration.DataSourcePlugins.ReturnsForAnyArgs(new List<string> { "testPlugin2", "testDepPlugin", "testSrcPlugin" });
            mockConfiguration.PluginDirs.ReturnsForAnyArgs(new List<string> { TestingHelpers.ConvertFileName("c:\\temp\\"), TestingHelpers.ConvertFileName("c:\\temp\\does_not_exist") });
            mockPluginLoader.LoadByName<IDataSourcePlugin>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Action<IServiceCollection>>() ).Returns<IDataSourcePlugin>(l => null);
            mockPluginLoader.Configure().LoadByName<IDataSourcePlugin>(Arg.Is(TestingHelpers.ConvertFileName("c:\\temp\\does_not_exist")), Arg.Is("testPlugin2"), Arg.Any<Action<IServiceCollection>>()).Returns((IDataSourcePlugin)mockSLMSPlugin);
            mockPluginLoader.Configure().LoadByName<IDataSourcePlugin>(Arg.Is(TestingHelpers.ConvertFileName("c:\\temp\\does_not_exist")), Arg.Is("testDepPlugin"), Arg.Any<Action<IServiceCollection>>()).Returns((IDataSourcePlugin)mockDepMgmtPlugin);
            mockPluginLoader.Configure().LoadByName<IDataSourcePlugin>(Arg.Is(TestingHelpers.ConvertFileName("c:\\temp\\does_not_exist")), Arg.Is("testSrcPlugin"), Arg.Any<Action<IServiceCollection>>()).Returns((IDataSourcePlugin)mockSrcCodePlugin);
        }

        [Test]
        public void Successful_Creation_DataSources_VERIFIES_DataSources_Class_Creation_No_Throw()
        {
            Assert.DoesNotThrow(() => new PluginDataSources(mockConfiguration,mockPluginLoader,mockFileProvider));
        }

        [Test]
        public void Plugin_Search_Functionality_Works_Correctly_VERIFIES_DataSources_Traverses_All_Plugins_And_All_Directories()
        {
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileProvider);
            Assert.DoesNotThrow(() => mockPluginLoader.Received().LoadByName<IDataSourcePlugin>( 
                Arg.Is<string>(TestingHelpers.ConvertFileName("c:\\temp\\does_not_exist")),
                Arg.Is<string>("testPlugin2"),
                Arg.Any<Action<IServiceCollection>>()));
            Assert.DoesNotThrow(() => mockPluginLoader.Received().LoadByName<IDataSourcePlugin>(
                Arg.Is<string>(TestingHelpers.ConvertFileName("c:\\temp\\")),
                Arg.Is<string>("testPlugin2"),
                Arg.Any<Action<IServiceCollection>>()));
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
        private List<EliminatedRequirementItem> eliminatedSystemRequirements;
        private List<EliminatedRequirementItem> eliminatedSoftwareRequirements;
        private List<EliminatedRequirementItem> eliminatedDocumentationRequirements;
        private List<EliminatedSoftwareSystemTestItem> eliminatedSoftwareSystemTests;
        private List<EliminatedRiskItem> eliminatedRisks;
        private List<EliminatedSOUPItem> eliminatedSOUP;
        private List<EliminatedAnomalyItem> eliminatedAnomalies;
        private List<EliminatedDocContentItem> eliminatedDocContents;

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
            SetupEliminatedItems();
            mockSLMSPlugin.GetRisks().Returns(RISKs);
            mockSLMSPlugin.GetEliminatedSystemRequirements().Returns(eliminatedSystemRequirements);
            mockSLMSPlugin.GetEliminatedSoftwareRequirements().Returns(eliminatedSoftwareRequirements);
            mockSLMSPlugin.GetEliminatedDocumentationRequirements().Returns(eliminatedDocumentationRequirements);
            mockSLMSPlugin.GetEliminatedSoftwareSystemTests().Returns(eliminatedSoftwareSystemTests);
            mockSLMSPlugin.GetEliminatedRisks().Returns(eliminatedRisks);
            mockSLMSPlugin.GetEliminatedSOUP().Returns(eliminatedSOUP);
            mockSLMSPlugin.GetEliminatedAnomalies().Returns(eliminatedAnomalies);
            mockSLMSPlugin.GetEliminatedDocContents().Returns(eliminatedDocContents);
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

        private void SetupEliminatedItems()
        {
            // Setup eliminated system requirements
            var sysReq1 = new RequirementItem(RequirementType.SystemRequirement);
            sysReq1.ItemID = "SR_id1";
            sysReq1.ItemTitle = "System Requirement 1";
            var sysReq2 = new RequirementItem(RequirementType.SystemRequirement);
            sysReq2.ItemID = "SR_id2";
            sysReq2.ItemTitle = "System Requirement 2";
            eliminatedSystemRequirements = new List<EliminatedRequirementItem>
            {
                new EliminatedRequirementItem(sysReq1, "Filtered by release version", EliminationReason.FilteredOut),
                new EliminatedRequirementItem(sysReq2, "Filtered by category", EliminationReason.FilteredOut)
            };

            // Setup eliminated software requirements
            var swReq1 = new RequirementItem(RequirementType.SoftwareRequirement);
            swReq1.ItemID = "SWR_id1";
            swReq1.ItemTitle = "Software Requirement 1";
            var swReq2 = new RequirementItem(RequirementType.SoftwareRequirement);
            swReq2.ItemID = "SWR_id2";
            swReq2.ItemTitle = "Software Requirement 2";
            eliminatedSoftwareRequirements = new List<EliminatedRequirementItem>
            {
                new EliminatedRequirementItem(swReq1, "Parent item was filtered", EliminationReason.LinkedItemMissing),
                new EliminatedRequirementItem(swReq2, "Parent item was filtered", EliminationReason.LinkedItemMissing)
            };

            // Setup eliminated documentation requirements
            var docReq1 = new RequirementItem(RequirementType.DocumentationRequirement);
            docReq1.ItemID = "DOC_id1";
            docReq1.ItemTitle = "Documentation Requirement 1";
            var docReq2 = new RequirementItem(RequirementType.DocumentationRequirement);
            docReq2.ItemID = "DOC_id2";
            docReq2.ItemTitle = "Documentation Requirement 2";
            eliminatedDocumentationRequirements = new List<EliminatedRequirementItem>
            {
                new EliminatedRequirementItem(docReq1, "Filtered by category", EliminationReason.FilteredOut),
                new EliminatedRequirementItem(docReq2, "Linked to ignored item", EliminationReason.IgnoredLinkTarget)
            };

            // Setup eliminated software system tests
            var test1 = new SoftwareSystemTestItem();
            test1.ItemID = "SST_id1";
            test1.ItemTitle = "Software System Test 1";
            var test2 = new SoftwareSystemTestItem();
            test2.ItemID = "SST_id2";
            test2.ItemTitle = "Software System Test 2";
            eliminatedSoftwareSystemTests = new List<EliminatedSoftwareSystemTestItem>
            {
                new EliminatedSoftwareSystemTestItem(test1, "Filtered by test method", EliminationReason.FilteredOut),
                new EliminatedSoftwareSystemTestItem(test2, "Parent requirement was filtered", EliminationReason.LinkedItemMissing)
            };

            // Setup eliminated risks
            var risk1 = new RiskItem();
            risk1.ItemID = "RISK_id1";
            risk1.ItemTitle = "Risk 1";
            var risk2 = new RiskItem();
            risk2.ItemID = "RISK_id2";
            risk2.ItemTitle = "Risk 2";
            eliminatedRisks = new List<EliminatedRiskItem>
            {
                new EliminatedRiskItem(risk1, "Filtered by risk type", EliminationReason.FilteredOut),
                new EliminatedRiskItem(risk2, "Risk control was filtered", EliminationReason.LinkedItemMissing)
            };

            // Setup eliminated SOUP items
            var soup1 = new SOUPItem();
            soup1.ItemID = "SOUP_id1";
            soup1.SOUPName = "SOUP 1";
            var soup2 = new SOUPItem();
            soup2.ItemID = "SOUP_id2";
            soup2.SOUPName = "SOUP 2";
            eliminatedSOUP = new List<EliminatedSOUPItem>
            {
                new EliminatedSOUPItem(soup1, "Filtered by version", EliminationReason.FilteredOut),
                new EliminatedSOUPItem(soup2, "Filtered by licensed status", EliminationReason.FilteredOut)
            };

            // Setup eliminated anomalies
            var anomaly1 = new AnomalyItem();
            anomaly1.ItemID = "ANOMALY_id1";
            anomaly1.ItemTitle = "Anomaly 1";
            var anomaly2 = new AnomalyItem();
            anomaly2.ItemID = "ANOMALY_id2";
            anomaly2.ItemTitle = "Anomaly 2";
            eliminatedAnomalies = new List<EliminatedAnomalyItem>
            {
                new EliminatedAnomalyItem(anomaly1, "Filtered by status", EliminationReason.FilteredOut),
                new EliminatedAnomalyItem(anomaly2, "Related requirement was filtered", EliminationReason.LinkedItemMissing)
            };

            // Setup eliminated doc contents
            var docContent1 = new DocContentItem();
            docContent1.ItemID = "CONTENT_id1";
            docContent1.DocContent = "Document content 1";
            var docContent2 = new DocContentItem();
            docContent2.ItemID = "CONTENT_id2";
            docContent2.DocContent = "Document content 2";
            eliminatedDocContents = new List<EliminatedDocContentItem>
            {
                new EliminatedDocContentItem(docContent1, "Filtered by document type", EliminationReason.FilteredOut),
                new EliminatedDocContentItem(docContent2, "Parent document was filtered", EliminationReason.LinkedItemMissing)
            };
        }

        [Test]
        public void System_Requirements_Can_Be_Retrieved_VERIFIES_Supplied_Requirements_Are_Returned()
        {
            SetupSLMSPlugin();
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileProvider);
            var returnedReqs = ds.GetAllSystemRequirements();
            ClassicAssert.AreSame(SYSs[0], returnedReqs[0]);
            ClassicAssert.AreSame(SYSs[1], returnedReqs[1]);
            ClassicAssert.AreSame(SYSs[1], ds.GetSystemRequirement("SYS_id2"));
            ClassicAssert.AreSame(SYSs[0], ds.GetItem("SYS_id1"));
            var returnedItems = ds.GetItems(new TraceEntity("SystemRequirement", "Requirement", "SYS", TraceEntityType.Truth));
            Assert.That(returnedItems[0].ItemID, Is.EqualTo(SYSs[0].ItemID));
            Assert.That(returnedItems[1].ItemID, Is.EqualTo(SYSs[1].ItemID));
        }

        [Test]
        public void Software_Requirements_Can_Be_Retrieved_VERIFIES_Supplied_Requirements_Are_Returned()
        {
            SetupSLMSPlugin();
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileProvider);
            var returnedReqs = ds.GetAllSoftwareRequirements();
            ClassicAssert.AreSame(SWRs[0], returnedReqs[0]);
            ClassicAssert.AreSame(SWRs[1], returnedReqs[1]);
            ClassicAssert.AreSame(SWRs[1], ds.GetSoftwareRequirement("SWR_id2"));
            ClassicAssert.AreSame(SWRs[0], ds.GetItem("SWR_id1"));
            var returnedItems = ds.GetItems(new TraceEntity("SoftwareRequirement", "Requirement", "SWR", TraceEntityType.Truth));
            Assert.That(returnedItems[0].ItemID, Is.EqualTo(SWRs[0].ItemID));
            Assert.That(returnedItems[1].ItemID, Is.EqualTo(SWRs[1].ItemID));
        }

        [Test]
        public void Documentation_Requirements_Can_Be_Retrieved_VERIFIES_Supplied_Requirements_Are_Returned()
        {
            SetupSLMSPlugin();
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileProvider);
            var returnedReqs = ds.GetAllDocumentationRequirements();
            ClassicAssert.AreSame(DOCs[0], returnedReqs[0]);
            ClassicAssert.AreSame(DOCs[1], returnedReqs[1]);
            ClassicAssert.AreSame(DOCs[1], ds.GetDocumentationRequirement("DOC_id2"));
            ClassicAssert.AreSame(DOCs[0], ds.GetItem("DOC_id1"));
            var returnedItems = ds.GetItems(new TraceEntity("DocumentationRequirement", "Requirement", "DOC", TraceEntityType.Truth));
            Assert.That(returnedItems[0].ItemID, Is.EqualTo(DOCs[0].ItemID));
            Assert.That(returnedItems[1].ItemID, Is.EqualTo(DOCs[1].ItemID));
        }

        [Test]
        public void DocContents_Can_Be_Retrieved_VERIFIES_Supplied_DocContents_Are_Returned()
        {
            SetupSLMSPlugin();
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileProvider);
            var returnedDCTs = ds.GetAllDocContents();
            ClassicAssert.AreSame(DOCCTs[0], returnedDCTs[0]);
            ClassicAssert.AreSame(DOCCTs[1], returnedDCTs[1]);
            ClassicAssert.AreSame(DOCCTs[1], ds.GetDocContent("DOCCT_id2"));
            ClassicAssert.AreSame(DOCCTs[0], ds.GetItem("DOCCT_id1"));
            var returnedItems = ds.GetItems(new TraceEntity("DocContent", "Content", "DOCCT", TraceEntityType.Truth));
            Assert.That(returnedItems[0].ItemID, Is.EqualTo(DOCCTs[0].ItemID));
            Assert.That(returnedItems[1].ItemID, Is.EqualTo(DOCCTs[1].ItemID));
        }

        [Test]
        public void Test_Cases_Can_Be_Retrieved_VERIFIES_Supplied_Test_Cases_Are_Returned()
        {
            SetupSLMSPlugin();
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileProvider);
            var returnedReqs = ds.GetAllSoftwareSystemTests();
            ClassicAssert.AreSame(TCs[0], returnedReqs[0]);
            ClassicAssert.AreSame(TCs[1], returnedReqs[1]);
            ClassicAssert.AreSame(TCs[1], ds.GetSoftwareSystemTest("TC_id2"));
            ClassicAssert.AreSame(TCs[0], ds.GetItem("TC_id1"));
            var returnedItems = ds.GetItems(new TraceEntity("SoftwareSystemTest", "System Level Test", "SLT", TraceEntityType.Truth));
            Assert.That(returnedItems[0].ItemID, Is.EqualTo(TCs[0].ItemID));
            Assert.That(returnedItems[1].ItemID, Is.EqualTo(TCs[1].ItemID));
        }

        [Test]
        public void SOUP_Items_Can_Be_Retrieved_VERIFIES_Supplied_SOUP_Items_Are_Returned()
        {
            SetupSLMSPlugin();
            SetupSrcCodePlugin();
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileProvider);
            var returnedSOUP = ds.GetAllSOUP();
            ClassicAssert.AreSame(SOUPs[0], returnedSOUP[0]);
            ClassicAssert.AreSame(SOUPs[1], returnedSOUP[1]);
            ClassicAssert.AreSame(SOUPs[1], ds.GetSOUP("SOUP_id2"));
            ClassicAssert.AreSame(SOUPs[0], ds.GetItem("SOUP_id1"));
            var returnedItems = ds.GetItems(new TraceEntity("SOUP", "SOUP", "OTS", TraceEntityType.Truth));
            Assert.That(returnedItems[0].ItemID, Is.EqualTo(SOUPs[0].ItemID));
            Assert.That(returnedItems[1].ItemID, Is.EqualTo(SOUPs[1].ItemID));
        }

        [Test]
        public void RISKs_Can_Be_Retrieved_VERIFIES_Supplied_RISKs_Are_Returned()
        {
            SetupSLMSPlugin();
            SetupSrcCodePlugin();
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileProvider);
            var returnedRISKs = ds.GetAllRisks();
            ClassicAssert.AreSame(RISKs[0], returnedRISKs[0]);
            ClassicAssert.AreSame(RISKs[1], returnedRISKs[1]);
            ClassicAssert.AreSame(RISKs[1], ds.GetRisk("RISK_id2"));
            ClassicAssert.AreSame(RISKs[0], ds.GetItem("RISK_id1"));
            var returnedItems = ds.GetItems(new TraceEntity("Risk", "Risk", "RSK", TraceEntityType.Truth));
            Assert.That(returnedItems[0].ItemID, Is.EqualTo(RISKs[0].ItemID));
            Assert.That(returnedItems[1].ItemID, Is.EqualTo(RISKs[1].ItemID));
        }

        [Test]
        public void Unit_Tests_Can_Be_Retrieved_VERIFIES_Supplied_Unit_Tests_Are_Returned()
        {
            SetupSLMSPlugin();
            SetupSrcCodePlugin();
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileProvider);
            var returnedUTs = ds.GetAllUnitTests();
            ClassicAssert.AreSame(SLMSUTs[0], returnedUTs[0]);
            ClassicAssert.AreSame(SLMSUTs[1], returnedUTs[1]);
            ClassicAssert.AreSame(SRCUTs[0], returnedUTs[2]);
            ClassicAssert.AreSame(SRCUTs[1], returnedUTs[3]);
            ClassicAssert.AreSame(returnedUTs[2], ds.GetUnitTest("UT_id3"));
            ClassicAssert.AreSame(returnedUTs[0], ds.GetItem("UT_id1"));
            var returnedItems = ds.GetItems(new TraceEntity("UnitTest", "Unit Test", "UT", TraceEntityType.Truth));
            Assert.That(returnedItems[0].ItemID, Is.EqualTo(SLMSUTs[0].ItemID));
            Assert.That(returnedItems[2].ItemID, Is.EqualTo(SRCUTs[0].ItemID));
        }

        [Test]
        public void Anomalies_Can_Be_Retrieved_VERIFIES_Supplied_Anomalies_Are_Returned()
        {
            SetupSLMSPlugin();
            SetupSrcCodePlugin();
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileProvider);
            var returnedAnomalies = ds.GetAllAnomalies();
            ClassicAssert.IsTrue(returnedAnomalies.Count > 0);
            ClassicAssert.AreSame(ANOMALYs[0], returnedAnomalies[0]);
            ClassicAssert.AreSame(ANOMALYs[1], returnedAnomalies[1]);
            ClassicAssert.AreSame(ANOMALYs[1], ds.GetAnomaly("ANOMALY_id2"));
            ClassicAssert.AreSame(ANOMALYs[0], ds.GetItem("ANOMALY_id1"));
            var returnedItems = ds.GetItems(new TraceEntity("Anomaly","Bug","Bug",TraceEntityType.Truth));
            Assert.That(returnedItems[0].ItemID,Is.EqualTo(ANOMALYs[0].ItemID));
            Assert.That(returnedItems[1].ItemID, Is.EqualTo(ANOMALYs[1].ItemID));
        }

        [Test]
        public void GetItems_Function_Verifies_TraceEntity_VERIFIES_Exception_Is_Thrown_When_Unknown_Trace_Entity()
        {
            SetupSLMSPlugin();
            SetupSrcCodePlugin();
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileProvider);
            var ex = Assert.Throws<Exception>(() => ds.GetItems(new TraceEntity("wrongID", "wrong", "wrong", TraceEntityType.Truth)));
            Assert.That(ex.Message, Is.EqualTo("No datasource available for unknown trace entity: wrongID"));
        }

        [Test]
        public void External_Dependencies_Can_Be_Retrieved_VERIFIES_Supplied_External_Dependencies_Are_Returned()
        {
            SetupSLMSPlugin();
            SetupDepPlugin();
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileProvider);
            var returnedDeps = ds.GetAllExternalDependencies();
            ClassicAssert.IsTrue(returnedDeps.Count > 0);
            ClassicAssert.AreSame(DEPs[0], returnedDeps[0]);
            ClassicAssert.AreSame(DEPs[1], returnedDeps[1]);
        }


        [Test]
        public void GetItem_Can_Handle_Missing_ID_VERIFIES_Null_Returned_For_Missing_ID()
        {
            SetupSLMSPlugin();
            SetupSrcCodePlugin();
            var ds = new PluginDataSources(mockConfiguration,mockPluginLoader, mockFileProvider);
            var result = ds.GetItem("does not exist");
            ClassicAssert.AreSame(null, result);
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
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileProvider);
            ClassicAssert.AreSame("TestValue",ds.GetConfigValue("testKey"));
            ClassicAssert.AreSame("NOT FOUND", ds.GetConfigValue("estKey"));
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
            mockFileProvider = new LocalFileSystemPlugin(fileSystem);
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileProvider);
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
            mockFileProvider = new LocalFileSystemPlugin(fileSystem);
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileProvider);
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
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileProvider);
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

        [Test]
        public void EliminatedSystemRequirements_Can_Be_Retrieved_VERIFIES_Supplied_Items_Are_Returned()
        {
            SetupSLMSPlugin();
            SetupSrcCodePlugin();
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileProvider);
            var returnedItems = ds.GetAllEliminatedSystemRequirements();
            ClassicAssert.IsTrue(returnedItems.Count > 0);
            ClassicAssert.AreSame(eliminatedSystemRequirements[0], returnedItems[0]);
            ClassicAssert.AreSame(eliminatedSystemRequirements[1], returnedItems[1]);
            ClassicAssert.AreSame(eliminatedSystemRequirements[0], ds.GetEliminatedSystemRequirement("SR_id1"));
            ClassicAssert.AreEqual("System Requirement 1", returnedItems[0].ItemTitle);
            ClassicAssert.AreEqual(EliminationReason.FilteredOut, returnedItems[0].EliminationType);
            ClassicAssert.AreEqual("Filtered by release version", returnedItems[0].EliminationReason);
        }
        
        [Test]
        public void EliminatedSoftwareRequirements_Can_Be_Retrieved_VERIFIES_Supplied_Items_Are_Returned()
        {
            SetupSLMSPlugin();
            SetupSrcCodePlugin();
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileProvider);
            var returnedItems = ds.GetAllEliminatedSoftwareRequirements();
            ClassicAssert.IsTrue(returnedItems.Count > 0);
            ClassicAssert.AreSame(eliminatedSoftwareRequirements[0], returnedItems[0]);
            ClassicAssert.AreSame(eliminatedSoftwareRequirements[1], returnedItems[1]);
            ClassicAssert.AreSame(eliminatedSoftwareRequirements[0], ds.GetEliminatedSoftwareRequirement("SWR_id1"));
            ClassicAssert.AreEqual("Software Requirement 1", returnedItems[0].ItemTitle);
            ClassicAssert.AreEqual(EliminationReason.LinkedItemMissing, returnedItems[0].EliminationType);
            ClassicAssert.AreEqual("Parent item was filtered", returnedItems[0].EliminationReason);
        }

        [Test]
        public void EliminatedDocumentationRequirements_Can_Be_Retrieved_VERIFIES_Supplied_Items_Are_Returned()
        {
            SetupSLMSPlugin();
            SetupSrcCodePlugin();
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileProvider);
            var returnedItems = ds.GetAllEliminatedDocumentationRequirements();
            ClassicAssert.IsTrue(returnedItems.Count > 0);
            ClassicAssert.AreSame(eliminatedDocumentationRequirements[0], returnedItems[0]);
            ClassicAssert.AreSame(eliminatedDocumentationRequirements[1], returnedItems[1]);
            ClassicAssert.AreSame(eliminatedDocumentationRequirements[0], ds.GetEliminatedDocumentationRequirement("DOC_id1"));
            ClassicAssert.AreEqual("Documentation Requirement 1", returnedItems[0].ItemTitle);
            ClassicAssert.AreEqual(EliminationReason.FilteredOut, returnedItems[0].EliminationType);
            ClassicAssert.AreEqual("Filtered by category", returnedItems[0].EliminationReason);
        }

        [Test]
        public void EliminatedSoftwareSystemTests_Can_Be_Retrieved_VERIFIES_Supplied_Items_Are_Returned()
        {
            SetupSLMSPlugin();
            SetupSrcCodePlugin();
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileProvider);
            var returnedItems = ds.GetAllEliminatedSoftwareSystemTests();
            ClassicAssert.IsTrue(returnedItems.Count > 0);
            ClassicAssert.AreSame(eliminatedSoftwareSystemTests[0], returnedItems[0]);
            ClassicAssert.AreSame(eliminatedSoftwareSystemTests[1], returnedItems[1]);
            ClassicAssert.AreSame(eliminatedSoftwareSystemTests[0], ds.GetEliminatedSoftwareSystemTestItem("SST_id1"));
            ClassicAssert.AreEqual("Software System Test 1", returnedItems[0].ItemTitle);
            ClassicAssert.AreEqual(EliminationReason.FilteredOut, returnedItems[0].EliminationType);
            ClassicAssert.AreEqual("Filtered by test method", returnedItems[0].EliminationReason);
        }
        
        [Test]
        public void EliminatedRisks_Can_Be_Retrieved_VERIFIES_Supplied_Items_Are_Returned()
        {
            SetupSLMSPlugin();
            SetupSrcCodePlugin();
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileProvider);
            var returnedItems = ds.GetAllEliminatedRisks();
            ClassicAssert.IsTrue(returnedItems.Count > 0);
            ClassicAssert.AreSame(eliminatedRisks[0], returnedItems[0]);
            ClassicAssert.AreSame(eliminatedRisks[1], returnedItems[1]);
            ClassicAssert.AreSame(eliminatedRisks[0], ds.GetEliminatedRisk("RISK_id1"));
            ClassicAssert.AreEqual("Risk 1", returnedItems[0].ItemTitle);
            ClassicAssert.AreEqual(EliminationReason.FilteredOut, returnedItems[0].EliminationType);
            ClassicAssert.AreEqual("Filtered by risk type", returnedItems[0].EliminationReason);
        }

        [Test]
        public void EliminatedSOUP_Can_Be_Retrieved_VERIFIES_Supplied_Items_Are_Returned()
        {
            SetupSLMSPlugin();
            SetupSrcCodePlugin();
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileProvider);
            var returnedItems = ds.GetAllEliminatedSOUP();
            ClassicAssert.IsTrue(returnedItems.Count > 0);
            ClassicAssert.AreSame(eliminatedSOUP[0], returnedItems[0]);
            ClassicAssert.AreSame(eliminatedSOUP[1], returnedItems[1]);
            ClassicAssert.AreSame(eliminatedSOUP[0], ds.GetEliminatedSOUP("SOUP_id1"));
            ClassicAssert.AreEqual("SOUP 1", returnedItems[0].SOUPName);
            ClassicAssert.AreEqual(EliminationReason.FilteredOut, returnedItems[0].EliminationType);
            ClassicAssert.AreEqual("Filtered by version", returnedItems[0].EliminationReason);
        }

        [Test]
        public void EliminatedAnomalies_Can_Be_Retrieved_VERIFIES_Supplied_Items_Are_Returned()
        {
            SetupSLMSPlugin();
            SetupSrcCodePlugin();
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileProvider);
            var returnedItems = ds.GetAllEliminatedAnomalies();
            ClassicAssert.IsTrue(returnedItems.Count > 0);
            ClassicAssert.AreSame(eliminatedAnomalies[0], returnedItems[0]);
            ClassicAssert.AreSame(eliminatedAnomalies[1], returnedItems[1]);
            ClassicAssert.AreSame(eliminatedAnomalies[0], ds.GetEliminatedAnomaly("ANOMALY_id1"));
            ClassicAssert.AreEqual("Anomaly 1", returnedItems[0].ItemTitle);
            ClassicAssert.AreEqual(EliminationReason.FilteredOut, returnedItems[0].EliminationType);
            ClassicAssert.AreEqual("Filtered by status", returnedItems[0].EliminationReason);
        }

        [Test]
        public void EliminatedDocContents_Can_Be_Retrieved_VERIFIES_Supplied_Items_Are_Returned()
        {
            SetupSLMSPlugin();
            SetupSrcCodePlugin();
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileProvider);
            var returnedItems = ds.GetAllEliminatedDocContents();
            ClassicAssert.IsTrue(returnedItems.Count > 0);
            ClassicAssert.AreSame(eliminatedDocContents[0], returnedItems[0]);
            ClassicAssert.AreSame(eliminatedDocContents[1], returnedItems[1]);
            ClassicAssert.AreSame(eliminatedDocContents[0], ds.GetEliminatedDocContent("CONTENT_id1"));
            ClassicAssert.AreEqual("Document content 1", returnedItems[0].DocContent);
            ClassicAssert.AreEqual(EliminationReason.FilteredOut, returnedItems[0].EliminationType);
            ClassicAssert.AreEqual("Filtered by document type", returnedItems[0].EliminationReason);
        }

        [Test]
        public void GetItems_For_EliminatedEntity_VERIFIES_All_EliminatedItems_Are_Returned()
        {
            SetupSLMSPlugin();
            SetupSrcCodePlugin();
            var ds = new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileProvider);

            var eliminatedEntity = new TraceEntity("Eliminated", "Eliminated Item", "EI", TraceEntityType.Eliminated);
            var returnedItems = ds.GetItems(eliminatedEntity);

            // Verify we get all types of eliminated items combined
            ClassicAssert.IsTrue(returnedItems.Count > 0);

            // Expected count is the sum of all eliminated item types
            int expectedCount = eliminatedSystemRequirements.Count +
                                eliminatedSoftwareRequirements.Count +
                                eliminatedDocumentationRequirements.Count +
                                eliminatedSoftwareSystemTests.Count +
                                eliminatedRisks.Count +
                                eliminatedSOUP.Count +
                                eliminatedAnomalies.Count +
                                eliminatedDocContents.Count;

            Assert.That(returnedItems.Count, Is.EqualTo(expectedCount));

            // Verify specific items are included in the returned collection
            var firstSysReqId = eliminatedSystemRequirements[0].ItemID;
            var firstSoftReqId = eliminatedSoftwareRequirements[0].ItemID;
            var firstRiskId = eliminatedRisks[0].ItemID;

            Assert.That(returnedItems.Exists(i => i.ItemID == firstSysReqId), Is.True);
            Assert.That(returnedItems.Exists(i => i.ItemID == firstSoftReqId), Is.True);
            Assert.That(returnedItems.Exists(i => i.ItemID == firstRiskId), Is.True);
        }

        [UnitTestAttribute(
            Identifier = "A1B2C3D4-E5F6-7890-ABCD-EF1234567890",
            Purpose = "Plugin loading validation when all requested plugins are found",
            PostCondition = "No exception is thrown when all plugins are successfully loaded")]
        [Test]
        public void PluginLoading_AllPluginsFound_VERIFIES_NoExceptionThrown()
        {
            // Setup: Configure mock to return all requested plugins
            mockPluginLoader.LoadByName<IDataSourcePlugin>(Arg.Any<string>(), Arg.Is("testPlugin1"), Arg.Any<Action<IServiceCollection>>()).Returns(mockSLMSPlugin);
            mockPluginLoader.LoadByName<IDataSourcePlugin>(Arg.Any<string>(), Arg.Is("testPlugin2"), Arg.Any<Action<IServiceCollection>>()).Returns(mockDepMgmtPlugin);
            mockPluginLoader.LoadByName<IDataSourcePlugin>(Arg.Any<string>(), Arg.Is("testDepPlugin"), Arg.Any<Action<IServiceCollection>>()).Returns(mockDepMgmtPlugin);
            mockPluginLoader.LoadByName<IDataSourcePlugin>(Arg.Any<string>(), Arg.Is("testSrcPlugin"), Arg.Any<Action<IServiceCollection>>()).Returns(mockSrcCodePlugin);
            
            // Act & Assert: Should not throw
            Assert.DoesNotThrow(() => new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileProvider));
        }

        [UnitTestAttribute(
            Identifier = "B2C3D4E5-F6G7-8901-BCDE-F23456789012",
            Purpose = "Plugin loading validation when a required plugin is missing",
            PostCondition = "Exception is thrown when a required plugin cannot be found")]
        [Test]
        public void PluginLoading_MissingRequiredPlugin_VERIFIES_ExceptionThrown()
        {
            // Setup: Configure mock to return null for one plugin
            mockConfiguration.DataSourcePlugins.ReturnsForAnyArgs(new List<string> { "testPlugin2", "testDepPlugin", "testPlugin1", "testSrcPlugin" });
            mockPluginLoader.LoadByName<IDataSourcePlugin>(Arg.Any<string>(), Arg.Is("testPlugin1"), Arg.Any<Action<IServiceCollection>>()).Returns((IDataSourcePlugin)null);
            mockPluginLoader.LoadByName<IDataSourcePlugin>(Arg.Any<string>(), Arg.Is("testPlugin2"), Arg.Any<Action<IServiceCollection>>()).Returns(mockSLMSPlugin);
            mockPluginLoader.LoadByName<IDataSourcePlugin>(Arg.Any<string>(), Arg.Is("testDepPlugin"), Arg.Any<Action<IServiceCollection>>()).Returns(mockDepMgmtPlugin);
            mockPluginLoader.LoadByName<IDataSourcePlugin>(Arg.Any<string>(), Arg.Is("testSrcPlugin"), Arg.Any<Action<IServiceCollection>>()).Returns(mockSrcCodePlugin);
            
            // Act & Assert: Should throw exception for missing plugin
            var ex = Assert.Throws<Exception>(() => new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileProvider));
            Assert.That(ex.Message.Contains("Unable to find plugin testPlugin1"));
        }

        [UnitTestAttribute(
            Identifier = "C3D4E5F6-G7H8-9012-CDEF-345678901234",
            Purpose = "Plugin loading validation when multiple required plugins are missing",
            PostCondition = "Exception is thrown when multiple required plugins cannot be found")]
        [Test]
        public void PluginLoading_MultipleMissingPlugins_VERIFIES_ExceptionThrownForFirstMissingPlugin()
        {
            // Setup: Configure mock to return null for multiple plugins
            mockPluginLoader.LoadByName<IDataSourcePlugin>(Arg.Any<string>(), Arg.Is("testPlugin1"), Arg.Any<Action<IServiceCollection>>()).Returns((IDataSourcePlugin)null);
            mockPluginLoader.LoadByName<IDataSourcePlugin>(Arg.Any<string>(), Arg.Is("testPlugin2"), Arg.Any<Action<IServiceCollection>>()).Returns((IDataSourcePlugin)null);
            mockPluginLoader.LoadByName<IDataSourcePlugin>(Arg.Any<string>(), Arg.Is("testDepPlugin"), Arg.Any<Action<IServiceCollection>>()).Returns(mockDepMgmtPlugin);
            mockPluginLoader.LoadByName<IDataSourcePlugin>(Arg.Any<string>(), Arg.Is("testSrcPlugin"), Arg.Any<Action<IServiceCollection>>()).Returns(mockSrcCodePlugin);
            
            // Act & Assert: Should throw exception for first missing plugin
            var ex = Assert.Throws<Exception>(() => new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileProvider));
            Assert.That(ex.Message.Contains("Unable to find plugin testPlugin2"));
        }

        [UnitTestAttribute(
            Identifier = "D4E5F6G7-H8I9-0123-DEFG-456789012345",
            Purpose = "Plugin loading validation when all plugins are missing",
            PostCondition = "Exception is thrown when all required plugins cannot be found")]
        [Test]
        public void PluginLoading_AllPluginsMissing_VERIFIES_ExceptionThrownForFirstMissingPlugin()
        {
            // Setup: Configure mock to return null for all plugins
            mockPluginLoader.LoadByName<IDataSourcePlugin>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Action<IServiceCollection>>()).Returns((IDataSourcePlugin)null);
            
            // Act & Assert: Should throw exception for first missing plugin
            var ex = Assert.Throws<Exception>(() => new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileProvider));
            Assert.That(ex.Message.Contains("Unable to find plugin testPlugin2"));
        }

        [UnitTestAttribute(
            Identifier = "E5F6G7H8-I9J0-1234-EFGH-567890123456",
            Purpose = "Plugin loading validation with empty plugin list",
            PostCondition = "No exception is thrown when no plugins are configured")]
        [Test]
        public void PluginLoading_EmptyPluginList_VERIFIES_NoExceptionThrown()
        {
            // Setup: Configure empty plugin list
            mockConfiguration.DataSourcePlugins.Returns(new List<string>());
            
            // Act & Assert: Should not throw when no plugins are configured
            Assert.DoesNotThrow(() => new PluginDataSources(mockConfiguration, mockPluginLoader, mockFileProvider));
        }
    }
}
