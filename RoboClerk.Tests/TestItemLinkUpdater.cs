using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;

namespace RoboClerk.Tests
{
    [TestFixture]
    public class TestItemLinkUpdater
    {
        private IDataSources dataSources;
        private ItemLinkUpdater itemLinkUpdater;
        private IDataSourcePlugin plugin;

        [SetUp]
        public void Setup()
        {
            dataSources = Substitute.For<IDataSources>();
            itemLinkUpdater = new ItemLinkUpdater(dataSources);
            plugin = Substitute.For<IDataSourcePlugin>();
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "61F5C872-2861-4C0C-B20D-EA5A54D9D140",
            Purpose = "Test that parent-child relationships are bidirectional after update",
            PostCondition = "Parent item has child link and child item has parent link")]
        public void UpdateAllItemLinks_CreatesComplementaryParentChildLinks()
        {
            // Arrange
            var parentReq = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "REQ-001" };
            var childReq = new RequirementItem(RequirementType.SoftwareRequirement) { ItemID = "REQ-002" };
            
            // Parent links to child, but child doesn't link back to parent yet
            parentReq.AddLinkedItem(new ItemLink("REQ-002", ItemLinkType.Child));

            plugin.GetSystemRequirements().Returns(new[] { parentReq });
            plugin.GetSoftwareRequirements().Returns(new[] { childReq });
            plugin.GetDocumentationRequirements().Returns(new RequirementItem[0]);
            plugin.GetDocContents().Returns(new DocContentItem[0]);
            plugin.GetSoftwareSystemTests().Returns(new SoftwareSystemTestItem[0]);
            plugin.GetAnomalies().Returns(new AnomalyItem[0]);
            plugin.GetRisks().Returns(new RiskItem[0]);
            plugin.GetSOUP().Returns(new SOUPItem[0]);
            plugin.GetUnitTests().Returns(new UnitTestItem[0]);
            plugin.GetTestResults().Returns(new TestResult[0]);
            SetupEliminatedItemMethods();

            var plugins = new List<IDataSourcePlugin> { plugin };

            // Act
            itemLinkUpdater.UpdateAllItemLinks(plugins);

            // Assert
            var childLinks = childReq.LinkedItems.ToList();
            Assert.That(childLinks.Count, Is.EqualTo(1), "Child should have exactly one link back to parent");
            Assert.That(childLinks[0].TargetID, Is.EqualTo("REQ-001"), "Child should link back to parent");
            Assert.That(childLinks[0].LinkType, Is.EqualTo(ItemLinkType.Parent), "Child should have parent link type");

