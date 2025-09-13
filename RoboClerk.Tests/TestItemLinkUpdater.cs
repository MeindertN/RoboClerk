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
            Purpose = "Test that unidirectional links (DOC, UnitTest) don't create back links",
            PostCondition = "Only original unidirectional link exists")]
        public void UpdateAllItemLinks_DoesNotCreateBackLinksForUnidirectionalTypes()
        {
            // Arrange
            var requirement = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "REQ-001" };
            var docContent = new DocContentItem { ItemID = "DOC-001" };
            
            // Requirement links to documentation, this should remain unidirectional
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

            var plugins = new List<IDataSourcePlugin> { plugin };

            // Act
            itemLinkUpdater.UpdateAllItemLinks(plugins);

            // Assert
            var docLinks = docContent.LinkedItems.ToList();
            Assert.That(docLinks.Count, Is.EqualTo(0), "Doc content should not have any back links for DOC link type");
            
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
    }
}