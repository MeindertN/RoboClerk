using NUnit.Framework;
using NSubstitute;
using RoboClerk.Configuration;
using System.Collections.Generic;
using Tomlyn.Model;
using System;
using System.Linq;
using NSubstitute.Extensions;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("These tests are testing the traceability functionality")]
    internal class TestTraceFunctionality
    {
        IConfiguration mockConfig = null;

        [SetUp]
        public void TestSetup()
        {
            mockConfig = Substitute.For<IConfiguration>();
            List<TraceEntity> truth = new List<TraceEntity>();
            truth.Add(new TraceEntity("SystemRequirement", "SYS_name", "SYS",TraceEntityType.Truth));
            truth.Add(new TraceEntity("SoftwareRequirement", "SWR_name", "SWR", TraceEntityType.Truth));
            truth.Add(new TraceEntity("SoftwareSystemTest", "TC_name", "TC", TraceEntityType.Truth));
            truth.Add(new TraceEntity("SoftwareUnitTest", "UT_name", "UT", TraceEntityType.Truth));
            truth.Add(new TraceEntity("Risk","RSK_name","RSK", TraceEntityType.Truth));
            truth.Add(new TraceEntity("Anomaly", "ANOMALY_name", "ANOMALY", TraceEntityType.Truth));
            mockConfig.TruthEntities.Returns(truth);

            List<DocumentConfig> config = new List<DocumentConfig>();
            config.Add(new DocumentConfig("SystemRequirementsSpecification", "DOC001", "PRS Title", "PRS", "fake0"));
            config.Add(new DocumentConfig("SoftwareRequirementsSpecification", "DOC002", "SRS Title", "SRS", "fake1"));
            config.Add(new DocumentConfig("SystemLevelTestPlan", "DOC003", "SLTP Title", "SLTP", "fake2"));
            config.Add(new DocumentConfig("RiskAssessmentRecord", "DOC004", "RAR Title", "RAR", "fake3"));
            mockConfig.Documents.Returns(config);

            List<TraceConfig> config2 = new List<TraceConfig>();
            config2.Add(new TraceConfig("SystemRequirement"));
            config2.Add(new TraceConfig("SoftwareRequirement"));
            TomlTable toml = new TomlTable();
            TomlTable temp = new TomlTable();
            temp["forward"] = new TomlArray() { "ALL" };
            temp["backward"] = new TomlArray() { "ALL" };
            temp["forwardLink"] = "DOC";
            temp["backwardLink"] = "DOC";
            toml["SystemRequirementsSpecification"] = temp;

            temp = new TomlTable();
            temp["forward"] = new TomlArray() { "ALL" };
            temp["backward"] = new TomlArray() { "ALL" };
            temp["forwardLink"] = "Child";
            temp["backwardLink"] = "Parent";
            toml["SoftwareRequirement"] = temp;

            temp = new TomlTable();
            temp["forward"] = new TomlArray() { "CategoryName" };
            temp["backward"] = new TomlArray() { "ALL" };
            temp["forwardLink"] = "DOC";
            temp["backwardLink"] = "DOC";
            toml["RiskAssessmentRecord"] = temp;
            config2[0].AddTraces(toml);
            
            TomlTable toml2 = new TomlTable();
            temp = new TomlTable();
            temp["forward"] = new TomlArray() { "ALL" };
            temp["backward"] = new TomlArray() { "ALL" };
            temp["forwardLink"] = "DOC";
            temp["backwardLink"] = "DOC";
            toml2["SoftwareRequirementsSpecification"] = temp;

            temp = new TomlTable();
            temp["forward"] = new TomlArray() { "ALL" };
            temp["backward"] = new TomlArray() { "ALL" };
            temp["forwardLink"] = "Parent";
            temp["backwardLink"] = "Child";
            toml2["SystemRequirement"] = temp;

            temp = new TomlTable();
            temp["forward"] = new TomlArray() { "CategoryName" };
            temp["backward"] = new TomlArray() { "ALL" };
            temp["forwardLink"] = "DOC";
            temp["backwardLink"] = "DOC";
            toml2["RiskAssessmentRecord"] = temp;
            config2[1].AddTraces(toml2);
            mockConfig.TraceConfig.Returns(config2);

            mockConfig.DataSourcePlugins.ReturnsForAnyArgs(new List<string> { "testPlugin1", "testPlugin2" });
            mockConfig.PluginDirs.ReturnsForAnyArgs(new List<string> { "c:\\temp\\does_not_exist", "c:\\temp\\" });
        }

        [Test]
        public void TraceAbility_Analysis_Can_Be_Created_VERIFIES_Class_Does_Not_Throw_On_Creation_With_Valid_Input()
        {
            ITraceabilityAnalysis traceabilityAnalysis = new TraceabilityAnalysis(mockConfig);
        }

        [Test]
        public void Getting_The_Title_For_The_Trace_Entity_VERIFIES_Correct_Title_Returned_For_Given_Trace_Entity()
        {
            ITraceabilityAnalysis traceabilityAnalysis = new TraceabilityAnalysis(mockConfig);
            Assert.AreEqual("ANOMALY_name", traceabilityAnalysis.GetTitleForTraceEntity("Anomaly"));
            Assert.AreEqual("SLTP Title", traceabilityAnalysis.GetTitleForTraceEntity("SystemLevelTestPlan"));
            Assert.AreEqual("SLTP: No title", traceabilityAnalysis.GetTitleForTraceEntity("SLTP"));
        }

        [Test]
        public void Getting_The_Abbreviation_For_The_Trace_Entity_VERIFIES_Correct_Abbreviation_Returned_For_Given_Trace_Entity()
        {
            ITraceabilityAnalysis traceabilityAnalysis = new TraceabilityAnalysis(mockConfig);
            Assert.AreEqual("ANOMALY", traceabilityAnalysis.GetAbreviationForTraceEntity("Anomaly"));
            Assert.AreEqual("SLTP", traceabilityAnalysis.GetAbreviationForTraceEntity("SystemLevelTestPlan"));
            Assert.AreEqual("SLTP: No abbreviation", traceabilityAnalysis.GetAbreviationForTraceEntity("SLTP"));
        }

        [Test]
        public void Getting_The_Trace_Entity_For_The_Title_VERIFIES_Correct_Trace_Entity_Returned_For_Given_Title()
        {
            ITraceabilityAnalysis traceabilityAnalysis = new TraceabilityAnalysis(mockConfig);
            Assert.AreEqual("SoftwareSystemTest", traceabilityAnalysis.GetTraceEntityForTitle("TC_name").ID);
            Assert.AreEqual("RiskAssessmentRecord", traceabilityAnalysis.GetTraceEntityForTitle("RAR Title").ID);
            Assert.AreEqual(default(TraceEntity), traceabilityAnalysis.GetTraceEntityForTitle("SLTP"));
        }

        [Test]
        public void Getting_The_Trace_Entity_For_The_Identifier_VERIFIES_Correct_Trace_Entity_Returned_For_Given_ID()
        {
            ITraceabilityAnalysis traceabilityAnalysis = new TraceabilityAnalysis(mockConfig);
            Assert.AreEqual("Anomaly", traceabilityAnalysis.GetTraceEntityForID("Anomaly").ID);
            Assert.AreEqual("RiskAssessmentRecord", traceabilityAnalysis.GetTraceEntityForID("RiskAssessmentRecord").ID);
            Assert.AreEqual(default(TraceEntity), traceabilityAnalysis.GetTraceEntityForID("SLTP"));
        }

        [Test]
        public void Getting_The_Trace_Entity_For_Any_Property_VERIFIES_Correct_Trace_Entity_Returned_For_Any_Property()
        {
            ITraceabilityAnalysis traceabilityAnalysis = new TraceabilityAnalysis(mockConfig);
            Assert.AreEqual("Anomaly", traceabilityAnalysis.GetTraceEntityForAnyProperty("Anomaly").ID);
            Assert.AreEqual("Anomaly", traceabilityAnalysis.GetTraceEntityForAnyProperty("ANOMALY_name").ID);
            Assert.AreEqual("Anomaly", traceabilityAnalysis.GetTraceEntityForAnyProperty("ANOMALY").ID);
            Assert.AreEqual("RiskAssessmentRecord", traceabilityAnalysis.GetTraceEntityForAnyProperty("RiskAssessmentRecord").ID);
            Assert.AreEqual("RiskAssessmentRecord", traceabilityAnalysis.GetTraceEntityForAnyProperty("RAR Title").ID);
            Assert.AreEqual("RiskAssessmentRecord", traceabilityAnalysis.GetTraceEntityForAnyProperty("RAR").ID);
            Assert.AreEqual(default(TraceEntity), traceabilityAnalysis.GetTraceEntityForAnyProperty("RARS"));
        }

        [Test]
        public void Extracting_Trace_information_From_Trace_Tag_VERIFIES_Correct_Trace_Is_Recorded_From_Tag_And_Title()
        {
            RoboClerkTag tag = new RoboClerkTag(0, 20, "@@Trace:SWR(id=1234)@@", true);
            ITraceabilityAnalysis traceabilityAnalysis = new TraceabilityAnalysis(mockConfig);
            traceabilityAnalysis.AddTraceTag("RAR Title", tag);
            IEnumerable<TraceLink> tls = traceabilityAnalysis.GetTraceLinksForDocument(traceabilityAnalysis.GetTraceEntityForTitle("RAR Title"));
            List<TraceLink> links = new List<TraceLink>();
            foreach (var tl in tls)
            {
                links.Add(tl);
            }

            Assert.IsTrue(links.Count == 1);
            Assert.AreEqual("SoftwareRequirement", links[0].Source.ID);
            Assert.AreEqual("1234", links[0].SourceID);
            Assert.AreEqual("RiskAssessmentRecord", links[0].Target.ID);
            Assert.AreEqual("1234", links[0].TargetID);
            RoboClerkTag tagNoID = new RoboClerkTag(0, 13, "@@Trace:SWR()@@", true);
            var result = Assert.Throws<TagInvalidException>(() => { traceabilityAnalysis.AddTraceTag("RAR Title", tagNoID); });
            Assert.IsTrue(result.Message.Contains("RAR Title"));
            Assert.IsTrue(result.Message.Contains("Trace:SWR()"));
        }

        [Test]
        public void Adding_Trace_Link_Directly_VERIFIES_Trace_Link_Is_Stored_Correctly()
        {
            ITraceabilityAnalysis traceabilityAnalysis = new TraceabilityAnalysis(mockConfig);
            TraceEntity source = traceabilityAnalysis.GetTraceEntityForID("SoftwareRequirement");
            string sourceID = "source ID";
            TraceEntity target = traceabilityAnalysis.GetTraceEntityForID("SoftwareRequirementsSpecification");
            string targetID = "target ID";
            traceabilityAnalysis.AddTrace(source,sourceID,target,targetID);

            var tet = traceabilityAnalysis.GetTraceEntityForTitle("SRS Title");
            var result = traceabilityAnalysis.GetTraceLinksForDocument(tet).ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(sourceID, result[0].SourceID);
            Assert.AreEqual(targetID, result[0].TargetID);
            Assert.AreEqual(source, result[0].Source);
            Assert.AreEqual(target, result[0].Target);
            result = traceabilityAnalysis.GetTraceLinksForDocument(default(TraceEntity)).ToList();
            Assert.AreEqual(0, result.Count);
        }

        private List<RequirementItem> SYSs = null;
        private List<RequirementItem> SWRs = null;
        private List<TestCaseItem> TCs = null;
        private List<AnomalyItem> ANOMALYs = null;
        private List<UnitTestItem> UTs = null;
        private ISLMSPlugin mockPlugin = Substitute.For< ISLMSPlugin>();
        private List<SOUPItem> SOUPs = null;
        private List<RiskItem> RISKs = null;
        private void SetupPlugin()
        {
            SYSs = new List<RequirementItem> { new RequirementItem(RequirementType.SystemRequirement), new RequirementItem(RequirementType.SystemRequirement) };
            SYSs[0].RequirementTitle = "SYS_TestTitle1";
            SYSs[0].ItemID = "SYS_id1";
            SYSs[0].AddLinkedItem(new ItemLink("SWR_id1",ItemLinkType.Child));
            SYSs[1].RequirementTitle = "SYS_TestTitle2";
            SYSs[1].ItemID = "SYS_id2";
            SYSs[1].ItemCategory = "CategoryName";
            SYSs[1].AddLinkedItem(new ItemLink("SWR_id2",ItemLinkType.Child));
            mockPlugin.GetSystemRequirements().Returns(SYSs);
            SWRs = new List<RequirementItem> { new RequirementItem(RequirementType.SoftwareRequirement), new RequirementItem(RequirementType.SoftwareRequirement) };
            SWRs[0].RequirementTitle = "SWR_TestTitle1";
            SWRs[0].ItemID = "SWR_id1";
            SWRs[0].AddLinkedItem(new ItemLink(SYSs[0].ItemID, ItemLinkType.Parent));
            SWRs[1].RequirementTitle = "SWR_TestTitle2";
            SWRs[1].ItemID = "SWR_id2";
            SWRs[1].ItemCategory = "CategoryName";
            SWRs[1].AddLinkedItem(new ItemLink(SYSs[1].ItemID, ItemLinkType.Parent));
            mockPlugin.GetSoftwareRequirements().Returns(SWRs);
            TCs = new List<TestCaseItem> { new TestCaseItem(), new TestCaseItem() };
            TCs[0].TestCaseTitle = "TC_TestTitle1";
            TCs[0].ItemID = "TC_id1";
            TCs[0].AddLinkedItem(new ItemLink("SWR_id1", ItemLinkType.Parent));
            TCs[1].TestCaseTitle = "TC_TestTitle2";
            TCs[1].ItemID = "TC_id2";
            TCs[1].AddLinkedItem(new ItemLink("SWR_id2", ItemLinkType.Parent));
            mockPlugin.GetSoftwareSystemTests().Returns(TCs);
            ANOMALYs = new List<AnomalyItem> { new AnomalyItem(), new AnomalyItem() };
            ANOMALYs[0].AnomalyTitle = "ANOMALY_TestTitle1";
            ANOMALYs[0].ItemID = "ANOMALY_id1";
            ANOMALYs[1].AnomalyTitle = "ANOMALY_TestTitle2";
            ANOMALYs[1].ItemID = "ANOMALY_id2";
            mockPlugin.GetAnomalies().Returns(ANOMALYs);
            UTs = new List<UnitTestItem> { new UnitTestItem(), new UnitTestItem() };
            UTs[0].UnitTestPurpose = "UT_TestPurpose1";
            UTs[0].ItemID = "UT_id1";
            UTs[1].UnitTestPurpose = "UT_TestPurpose2";
            UTs[1].UnitTestPurpose = "UT_id2";
            mockPlugin.GetUnitTests().Returns(UTs);
            SOUPs = new List<SOUPItem> { new SOUPItem(), new SOUPItem() };
            SOUPs[0].SOUPName = "SOUP_name1";
            SOUPs[0].SOUPLicense = "SOUP_license1";
            SOUPs[0].SOUPDetailedDescription = "SOUP_details1";
            SOUPs[0].ItemID = "SOUP_id1";
            SOUPs[1].SOUPName = "SOUP_name2";
            SOUPs[1].SOUPLicense = "SOUP_license2";
            SOUPs[1].SOUPDetailedDescription = "SOUP_details2";
            SOUPs[1].ItemID = "SOUP_id2";
            mockPlugin.GetSOUP().Returns(SOUPs);
            RISKs = new List<RiskItem> { new RiskItem(), new RiskItem() };
            RISKs[0].DetectabilityScore = 100;
            RISKs[0].CauseOfFailure = "RISK_cause1";
            RISKs[0].ItemID = "RISK_id1";
            RISKs[1].DetectabilityScore = 200;
            RISKs[1].CauseOfFailure = "RISK_cause2";
            RISKs[1].ItemID = "RISK_id2";
            mockPlugin.GetRisks().Returns(RISKs);
        }


        private IDataSources GenerateDataSources(List<RequirementItem> systemReqs, List<RequirementItem> softwareReqs, 
            List<TestCaseItem> tcs, List<AnomalyItem> ans)
        {
            SetupPlugin();
            IPluginLoader mockPluginLoader = Substitute.For<IPluginLoader>();
            mockPluginLoader.LoadPlugin<IPlugin>(Arg.Any<string>(), Arg.Any<string>()).Returns<IPlugin>(l => null);
            mockPluginLoader.Configure().LoadPlugin<IPlugin>(Arg.Is("testPlugin2"), Arg.Is("c:\\temp\\does_not_exist")).Returns(mockPlugin);
            IDataSources dataSources = new DataSources(mockConfig, mockPluginLoader, null);
            SYSs.AddRange(systemReqs);
            SWRs.AddRange(softwareReqs);
            TCs.AddRange(tcs);
            ANOMALYs.AddRange(ans);
            return dataSources;
        }

        [Test]
        public void Trace_From_Truth_To_Document_VERIFIES_Correct_Trace_Matrix_Is_Generated()
        {
            ITraceabilityAnalysis traceabilityAnalysis = new TraceabilityAnalysis(mockConfig);
            var tet = traceabilityAnalysis.GetTraceEntityForID("SystemRequirement");
            IDataSources dataSources = GenerateDataSources(new List<RequirementItem>(), new List<RequirementItem>(),
                new List<TestCaseItem>(),new List<AnomalyItem>());

            //add valid trace in PRS / SRS / RAR
            traceabilityAnalysis.AddTrace(traceabilityAnalysis.GetTraceEntityForID("SystemRequirement"),"SYS_id1",
                traceabilityAnalysis.GetTraceEntityForID("SystemRequirementsSpecification"),"SYS_id1");
            traceabilityAnalysis.AddTrace(traceabilityAnalysis.GetTraceEntityForID("SystemRequirement"), "SYS_id2",
                traceabilityAnalysis.GetTraceEntityForID("SystemRequirementsSpecification"), "SYS_id2");
            traceabilityAnalysis.AddTrace(traceabilityAnalysis.GetTraceEntityForID("SystemRequirement"), "SYS_id2",
                traceabilityAnalysis.GetTraceEntityForID("RiskAssessmentRecord"), "SYS_id2");

            var tet1 = traceabilityAnalysis.GetTraceEntityForID("SystemRequirementsSpecification");
            var tet2 = traceabilityAnalysis.GetTraceEntityForID("SoftwareRequirement");
            var tet3 = traceabilityAnalysis.GetTraceEntityForID("RiskAssessmentRecord");


            var matrix = traceabilityAnalysis.PerformAnalysis(dataSources, tet);
            Assert.AreEqual(4, matrix.Count);
            Assert.AreEqual(2, matrix[tet].Count);
            Assert.AreEqual(2, matrix[tet1].Count);
            Assert.AreEqual(2, matrix[tet2].Count);
            Assert.AreEqual(2, matrix[tet3].Count);
            
            Assert.AreEqual(1, matrix[tet][0].Count);
            Assert.AreEqual(1, matrix[tet][1].Count);
            Assert.AreEqual("SYS_id1", matrix[tet][0][0].ItemID);
            Assert.AreEqual("SYS_id2", matrix[tet][1][0].ItemID);

            Assert.AreEqual(1, matrix[tet1][0].Count);
            Assert.AreEqual(1, matrix[tet1][1].Count);
            Assert.AreEqual("SYS_id1", matrix[tet1][0][0].ItemID);
            Assert.AreEqual("SYS_id2", matrix[tet1][1][0].ItemID);

            Assert.AreEqual(1, matrix[tet2][0].Count);
            Assert.AreEqual(1, matrix[tet2][1].Count);
            Assert.AreEqual("SWR_id1", matrix[tet2][0][0].ItemID);
            Assert.AreEqual("SWR_id2", matrix[tet2][1][0].ItemID);

            Assert.AreEqual(0, matrix[tet3][0].Count);
            Assert.AreEqual(1, matrix[tet3][1].Count);
            Assert.AreEqual("SYS_id2", matrix[tet3][1][0].ItemID);

            Assert.AreEqual(0, traceabilityAnalysis.GetTraceIssuesForTruth(tet).Count());
            Assert.AreEqual(0, traceabilityAnalysis.GetTraceIssuesForTruth(tet2).Count());
            Assert.AreEqual(0, traceabilityAnalysis.GetTraceIssuesForDocument(tet1).Count());
            Assert.AreEqual(0, traceabilityAnalysis.GetTraceIssuesForDocument(tet3).Count());
        }

        [Test]
        public void Trace_From_Truth_To_Document_Missing_Truth_Trace_VERIFIES_Correct_Trace_Matrix_Is_Generated_And_Issue()
        {
            ITraceabilityAnalysis traceabilityAnalysis = new TraceabilityAnalysis(mockConfig);
            var tet = traceabilityAnalysis.GetTraceEntityForID("SystemRequirement");
            IDataSources dataSources = GenerateDataSources(new List<RequirementItem>(), new List<RequirementItem>(),
                new List<TestCaseItem>(), new List<AnomalyItem>());

            //add valid trace in PRS / SRS / RAR
            traceabilityAnalysis.AddTrace(traceabilityAnalysis.GetTraceEntityForID("SystemRequirement"), "SYS_id1",
                traceabilityAnalysis.GetTraceEntityForID("SystemRequirementsSpecification"), "SYS_id1");
            //traceabilityAnalysis.AddTrace(traceabilityAnalysis.GetTraceEntityForID("SystemRequirement"), "SYS_id2",
            //    traceabilityAnalysis.GetTraceEntityForID("SystemRequirementsSpecification"), "SYS_id2");
            traceabilityAnalysis.AddTrace(traceabilityAnalysis.GetTraceEntityForID("SystemRequirement"), "SYS_id2",
                traceabilityAnalysis.GetTraceEntityForID("RiskAssessmentRecord"), "SYS_id2");

            var tet1 = traceabilityAnalysis.GetTraceEntityForID("SystemRequirementsSpecification");
            var tet2 = traceabilityAnalysis.GetTraceEntityForID("SoftwareRequirement");
            var tet3 = traceabilityAnalysis.GetTraceEntityForID("RiskAssessmentRecord");

            var matrix = traceabilityAnalysis.PerformAnalysis(dataSources, tet);
            Assert.AreEqual(4, matrix.Count);
            Assert.AreEqual(2, matrix[tet].Count);
            Assert.AreEqual(2, matrix[tet1].Count);
            Assert.AreEqual(2, matrix[tet2].Count);
            Assert.AreEqual(2, matrix[tet3].Count);

            Assert.AreEqual(1, matrix[tet][0].Count);
            Assert.AreEqual(1, matrix[tet][1].Count);
            Assert.AreEqual("SYS_id1", matrix[tet][0][0].ItemID);
            Assert.AreEqual("SYS_id2", matrix[tet][1][0].ItemID);

            Assert.AreEqual(1, matrix[tet1][0].Count);
            Assert.AreEqual(1, matrix[tet1][1].Count);
            Assert.AreEqual("SYS_id1", matrix[tet1][0][0].ItemID);
            Assert.AreEqual(null, matrix[tet1][1][0]);

            Assert.AreEqual(1, matrix[tet2][0].Count);
            Assert.AreEqual(1, matrix[tet2][1].Count);
            Assert.AreEqual("SWR_id1", matrix[tet2][0][0].ItemID);
            Assert.AreEqual("SWR_id2", matrix[tet2][1][0].ItemID);

            Assert.AreEqual(0, matrix[tet3][0].Count);
            Assert.AreEqual(1, matrix[tet3][1].Count);
            Assert.AreEqual("SYS_id2", matrix[tet3][1][0].ItemID);

            Assert.AreEqual(0, traceabilityAnalysis.GetTraceIssuesForTruth(tet).Count());
            Assert.AreEqual(0, traceabilityAnalysis.GetTraceIssuesForTruth(tet2).Count());
            Assert.AreEqual(1, traceabilityAnalysis.GetTraceIssuesForDocument(tet1).Count());
            var issues = traceabilityAnalysis.GetTraceIssuesForDocument(tet1).ToList();
            Assert.AreEqual(TraceIssueType.Missing, issues[0].IssueType);
            Assert.AreEqual(tet, issues[0].Source);
            Assert.AreEqual("SYS_id2", issues[0].SourceID);
            Assert.AreEqual(tet1, issues[0].Target);
            Assert.AreEqual("SYS_id2", issues[0].TargetID);
            Assert.AreEqual(false, issues[0].Valid);
            Assert.AreEqual(0, traceabilityAnalysis.GetTraceIssuesForDocument(tet3).Count());
        }

        [Test]
        public void Trace_From_Truth_To_Document_Missing_ALL_Document_Trace_VERIFIES_Correct_Trace_Matrix_Is_Generated_And_Issue()
        {
            ITraceabilityAnalysis traceabilityAnalysis = new TraceabilityAnalysis(mockConfig);
            var tet = traceabilityAnalysis.GetTraceEntityForID("SystemRequirement");
            IDataSources dataSources = GenerateDataSources(new List<RequirementItem>(), new List<RequirementItem>(),
                new List<TestCaseItem>(), new List<AnomalyItem>());

            //add valid trace in PRS / SRS / RAR
            //traceabilityAnalysis.AddTrace(traceabilityAnalysis.GetTraceEntityForID("SystemRequirement"), "SYS_id1",
            //    traceabilityAnalysis.GetTraceEntityForID("SystemRequirementsSpecification"), "SYS_id1");
            //traceabilityAnalysis.AddTrace(traceabilityAnalysis.GetTraceEntityForID("SystemRequirement"), "SYS_id2",
            //    traceabilityAnalysis.GetTraceEntityForID("SystemRequirementsSpecification"), "SYS_id2");
            traceabilityAnalysis.AddTrace(traceabilityAnalysis.GetTraceEntityForID("SystemRequirement"), "SYS_id2",
                traceabilityAnalysis.GetTraceEntityForID("RiskAssessmentRecord"), "SYS_id2");

            var tet1 = traceabilityAnalysis.GetTraceEntityForID("SystemRequirementsSpecification");
            var tet2 = traceabilityAnalysis.GetTraceEntityForID("SoftwareRequirement");
            var tet3 = traceabilityAnalysis.GetTraceEntityForID("RiskAssessmentRecord");

            var matrix = traceabilityAnalysis.PerformAnalysis(dataSources, tet);
            Assert.AreEqual(4, matrix.Count);
            Assert.AreEqual(2, matrix[tet].Count);
            Assert.AreEqual(2, matrix[tet1].Count);
            Assert.AreEqual(2, matrix[tet2].Count);
            Assert.AreEqual(2, matrix[tet3].Count);

            Assert.AreEqual(1, matrix[tet][0].Count);
            Assert.AreEqual(1, matrix[tet][1].Count);
            Assert.AreEqual("SYS_id1", matrix[tet][0][0].ItemID);
            Assert.AreEqual("SYS_id2", matrix[tet][1][0].ItemID);

            Assert.AreEqual(1, matrix[tet1][0].Count);
            Assert.AreEqual(1, matrix[tet1][1].Count);
            Assert.AreEqual(null, matrix[tet1][0][0]);
            Assert.AreEqual(null, matrix[tet1][1][0]);

            Assert.AreEqual(1, matrix[tet2][0].Count);
            Assert.AreEqual(1, matrix[tet2][1].Count);
            Assert.AreEqual("SWR_id1", matrix[tet2][0][0].ItemID);
            Assert.AreEqual("SWR_id2", matrix[tet2][1][0].ItemID);

            Assert.AreEqual(0, matrix[tet3][0].Count);
            Assert.AreEqual(1, matrix[tet3][1].Count);
            Assert.AreEqual("SYS_id2", matrix[tet3][1][0].ItemID);

            Assert.AreEqual(0, traceabilityAnalysis.GetTraceIssuesForTruth(tet).Count());
            Assert.AreEqual(0, traceabilityAnalysis.GetTraceIssuesForTruth(tet2).Count());
            Assert.AreEqual(2, traceabilityAnalysis.GetTraceIssuesForDocument(tet1).Count());
            var issues = traceabilityAnalysis.GetTraceIssuesForDocument(tet1).ToList();
            Assert.AreEqual(TraceIssueType.Missing, issues[0].IssueType);
            Assert.AreEqual(tet, issues[0].Source);
            Assert.AreEqual("SYS_id1", issues[0].SourceID);
            Assert.AreEqual(tet1, issues[0].Target);
            Assert.AreEqual("SYS_id1", issues[0].TargetID);
            Assert.AreEqual(false, issues[0].Valid);
            Assert.AreEqual(TraceIssueType.Missing, issues[1].IssueType);
            Assert.AreEqual(tet, issues[1].Source);
            Assert.AreEqual("SYS_id2", issues[1].SourceID);
            Assert.AreEqual(tet1, issues[1].Target);
            Assert.AreEqual("SYS_id2", issues[1].TargetID);
            Assert.AreEqual(false, issues[1].Valid);
            Assert.AreEqual(0, traceabilityAnalysis.GetTraceIssuesForDocument(tet3).Count());
        }

        [Test]
        public void Trace_From_Truth_To_Document_Extra_Truth_Trace_VERIFIES_Correct_Trace_Matrix_Is_Generated_And_Issue()
        {
            ITraceabilityAnalysis traceabilityAnalysis = new TraceabilityAnalysis(mockConfig);
            var tet = traceabilityAnalysis.GetTraceEntityForID("SystemRequirement");
            IDataSources dataSources = GenerateDataSources(new List<RequirementItem>(), new List<RequirementItem>(),
                new List<TestCaseItem>(), new List<AnomalyItem>());

            //add valid trace in PRS / SRS / RAR
            traceabilityAnalysis.AddTrace(traceabilityAnalysis.GetTraceEntityForID("SystemRequirement"), "SYS_id1",
                traceabilityAnalysis.GetTraceEntityForID("SystemRequirementsSpecification"), "SYS_id1");
            traceabilityAnalysis.AddTrace(traceabilityAnalysis.GetTraceEntityForID("SystemRequirement"), "SYS_id2",
                traceabilityAnalysis.GetTraceEntityForID("SystemRequirementsSpecification"), "SYS_id2");
            traceabilityAnalysis.AddTrace(traceabilityAnalysis.GetTraceEntityForID("SystemRequirement"), "SYS_id3",
                traceabilityAnalysis.GetTraceEntityForID("SystemRequirementsSpecification"), "SYS_id3");
            traceabilityAnalysis.AddTrace(traceabilityAnalysis.GetTraceEntityForID("SystemRequirement"), "SYS_id2",
                traceabilityAnalysis.GetTraceEntityForID("RiskAssessmentRecord"), "SYS_id2");

            var tet1 = traceabilityAnalysis.GetTraceEntityForID("SystemRequirementsSpecification");
            var tet2 = traceabilityAnalysis.GetTraceEntityForID("SoftwareRequirement");
            var tet3 = traceabilityAnalysis.GetTraceEntityForID("RiskAssessmentRecord");

            var matrix = traceabilityAnalysis.PerformAnalysis(dataSources, tet);
            Assert.AreEqual(4, matrix.Count);
            Assert.AreEqual(2, matrix[tet].Count);
            Assert.AreEqual(2, matrix[tet1].Count);
            Assert.AreEqual(2, matrix[tet2].Count);
            Assert.AreEqual(2, matrix[tet3].Count);

            Assert.AreEqual(1, matrix[tet][0].Count);
            Assert.AreEqual(1, matrix[tet][1].Count);
            Assert.AreEqual("SYS_id1", matrix[tet][0][0].ItemID);
            Assert.AreEqual("SYS_id2", matrix[tet][1][0].ItemID);

            Assert.AreEqual(1, matrix[tet1][0].Count);
            Assert.AreEqual(1, matrix[tet1][1].Count);
            Assert.AreEqual("SYS_id1", matrix[tet1][0][0].ItemID);
            Assert.AreEqual("SYS_id2", matrix[tet1][1][0].ItemID);

            Assert.AreEqual(1, matrix[tet2][0].Count);
            Assert.AreEqual(1, matrix[tet2][1].Count);
            Assert.AreEqual("SWR_id1", matrix[tet2][0][0].ItemID);
            Assert.AreEqual("SWR_id2", matrix[tet2][1][0].ItemID);

            Assert.AreEqual(0, matrix[tet3][0].Count);
            Assert.AreEqual(1, matrix[tet3][1].Count);
            Assert.AreEqual("SYS_id2", matrix[tet3][1][0].ItemID);

            Assert.AreEqual(0, traceabilityAnalysis.GetTraceIssuesForTruth(tet).Count());
            Assert.AreEqual(0, traceabilityAnalysis.GetTraceIssuesForTruth(tet2).Count());
            Assert.AreEqual(1, traceabilityAnalysis.GetTraceIssuesForDocument(tet1).Count());
            var issues = traceabilityAnalysis.GetTraceIssuesForDocument(tet1).ToList();
            Assert.AreEqual(TraceIssueType.Extra, issues[0].IssueType);
            Assert.AreEqual(tet1, issues[0].Source);
            Assert.AreEqual("SYS_id3", issues[0].SourceID);
            Assert.AreEqual(tet, issues[0].Target);
            Assert.AreEqual("SYS_id3", issues[0].TargetID);
            Assert.AreEqual(false, issues[0].Valid);
            Assert.AreEqual(0, traceabilityAnalysis.GetTraceIssuesForDocument(tet3).Count());
        }

        [Test]
        public void Duplicate_Truth_Entity_Name_VERIFIES_Duplicate_Truth_Entity_Names_Are_Detected()
        {
            List<TraceEntity> truth = new List<TraceEntity>();
            truth.Add(new TraceEntity("SystemRequirement", "SYS_name", "SYS", TraceEntityType.Truth));
            truth.Add(new TraceEntity("SoftwareRequirement", "SWR_name", "SWR", TraceEntityType.Truth));
            truth.Add(new TraceEntity("SoftwareSystemTest", "TC_name", "TC", TraceEntityType.Truth));
            truth.Add(new TraceEntity("SoftwareUnitTest", "UT_name", "UT", TraceEntityType.Truth));
            truth.Add(new TraceEntity("Risk", "SWR_name", "RSK", TraceEntityType.Truth));
            truth.Add(new TraceEntity("Anomaly", "ANOMALY_name", "ANOMALY", TraceEntityType.Truth));
            mockConfig.TruthEntities.Returns(truth);

            Assert.Throws<Exception>(() => { new TraceabilityAnalysis(mockConfig); });
        }

        [Test]
        public void Duplicate_Truth_Entity_Abbreviation_VERIFIES_Duplicate_Truth_Entity_Abbreviations_Are_Detected()
        {
            List<TraceEntity> truth = new List<TraceEntity>();
            truth.Add(new TraceEntity("SystemRequirement", "SYS_name", "SYS", TraceEntityType.Truth));
            truth.Add(new TraceEntity("SoftwareRequirement", "SWR_name", "SWR", TraceEntityType.Truth));
            truth.Add(new TraceEntity("SoftwareSystemTest", "TC_name", "TC", TraceEntityType.Truth));
            truth.Add(new TraceEntity("SoftwareUnitTest", "UT_name", "UT", TraceEntityType.Truth));
            truth.Add(new TraceEntity("Risk", "RSK_name", "RSK", TraceEntityType.Truth));
            truth.Add(new TraceEntity("Anomaly", "ANOMALY_name", "SYS", TraceEntityType.Truth));
            mockConfig.TruthEntities.Returns(truth);

            Assert.Throws<Exception>(() => { new TraceabilityAnalysis(mockConfig); });
        }

        [Test]
        public void Duplicate_Document_Name_VERIFIES_Duplicate_Document_Names_Are_Detected()
        {
            List<DocumentConfig> config = new List<DocumentConfig>();
            config.Add(new DocumentConfig("SystemRequirementsSpecification", "DOC001", "PRS Title", "PRS", "fake0"));
            config.Add(new DocumentConfig("SoftwareRequirementsSpecification", "DOC002", "SRS Title", "SRS", "fake1"));
            config.Add(new DocumentConfig("SystemLevelTestPlan", "DOC003", "SRS Title", "SLTP", "fake2"));
            config.Add(new DocumentConfig("RiskAssessmentRecord", "DOC004", "RAR Title", "RAR", "fake3"));
            mockConfig.Documents.Returns(config);

            Assert.Throws<Exception>(() => { new TraceabilityAnalysis(mockConfig); });
        }

        [Test]
        public void Duplicate_Document_Abbreviation_VERIFIES_Duplicate_Document_Abbreviations_Are_Detected()
        {
            List<DocumentConfig> config = new List<DocumentConfig>();
            config.Add(new DocumentConfig("SystemRequirementsSpecification", "DOC001", "PRS Title", "PRS", "fake0"));
            config.Add(new DocumentConfig("SoftwareRequirementsSpecification", "DOC002", "SRS Title", "RAR", "fake1"));
            config.Add(new DocumentConfig("SystemLevelTestPlan", "DOC003", "SLTP Title", "SLTP", "fake2"));
            config.Add(new DocumentConfig("RiskAssessmentRecord", "DOC004", "RAR Title", "RAR", "fake3"));
            mockConfig.Documents.Returns(config);

            Assert.Throws<Exception>(() => { new TraceabilityAnalysis(mockConfig); });
        }

        [Test]
        public void Unknown_Target_Document_Name_Supplied_In_TraceConfig_VERIFIES_Unknown_Document_Names_Are_Detected()
        {
            List<TraceConfig> config2 = new List<TraceConfig>();
            config2.Add(new TraceConfig("SystemRequirement"));
            TomlTable toml = new TomlTable();
            TomlTable temp = new TomlTable();
            temp["forward"] = new TomlArray() { "ALL" };
            temp["backward"] = new TomlArray() { "ALL" };
            temp["forwardLink"] = "DOC";
            temp["backwardLink"] = "DOC";
            toml["SystemSpecification"] = temp; //this documents name is not in the known document list

            config2[0].AddTraces(toml);
            mockConfig.TraceConfig.Returns(config2);

            Assert.Throws<Exception>(() => { new TraceabilityAnalysis(mockConfig); });
        }

        [Test]
        public void Unknown_Source_Truth_Name_Supplied_In_TraceConfig_VERIFIES_Unknown_Source_Truth_Names_Are_Detected()
        {
            List<TraceConfig> config2 = new List<TraceConfig>();
            config2.Add(new TraceConfig("UnknownRequirement"));  //this truth source does not exist
            TomlTable toml = new TomlTable();
            TomlTable temp = new TomlTable();
            temp["forward"] = new TomlArray() { "ALL" };
            temp["backward"] = new TomlArray() { "ALL" };
            temp["forwardLink"] = "DOC";
            temp["backwardLink"] = "DOC";
            toml["SystemRequirementsSpecification"] = temp; //this documents name is not in the known document list

            config2[0].AddTraces(toml);
            mockConfig.TraceConfig.Returns(config2);

            Assert.Throws<Exception>(() => { new TraceabilityAnalysis(mockConfig); });
        }

        [Test]
        public void One_Missing_Truth_Entity_In_Config_File_VERIFIES_Config_File_Truth_Entity_Presence_Is_Checked()
        {
            List<TraceEntity> truth = new List<TraceEntity>();
            truth.Add(new TraceEntity("SystemRequirement", "SYS_name", "SYS", TraceEntityType.Truth));
            truth.Add(new TraceEntity("SoftwareRequirement", "SWR_name", "SWR", TraceEntityType.Truth));
            truth.Add(new TraceEntity("SoftwareSystemTest", "TC_name", "TC", TraceEntityType.Truth));
            truth.Add(new TraceEntity("SoftwareUnitTest", "UT_name", "UT", TraceEntityType.Truth));
            truth.Add(new TraceEntity("Anomaly", "ANOMALY_name", "ANOMALY", TraceEntityType.Truth));
            mockConfig.TruthEntities.Returns(truth);

            Assert.Throws<Exception>(() => { new TraceabilityAnalysis(mockConfig); });
        }

        [Test]
        public void Trace_Incorrect_VERIFIES_Trace_Identified_As_Incorrect()
        {
            List<TraceConfig> config2 = new List<TraceConfig>();
            config2.Add(new TraceConfig("SystemRequirement"));
            TomlTable toml = new TomlTable();
            TomlTable temp = new TomlTable();
            temp["forward"] = new TomlArray() { "CategoryName" };
            temp["backward"] = new TomlArray() { "ALL" };
            temp["forwardLink"] = "DOC";
            temp["backwardLink"] = "DOC";
            toml["SystemRequirementsSpecification"] = temp;
            config2[0].AddTraces(toml);
            mockConfig.TraceConfig.Returns(config2);

            ITraceabilityAnalysis traceabilityAnalysis = new TraceabilityAnalysis(mockConfig);
            var tet = traceabilityAnalysis.GetTraceEntityForID("SystemRequirement");
            IDataSources dataSources = GenerateDataSources(new List<RequirementItem>(), new List<RequirementItem>(),
                new List<TestCaseItem>(), new List<AnomalyItem>());

            //add valid trace in PRS / SRS / RAR
            traceabilityAnalysis.AddTrace(traceabilityAnalysis.GetTraceEntityForID("SystemRequirement"), "SYS_id",
                traceabilityAnalysis.GetTraceEntityForID("SystemRequirementsSpecification"), "SYS_id1");
            traceabilityAnalysis.AddTrace(traceabilityAnalysis.GetTraceEntityForID("SystemRequirement"), "SYS_id2",
                traceabilityAnalysis.GetTraceEntityForID("SystemRequirementsSpecification"), "SYS_id2");
            traceabilityAnalysis.AddTrace(traceabilityAnalysis.GetTraceEntityForID("SystemRequirement"), "SYS_id2",
                traceabilityAnalysis.GetTraceEntityForID("RiskAssessmentRecord"), "SYS_id2");

            var tet1 = traceabilityAnalysis.GetTraceEntityForID("SystemRequirementsSpecification");

            var matrix = traceabilityAnalysis.PerformAnalysis(dataSources, tet);
            Assert.AreEqual(2, matrix.Count);
            Assert.AreEqual(2, matrix[tet].Count);
            Assert.AreEqual(2, matrix[tet1].Count);

            Assert.AreEqual(1, matrix[tet][0].Count);
            Assert.AreEqual(1, matrix[tet][1].Count);
            Assert.AreEqual("SYS_id1", matrix[tet][0][0].ItemID);
            Assert.AreEqual("SYS_id2", matrix[tet][1][0].ItemID);

            Assert.AreEqual(0, matrix[tet1][0].Count);
            Assert.AreEqual(1, matrix[tet1][1].Count);
            Assert.AreEqual("SYS_id2", matrix[tet1][1][0].ItemID);

            Assert.AreEqual(0, traceabilityAnalysis.GetTraceIssuesForTruth(tet).Count());
            Assert.AreEqual(1, traceabilityAnalysis.GetTraceIssuesForDocument(tet1).Count());
            Assert.AreEqual(TraceIssueType.Incorrect, traceabilityAnalysis.GetTraceIssuesForDocument(tet1).First().IssueType);
        }

        [Test]
        public void Truth_Entity_Name_Not_Known_VERIFIES_Unknown_Truth_Entity_Detected()
        {
            var analysis = new TraceabilityAnalysis(mockConfig);
            IDataSources dataSources = GenerateDataSources(new List<RequirementItem>(), new List<RequirementItem>(),
                new List<TestCaseItem>(), new List<AnomalyItem>());
            var tet = new TraceEntity("test", "invalid", "abbreviation", TraceEntityType.Truth);

            Assert.Throws<Exception>(() => { analysis.PerformAnalysis(dataSources, tet); });
        }

        [Test]
        public void Truth_Entity_Type_Unknown_VERIFIES_Unknown_Truth_Entity_Detected()
        {
            var analysis = new TraceabilityAnalysis(mockConfig);
            IDataSources dataSources = GenerateDataSources(new List<RequirementItem>(), new List<RequirementItem>(),
                new List<TestCaseItem>(), new List<AnomalyItem>());
            var tet = new TraceEntity("SystemRequirement", "System Requirement", "SYS", TraceEntityType.Unknown);

            Assert.Throws<Exception>(() => { analysis.PerformAnalysis(dataSources, tet); });
        }

        [Test]
        public void Incorrect_Link_Type_Used_VERIFIES_Only_Correct_Link_Types_Are_Traced()
        {
            IDataSources dataSources = GenerateDataSources(new List<RequirementItem>(), new List<RequirementItem>(),
                                                new List<TestCaseItem>(), new List<AnomalyItem>());
            SWRs = new List<RequirementItem> { new RequirementItem(RequirementType.SoftwareRequirement), new RequirementItem(RequirementType.SoftwareRequirement) };
            SWRs[0].RequirementTitle = "SWR_TestTitle1";
            SWRs[0].ItemID = "SWR_id1";
            SWRs[0].AddLinkedItem(new ItemLink("SYS_id1", ItemLinkType.Related));
            SWRs[1].RequirementTitle = "SWR_TestTitle2";
            SWRs[1].ItemID = "SWR_id2";
            SWRs[1].ItemCategory = "CategoryName";
            SWRs[1].AddLinkedItem(new ItemLink("SYS_id2", ItemLinkType.Related));
            mockPlugin.GetSoftwareRequirements().Returns(SWRs);

            var analysis = new TraceabilityAnalysis(mockConfig);

            var tet = analysis.GetTraceEntityForID("SystemRequirement");

            var tet2 = analysis.GetTraceEntityForID("SoftwareRequirement");

            var matrix = analysis.PerformAnalysis(dataSources, tet);
            Assert.AreEqual(4, matrix.Count);
            Assert.AreEqual(2, matrix[tet].Count);
            Assert.AreEqual(2, matrix[tet2].Count);

            Assert.AreEqual(1, matrix[tet][0].Count);
            Assert.AreEqual(1, matrix[tet][1].Count);
            Assert.AreEqual("SYS_id1", matrix[tet][0][0].ItemID);
            Assert.AreEqual("SYS_id2", matrix[tet][1][0].ItemID);

            Assert.AreEqual(1, matrix[tet2][0].Count);
            Assert.AreEqual(1, matrix[tet2][1].Count);
            Assert.AreEqual(null, matrix[tet2][0][0]);
            Assert.AreEqual(null, matrix[tet2][1][0]);

            Assert.AreEqual(2, analysis.GetTraceIssuesForTruth(tet).Count());
            Assert.AreEqual(0, analysis.GetTraceIssuesForTruth(tet2).Count());
            
            var issues = analysis.GetTraceIssuesForTruth(tet).ToList();

            Assert.AreEqual(TraceIssueType.PossiblyMissing, issues[0].IssueType);
            Assert.AreEqual("SYS_id1", issues[0].SourceID);
            Assert.AreEqual(tet2, issues[0].Target);
            Assert.AreEqual("SYS_id1", issues[0].TargetID);
            Assert.AreEqual(false, issues[0].Valid);
        }
    }
}
