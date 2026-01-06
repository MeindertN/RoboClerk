using NUnit.Framework;
using NSubstitute;
using RoboClerk.Core;
using RoboClerk.Core.Configuration;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;

namespace RoboClerk.Tests
{
    [TestFixture]
    public class TestDataSourcePluginBase
    {
        private IFileProviderPlugin _fileSystem;
        private TestableDataSourcePlugin _plugin;

        private class TestableDataSourcePlugin : DataSourcePluginBase
        {
            public TestableDataSourcePlugin(IFileProviderPlugin fileSystem) : base(fileSystem)
            {
            }

            public override void InitializePlugin(IConfiguration configuration)
            {
                // No-op for testing
            }

            public override void RefreshItems()
            {
                // No-op for testing
            }

            // Helper methods to populate protected lists for testing
            public void AddSystemRequirement(RequirementItem item) => systemRequirements.Add(item);
            public void AddSoftwareRequirement(RequirementItem item) => softwareRequirements.Add(item);
            public void AddDocumentationRequirement(RequirementItem item) => documentationRequirements.Add(item);
            public void AddSoftwareSystemTest(SoftwareSystemTestItem item) => testCases.Add(item);
            public void AddUnitTest(UnitTestItem item) => unitTests.Add(item);
            public void AddRisk(RiskItem item) => risks.Add(item);
            public void AddAnomaly(AnomalyItem item) => anomalies.Add(item);
            public void AddSOUP(SOUPItem item) => soup.Add(item);
            public void AddDocContent(DocContentItem item) => docContents.Add(item);
            public void AddTestResult(TestResult item) => testResults.Add(item);
        }

        [SetUp]
        public void Setup()
        {
            _fileSystem = Substitute.For<IFileProviderPlugin>();
            _plugin = new TestableDataSourcePlugin(_fileSystem);
        }

        [UnitTestAttribute(
            Identifier = "8e299f35-0c36-42ca-8f74-41a5f288c885",
            Purpose = "Test EliminateItem removes SystemRequirement and adds to eliminated list",
            PostCondition = "Item is moved to eliminated list")]
        [Test]
        public void EliminateItem_SystemRequirement_RemovesAndAddsToEliminated()
        {
            var item = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "SYS1" };
            _plugin.AddSystemRequirement(item);

            _plugin.EliminateItem("SYS1", "Reason", EliminationReason.FilteredOut);

            Assert.That(_plugin.GetSystemRequirements(), Does.Not.Contain(item));
            var eliminated = _plugin.GetEliminatedSystemRequirements().FirstOrDefault();
            Assert.That(eliminated, Is.Not.Null);
            Assert.That(eliminated.ItemID, Is.EqualTo("SYS1"));
            Assert.That(eliminated.EliminationReason, Is.EqualTo("Reason"));
            Assert.That(eliminated.EliminationType, Is.EqualTo(EliminationReason.FilteredOut));
        }

        [UnitTestAttribute(
            Identifier = "c046f5a6-9bc1-4642-8ac2-8d1ed80deeba",
            Purpose = "Test EliminateItem removes SoftwareRequirement and adds to eliminated list",
            PostCondition = "Item is moved to eliminated list")]
        [Test]
        public void EliminateItem_SoftwareRequirement_RemovesAndAddsToEliminated()
        {
            var item = new RequirementItem(RequirementType.SoftwareRequirement) { ItemID = "SWR1" };
            _plugin.AddSoftwareRequirement(item);

            _plugin.EliminateItem("SWR1", "Reason", EliminationReason.LinkedItemMissing);

            Assert.That(_plugin.GetSoftwareRequirements(), Does.Not.Contain(item));
            var eliminated = _plugin.GetEliminatedSoftwareRequirements().FirstOrDefault();
            Assert.That(eliminated, Is.Not.Null);
            Assert.That(eliminated.ItemID, Is.EqualTo("SWR1"));
        }

