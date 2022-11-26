using NUnit.Framework;
using System.Collections.Generic;


namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("These tests test the RoboClerk Checkpoint Data Storage")]
    internal class TestCheckpointDataStorage
    {
        private List<RequirementItem> systemRequirements = null;
        private List<RequirementItem> softwareRequirements = null;
        private List<RequirementItem> documentationRequirements = null;
        private List<RiskItem> risks = null;
        private List<SOUPItem> soups = null;
        private List<TestCaseItem> softwareSystemTests = null;
        private List<UnitTestItem> unitTests = null;
        private List<AnomalyItem> anomalies = null;
        private List<DocContentItem> docContents = null;

        [SetUp]
        public void TestSetup()
        {
            systemRequirements = new List<RequirementItem> { new RequirementItem(RequirementType.SystemRequirement) { ItemID = "sr1" } };
            softwareRequirements = new List<RequirementItem> { new RequirementItem(RequirementType.SoftwareRequirement) { ItemID = "sor1" } };
            documentationRequirements = new List<RequirementItem> { new RequirementItem(RequirementType.DocumentationRequirement) { ItemID = "dr1" } }; ;
            risks = new List<RiskItem> { new RiskItem() { ItemID = "r1" } };
            soups = new List<SOUPItem>() { new SOUPItem() { ItemID = "s1" } };
            softwareSystemTests = new List<TestCaseItem> { new TestCaseItem() { ItemID = "tc1" } };
            unitTests = new List<UnitTestItem> { new UnitTestItem() { ItemID = "ut1" } };
            anomalies = new List<AnomalyItem> { new AnomalyItem() { ItemID = "a1" } };
            docContents = new List<DocContentItem> { new DocContentItem() { ItemID = "dc1" } };
        }

        [UnitTestAttribute(
        Identifier = "4F67920C-16DA-4D0A-A29A-AC0D1869B38D",
        Purpose = "Checkpoint Data Storage is created",
        PostCondition = "No exception is thrown")]
        [Test]
        public void CreateCheckpointDataStorage1()
        {
            var sst = new CheckpointDataStorage();
        }

        [UnitTestAttribute(
        Identifier = "A69CB2DF-445B-4C77-B8A5-E185E3D51448",
        Purpose = "Checkpoint Data Storage is created, and items are stored and retrieved",
        PostCondition = "The stored and retrieved items are the same")]
        [Test]
        public void CreateCheckpointDataStorage2()
        {
            var sst = new CheckpointDataStorage();
            sst.SystemRequirements = systemRequirements;
            sst.SoftwareRequirements = softwareRequirements;
            sst.DocumentationRequirements = documentationRequirements;
            sst.Risks = risks;
            sst.SOUPs = soups;
            sst.SoftwareSystemTests = softwareSystemTests;
            sst.UnitTests = unitTests;
            sst.Anomalies = anomalies;
            sst.DocContents = docContents;

            Assert.AreSame(systemRequirements, sst.SystemRequirements);
            Assert.AreSame(softwareRequirements, sst.SoftwareRequirements);
            Assert.AreSame(documentationRequirements, sst.DocumentationRequirements);
            Assert.AreSame(risks, sst.Risks);
            Assert.AreSame(soups, sst.SOUPs);
            Assert.AreSame(unitTests, sst.UnitTests);
            Assert.AreSame(softwareSystemTests,sst.SoftwareSystemTests);
            Assert.AreSame(anomalies, sst.Anomalies);
            Assert.AreSame(docContents, sst.DocContents);
        }

        [UnitTestAttribute(
        Identifier = "BFCAC4FA-8723-4A36-92B9-63D91C1E2932",
        Purpose = "Checkpoint Data Storage is created, and items are stored, updated and retrieved",
        PostCondition = "The updated and retrieved items are the same")]
        [Test]
        public void CreateCheckpointDataStorage3()
        {
            var sst = new CheckpointDataStorage();
            sst.SystemRequirements = systemRequirements;
            sst.SoftwareRequirements = softwareRequirements;
            sst.DocumentationRequirements = documentationRequirements;
            sst.Risks = risks;
            sst.SOUPs = soups;
            sst.SoftwareSystemTests = softwareSystemTests;
            sst.UnitTests = unitTests;
            sst.Anomalies = anomalies;
            sst.DocContents = docContents;

            //create updated versions of the items
            var systemRequirement = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "sr1", ItemCategory = "new" } ;
            var softwareRequirement = new RequirementItem(RequirementType.SoftwareRequirement) { ItemID = "sor1", ItemCategory = "new" } ;
            var documentationRequirement = new RequirementItem(RequirementType.DocumentationRequirement) { ItemID = "dr1", ItemCategory = "new" } ;
            var risk = new RiskItem() { ItemID = "r1", ItemCategory = "new" } ;
            var soup = new SOUPItem() { ItemID = "s1", ItemCategory = "new" } ;
            var softwareSystemTest = new TestCaseItem() { ItemID = "tc1", ItemCategory = "new" } ;
            var unitTest = new UnitTestItem() { ItemID = "ut1", ItemCategory = "new" } ;
            var anomaly = new AnomalyItem() { ItemID = "a1", ItemCategory = "new" } ;
            var docContent = new DocContentItem() { ItemID = "dc1", ItemCategory = "new" } ;

            //update the items in the storage
            sst.UpdateSystemRequirement(systemRequirement);
            sst.UpdateSoftwareRequirement(softwareRequirement);
            sst.UpdateDocumentationRequirement(documentationRequirement);
            sst.UpdateRisk(risk);
            sst.UpdateSOUP(soup);
            sst.UpdateSoftwareSystemTest(softwareSystemTest);
            sst.UpdateUnitTest(unitTest);
            sst.UpdateAnomaly(anomaly);
            sst.UpdateDocContent(docContent);

            Assert.That(sst.SystemRequirements.Count, Is.EqualTo(1));
            Assert.That(sst.SoftwareRequirements.Count, Is.EqualTo(1));
            Assert.That(sst.DocumentationRequirements.Count, Is.EqualTo(1));
            Assert.That(sst.Risks.Count, Is.EqualTo(1));
            Assert.That(sst.SOUPs.Count, Is.EqualTo(1));
            Assert.That(sst.SoftwareSystemTests.Count, Is.EqualTo(1));
            Assert.That(sst.UnitTests.Count, Is.EqualTo(1));
            Assert.That(sst.Anomalies.Count, Is.EqualTo(1));
            Assert.That(sst.DocContents.Count, Is.EqualTo(1));

            Assert.That(sst.SystemRequirements[0].ItemCategory, Is.EqualTo("new"));
            Assert.That(sst.DocumentationRequirements[0].ItemCategory, Is.EqualTo("new"));
            Assert.That(sst.SoftwareRequirements[0].ItemCategory, Is.EqualTo("new"));
            Assert.That(sst.Risks[0].ItemCategory, Is.EqualTo("new"));
            Assert.That(sst.SOUPs[0].ItemCategory, Is.EqualTo("new"));
            Assert.That(sst.SoftwareSystemTests[0].ItemCategory, Is.EqualTo("new"));
            Assert.That(sst.UnitTests[0].ItemCategory, Is.EqualTo("new"));
            Assert.That(sst.Anomalies[0].ItemCategory, Is.EqualTo("new"));
            Assert.That(sst.DocContents[0].ItemCategory, Is.EqualTo("new"));

            Assert.AreSame(systemRequirements, sst.SystemRequirements);
            Assert.AreSame(softwareRequirements, sst.SoftwareRequirements);
            Assert.AreSame(documentationRequirements, sst.DocumentationRequirements);
            Assert.AreSame(risks, sst.Risks);
            Assert.AreSame(soups, sst.SOUPs);
            Assert.AreSame(unitTests, sst.UnitTests);
            Assert.AreSame(softwareSystemTests, sst.SoftwareSystemTests);
            Assert.AreSame(anomalies, sst.Anomalies);
            Assert.AreSame(docContents, sst.DocContents);
        }


    }
}
