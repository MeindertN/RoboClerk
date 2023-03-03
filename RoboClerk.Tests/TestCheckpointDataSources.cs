using NSubstitute;
using NSubstitute.Extensions;
using NUnit.Framework;
using RoboClerk.Configuration;
using RoboClerk.ContentCreators;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("These tests test the RoboClerk Checkpoint Datasources")]
    internal class TestCheckpointDataSources
    {
        private IFileSystem mockFileSystem = null;
        private IPlugin mockPlugin = null;
        private IPlugin mockDepMgmtPlugin = null;
        private IPluginLoader mockPluginLoader = null;
        private IConfiguration mockConfiguration = null;

        [SetUp]
        public void TestSetup()
        {
            string filecontent2 = @"
{
  ""SystemRequirements"": [
    {
      ""TypeOfRequirement"": 0,
      ""ItemID"": ""11""
    }
],
  ""SoftwareRequirements"": [
    {
      ""TypeOfRequirement"": 1,
      ""ItemID"": ""12""
    }
],
  ""DocumentationRequirements"": [
    {
      ""TypeOfRequirement"": 2,
      ""ItemID"": ""44""
    }
],
  ""DocContents"": [
    {
      ""ItemID"": ""45""
    }
],
  ""Risks"": [
    {
      ""ItemID"": ""15""
    }
],
  ""SOUPs"": [
    {
      ""ItemID"": ""17""
    }
],
  ""SoftwareSystemTests"": [
    {
      ""ItemID"": ""20""
    }
],
  ""UnitTests"": [
    {
      ""ItemID"": ""TestDatasources.cs:49""
    }
],
  ""Anomalies"": [
    {
      ""ItemID"": ""19""
    }
]
}";
            mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"c:\temp\fake.json", new MockFileData(filecontent2) },
                { @"c:\out\placeholder.bin", new MockFileData(new byte[] { 0x11, 0x33, 0x55, 0xd1 }) },
            });

            mockPlugin = Substitute.For<IPlugin>();
            mockPlugin.Name.Returns("SLMS Test Plugin");
            mockPlugin.Description.Returns("SLMS Test Plugin Description");
            mockPlugin.GetDependencies().Returns(new List<ExternalDependency>());
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
            mockPluginLoader = Substitute.For<IPluginLoader>();
            mockConfiguration = Substitute.For<IConfiguration>();

            mockConfiguration.DataSourcePlugins.ReturnsForAnyArgs(new List<string> { "testPlugin1", "testPlugin2", "testDepPlugin", "testSrcPlugin" });
            mockConfiguration.PluginDirs.ReturnsForAnyArgs(new List<string> { "c:\\temp\\does_not_exist", "c:\\temp\\" });
            mockConfiguration.TemplateDir.Returns(@"c:\\temp");
            mockConfiguration.CheckpointConfig.Returns(new CheckpointConfig());
            mockPluginLoader.LoadPlugin<IPlugin>(Arg.Any<string>(), Arg.Any<string>(), mockFileSystem).Returns<IPlugin>(l => null);
            mockPluginLoader.Configure().LoadPlugin<IPlugin>(Arg.Is("testPlugin2"), Arg.Is("c:\\temp\\does_not_exist"), mockFileSystem).Returns(mockPlugin);
            mockPluginLoader.Configure().LoadPlugin<IPlugin>(Arg.Is("testDepPlugin"), Arg.Is("c:\\temp\\does_not_exist"), mockFileSystem).Returns(mockDepMgmtPlugin);
            
        }

        [UnitTestAttribute(
        Identifier = "52FBE3FA-153F-4330-A119-5496DE0EF153",
        Purpose = "Checkpoint datasources object is created",
        PostCondition = "No exception is thrown")]
        [Test]
        public void CreateCheckpointDatasources()
        {
            var cpds = new CheckpointDataSources(mockConfiguration,mockPluginLoader,mockFileSystem,"fake.json");
        }

        [UnitTestAttribute(
        Identifier = "B85A9305-BF1A-4889-B828-51FF00D98AF3",
        Purpose = "Checkpoint datasources object is created with invalid file",
        PostCondition = "Exception is thrown")]
        [Test]
        public void CreateCheckpointDatasources2()
        {
            Assert.Throws<Exception>(()=>new CheckpointDataSources(mockConfiguration, mockPluginLoader, mockFileSystem, "does_not_exist.json"));
        }

        [UnitTestAttribute(
        Identifier = "1296EAD8-9829-4122-81C1-23BE7D9836FE",
        Purpose = "Checkpoint datasources object is created, every item type in the json file is updated, slms plugin indicates removal of items",
        PostCondition = "All items are removed")]
        [Test]
        public void CheckpointDatasourcesUpdate1()
        {
            var cpc = new CheckpointConfig();
            cpc.UpdatedSystemRequirementIDs.Add("11");
            cpc.UpdatedSoftwareRequirementIDs.Add("12");
            cpc.UpdatedDocumentationRequirementIDs.Add("44");
            cpc.UpdatedDocContentIDs.Add("45");
            cpc.UpdatedRiskIDs.Add("15");
            cpc.UpdatedSOUPIDs.Add("17");
            cpc.UpdatedSoftwareSystemTestIDs.Add("20");
            cpc.UpdatedUnitTestIDs.Add("TestDatasources.cs:49");
            cpc.UpdatedAnomalyIDs.Add("19");
            mockConfiguration.CheckpointConfig.Returns(cpc);

            mockPlugin.GetSystemRequirements().Returns(new List<RequirementItem>());
            mockPlugin.GetSoftwareRequirements().Returns(new List<RequirementItem>());
            mockPlugin.GetDocumentationRequirements().Returns(new List<RequirementItem>());
            mockPlugin.GetDocContents().Returns(new List<DocContentItem>());
            mockPlugin.GetAnomalies().Returns(new List<AnomalyItem>());
            mockPlugin.GetUnitTests().Returns(new List<UnitTestItem>());
            mockPlugin.GetSoftwareSystemTests().Returns(new List<SoftwareSystemTestItem>());
            mockPlugin.GetSOUP().Returns(new List<SOUPItem>());
            mockPlugin.GetRisks().Returns(new List<RiskItem>());
            mockPlugin.GetUnitTests().Returns(new List<UnitTestItem>());

            var cpds = new CheckpointDataSources(mockConfiguration, mockPluginLoader, mockFileSystem, "fake.json");
            
            Assert.That(cpds.GetAllSystemRequirements().Count,Is.EqualTo(0));
            Assert.That(cpds.GetAllSoftwareRequirements().Count, Is.EqualTo(0));
            Assert.That(cpds.GetAllDocumentationRequirements().Count, Is.EqualTo(0));
            Assert.That(cpds.GetAllDocContents().Count, Is.EqualTo(0));
            Assert.That(cpds.GetAllAnomalies().Count, Is.EqualTo(0));
            Assert.That(cpds.GetAllUnitTests().Count, Is.EqualTo(0));
            Assert.That(cpds.GetAllSoftwareSystemTests().Count, Is.EqualTo(0));
            Assert.That(cpds.GetAllSOUP().Count, Is.EqualTo(0));
            Assert.That(cpds.GetAllRisks().Count, Is.EqualTo(0));
        }

        [UnitTestAttribute(
        Identifier = "A41400CE-DEDC-4EFF-9CE5-C2AC5E5C177C",
        Purpose = "Checkpoint datasources object is created, every item type in the json file is updated",
        PostCondition = "Updated items are returned")]
        [Test]
        public void CheckpointDatasourcesUpdate2()
        {
            var cpc = new CheckpointConfig();
            cpc.UpdatedSystemRequirementIDs.Add("11");
            cpc.UpdatedSoftwareRequirementIDs.Add("12");
            cpc.UpdatedDocumentationRequirementIDs.Add("44");
            cpc.UpdatedDocContentIDs.Add("45");
            cpc.UpdatedRiskIDs.Add("15");
            cpc.UpdatedSOUPIDs.Add("17");
            cpc.UpdatedSoftwareSystemTestIDs.Add("20");
            cpc.UpdatedUnitTestIDs.Add("TestDatasources.cs:49");
            cpc.UpdatedAnomalyIDs.Add("19");
            mockConfiguration.CheckpointConfig.Returns(cpc);

            mockPlugin.GetSystemRequirements().Returns(new List<RequirementItem> 
            { 
                new RequirementItem(RequirementType.SystemRequirement) { ItemID = "11", ItemCategory = "new"} 
            });
            mockPlugin.GetSoftwareRequirements().Returns(new List<RequirementItem>
            {
                new RequirementItem(RequirementType.SoftwareRequirement) { ItemID = "12", ItemCategory = "new"}
            });
            mockPlugin.GetDocumentationRequirements().Returns(new List<RequirementItem>
            {
                new RequirementItem(RequirementType.DocumentationRequirement) { ItemID = "44", ItemCategory = "new"}
            });
            mockPlugin.GetDocContents().Returns(new List<DocContentItem> 
            {
                new DocContentItem() { ItemID = "45", ItemCategory = "new"}
            });
            mockPlugin.GetAnomalies().Returns(new List<AnomalyItem> 
            {
                new AnomalyItem() {ItemID = "19", ItemCategory = "new"}
            });
            mockPlugin.GetUnitTests().Returns(new List<UnitTestItem>
            {
                new UnitTestItem() { ItemID = "TestDatasources.cs:49", ItemCategory = "new"}
            });
            mockPlugin.GetSoftwareSystemTests().Returns(new List<SoftwareSystemTestItem>
            {
                new SoftwareSystemTestItem() {ItemID = "20", ItemCategory = "new"}
            });
            mockPlugin.GetSOUP().Returns(new List<SOUPItem>
            {
                new SOUPItem() {ItemID = "17", ItemCategory = "new"}
            });
            mockPlugin.GetRisks().Returns(new List<RiskItem>
            {
                new RiskItem() {ItemID = "15", ItemCategory = "new"}
            });

            var cpds = new CheckpointDataSources(mockConfiguration, mockPluginLoader, mockFileSystem, "fake.json");

            Assert.That(cpds.GetAllSystemRequirements().Count, Is.EqualTo(1));
            Assert.That(cpds.GetAllSoftwareRequirements().Count, Is.EqualTo(1));
            Assert.That(cpds.GetAllDocumentationRequirements().Count, Is.EqualTo(1));
            Assert.That(cpds.GetAllDocContents().Count, Is.EqualTo(1));
            Assert.That(cpds.GetAllAnomalies().Count, Is.EqualTo(1));
            Assert.That(cpds.GetAllUnitTests().Count, Is.EqualTo(1));
            Assert.That(cpds.GetAllSoftwareSystemTests().Count, Is.EqualTo(1));
            Assert.That(cpds.GetAllSOUP().Count, Is.EqualTo(1));
            Assert.That(cpds.GetAllRisks().Count, Is.EqualTo(1));
            Assert.That(cpds.GetAllSystemRequirements()[0].ItemCategory, Is.EqualTo("new"));
            Assert.That(cpds.GetAllSoftwareRequirements()[0].ItemCategory, Is.EqualTo("new"));
            Assert.That(cpds.GetAllDocumentationRequirements()[0].ItemCategory, Is.EqualTo("new"));
            Assert.That(cpds.GetAllDocContents()[0].ItemCategory, Is.EqualTo("new"));
            Assert.That(cpds.GetAllAnomalies()[0].ItemCategory, Is.EqualTo("new"));
            Assert.That(cpds.GetAllUnitTests()[0].ItemCategory, Is.EqualTo("new"));
            Assert.That(cpds.GetAllSoftwareSystemTests()[0].ItemCategory, Is.EqualTo("new"));
            Assert.That(cpds.GetAllSOUP()[0].ItemCategory, Is.EqualTo("new"));
            Assert.That(cpds.GetAllRisks()[0].ItemCategory, Is.EqualTo("new"));
        }

        [UnitTestAttribute(
        Identifier = "329E681A-C084-40FC-828C-8DB00F3A6EBB",
        Purpose = "Checkpoint datasources object is created, external dependencies are requested",
        PostCondition = "External dependencies are retrieved directly from the external dependency plugin")]
        [Test]
        public void CheckpointDatasourcesUpdate3()
        {
            
            mockDepMgmtPlugin.GetDependencies().Returns(new List<ExternalDependency>()
            {
                new ExternalDependency("ext","ver",false)
            });

            var cpds = new CheckpointDataSources(mockConfiguration, mockPluginLoader, mockFileSystem, "fake.json");

            var extdep = cpds.GetAllExternalDependencies();
            
            Assert.That(extdep.Count, Is.EqualTo(1));
            Assert.That(extdep[0].Name, Is.EqualTo("ext"));
        }

    }
}