        [UnitTestAttribute(
            Identifier = "1bc8025a-22dd-4900-9dce-3edf07528f0f",
            Purpose = "Test EliminateItem removes DocumentationRequirement and adds to eliminated list",
            PostCondition = "Item is moved to eliminated list")]
        [Test]
        public void EliminateItem_DocumentationRequirement_RemovesAndAddsToEliminated()
        {
            var item = new RequirementItem(RequirementType.DocumentationRequirement) { ItemID = "DOC1" };
            _plugin.AddDocumentationRequirement(item);

            _plugin.EliminateItem("DOC1", "Reason", EliminationReason.FilteredOut);

            Assert.That(_plugin.GetDocumentationRequirements(), Does.Not.Contain(item));
            var eliminated = _plugin.GetEliminatedDocumentationRequirements().FirstOrDefault();
            Assert.That(eliminated, Is.Not.Null);
            Assert.That(eliminated.ItemID, Is.EqualTo("DOC1"));
        }

        [UnitTestAttribute(
            Identifier = "6e8c8334-0ca2-4a59-b124-743e32ca6ca5",
            Purpose = "Test EliminateItem removes SoftwareSystemTest and adds to eliminated list",
            PostCondition = "Item is moved to eliminated list")]
        [Test]
        public void EliminateItem_SoftwareSystemTest_RemovesAndAddsToEliminated()
        {
            var item = new SoftwareSystemTestItem { ItemID = "TC1" };
            _plugin.AddSoftwareSystemTest(item);

            _plugin.EliminateItem("TC1", "Reason", EliminationReason.FilteredOut);

            Assert.That(_plugin.GetSoftwareSystemTests(), Does.Not.Contain(item));
            var eliminated = _plugin.GetEliminatedSoftwareSystemTests().FirstOrDefault();
            Assert.That(eliminated, Is.Not.Null);
            Assert.That(eliminated.ItemID, Is.EqualTo("TC1"));
        }

        [UnitTestAttribute(
            Identifier = "9a3dff9c-97cf-4a21-b281-fa1f7aac78b5",
            Purpose = "Test EliminateItem removes UnitTest and adds to eliminated list",
            PostCondition = "Item is moved to eliminated list")]
        [Test]
        public void EliminateItem_UnitTest_RemovesAndAddsToEliminated()
        {
            var item = new UnitTestItem { ItemID = "UT1" };
            _plugin.AddUnitTest(item);

            _plugin.EliminateItem("UT1", "Reason", EliminationReason.FilteredOut);

            Assert.That(_plugin.GetUnitTests(), Does.Not.Contain(item));
            var eliminated = _plugin.GetEliminatedUnitTests().FirstOrDefault();
            Assert.That(eliminated, Is.Not.Null);
            Assert.That(eliminated.ItemID, Is.EqualTo("UT1"));
        }

        [UnitTestAttribute(
            Identifier = "2a9698b3-b440-48ba-b1c7-9e84263b3e0a",
            Purpose = "Test EliminateItem removes Risk and adds to eliminated list",
            PostCondition = "Item is moved to eliminated list")]
        [Test]
        public void EliminateItem_Risk_RemovesAndAddsToEliminated()
        {
            var item = new RiskItem { ItemID = "RSK1" };
            _plugin.AddRisk(item);

            _plugin.EliminateItem("RSK1", "Reason", EliminationReason.FilteredOut);

            Assert.That(_plugin.GetRisks(), Does.Not.Contain(item));
            var eliminated = _plugin.GetEliminatedRisks().FirstOrDefault();
            Assert.That(eliminated, Is.Not.Null);
            Assert.That(eliminated.ItemID, Is.EqualTo("RSK1"));
        }

        [UnitTestAttribute(
            Identifier = "113a06b9-6b3a-4988-83cd-f4c819ea3315",
            Purpose = "Test EliminateItem removes Anomaly and adds to eliminated list",
            PostCondition = "Item is moved to eliminated list")]
        [Test]
        public void EliminateItem_Anomaly_RemovesAndAddsToEliminated()
        {
            var item = new AnomalyItem { ItemID = "BUG1" };
            _plugin.AddAnomaly(item);

            _plugin.EliminateItem("BUG1", "Reason", EliminationReason.FilteredOut);

            Assert.That(_plugin.GetAnomalies(), Does.Not.Contain(item));
            var eliminated = _plugin.GetEliminatedAnomalies().FirstOrDefault();
            Assert.That(eliminated, Is.Not.Null);
            Assert.That(eliminated.ItemID, Is.EqualTo("BUG1"));
        }

