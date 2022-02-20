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
            truth.Add(new TraceEntity("SystemRequirement", "SYS_name", "SYS"));
            truth.Add(new TraceEntity("SoftwareRequirement", "SWR_name", "SWR"));
            truth.Add(new TraceEntity("SoftwareSystemTest", "TC_name", "TC"));
            truth.Add(new TraceEntity("SoftwareUnitTest", "UT_name", "UT"));
            truth.Add(new TraceEntity("Risk","RSK_name","RSK"));
            truth.Add(new TraceEntity("Anomaly", "ANOMALY_name", "ANOMALY"));
            mockConfig.TruthEntities.Returns(truth);

            List<DocumentConfig> config = new List<DocumentConfig>();
            config.Add(new DocumentConfig("SystemRequirementsSpecification", "PRS Title", "PRS", "fake0"));
            config.Add(new DocumentConfig("SoftwareRequirementsSpecification", "SRS Title", "SRS", "fake1"));
            config.Add(new DocumentConfig("SystemLevelTestPlan", "SLTP Title", "SLTP", "fake2"));
            config.Add(new DocumentConfig("RiskAssessmentRecord", "RAR Title", "RAR", "fake3"));
            mockConfig.Documents.Returns(config);

            List<TraceConfig> config2 = new List<TraceConfig>();
            config2.Add(new TraceConfig("SystemRequirement"));
            config2.Add(new TraceConfig("SoftwareRequirement"));
            TomlTable toml = new TomlTable();
            toml["SystemRequirementsSpecification"] = new TomlArray() { "ALL" };
            toml["SoftwareRequirement"] = new TomlArray() { "ALL" };
            toml["RiskAssessmentRecord"] = new TomlArray() { "CategoryName" };
            config2[0].AddTraces(toml);
            TomlTable toml2 = new TomlTable();
            toml2["SoftwareRequirementsSpecification"] = new TomlArray() { "ALL" };
            toml2["SystemRequirement"] = new TomlArray() { "ALL" };
            toml2["RiskAssessmentRecord"] = new TomlArray() { "CategoryName" };
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
        private ISLMSPlugin mockPlugin = Substitute.For< ISLMSPlugin>();
        private void SetupPlugin()
        {
            SYSs = new List<RequirementItem> { new RequirementItem(), new RequirementItem() };
            SYSs[0].RequirementTitle = "SYS_TestTitle1";
            SYSs[0].RequirementID = "SYS_id1";
            SYSs[0].AddChild("SWR_id1", default(Uri));
            SYSs[1].RequirementTitle = "SYS_TestTitle2";
            SYSs[1].RequirementID = "SYS_id2";
            SYSs[1].RequirementCategory = "CategoryName";
            SYSs[1].AddChild("SWR_id2",default(Uri));
            mockPlugin.GetSystemRequirements().Returns(SYSs);
            SWRs = new List<RequirementItem> { new RequirementItem(), new RequirementItem() };
            SWRs[0].RequirementTitle = "SWR_TestTitle1";
            SWRs[0].RequirementID = "SWR_id1";
            SWRs[0].AddParent(SYSs[0].RequirementID, default(Uri));
            SWRs[1].RequirementTitle = "SWR_TestTitle2";
            SWRs[1].RequirementID = "SWR_id2";
            SWRs[1].RequirementCategory = "CategoryName";
            SWRs[1].AddParent(SYSs[1].RequirementID, default(Uri));
            mockPlugin.GetSoftwareRequirements().Returns(SWRs);
            TCs = new List<TestCaseItem> { new TestCaseItem(), new TestCaseItem() };
            TCs[0].TestCaseTitle = "TC_TestTitle1";
            TCs[0].TestCaseID = "TC_id1";
            TCs[0].AddParent("SWR_id1",default (Uri));
            TCs[1].TestCaseTitle = "TC_TestTitle2";
            TCs[1].TestCaseID = "TC_id2";
            TCs[1].AddParent("SWR_id2", default(Uri));
            mockPlugin.GetSoftwareSystemTests().Returns(TCs);
            ANOMALYs = new List<AnomalyItem> { new AnomalyItem(), new AnomalyItem() };
            ANOMALYs[0].AnomalyTitle = "ANOMALY_TestTitle1";
            ANOMALYs[0].AnomalyID = "ANOMALY_id1";
            ANOMALYs[1].AnomalyTitle = "ANOMALY_TestTitle2";
            ANOMALYs[1].AnomalyID = "ANOMALY_id2";
            mockPlugin.GetAnomalies().Returns(ANOMALYs);
        }


        private IDataSources GenerateDataSources(List<RequirementItem> systemReqs, List<RequirementItem> softwareReqs, 
            List<TestCaseItem> tcs, List<AnomalyItem> ans)
        {
            SetupPlugin();
            IPluginLoader mockPluginLoader = Substitute.For<IPluginLoader>();
            mockPluginLoader.LoadPlugin<ISLMSPlugin>(Arg.Any<string>(), Arg.Any<string>()).Returns<ISLMSPlugin>(l => null);
            mockPluginLoader.Configure().LoadPlugin<ISLMSPlugin>(Arg.Is("testPlugin2"), Arg.Is("c:\\temp\\does_not_exist")).Returns(mockPlugin);
            IDataSources dataSources = new DataSources(mockConfig, mockPluginLoader);
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


    }
}
