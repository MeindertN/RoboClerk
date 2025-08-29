using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using NSubstitute;
using RoboClerk.Configuration;
using RoboClerk.ContentCreators;
using RoboClerk.Core;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("These tests test the Eliminated content creator")]
    internal class TestEliminatedContentCreator
    {
        private IConfiguration config = null;
        private IDataSources dataSources = null;
        private ITraceabilityAnalysis traceAnalysis = null;
        private DocumentConfig documentConfig = null;
        private Eliminated eliminatedContentCreator = null;
        private IRoboClerkTag tag = null;
        private TraceEntity eliminatedTE = null;

        [SetUp]
        public void TestSetup()
        {
            config = Substitute.For<IConfiguration>();
            config.OutputFormat.Returns("ASCIIDOC");

            dataSources = Substitute.For<IDataSources>();
            traceAnalysis = Substitute.For<ITraceabilityAnalysis>();
            documentConfig = new DocumentConfig("EliminatedItems", "docID", "Eliminated Items Document", "EID", @"c:\in\template.adoc");

            eliminatedTE = new TraceEntity("Eliminated", "Eliminated Item", "EI", TraceEntityType.Eliminated);
            traceAnalysis.GetTraceEntityForAnyProperty("Eliminated").Returns(eliminatedTE);
            traceAnalysis.GetTraceEntityForID("Eliminated").Returns(eliminatedTE);
            traceAnalysis.GetTraceEntityForTitle("Eliminated Items Document").Returns(
                new TraceEntity("docID", "Eliminated Items Document", "EID", TraceEntityType.Document));

            eliminatedContentCreator = new Eliminated(dataSources, traceAnalysis, config);

            tag = new RoboClerkTextTag(0, 29, "@@SLMS:Eliminated(TYPE=ALL)@@", true);
            dataSources.GetTemplateFile(@"./ItemTemplates/ASCIIDOC/Eliminated.adoc").Returns(
                "[csx:\n" +
                "using RoboClerk;\n" +
                "TraceEntity te = SourceTraceEntity;\n" +
                "string CreateRows()\n" +
                "{\n" +
                "   System.Text.StringBuilder sb = new System.Text.StringBuilder();\n" +
                "   foreach( var item in Items)\n" +
                "   {\n" +
                "       EliminatedLinkedItem eli = (EliminatedLinkedItem)item;\n" +
                "       sb.Append($\"| {GetItemLinkString(item)} | {eli.ItemType} | {eli.ItemTitle} | {eli.EliminationReason} \\n\\n\");\n" +
                "   }\n" +
                "   return sb.ToString();\n" +
                "}]|====\n" +
                "| Item ID | Item Type | Item Title | Elimination Reason\n" +
                "\n" +
                "[csx:CreateRows()]|===="
            );
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "A01BBB01-1234-5678-9012-ABCDEFABCDEF",
            Purpose = "Test that Eliminated content creator is created",
            PostCondition = "No exception is thrown")]
        public void TestEliminatedContentCreator_Constructor()
        {
            // Act & Assert - just verifying constructor doesn't throw
            var contentCreator = new Eliminated(dataSources, traceAnalysis, config);
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "A02BBB02-2345-6789-0123-BCDEFABCDEFG",
            Purpose = "Test that GetContent returns appropriate message when no eliminated items found",
            PostCondition = "A message indicating no eliminated items is returned")]
        public void TestEliminatedContentCreator_NoItems()
        {
            // Arrange
            SetupNoEliminatedItemsMock();

            // Act
            string content = eliminatedContentCreator.GetContent(tag, documentConfig);

            // Assert
            ClassicAssert.IsTrue(content.Contains("Unable to find specified Eliminated Item"));
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "A03BBB03-3456-7890-1234-CDEFABCDEFGH",
            Purpose = "Test that GetContent handles ALL type parameter",
            PostCondition = "Content is generated based on all eliminated item types")]
        public void TestEliminatedContentCreator_AllTypes()
        {
            // Arrange
            SetupAllEliminatedItemsMock();

            // Act
            string content = eliminatedContentCreator.GetContent(tag, documentConfig);

            // Assert
            ClassicAssert.IsTrue(content.Contains("| RISK-001 | Risk | Eliminated Risk | Risk elimination reason"));
            ClassicAssert.IsTrue(content.Contains("| SWR-001 | SoftwareRequirement | Eliminated Software Requirement | Software requirement elimination reason"));
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "A04BBB04-4567-8901-2345-DEFABCDEFGHI",
            Purpose = "Test that GetContent handles SYSTEM type parameter",
            PostCondition = "Content is generated based only on eliminated system requirements")]
        public void TestEliminatedContentCreator_SystemType()
        {
            // Arrange
            SetupSystemRequirementEliminatedItemsMock();
            tag = new RoboClerkTextTag(0, 32, "@@SLMS:Eliminated(TYPE=SYSTEM)@@", true);

            // Act
            string content = eliminatedContentCreator.GetContent(tag, documentConfig);

            // Assert
            ClassicAssert.IsTrue(content.Contains("| SYS-001 | SystemRequirement | Eliminated System Requirement | System requirement elimination reason"));
            ClassicAssert.IsFalse(content.Contains("SWR-001"));
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "A05BBB05-5678-9012-3456-EFABCDEFGHIJ",
            Purpose = "Test that GetContent handles SOFTWARE type parameter",
            PostCondition = "Content is generated based only on eliminated software requirements")]
        public void TestEliminatedContentCreator_SoftwareType()
        {
            // Arrange
            SetupSoftwareRequirementEliminatedItemsMock();
            tag = new RoboClerkTextTag(0, 34, "@@SLMS:Eliminated(TYPE=SOFTWARE)@@", true);

            // Act
            string content = eliminatedContentCreator.GetContent(tag, documentConfig);

            // Assert
            ClassicAssert.IsTrue(content.Contains("| SWR-001 | SoftwareRequirement | Eliminated Software Requirement | Software requirement elimination reason"));
            ClassicAssert.IsFalse(content.Contains("SYS-001"));
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "A06BBB06-6789-0123-4567-FABCDEFGHIJK",
            Purpose = "Test that GetContent handles DOCUMENTATION type parameter",
            PostCondition = "Content is generated based only on eliminated documentation requirements")]
        public void TestEliminatedContentCreator_DocumentationType()
        {
            // Arrange
            SetupDocumentationRequirementEliminatedItemsMock();
            tag = new RoboClerkTextTag(0, 39, "@@SLMS:Eliminated(TYPE=DOCUMENTATION)@@", true);

            // Act
            string content = eliminatedContentCreator.GetContent(tag, documentConfig);

            // Assert
            ClassicAssert.IsTrue(content.Contains("| DOC-001 | DocumentationRequirement | Eliminated Documentation Requirement | Documentation requirement elimination reason"));
            ClassicAssert.IsFalse(content.Contains("SWR-001"));
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "A07BBB07-7890-1234-5678-ABCDEFGHIJKL",
            Purpose = "Test that GetContent handles TESTCASE type parameter",
            PostCondition = "Content is generated based only on eliminated software system tests")]
        public void TestEliminatedContentCreator_TestcaseType()
        {
            // Arrange
            SetupSoftwareSystemTestEliminatedItemsMock();
            tag = new RoboClerkTextTag(0, 34, "@@SLMS:Eliminated(TYPE=TESTCASE)@@", true);

            // Act
            string content = eliminatedContentCreator.GetContent(tag, documentConfig);

            // Assert
            ClassicAssert.IsTrue(content.Contains("| TEST-001 | SoftwareSystemTest | Eliminated Test Case | Test case elimination reason"));
            ClassicAssert.IsFalse(content.Contains("SWR-001"));
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "A08BBB08-8901-2345-6789-BCDEFGHIJKLM",
            Purpose = "Test that GetContent handles RISK type parameter",
            PostCondition = "Content is generated based only on eliminated risks")]
        public void TestEliminatedContentCreator_RiskType()
        {
            // Arrange
            SetupRiskEliminatedItemsMock();
            tag = new RoboClerkTextTag(0, 30, "@@SLMS:Eliminated(TYPE=RISK)@@", true);

            // Act
            string content = eliminatedContentCreator.GetContent(tag, documentConfig);

            // Assert
            ClassicAssert.IsTrue(content.Contains("| RISK-001 | Risk | Eliminated Risk | Risk elimination reason"));
            ClassicAssert.IsFalse(content.Contains("SWR-001"));
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "A09BBB09-9012-3456-7890-CDEFGHIJKLMN",
            Purpose = "Test that GetContent handles DOCCONTENT type parameter",
            PostCondition = "Content is generated based only on eliminated doc contents")]
        public void TestEliminatedContentCreator_DocContentType()
        {
            // Arrange
            SetupDocContentEliminatedItemsMock();
            tag = new RoboClerkTextTag(0, 36, "@@SLMS:Eliminated(TYPE=DOCCONTENT)@@", true);

            // Act
            string content = eliminatedContentCreator.GetContent(tag, documentConfig);

            // Assert
            ClassicAssert.IsTrue(content.Contains("| DCNT-001 | DocContent | Eliminated Doc Content | Doc content elimination reason"));
            ClassicAssert.IsFalse(content.Contains("SWR-001"));
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "A10BBB10-0123-4567-8901-DEFGHIJKLMNO",
            Purpose = "Test that GetContent handles ANOMALY type parameter",
            PostCondition = "Content is generated based only on eliminated anomalies")]
        public void TestEliminatedContentCreator_AnomalyType()
        {
            // Arrange
            SetupAnomalyEliminatedItemsMock();
            tag = new RoboClerkTextTag(0, 33, "@@SLMS:Eliminated(TYPE=ANOMALY)@@", true);

            // Act
            string content = eliminatedContentCreator.GetContent(tag, documentConfig);

            // Assert
            ClassicAssert.IsTrue(content.Contains("| ANOM-001 | Anomaly | Eliminated Anomaly | Anomaly elimination reason"));
            ClassicAssert.IsFalse(content.Contains("SWR-001"));
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "A11BBB11-1234-5678-9012-EFGHIJKLMNOP",
            Purpose = "Test that GetContent handles SOUP type parameter",
            PostCondition = "Content is generated based only on eliminated SOUPs")]
        public void TestEliminatedContentCreator_SoupType()
        {
            // Arrange
            SetupSoupEliminatedItemsMock();
            tag = new RoboClerkTextTag(0, 30, "@@SLMS:Eliminated(TYPE=SOUP)@@", true);

            // Act
            string content = eliminatedContentCreator.GetContent(tag, documentConfig);

            // Assert
            ClassicAssert.IsTrue(content.Contains("| SOUP-001 | SOUP | Eliminated SOUP | SOUP elimination reason"));
            ClassicAssert.IsFalse(content.Contains("SWR-001"));
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "A12BBB12-2345-6789-0123-FGHIJKLMNOPQ",
            Purpose = "Test that GetContent handles unknown type parameter",
            PostCondition = "Content is generated based on all eliminated items when type is unknown")]
        public void TestEliminatedContentCreator_UnknownType()
        {
            // Arrange
            SetupAllEliminatedItemsMock();
            tag = new RoboClerkTextTag(0, 32, "@@SLMS:Eliminated(TYPE=UNKNOWN)@@", true);

            // Act
            string content = eliminatedContentCreator.GetContent(tag, documentConfig);

            // Assert
            ClassicAssert.IsTrue(content.Contains("| RISK-001 | Risk | Eliminated Risk | Risk elimination reason"));
            ClassicAssert.IsTrue(content.Contains("| SWR-001 | SoftwareRequirement | Eliminated Software Requirement | Software requirement elimination reason"));
        }

        // Helper methods to set up different mocks

        private void SetupNoEliminatedItemsMock()
        {
            var emptyList = new List<LinkedItem>();

            dataSources.GetAllEliminatedRisks().Returns(new List<EliminatedRiskItem>()); 
            dataSources.GetAllEliminatedSystemRequirements().Returns(new List<EliminatedRequirementItem>());
            dataSources.GetAllEliminatedSoftwareRequirements().Returns(new List<EliminatedRequirementItem>());
            dataSources.GetAllEliminatedDocumentationRequirements().Returns(new List<EliminatedRequirementItem>());
            dataSources.GetAllEliminatedSoftwareSystemTests().Returns(new List<EliminatedSoftwareSystemTestItem>());
            dataSources.GetAllEliminatedDocContents().Returns(new List<EliminatedDocContentItem>());
            dataSources.GetAllEliminatedAnomalies().Returns(new List<EliminatedAnomalyItem>());
            dataSources.GetAllEliminatedSOUP().Returns(new List<EliminatedSOUPItem>());

            // Mock the critical GetItems method for empty lists
            dataSources.GetItems(Arg.Any<TraceEntity>()).Returns(emptyList);
        }

        private void SetupSystemRequirementEliminatedItemsMock()
        {
            SetupNoEliminatedItemsMock();

            var sysReq = new RequirementItem(RequirementType.SystemRequirement)
            {
                ItemID = "SYS-001",
                ItemTitle = "Eliminated System Requirement"
            };

            var eliminatedSysReq = new EliminatedRequirementItem(
                sysReq,
                "System requirement elimination reason",
                EliminationReason.FilteredOut
            );

            var sysReqList = new List<EliminatedRequirementItem> { eliminatedSysReq };
            dataSources.GetAllEliminatedSystemRequirements().Returns(sysReqList);

            // Mock GetItems to return the correct list
            dataSources.GetItems(Arg.Is<TraceEntity>(te => te.ID == "Eliminated")).Returns(sysReqList.Cast<LinkedItem>().ToList());
        }

        private void SetupSoftwareRequirementEliminatedItemsMock()
        {
            SetupNoEliminatedItemsMock();

            var swReq = new RequirementItem(RequirementType.SoftwareRequirement)
            {
                ItemID = "SWR-001",
                ItemTitle = "Eliminated Software Requirement"
            };

            var eliminatedSwReq = new EliminatedRequirementItem(
                swReq,
                "Software requirement elimination reason",
                EliminationReason.FilteredOut
            );

            var swReqList = new List<EliminatedRequirementItem> { eliminatedSwReq };
            dataSources.GetAllEliminatedSoftwareRequirements().Returns(swReqList);

            // Mock GetItems to return the correct list
            dataSources.GetItems(Arg.Is<TraceEntity>(te => te.ID == "Eliminated")).Returns(swReqList.Cast<LinkedItem>().ToList());
        }

        private void SetupDocumentationRequirementEliminatedItemsMock()
        {
            SetupNoEliminatedItemsMock();

            var docReq = new RequirementItem(RequirementType.DocumentationRequirement)
            {
                ItemID = "DOC-001",
                ItemTitle = "Eliminated Documentation Requirement"
            };

            var eliminatedDocReq = new EliminatedRequirementItem(
                docReq,
                "Documentation requirement elimination reason",
                EliminationReason.FilteredOut
            );

            var docReqList = new List<EliminatedRequirementItem> { eliminatedDocReq };
            dataSources.GetAllEliminatedDocumentationRequirements().Returns(docReqList);

            // Mock GetItems to return the correct list
            dataSources.GetItems(Arg.Is<TraceEntity>(te => te.ID == "Eliminated")).Returns(docReqList.Cast<LinkedItem>().ToList());
        }

        private void SetupSoftwareSystemTestEliminatedItemsMock()
        {
            SetupNoEliminatedItemsMock();

            var testCase = new SoftwareSystemTestItem
            {
                ItemID = "TEST-001",
                ItemTitle = "Eliminated Test Case"
            };

            var eliminatedTestCase = new EliminatedSoftwareSystemTestItem(
                testCase,
                "Test case elimination reason",
                EliminationReason.FilteredOut
            );

            var testCaseList = new List<EliminatedSoftwareSystemTestItem> { eliminatedTestCase };
            dataSources.GetAllEliminatedSoftwareSystemTests().Returns(testCaseList);

            // Mock GetItems to return the correct list
            dataSources.GetItems(Arg.Is<TraceEntity>(te => te.ID == "Eliminated")).Returns(testCaseList.Cast<LinkedItem>().ToList());
        }

        private void SetupRiskEliminatedItemsMock()
        {
            SetupNoEliminatedItemsMock();

            var risk = new RiskItem
            {
                ItemID = "RISK-001",
                ItemTitle = "Eliminated Risk"
            };

            var eliminatedRisk = new EliminatedRiskItem(
                risk,
                "Risk elimination reason",
                EliminationReason.FilteredOut
            );

            var riskList = new List<EliminatedRiskItem> { eliminatedRisk };
            dataSources.GetAllEliminatedRisks().Returns(riskList);

            // Mock GetItems to return the correct list
            dataSources.GetItems(Arg.Is<TraceEntity>(te => te.ID == "Eliminated")).Returns(riskList.Cast<LinkedItem>().ToList());
        }

        private void SetupDocContentEliminatedItemsMock()
        {
            SetupNoEliminatedItemsMock();

            var docContent = new DocContentItem
            {
                ItemID = "DCNT-001",
                ItemTitle = "Eliminated Doc Content"
            };

            var eliminatedDocContent = new EliminatedDocContentItem(
                docContent,
                "Doc content elimination reason",
                EliminationReason.FilteredOut
            );

            var docContentList = new List<EliminatedDocContentItem> { eliminatedDocContent };
            dataSources.GetAllEliminatedDocContents().Returns(docContentList);

            // Mock GetItems to return the correct list
            dataSources.GetItems(Arg.Is<TraceEntity>(te => te.ID == "Eliminated")).Returns(docContentList.Cast<LinkedItem>().ToList());
        }

        private void SetupAnomalyEliminatedItemsMock()
        {
            SetupNoEliminatedItemsMock();

            var anomaly = new AnomalyItem
            {
                ItemID = "ANOM-001",
                ItemTitle = "Eliminated Anomaly"
            };

            var eliminatedAnomaly = new EliminatedAnomalyItem(
                anomaly,
                "Anomaly elimination reason",
                EliminationReason.FilteredOut
            );

            var anomalyList = new List<EliminatedAnomalyItem> { eliminatedAnomaly };
            dataSources.GetAllEliminatedAnomalies().Returns(anomalyList);

            // Mock GetItems to return the correct list
            dataSources.GetItems(Arg.Is<TraceEntity>(te => te.ID == "Eliminated")).Returns(anomalyList.Cast<LinkedItem>().ToList());
        }

        private void SetupSoupEliminatedItemsMock()
        {
            SetupNoEliminatedItemsMock();

            var soup = new SOUPItem
            {
                ItemID = "SOUP-001",
                ItemTitle = "Eliminated SOUP"
            };

            var eliminatedSoup = new EliminatedSOUPItem(
                soup,
                "SOUP elimination reason",
                EliminationReason.FilteredOut
            );

            var soupList = new List<EliminatedSOUPItem> { eliminatedSoup };
            dataSources.GetAllEliminatedSOUP().Returns(soupList);

            // Mock GetItems to return the correct list
            dataSources.GetItems(Arg.Is<TraceEntity>(te => te.ID == "Eliminated")).Returns(soupList.Cast<LinkedItem>().ToList());
        }

        private void SetupAllEliminatedItemsMock()
        {
            SetupNoEliminatedItemsMock();

            var risk = new RiskItem
            {
                ItemID = "RISK-001",
                ItemTitle = "Eliminated Risk"
            };

            var eliminatedRisk = new EliminatedRiskItem(
                risk,
                "Risk elimination reason",
                EliminationReason.FilteredOut
            );

            var swReq = new RequirementItem(RequirementType.SoftwareRequirement)
            {
                ItemID = "SWR-001",
                ItemTitle = "Eliminated Software Requirement"
            };

            var eliminatedSwReq = new EliminatedRequirementItem(
                swReq,
                "Software requirement elimination reason",
                EliminationReason.FilteredOut
            );

            var riskList = new List<EliminatedRiskItem> { eliminatedRisk };
            var swReqList = new List<EliminatedRequirementItem> { eliminatedSwReq };

            dataSources.GetAllEliminatedRisks().Returns(riskList);
            dataSources.GetAllEliminatedSoftwareRequirements().Returns(swReqList);

            // Mock GetItems to return the combined list
            var combinedList = new List<LinkedItem>();
            combinedList.AddRange(riskList);
            combinedList.AddRange(swReqList);

            dataSources.GetItems(Arg.Is<TraceEntity>(te => te.ID == "Eliminated")).Returns(combinedList);
        }
    }
}