            var parentLinks = parentReq.LinkedItems.ToList();
            Assert.That(parentLinks.Count, Is.EqualTo(1), "Parent should still have its child link");
            Assert.That(parentLinks[0].LinkType, Is.EqualTo(ItemLinkType.Child), "Parent should maintain child link type");
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "A6CF67A4-AC78-4341-A2A5-D2C480AF2FDB", 
            Purpose = "Test that TestedBy-Tests relationships are bidirectional after update",
            PostCondition = "Test case has Tests link and requirement has TestedBy link")]
        public void UpdateAllItemLinks_CreatesComplementaryTestLinks()
        {
            // Arrange
            var requirement = new RequirementItem(RequirementType.SoftwareRequirement) { ItemID = "REQ-001" };
            var testCase = new SoftwareSystemTestItem { ItemID = "TEST-001" };
            
            // Test case tests the requirement, but requirement doesn't know it's being tested yet
            testCase.AddLinkedItem(new ItemLink("REQ-001", ItemLinkType.Tests));

            plugin.GetSystemRequirements().Returns(new RequirementItem[0]);
            plugin.GetSoftwareRequirements().Returns(new[] { requirement });
            plugin.GetDocumentationRequirements().Returns(new RequirementItem[0]);
            plugin.GetDocContents().Returns(new DocContentItem[0]);
            plugin.GetSoftwareSystemTests().Returns(new[] { testCase });
            plugin.GetAnomalies().Returns(new AnomalyItem[0]);
            plugin.GetRisks().Returns(new RiskItem[0]);
            plugin.GetSOUP().Returns(new SOUPItem[0]);
            plugin.GetUnitTests().Returns(new UnitTestItem[0]);
            plugin.GetTestResults().Returns(new TestResult[0]);
            SetupEliminatedItemMethods();

            var plugins = new List<IDataSourcePlugin> { plugin };

            // Act
            itemLinkUpdater.UpdateAllItemLinks(plugins);

            // Assert
            var reqLinks = requirement.LinkedItems.ToList();
            Assert.That(reqLinks.Count, Is.EqualTo(1), "Requirement should have exactly one link back to test");
            Assert.That(reqLinks[0].TargetID, Is.EqualTo("TEST-001"), "Requirement should link back to test");
            Assert.That(reqLinks[0].LinkType, Is.EqualTo(ItemLinkType.TestedBy), "Requirement should have TestedBy link type");
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "87905DDC-BA3D-4851-8095-E296EE0B8B74",
            Purpose = "Test that Risk-RiskControl relationships are bidirectional after update", 
            PostCondition = "Risk has RiskControl link and control has Risk link")]
        public void UpdateAllItemLinks_CreatesComplementaryRiskControlLinks()
        {
            // Arrange
            var risk = new RiskItem { ItemID = "RISK-001" };
            var control = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "REQ-001" };
            
            // Risk links to its control, but control doesn't link back yet
            risk.AddLinkedItem(new ItemLink("REQ-001", ItemLinkType.RiskControl));

            plugin.GetSystemRequirements().Returns(new[] { control });
            plugin.GetSoftwareRequirements().Returns(new RequirementItem[0]);
            plugin.GetDocumentationRequirements().Returns(new RequirementItem[0]);
            plugin.GetDocContents().Returns(new DocContentItem[0]);
            plugin.GetSoftwareSystemTests().Returns(new SoftwareSystemTestItem[0]);
            plugin.GetAnomalies().Returns(new AnomalyItem[0]);
            plugin.GetRisks().Returns(new[] { risk });
            plugin.GetSOUP().Returns(new SOUPItem[0]);
            plugin.GetUnitTests().Returns(new UnitTestItem[0]);
            plugin.GetTestResults().Returns(new TestResult[0]);
            SetupEliminatedItemMethods();

            var plugins = new List<IDataSourcePlugin> { plugin };

            // Act
            itemLinkUpdater.UpdateAllItemLinks(plugins);

            // Assert
            var controlLinks = control.LinkedItems.ToList();
            Assert.That(controlLinks.Count, Is.EqualTo(1), "Control should have exactly one link back to risk");
            Assert.That(controlLinks[0].TargetID, Is.EqualTo("RISK-001"), "Control should link back to risk");
            Assert.That(controlLinks[0].LinkType, Is.EqualTo(ItemLinkType.Risk), "Control should have Risk link type");
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "6E755F06-7161-4466-A740-919F362BDE4A",
            Purpose = "Test that Related links remain bidirectional (Related -> Related)",
            PostCondition = "Both items have Related links to each other")]
        public void UpdateAllItemLinks_MaintainsBidirectionalRelatedLinks()
        {
            // Arrange
            var req1 = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "REQ-001" };
            var req2 = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "REQ-002" };
            
            // REQ-001 is related to REQ-002, but REQ-002 doesn't know about the relationship yet
            req1.AddLinkedItem(new ItemLink("REQ-002", ItemLinkType.Related));

            plugin.GetSystemRequirements().Returns(new[] { req1, req2 });
            plugin.GetSoftwareRequirements().Returns(new RequirementItem[0]);
            plugin.GetDocumentationRequirements().Returns(new RequirementItem[0]);
            plugin.GetDocContents().Returns(new DocContentItem[0]);
            plugin.GetSoftwareSystemTests().Returns(new SoftwareSystemTestItem[0]);
            plugin.GetAnomalies().Returns(new AnomalyItem[0]);
            plugin.GetRisks().Returns(new RiskItem[0]);
            plugin.GetSOUP().Returns(new SOUPItem[0]);
            plugin.GetUnitTests().Returns(new UnitTestItem[0]);
            plugin.GetTestResults().Returns(new TestResult[0]);
            SetupEliminatedItemMethods();

            var plugins = new List<IDataSourcePlugin> { plugin };

            // Act
            itemLinkUpdater.UpdateAllItemLinks(plugins);

            // Assert
            var req2Links = req2.LinkedItems.ToList();
            Assert.That(req2Links.Count, Is.EqualTo(1), "REQ-002 should have exactly one link back to REQ-001");
            Assert.That(req2Links[0].TargetID, Is.EqualTo("REQ-001"), "REQ-002 should link back to REQ-001");
            Assert.That(req2Links[0].LinkType, Is.EqualTo(ItemLinkType.Related), "REQ-002 should have Related link type");
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "02BA04E4-F992-4588-ADAA-FE12E72D0656",
            Purpose = "Test that DOC-DocumentedBy relationships are bidirectional after update",
            PostCondition = "Item has DOC link to document and document has DocumentedBy link back")]
        public void UpdateAllItemLinks_CreatesComplementaryDocumentationLinks()
        {
            // Arrange
            var requirement = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "REQ-001" };
            var docContent = new DocContentItem { ItemID = "DOC-001" };
            
            // Requirement links to documentation, document should get complementary link back
            requirement.AddLinkedItem(new ItemLink("DOC-001", ItemLinkType.DOC));

            plugin.GetSystemRequirements().Returns(new[] { requirement });
            plugin.GetSoftwareRequirements().Returns(new RequirementItem[0]);
            plugin.GetDocumentationRequirements().Returns(new RequirementItem[0]);
            plugin.GetDocContents().Returns(new[] { docContent });
            plugin.GetSoftwareSystemTests().Returns(new SoftwareSystemTestItem[0]);
            plugin.GetAnomalies().Returns(new AnomalyItem[0]);
            plugin.GetRisks().Returns(new RiskItem[0]);
            plugin.GetSOUP().Returns(new SOUPItem[0]);
            plugin.GetUnitTests().Returns(new UnitTestItem[0]);
            plugin.GetTestResults().Returns(new TestResult[0]);
            SetupEliminatedItemMethods();

            var plugins = new List<IDataSourcePlugin> { plugin };

            // Act
            itemLinkUpdater.UpdateAllItemLinks(plugins);

            // Assert
            var docLinks = docContent.LinkedItems.ToList();
            Assert.That(docLinks.Count, Is.EqualTo(1), "Doc content should have exactly one link back to requirement");
            Assert.That(docLinks[0].TargetID, Is.EqualTo("REQ-001"), "Doc content should link back to requirement");
            Assert.That(docLinks[0].LinkType, Is.EqualTo(ItemLinkType.DocumentedBy), "Doc content should have DocumentedBy link type");
            
            var reqLinks = requirement.LinkedItems.ToList();
            Assert.That(reqLinks.Count, Is.EqualTo(1), "Requirement should still have its DOC link");
            Assert.That(reqLinks[0].LinkType, Is.EqualTo(ItemLinkType.DOC), "Requirement should maintain DOC link type");
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "1518850C-467E-48C6-995F-BF95831595BB",
            Purpose = "Test that existing complementary links are not duplicated",
            PostCondition = "Only one complementary link exists, no duplicates")]
        public void UpdateAllItemLinks_DoesNotDuplicateExistingComplementaryLinks()
        {
            // Arrange
            var parentReq = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "REQ-001" };
            var childReq = new RequirementItem(RequirementType.SoftwareRequirement) { ItemID = "REQ-002" };
            
            // Both sides of the relationship already exist
            parentReq.AddLinkedItem(new ItemLink("REQ-002", ItemLinkType.Child));
            childReq.AddLinkedItem(new ItemLink("REQ-001", ItemLinkType.Parent));

            plugin.GetSystemRequirements().Returns(new[] { parentReq });
            plugin.GetSoftwareRequirements().Returns(new[] { childReq });
            plugin.GetDocumentationRequirements().Returns(new RequirementItem[0]);
            plugin.GetDocContents().Returns(new DocContentItem[0]);
            plugin.GetSoftwareSystemTests().Returns(new SoftwareSystemTestItem[0]);
            plugin.GetAnomalies().Returns(new AnomalyItem[0]);
            plugin.GetRisks().Returns(new RiskItem[0]);
            plugin.GetSOUP().Returns(new SOUPItem[0]);
            plugin.GetUnitTests().Returns(new UnitTestItem[0]);
            plugin.GetTestResults().Returns(new TestResult[0]);
            SetupEliminatedItemMethods();

            var plugins = new List<IDataSourcePlugin> { plugin };

            // Act
            itemLinkUpdater.UpdateAllItemLinks(plugins);

            // Assert
            var childLinks = childReq.LinkedItems.ToList();
            Assert.That(childLinks.Count, Is.EqualTo(1), "Child should have exactly one parent link (not duplicated)");
            Assert.That(childLinks[0].LinkType, Is.EqualTo(ItemLinkType.Parent), "Child should maintain parent link type");

            var parentLinks = parentReq.LinkedItems.ToList();
            Assert.That(parentLinks.Count, Is.EqualTo(1), "Parent should have exactly one child link (not duplicated)");
            Assert.That(parentLinks[0].LinkType, Is.EqualTo(ItemLinkType.Child), "Parent should maintain child link type");
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "0703EBEA-E503-4581-8FED-54353102C686",
            Purpose = "Test that UnitTest-UnitTests relationships are bidirectional after update",
            PostCondition = "Requirement has UnitTest link and unit test has UnitTests link")]
        public void UpdateAllItemLinks_CreatesComplementaryUnitTestLinks()
        {
            // Arrange
            var requirement = new RequirementItem(RequirementType.SoftwareRequirement) { ItemID = "REQ-001" };
            var unitTest = new UnitTestItem { ItemID = "UNIT-001" };
            
            // Requirement links to unit test, but unit test doesn't link back yet
            requirement.AddLinkedItem(new ItemLink("UNIT-001", ItemLinkType.UnitTest));

            plugin.GetSystemRequirements().Returns(new RequirementItem[0]);
            plugin.GetSoftwareRequirements().Returns(new[] { requirement });
            plugin.GetDocumentationRequirements().Returns(new RequirementItem[0]);
            plugin.GetDocContents().Returns(new DocContentItem[0]);
            plugin.GetSoftwareSystemTests().Returns(new SoftwareSystemTestItem[0]);
            plugin.GetAnomalies().Returns(new AnomalyItem[0]);
            plugin.GetRisks().Returns(new RiskItem[0]);
            plugin.GetSOUP().Returns(new SOUPItem[0]);
            plugin.GetUnitTests().Returns(new[] { unitTest });
            plugin.GetTestResults().Returns(new TestResult[0]);
            SetupEliminatedItemMethods();

            var plugins = new List<IDataSourcePlugin> { plugin };

            // Act
            itemLinkUpdater.UpdateAllItemLinks(plugins);

            // Assert
            var unitTestLinks = unitTest.LinkedItems.ToList();
            Assert.That(unitTestLinks.Count, Is.EqualTo(1), "Unit test should have exactly one link back to requirement");
            Assert.That(unitTestLinks[0].TargetID, Is.EqualTo("REQ-001"), "Unit test should link back to requirement");
            Assert.That(unitTestLinks[0].LinkType, Is.EqualTo(ItemLinkType.UnitTests), "Unit test should have UnitTests link type");

            var reqLinks = requirement.LinkedItems.ToList();
            Assert.That(reqLinks.Count, Is.EqualTo(1), "Requirement should still have its UnitTest link");
            Assert.That(reqLinks[0].LinkType, Is.EqualTo(ItemLinkType.UnitTest), "Requirement should maintain UnitTest link type");
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "B220B225-B3AE-4763-B13B-1C4936B5B19E",
            Purpose = "Test that test cases can link to unit tests using UnitTest link type",
            PostCondition = "Test case has UnitTest link to unit test and unit test gets complementary UnitTests link back")]
        public void UpdateAllItemLinks_TestCaseToUnitTestLinks()
        {
            // Arrange
            var testCase = new SoftwareSystemTestItem { ItemID = "TC-001" };
            var unitTest = new UnitTestItem { ItemID = "UNIT-001" };
            
            // Test case links to unit test (e.g., via KickToUnitTest method from Redmine plugin)
            testCase.KickToUnitTest("UNIT-001"); // This adds UnitTest link type

            plugin.GetSystemRequirements().Returns(new RequirementItem[0]);
            plugin.GetSoftwareRequirements().Returns(new RequirementItem[0]);
            plugin.GetDocumentationRequirements().Returns(new RequirementItem[0]);
            plugin.GetDocContents().Returns(new DocContentItem[0]);
            plugin.GetSoftwareSystemTests().Returns(new[] { testCase });
            plugin.GetAnomalies().Returns(new AnomalyItem[0]);
            plugin.GetRisks().Returns(new RiskItem[0]);
            plugin.GetSOUP().Returns(new SOUPItem[0]);
            plugin.GetUnitTests().Returns(new[] { unitTest });
            plugin.GetTestResults().Returns(new TestResult[0]);
            SetupEliminatedItemMethods();

            var plugins = new List<IDataSourcePlugin> { plugin };

            // Act
            itemLinkUpdater.UpdateAllItemLinks(plugins);

            // Assert
            var testCaseLinks = testCase.LinkedItems.ToList();
            Assert.That(testCaseLinks.Count, Is.EqualTo(1), "Test case should have one link to unit test");
            Assert.That(testCaseLinks[0].TargetID, Is.EqualTo("UNIT-001"), "Test case should link to unit test");
            Assert.That(testCaseLinks[0].LinkType, Is.EqualTo(ItemLinkType.UnitTest), "Test case should use UnitTest link type");

            var unitTestLinks = unitTest.LinkedItems.ToList();
            Assert.That(unitTestLinks.Count, Is.EqualTo(1), "Unit test should have exactly one link back to test case");
            Assert.That(unitTestLinks[0].TargetID, Is.EqualTo("TC-001"), "Unit test should link back to test case");
            Assert.That(unitTestLinks[0].LinkType, Is.EqualTo(ItemLinkType.UnitTests), "Unit test should have UnitTests link type");
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "67D7E378-7202-4F43-944A-98443F2BA80F",
            Purpose = "Test DocumentedBy links create complementary DOC links in reverse direction",
            PostCondition = "Document has DocumentedBy link and item gets complementary DOC link")]
        public void UpdateAllItemLinks_CreatesComplementaryDocLinksFromDocumentedBy()
        {
            // Arrange
            var requirement = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "REQ-001" };
            var docContent = new DocContentItem { ItemID = "DOC-001" };
            
            // Document links back to what it documents, requirement should get complementary DOC link
            docContent.AddLinkedItem(new ItemLink("REQ-001", ItemLinkType.DocumentedBy));

            plugin.GetSystemRequirements().Returns(new[] { requirement });
            plugin.GetSoftwareRequirements().Returns(new RequirementItem[0]);
            plugin.GetDocumentationRequirements().Returns(new RequirementItem[0]);
            plugin.GetDocContents().Returns(new[] { docContent });
            plugin.GetSoftwareSystemTests().Returns(new SoftwareSystemTestItem[0]);
            plugin.GetAnomalies().Returns(new AnomalyItem[0]);
            plugin.GetRisks().Returns(new RiskItem[0]);
            plugin.GetSOUP().Returns(new SOUPItem[0]);
            plugin.GetUnitTests().Returns(new UnitTestItem[0]);
            plugin.GetTestResults().Returns(new TestResult[0]);
            SetupEliminatedItemMethods();

            var plugins = new List<IDataSourcePlugin> { plugin };

            // Act
            itemLinkUpdater.UpdateAllItemLinks(plugins);

            // Assert
            var reqLinks = requirement.LinkedItems.ToList();
            Assert.That(reqLinks.Count, Is.EqualTo(1), "Requirement should have exactly one link to document");
            Assert.That(reqLinks[0].TargetID, Is.EqualTo("DOC-001"), "Requirement should link to document");
            Assert.That(reqLinks[0].LinkType, Is.EqualTo(ItemLinkType.DOC), "Requirement should have DOC link type");
            
            var docLinks = docContent.LinkedItems.ToList();
            Assert.That(docLinks.Count, Is.EqualTo(1), "Document should still have its DocumentedBy link");
            Assert.That(docLinks[0].LinkType, Is.EqualTo(ItemLinkType.DocumentedBy), "Document should maintain DocumentedBy link type");
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "3E4F5A6B-7C8D-9E0F-1A2B-3C4D5E6F7A8B",
            Purpose = "Test that TestResult-ResultOf relationships are bidirectional after update",
            PostCondition = "Test has Result link and test result has ResultOf link")]
        public void UpdateAllItemLinks_CreatesComplementaryTestResultLinks()
        {
            // Arrange
            var unitTest = new UnitTestItem { ItemID = "UNIT-001" };
            var testResult = new TestResult("UNIT-001", TestType.UNIT, TestResultStatus.PASS, "Unit Test Result", "Test passed", System.DateTime.Now);
            
            plugin.GetSystemRequirements().Returns(new RequirementItem[0]);
            plugin.GetSoftwareRequirements().Returns(new RequirementItem[0]);
            plugin.GetDocumentationRequirements().Returns(new RequirementItem[0]);
            plugin.GetDocContents().Returns(new DocContentItem[0]);
            plugin.GetSoftwareSystemTests().Returns(new SoftwareSystemTestItem[0]);
            plugin.GetAnomalies().Returns(new AnomalyItem[0]);
            plugin.GetRisks().Returns(new RiskItem[0]);
            plugin.GetSOUP().Returns(new SOUPItem[0]);
            plugin.GetUnitTests().Returns(new[] { unitTest });
            plugin.GetTestResults().Returns(new[] { testResult });
            SetupEliminatedItemMethods();

            var plugins = new List<IDataSourcePlugin> { plugin };

            // Act
            itemLinkUpdater.UpdateAllItemLinks(plugins);

            // Assert - TestResult constructor should have automatically created the links
            var testResultLinks = testResult.LinkedItems.ToList();
            var unitTestLinks = unitTest.LinkedItems.ToList();
            
            // Verify the links were created properly by the TestResult constructor and maintained
            Assert.That(testResultLinks.Any(link => link.TargetID == "UNIT-001" && link.LinkType == ItemLinkType.ResultOf), 
                Is.True, "Test result should have ResultOf link to unit test");
            Assert.That(unitTestLinks.Any(link => link.TargetID == testResult.ItemID && link.LinkType == ItemLinkType.Result), 
                Is.True, "Unit test should have Result link to test result");
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "8A9B0C1D-2E3F-4A5B-6C7D-8E9F0A1B2C3D",
            Purpose = "Test that Result-ResultOf relationships are properly complemented in both directions",
            PostCondition = "Test has Result link and TestResult has ResultOf link when created from either direction")]
        public void UpdateAllItemLinks_CreatesComplementaryResultLinks()
        {
            // Arrange - Create a test that should get a result link to a test result
            var systemTest = new SoftwareSystemTestItem { ItemID = "TC-001" };
            var testResult = new TestResult("TC-001", TestType.SYSTEM, TestResultStatus.PASS, "System Test Result", "Test passed", System.DateTime.Now);
            
            // Remove the automatic link that TestResult constructor creates to test our logic
            var existingLinks = testResult.LinkedItems.ToList();
            foreach (var link in existingLinks)
            {
                testResult.RemoveLinkedItem(link);
            }
            
            // Manually add only the ResultOf link to test the complementary logic
            testResult.AddLinkedItem(new ItemLink("TC-001", ItemLinkType.ResultOf));

            plugin.GetSystemRequirements().Returns(new RequirementItem[0]);
            plugin.GetSoftwareRequirements().Returns(new RequirementItem[0]);
            plugin.GetDocumentationRequirements().Returns(new RequirementItem[0]);
            plugin.GetDocContents().Returns(new DocContentItem[0]);
            plugin.GetSoftwareSystemTests().Returns(new[] { systemTest });
            plugin.GetAnomalies().Returns(new AnomalyItem[0]);
            plugin.GetRisks().Returns(new RiskItem[0]);
            plugin.GetSOUP().Returns(new SOUPItem[0]);
            plugin.GetUnitTests().Returns(new UnitTestItem[0]);
            plugin.GetTestResults().Returns(new[] { testResult });
            SetupEliminatedItemMethods();

            var plugins = new List<IDataSourcePlugin> { plugin };

            // Act
            itemLinkUpdater.UpdateAllItemLinks(plugins);

            // Assert
            var systemTestLinks = systemTest.LinkedItems.ToList();
            Assert.That(systemTestLinks.Count, Is.EqualTo(1), "System test should have exactly one link to test result");
            Assert.That(systemTestLinks[0].TargetID, Is.EqualTo(testResult.ItemID), "System test should link to test result");
            Assert.That(systemTestLinks[0].LinkType, Is.EqualTo(ItemLinkType.Result), "System test should have Result link type");
            
            var testResultLinks = testResult.LinkedItems.ToList();
            Assert.That(testResultLinks.Count, Is.EqualTo(1), "Test result should still have its ResultOf link");
            Assert.That(testResultLinks[0].LinkType, Is.EqualTo(ItemLinkType.ResultOf), "Test result should maintain ResultOf link type");
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "9A8B7C6D-5E4F-3A2B-1C0D-9E8F7A6B5C4D",
            Purpose = "Test that items linking to eliminated targets have their links removed",
            PostCondition = "Links to eliminated targets are removed from source items")]
        public void UpdateAllItemLinks_RemovesLinksToEliminatedTargets()
        {
            // Arrange
            var requirement1 = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "REQ-001" };
            var requirement2 = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "REQ-002" };
            
            // REQ-001 links to REQ-002, but REQ-002 gets eliminated
            requirement1.AddLinkedItem(new ItemLink("REQ-002", ItemLinkType.Related));
            
            var eliminatedReq2 = new EliminatedRequirementItem(requirement2, "Test elimination", EliminationReason.LinkedItemMissing);

            plugin.GetSystemRequirements().Returns(new[] { requirement1 });
            plugin.GetSoftwareRequirements().Returns(new RequirementItem[0]);
            plugin.GetDocumentationRequirements().Returns(new RequirementItem[0]);
            plugin.GetDocContents().Returns(new DocContentItem[0]);
            plugin.GetSoftwareSystemTests().Returns(new SoftwareSystemTestItem[0]);
            plugin.GetAnomalies().Returns(new AnomalyItem[0]);
            plugin.GetRisks().Returns(new RiskItem[0]);
            plugin.GetSOUP().Returns(new SOUPItem[0]);
            plugin.GetUnitTests().Returns(new UnitTestItem[0]);
            plugin.GetTestResults().Returns(new TestResult[0]);
            
            // Setup eliminated items
            plugin.GetEliminatedSystemRequirements().Returns(new[] { eliminatedReq2 });
            plugin.GetEliminatedSoftwareRequirements().Returns(new EliminatedRequirementItem[0]);
            plugin.GetEliminatedDocumentationRequirements().Returns(new EliminatedRequirementItem[0]);
            plugin.GetEliminatedDocContents().Returns(new EliminatedDocContentItem[0]);
            plugin.GetEliminatedSoftwareSystemTests().Returns(new EliminatedSoftwareSystemTestItem[0]);
            plugin.GetEliminatedAnomalies().Returns(new EliminatedAnomalyItem[0]);
            plugin.GetEliminatedRisks().Returns(new EliminatedRiskItem[0]);
            plugin.GetEliminatedSOUP().Returns(new EliminatedSOUPItem[0]);
            plugin.GetEliminatedTestResults().Returns(new EliminatedTestResult[0]);
            plugin.GetEliminatedUnitTests().Returns(new EliminatedUnitTestItem[0]);

            var plugins = new List<IDataSourcePlugin> { plugin };

            // Act
            itemLinkUpdater.UpdateAllItemLinks(plugins);

            // Assert
            var req1Links = requirement1.LinkedItems.ToList();
            Assert.That(req1Links.Count, Is.EqualTo(0), "Requirement should have no links after target was eliminated");
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "1F2E3D4C-5B6A-9C8D-7E6F-5A4B3C2D1E0F",
            Purpose = "Test that items with no remaining links get eliminated during rescan",
            PostCondition = "Items with no links are eliminated and EliminateItem is called")]
        public void UpdateAllItemLinks_EliminatesItemsWithNoRemainingLinks()
        {
            // Arrange
            var requirement1 = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "REQ-001" };
            var requirement2 = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "REQ-002" };
            
            // REQ-001 only links to REQ-002, and REQ-002 gets eliminated, leaving REQ-001 with no links
            requirement1.AddLinkedItem(new ItemLink("REQ-002", ItemLinkType.Related));
            
            var eliminatedReq2 = new EliminatedRequirementItem(requirement2, "Test elimination", EliminationReason.LinkedItemMissing);

            plugin.GetSystemRequirements().Returns(new[] { requirement1 });
            plugin.GetSoftwareRequirements().Returns(new RequirementItem[0]);
            plugin.GetDocumentationRequirements().Returns(new RequirementItem[0]);
            plugin.GetDocContents().Returns(new DocContentItem[0]);
            plugin.GetSoftwareSystemTests().Returns(new SoftwareSystemTestItem[0]);
            plugin.GetAnomalies().Returns(new AnomalyItem[0]);
            plugin.GetRisks().Returns(new RiskItem[0]);
            plugin.GetSOUP().Returns(new SOUPItem[0]);
            plugin.GetUnitTests().Returns(new UnitTestItem[0]);
            plugin.GetTestResults().Returns(new TestResult[0]);
            
            // Setup eliminated items
            plugin.GetEliminatedSystemRequirements().Returns(new[] { eliminatedReq2 });
            plugin.GetEliminatedSoftwareRequirements().Returns(new EliminatedRequirementItem[0]);
            plugin.GetEliminatedDocumentationRequirements().Returns(new EliminatedRequirementItem[0]);
            plugin.GetEliminatedDocContents().Returns(new EliminatedDocContentItem[0]);
            plugin.GetEliminatedSoftwareSystemTests().Returns(new EliminatedSoftwareSystemTestItem[0]);
            plugin.GetEliminatedAnomalies().Returns(new EliminatedAnomalyItem[0]);
            plugin.GetEliminatedRisks().Returns(new EliminatedRiskItem[0]);
            plugin.GetEliminatedSOUP().Returns(new EliminatedSOUPItem[0]);

            var plugins = new List<IDataSourcePlugin> { plugin };

            // Act
            itemLinkUpdater.UpdateAllItemLinks(plugins);

            // Assert
            plugin.Received().EliminateItem("REQ-001", "All items this item linked to were eliminated.", EliminationReason.LinkedItemMissing);
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "2A1B2C3D-4E5F-6A7B-8C9D-0E1F2A3B4C5D",
            Purpose = "Test that links to non-existent, non-eliminated targets throw exception",
            PostCondition = "Exception is thrown when target item is not found and not eliminated")]
        public void UpdateAllItemLinks_ThrowsExceptionForMissingNonEliminatedTargets()
        {
            // Arrange
            var requirement1 = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "REQ-001" };
            
            // REQ-001 links to REQ-999 which doesn't exist and wasn't eliminated
            requirement1.AddLinkedItem(new ItemLink("REQ-999", ItemLinkType.Related));

            plugin.GetSystemRequirements().Returns(new[] { requirement1 });
            plugin.GetSoftwareRequirements().Returns(new RequirementItem[0]);
            plugin.GetDocumentationRequirements().Returns(new RequirementItem[0]);
            plugin.GetDocContents().Returns(new DocContentItem[0]);
            plugin.GetSoftwareSystemTests().Returns(new SoftwareSystemTestItem[0]);
            plugin.GetAnomalies().Returns(new AnomalyItem[0]);
            plugin.GetRisks().Returns(new RiskItem[0]);
            plugin.GetSOUP().Returns(new SOUPItem[0]);
            plugin.GetUnitTests().Returns(new UnitTestItem[0]);
            plugin.GetTestResults().Returns(new TestResult[0]);
            SetupEliminatedItemMethods(); // All return empty arrays

            var plugins = new List<IDataSourcePlugin> { plugin };

            // Act & Assert
            var ex = Assert.Throws<System.Exception>(() => itemLinkUpdater.UpdateAllItemLinks(plugins));
            Assert.That(ex.Message, Does.Contain("Target item with ID 'REQ-999' not found and was not eliminated"));
            Assert.That(ex.Message, Does.Contain("Link from item 'REQ-001' of type 'SystemRequirement' is invalid"));
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "3B2C3D4E-5F6A-7B8C-9D0E-1F2A3B4C5D6E",
            Purpose = "Test that item with only eliminated target links gets eliminated", 
            PostCondition = "Item linking only to eliminated targets is eliminated")]
        public void UpdateAllItemLinks_EliminatesItemLinkingOnlyToEliminatedTargets()
        {
            // Arrange - REQ-002 only links to REQ-003 which is eliminated
            var requirement2 = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "REQ-002" };
            
            requirement2.AddLinkedItem(new ItemLink("REQ-003", ItemLinkType.Related));
            
            var eliminatedReq3 = new EliminatedRequirementItem(
                new RequirementItem(RequirementType.SystemRequirement) { ItemID = "REQ-003" }, 
                "Initial elimination", 
                EliminationReason.LinkedItemMissing);

            plugin.GetSystemRequirements().Returns(new[] { requirement2 });
            plugin.GetSoftwareRequirements().Returns(new RequirementItem[0]);
            plugin.GetDocumentationRequirements().Returns(new RequirementItem[0]);
            plugin.GetDocContents().Returns(new DocContentItem[0]);
            plugin.GetSoftwareSystemTests().Returns(new SoftwareSystemTestItem[0]);
            plugin.GetAnomalies().Returns(new AnomalyItem[0]);
            plugin.GetRisks().Returns(new RiskItem[0]);
            plugin.GetSOUP().Returns(new SOUPItem[0]);
            plugin.GetUnitTests().Returns(new UnitTestItem[0]);
            plugin.GetTestResults().Returns(new TestResult[0]);
            
            // Setup eliminated items
            plugin.GetEliminatedSystemRequirements().Returns(new[] { eliminatedReq3 });
            plugin.GetEliminatedSoftwareRequirements().Returns(new EliminatedRequirementItem[0]);
            plugin.GetEliminatedDocumentationRequirements().Returns(new EliminatedRequirementItem[0]);
            plugin.GetEliminatedDocContents().Returns(new EliminatedDocContentItem[0]);
            plugin.GetEliminatedSoftwareSystemTests().Returns(new EliminatedSoftwareSystemTestItem[0]);
            plugin.GetEliminatedAnomalies().Returns(new EliminatedAnomalyItem[0]);
            plugin.GetEliminatedRisks().Returns(new EliminatedRiskItem[0]);
            plugin.GetEliminatedSOUP().Returns(new EliminatedSOUPItem[0]);
            plugin.GetEliminatedTestResults().Returns(new EliminatedTestResult[0]);
            plugin.GetEliminatedUnitTests().Returns(new EliminatedUnitTestItem[0]);

            var plugins = new List<IDataSourcePlugin> { plugin };

            // Act
            itemLinkUpdater.UpdateAllItemLinks(plugins);

            // Assert - REQ-002 should be eliminated because its only link (to REQ-003) was removed
            plugin.Received().EliminateItem("REQ-002", "All items this item linked to were eliminated.", EliminationReason.LinkedItemMissing);
            
            // Verify the link was actually removed from REQ-002
            Assert.That(requirement2.LinkedItems.Count(), Is.EqualTo(0), "REQ-002 should have no remaining links");
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "5D4E5F6A-7B8C-9D0E-1F2A-3B4C5D6E7F8A",
            Purpose = "Test that constructor throws ArgumentNullException for null dataSources",
            PostCondition = "ArgumentNullException is thrown")]
        public void Constructor_ThrowsArgumentNullException_WhenDataSourcesIsNull()
        {
            // Act & Assert
            var ex = Assert.Throws<System.ArgumentNullException>(() => new ItemLinkUpdater(null));
            Assert.That(ex.ParamName, Is.EqualTo("dataSources"));
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "6E5F6A7B-8C9D-0E1F-2A3B-4C5D6E7F8A9B",
            Purpose = "Test that UpdateAllItemLinks throws ArgumentNullException for null plugins",
            PostCondition = "ArgumentNullException is thrown")]
        public void UpdateAllItemLinks_ThrowsArgumentNullException_WhenPluginsIsNull()
        {
            // Act & Assert
            var ex = Assert.Throws<System.ArgumentNullException>(() => itemLinkUpdater.UpdateAllItemLinks(null));
            Assert.That(ex.ParamName, Is.EqualTo("plugins"));
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "7F6A7B8C-9D0E-1F2A-3B4C-5D6E7F8A9B0C",
            Purpose = "Test that EliminatedItemIDs property returns read-only list of eliminated items",
            PostCondition = "EliminatedItemIDs contains expected items")]
        public void EliminatedItemIDs_ReturnsReadOnlyListOfEliminatedItems()
        {
            // The EliminatedItemIDs property should be empty initially since no items have been eliminated yet
            // This test verifies the property exists and returns a read-only collection
            
            // Act
            var eliminatedIds = itemLinkUpdater.EliminatedItemIDs;

            // Assert
            Assert.That(eliminatedIds, Is.Not.Null, "EliminatedItemIDs should not be null");
            Assert.That(eliminatedIds.Count, Is.EqualTo(0), "EliminatedItemIDs should be empty initially");
            Assert.That(eliminatedIds, Is.InstanceOf<System.Collections.Generic.IReadOnlyList<string>>(), 
                "EliminatedItemIDs should be a read-only list");
        }

        private void SetupEliminatedItemMethods()
        {
            plugin.GetEliminatedSystemRequirements().Returns(new EliminatedRequirementItem[0]);
            plugin.GetEliminatedSoftwareRequirements().Returns(new EliminatedRequirementItem[0]);
            plugin.GetEliminatedDocumentationRequirements().Returns(new EliminatedRequirementItem[0]);
            plugin.GetEliminatedDocContents().Returns(new EliminatedDocContentItem[0]);
            plugin.GetEliminatedSoftwareSystemTests().Returns(new EliminatedSoftwareSystemTestItem[0]);
            plugin.GetEliminatedAnomalies().Returns(new EliminatedAnomalyItem[0]);
            plugin.GetEliminatedRisks().Returns(new EliminatedRiskItem[0]);
            plugin.GetEliminatedSOUP().Returns(new EliminatedSOUPItem[0]);
            plugin.GetEliminatedTestResults().Returns(new EliminatedTestResult[0]);
            plugin.GetEliminatedUnitTests().Returns(new EliminatedUnitTestItem[0]);
        }
    }
}