        [UnitTestAttribute(
            Identifier = "8e38de48-c42e-417e-b156-0571936cfc93",
            Purpose = "Test EliminateItem removes SOUP and adds to eliminated list",
            PostCondition = "Item is moved to eliminated list")]
        [Test]
        public void EliminateItem_SOUP_RemovesAndAddsToEliminated()
        {
            var item = new SOUPItem { ItemID = "SOUP1" };
            _plugin.AddSOUP(item);

            _plugin.EliminateItem("SOUP1", "Reason", EliminationReason.FilteredOut);

            Assert.That(_plugin.GetSOUP(), Does.Not.Contain(item));
            var eliminated = _plugin.GetEliminatedSOUP().FirstOrDefault();
            Assert.That(eliminated, Is.Not.Null);
            Assert.That(eliminated.ItemID, Is.EqualTo("SOUP1"));
        }

        [UnitTestAttribute(
            Identifier = "2fc50934-c8c2-4f86-804e-a9f3df61929f",
            Purpose = "Test EliminateItem removes DocContent and adds to eliminated list",
            PostCondition = "Item is moved to eliminated list")]
        [Test]
        public void EliminateItem_DocContent_RemovesAndAddsToEliminated()
        {
            var item = new DocContentItem { ItemID = "DOCCT1" };
            _plugin.AddDocContent(item);

            _plugin.EliminateItem("DOCCT1", "Reason", EliminationReason.FilteredOut);

            Assert.That(_plugin.GetDocContents(), Does.Not.Contain(item));
            var eliminated = _plugin.GetEliminatedDocContents().FirstOrDefault();
            Assert.That(eliminated, Is.Not.Null);
            Assert.That(eliminated.ItemID, Is.EqualTo("DOCCT1"));
        }

        [UnitTestAttribute(
            Identifier = "e400a062-d9f4-4fb5-8797-42887f4298ea",
            Purpose = "Test EliminateItem removes TestResult and adds to eliminated list",
            PostCondition = "Item is moved to eliminated list")]
        [Test]
        public void EliminateItem_TestResult_RemovesAndAddsToEliminated()
        {
            var item = new TestResult("TR1", TestType.UNIT, TestResultStatus.PASS);
            // The ItemID is generated in the constructor, so we need to use the generated ID for elimination
            // However, the EliminateItem method looks up by ItemID.
            // Let's check how TestResult sets ItemID. It sets 'this.id = Guid.NewGuid().ToString();'
            // But EliminateItem uses 'item.ItemID'.
            // Wait, TestResult inherits from LinkedItem, which inherits from Item.
            // Item has ItemID property which wraps 'id'.
            // So we need to get the ItemID from the created item.
            
            _plugin.AddTestResult(item);

            _plugin.EliminateItem(item.ItemID, "Reason", EliminationReason.FilteredOut);

            Assert.That(_plugin.GetTestResults(), Does.Not.Contain(item));
            var eliminated = _plugin.GetEliminatedTestResults().FirstOrDefault();
            Assert.That(eliminated, Is.Not.Null);
            Assert.That(eliminated.ItemID, Is.EqualTo(item.ItemID));
        }

        [UnitTestAttribute(
            Identifier = "bc01fd90-97e2-4bc1-a57c-c8a75a336ff9",
            Purpose = "Test EliminateItem throws exception when item not found",
            PostCondition = "ArgumentException is thrown")]
        [Test]
        public void EliminateItem_ItemNotFound_ThrowsException()
        {
            var ex = Assert.Throws<ArgumentException>(() => _plugin.EliminateItem("NONEXISTENT", "Reason", EliminationReason.FilteredOut));
            Assert.That(ex.Message, Does.Contain("Item with ID 'NONEXISTENT' not found"));
        }
    }
}
