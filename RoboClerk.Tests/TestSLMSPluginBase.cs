using Castle.Core.Smtp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NSubstitute;
using NSubstitute.Routing.Handlers;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using RoboClerk.Core.Configuration;
using RoboClerk.ContentCreators;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tomlyn.Model;

namespace RoboClerk.Tests
{

    public class SLMSPlugin : SLMSPluginBase
    {
        public bool contentsChecked = true;

        public SLMSPlugin(IFileProviderPlugin fileSystem) : base(fileSystem) 
        {
            name = "testplugin";
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            //this test plugin does not need to register any services
        }

        public override void RefreshItems()
        {
            TruthItemConfig conf = PrsConfig;
            contentsChecked = conf.Name == "SystemRequirement" && conf.Filtered == true && contentsChecked;
            conf = SrsConfig;
            contentsChecked = conf.Name == "SoftwareRequirement" && conf.Filtered == true && contentsChecked;
            conf = DocConfig;
            contentsChecked = conf.Name == "Documentation" && conf.Filtered == true && contentsChecked;
            conf = CntConfig;
            contentsChecked = conf.Name == "DocContent" && conf.Filtered == false && contentsChecked;
            conf = TcConfig;
            contentsChecked = conf.Name == "SoftwareSystemTest" && conf.Filtered == false && contentsChecked;
            conf = BugConfig;
            contentsChecked = conf.Name == "Bug" && conf.Filtered == false && contentsChecked;
            conf = RiskConfig;
            contentsChecked = conf.Name == "Risk" && conf.Filtered == true && contentsChecked;
            conf = SoupConfig;
            contentsChecked = conf.Name == "SOUP" && conf.Filtered == true && contentsChecked;

            contentsChecked = ignoreList.Contains("Rejected") && ignoreList.Contains("Dejected") && contentsChecked;
        }

        public void TestTrimFunction<T>(List<T> items, List<string> retrievedIDs)
        {
            TrimLinkedItems<T>(items, retrievedIDs);
        }

        public bool TestInclusionFilter(string fieldName, HashSet<string> values)
        {
            return IncludeItem(fieldName, values);
        }

        public bool TestExclusionFilter(string fieldName, HashSet<string> values)
        {
            return ExcludeItem(fieldName, values);
        }

        public void AddTestItems()
        {
            ClearAllItems();
            systemRequirements.Add(new RequirementItem(RequirementType.SystemRequirement) { ItemID = "1" });
            softwareRequirements.Add(new RequirementItem(RequirementType.SoftwareRequirement) { ItemID = "2" });
            documentationRequirements.Add(new RequirementItem(RequirementType.DocumentationRequirement) { ItemID = "3" });
            testCases.Add(new SoftwareSystemTestItem() { ItemID = "4" });
            anomalies.Add(new AnomalyItem() { ItemID = "5" });
            risks.Add(new RiskItem() { ItemID = "6" });
            soup.Add(new SOUPItem() { ItemID = "7" });
            docContents.Add(new DocContentItem() { ItemID = "8" });
            unitTests.Add(new UnitTestItem() { ItemID = "9" });
            
            eliminatedSystemRequirements.Add(new EliminatedRequirementItem(new RequirementItem(RequirementType.SystemRequirement),"test1",EliminationReason.FilteredOut) { ItemID = "10" });
            eliminatedSoftwareRequirements.Add(new EliminatedRequirementItem(new RequirementItem(RequirementType.SoftwareRequirement), "test2", EliminationReason.FilteredOut) { ItemID = "11" });
            eliminatedDocumentationRequirements.Add(new EliminatedRequirementItem(new RequirementItem(RequirementType.DocumentationRequirement), "test3", EliminationReason.FilteredOut) { ItemID = "12" });
            eliminatedSoftwareSystemTests.Add(new EliminatedSoftwareSystemTestItem(new SoftwareSystemTestItem(),"test4",EliminationReason.LinkedItemMissing) { ItemID = "13" });
            eliminatedRisks.Add(new EliminatedRiskItem(new RiskItem(), "test5", EliminationReason.LinkedItemMissing) { ItemID = "14" });
            eliminatedDocContents.Add(new EliminatedDocContentItem(new DocContentItem(), "test6", EliminationReason.IgnoredLinkTarget) { ItemID = "15" });
            eliminatedSOUP.Add(new EliminatedSOUPItem(new SOUPItem(), "test7", EliminationReason.IgnoredLinkTarget) { ItemID = "16" });
            eliminatedAnomalies.Add(new EliminatedAnomalyItem(new AnomalyItem(), "test8", EliminationReason.IgnoredLinkTarget) { ItemID = "17" });
        }

        public string PublicEscapeNonTablePipes(string text)
        {
            return EscapeNonTablePipes(text);
        }

        public void PublicScrubItemContents()
        {
            ScrubItemContents();
        }

        public void PublicClearAllItems()
        {
            ClearAllItems();
        }

        // Helper methods for tests
        public void AddSystemRequirement(RequirementItem req)
        {
            systemRequirements.Add(req);
        }

        public void AddSoftwareRequirement(RequirementItem req)
        {
            softwareRequirements.Add(req);
        }

        public void AddDocumentationRequirement(RequirementItem docReq)
        {
            documentationRequirements.Add(docReq);
        }

        public void AddDocContent(DocContentItem docContent)
        {
            docContents.Add(docContent);
        }

        public void AddTestCase(SoftwareSystemTestItem testCase)
        {
            testCases.Add(testCase);
        }

        public void AddAnomaly(AnomalyItem nomaly)
        {
            anomalies.Add(nomaly);
        }
        public void AddRisk(RiskItem risk)
        {
            risks.Add(risk);
        }

        public void AddSOUP(SOUPItem sp)
        {
            soup.Add(sp);
        }
    }

    internal class TestSLMSPluginBase
    {
        private SLMSPlugin basePlugin;
        private IFileSystem fileSystem;
        private IFileProviderPlugin fileProviderPlugin;
        private IConfiguration config;

        private void addTableToTable(TomlTable table, string tableName, string name, bool filter)
        {
            TomlTable subTable = new TomlTable();
            subTable["name"] = name;
            subTable["filter"] = filter;
            table[tableName] = subTable;
        }

        [SetUp]
        public void TestSetup()
        {
            fileSystem = Substitute.For<IFileSystem>();
            fileProviderPlugin = new LocalFileSystemPlugin(fileSystem);
            basePlugin = new SLMSPlugin(fileProviderPlugin);

            config = Substitute.For<IConfiguration>();

            string testpluginconfig = @"
Ignore = [ ""Rejected"", ""Dejected"" ]

[SystemRequirement]
	name = ""SystemRequirement""
	filter = true

[SoftwareRequirement]
	name = ""SoftwareRequirement""
	filter = true

[DocumentationRequirement]
	name = ""Documentation""
	filter = true

[DocContent]
	name = ""DocContent""
	filter = false

[SoftwareSystemTest]
	name = ""SoftwareSystemTest""
	filter = false

[Risk]
	name = ""Risk""
	filter = true

[Anomaly]
	name = ""Bug""
	filter = false

[SOUP]
	name = ""SOUP""
	filter = true

[VersionCustomFields]
	FieldNames = [""TestVersion""]

[ExcludedItemFilter]
	TestVersion = [""0.5.0""]

[IncludedItemFilter]
	ReleaseRegion = [""EU"",""US""]
";

            fileSystem.File.ReadAllText(Arg.Any<string>()).Returns(testpluginconfig);
            fileSystem.File.Exists(Arg.Any<string>()).Returns(true);
            config.PluginConfigDir.Returns(TestingHelpers.ConvertFilePath(@"c:\temp"));
            fileProviderPlugin.Combine(Arg.Any<string>(), Arg.Any<string>()).Returns("C:\\temp\\testplugin.toml");
        }

        [UnitTestAttribute(
        Identifier = "DDB9A49A-6CE2-4CA7-B892-5BF1183DF72C",
        Purpose = "A new SLMS Plugin based class is created and initialized with empty config.",
        PostCondition = "No exception is thrown")]
        [Test]
        public void CreateSLMSPlugin1()
        {
            fileSystem.File.ReadAllText(Arg.Any<string>()).Returns("");
            config = Substitute.For<IConfiguration>();
            var ex = Assert.Throws<Exception>(() => basePlugin.InitializePlugin(config));
            Assert.That(ex.Message.Contains("The testplugin could not read its configuration"));
        }

        [UnitTestAttribute(
        Identifier = "A3A765BA-EEDF-4A70-972D-11BC595AA8B1",
        Purpose = "A new SLMS Plugin based class is created and initialized with appropriate config.",
        PostCondition = "No exception is thrown")]
        [Test]
        public void CreateSLMSPlugin2()
        {
            basePlugin.InitializePlugin(config);
        }

        [UnitTestAttribute(
        Identifier = "7FC0DC2C-1BF6-428B-993A-82D951793010",
        Purpose = "A new SLMS Plugin based class is created and initialized with appropriate config. Its data is requested.",
        PostCondition = "The correct data is returned")]
        [Test]
        public void CreateSLMSPlugin3()
        {
            basePlugin.InitializePlugin(config);
            basePlugin.RefreshItems();
            Assert.That(basePlugin.contentsChecked, Is.True);
        }

        [UnitTestAttribute(
        Identifier = "F5E04389-0C72-4935-AFFF-19A5909E0307",
        Purpose = "A new SLMS Plugin based class is created and initialized with a config in which the SoftwareRequirement table is malformed.",
        PostCondition = "An exception is thrown")]
        [Test]
        public void CreateSLMSPlugin4()
        {
            string testpluginconfig = @"
Ignore = [ ""Rejected"", ""Dejected"" ]

[SystemRequirement]
	name = ""SystemRequirement""
	filter = true

[SoftwareRequirement]
	fame = ""SoftwareRequirement""
	filter = true
";
            fileSystem.File.ReadAllText(Arg.Any<string>()).Returns(testpluginconfig);
            var ex = Assert.Throws<Exception>(() => basePlugin.InitializePlugin(config));
            Assert.That(ex.Message.Contains("The testplugin could not read its configuration"));
        }

        [UnitTestAttribute(
        Identifier = "16D9F3CD-E390-492A-9AFB-A90A35E980CB",
        Purpose = "A new SLMS Plugin based class is created and initialized with a config in which the IncludedItemFilter contains a non-string.",
        PostCondition = "An exception is thrown")]
        [Test]
        public void CreateSLMSPlugin5()
        {
            string testpluginconfig = @"
Ignore = [ ""Rejected"", ""Dejected"" ]

[SystemRequirement]
	name = ""SystemRequirement""
	filter = true

[SoftwareRequirement]
	name = ""SoftwareRequirement""
	filter = true

[DocumentationRequirement]
	name = ""Documentation""
	filter = true

[DocContent]
	name = ""DocContent""
	filter = false

[SoftwareSystemTest]
	name = ""SoftwareSystemTest""
	filter = false

[Risk]
	name = ""Risk""
	filter = true

[Anomaly]
	name = ""Bug""
	filter = false

[SOUP]
	name = ""SOUP""
	filter = true

[VersionCustomFields]
	FieldNames = [""TestVersion""]

[ExcludedItemFilter]
	TestVersion = [""0.5.0""]

[IncludedItemFilter]
	ReleaseRegion = [""EU"",true]
";
            fileSystem.File.ReadAllText(Arg.Any<string>()).Returns(testpluginconfig);
            var ex = Assert.Throws<Exception>(() => basePlugin.InitializePlugin(config));
            Assert.That(ex.Message.Contains("The testplugin could not read its configuration"));
        }

        [UnitTestAttribute(
        Identifier = "2780C4F4-D9A6-4C27-B350-486B7144C333",
        Purpose = "Only one item is retrieved, one link exists. Trim the links",
        PostCondition = "One items links removed and one remains")]
        [Test]
        public void TestTrimLinkedItems1()
        {
            basePlugin.InitializePlugin(config);
            RequirementItem item1 = new RequirementItem(RequirementType.SystemRequirement);
            RequirementItem item2 = new RequirementItem(RequirementType.SoftwareRequirement);
            ItemLink item2item1 = new ItemLink(item1.ItemID, ItemLinkType.Parent);
            item2.AddLinkedItem(item2item1);
            ItemLink item1item2 = new ItemLink(item2.ItemID, ItemLinkType.Child);
            item1.AddLinkedItem(item1item2);
            var encountered = new List<string>() { item2.ItemID };

            var items = new List<RequirementItem>() { item1, item2 };

            Assert.That(item2.LinkedItems.Count() == 1, Is.True);
            Assert.That(item1.LinkedItems.Count() == 1, Is.True);
            Assert.That(item2.LinkedItems.First(), Is.EqualTo(item2item1));
            Assert.That(item1.LinkedItems.First(), Is.EqualTo(item1item2));

            basePlugin.TestTrimFunction<RequirementItem>(items, encountered);

            Assert.That(item2.LinkedItems.Count() == 0, Is.True);
            Assert.That(item1.LinkedItems.Count() == 1, Is.True);
            Assert.That(item1.LinkedItems.First(), Is.EqualTo(item1item2));
        }

        [UnitTestAttribute(
        Identifier = "D4B45606-4166-44F4-B4D6-12BB4FF0EA14",
        Purpose = "All different codepaths in the inclusion filter are excercised.",
        PostCondition = "The expected responses are returned.")]
        [Test]
        public void TestInclusionFilter1()
        {
            Assert.That(basePlugin.TestInclusionFilter("ReleaseRegion", new HashSet<string>() { "BU" }), Is.True);
            basePlugin.InitializePlugin(config);

            Assert.That(basePlugin.TestInclusionFilter("ReleaseRegion", new HashSet<string>() { "EU" }), Is.True);
            Assert.That(basePlugin.TestInclusionFilter("ReleaseRegion", new HashSet<string>() { "BU" }), Is.False);
            Assert.That(basePlugin.TestInclusionFilter("DifferentField", new HashSet<string>() { "BU" }), Is.True);
        }

        [UnitTestAttribute(
        Identifier = "A4869582-AD77-43D2-B250-A37EF1B03563",
        Purpose = "All different codepaths in the exclusion filter are excercised.",
        PostCondition = "The expected responses are returned.")]
        [Test]
        public void TestExclusionFilter1()
        {
            Assert.That(basePlugin.TestExclusionFilter("TestVersion", new HashSet<string>() { "0.5.0" }), Is.False);
            basePlugin.InitializePlugin(config);

            Assert.That(basePlugin.TestExclusionFilter("TestVersion", new HashSet<string>() { "0.5.0" }), Is.True);
            Assert.That(basePlugin.TestExclusionFilter("TestVersion", new HashSet<string>() { "0.4.0" }), Is.False);
            Assert.That(basePlugin.TestExclusionFilter("DifferentVersion", new HashSet<string>() { "BU" }), Is.False);
        }

        [UnitTestAttribute(
        Identifier = "ND5S2582-AD77-43D2-B250-A37EF1B03563",
        Purpose = "All test items can be retrieved, including the eliminated ones.",
        PostCondition = "The expected items are returned.")]
        [Test]
        public void TestItemStorage()
        {
            basePlugin.InitializePlugin(config);
            basePlugin.AddTestItems();

            Assert.That(basePlugin.GetSystemRequirements(), Has.Exactly(1).Items);
            Assert.That(basePlugin.GetSystemRequirements().Single().ItemID, Is.EqualTo("1"));
            Assert.That(basePlugin.GetSoftwareRequirements(), Has.Exactly(1).Items);
            Assert.That(basePlugin.GetSoftwareRequirements().Single().ItemID, Is.EqualTo("2"));
            Assert.That(basePlugin.GetDocumentationRequirements(), Has.Exactly(1).Items);
            Assert.That(basePlugin.GetDocumentationRequirements().Single().ItemID, Is.EqualTo("3"));
            Assert.That(basePlugin.GetSoftwareSystemTests(), Has.Exactly(1).Items);
            Assert.That(basePlugin.GetSoftwareSystemTests().Single().ItemID, Is.EqualTo("4"));
            Assert.That(basePlugin.GetAnomalies(), Has.Exactly(1).Items);
            Assert.That(basePlugin.GetAnomalies().Single().ItemID, Is.EqualTo("5"));
            Assert.That(basePlugin.GetRisks(), Has.Exactly(1).Items);
            Assert.That(basePlugin.GetRisks().Single().ItemID, Is.EqualTo("6"));
            Assert.That(basePlugin.GetSOUP(), Has.Exactly(1).Items);
            Assert.That(basePlugin.GetSOUP().Single().ItemID, Is.EqualTo("7"));
            Assert.That(basePlugin.GetDocContents(), Has.Exactly(1).Items);
            Assert.That(basePlugin.GetDocContents().Single().ItemID, Is.EqualTo("8"));
            Assert.That(basePlugin.GetUnitTests(), Has.Exactly(1).Items);
            Assert.That(basePlugin.GetUnitTests().Single().ItemID, Is.EqualTo("9"));

            Assert.That(basePlugin.GetEliminatedSystemRequirements(), Has.Exactly(1).Items);
            Assert.That(basePlugin.GetEliminatedSystemRequirements().Single().ItemID, Is.EqualTo("10"));
            Assert.That(basePlugin.GetEliminatedSystemRequirements().Single().EliminationReason, Is.EqualTo("test1"));
            Assert.That(basePlugin.GetEliminatedSystemRequirements().Single().EliminationType, Is.EqualTo(EliminationReason.FilteredOut));
            Assert.That(basePlugin.GetEliminatedSoftwareRequirements(), Has.Exactly(1).Items);
            Assert.That(basePlugin.GetEliminatedSoftwareRequirements().Single().ItemID, Is.EqualTo("11"));
            Assert.That(basePlugin.GetEliminatedDocumentationRequirements(), Has.Exactly(1).Items);
            Assert.That(basePlugin.GetEliminatedDocumentationRequirements().Single().ItemID, Is.EqualTo("12"));
            Assert.That(basePlugin.GetEliminatedSoftwareSystemTests(), Has.Exactly(1).Items);
            Assert.That(basePlugin.GetEliminatedSoftwareSystemTests().Single().ItemID, Is.EqualTo("13"));
            Assert.That(basePlugin.GetEliminatedRisks(), Has.Exactly(1).Items);
            Assert.That(basePlugin.GetEliminatedRisks().Single().ItemID, Is.EqualTo("14"));
            Assert.That(basePlugin.GetEliminatedDocContents(), Has.Exactly(1).Items);
            Assert.That(basePlugin.GetEliminatedDocContents().Single().ItemID, Is.EqualTo("15"));
            Assert.That(basePlugin.GetEliminatedSOUP(), Has.Exactly(1).Items);
            Assert.That(basePlugin.GetEliminatedSOUP().Single().ItemID, Is.EqualTo("16"));
            Assert.That(basePlugin.GetEliminatedAnomalies(), Has.Exactly(1).Items);
            Assert.That(basePlugin.GetEliminatedAnomalies().Single().ItemID, Is.EqualTo("17"));
            Assert.That(basePlugin.GetEliminatedAnomalies().Single().EliminationReason, Is.EqualTo("test8"));
            Assert.That(basePlugin.GetEliminatedAnomalies().Single().EliminationType, Is.EqualTo(EliminationReason.IgnoredLinkTarget));
        }

        [Test]
        [UnitTestAttribute(
        Identifier = "C1D52A36-7893-4F28-A0EC-6B2D3E8A5F17",
        Purpose = "Test that EscapeNonTablePipes escapes pipe characters outside of table blocks",
        PostCondition = "Pipe characters outside of table blocks are escaped")]
        public void TestEscapeNonTablePipes_BasicFunctionality()
        {
            // Arrange
            string input = "This text has a | pipe character that should be escaped.";
            string expected = "This text has a \\| pipe character that should be escaped.";
            SLMSPlugin plugin = new SLMSPlugin(fileProviderPlugin);

            // Act
            string result = plugin.PublicEscapeNonTablePipes(input);

            // Assert
            ClassicAssert.AreEqual(expected, result);
        }

        [Test]
        [UnitTestAttribute(
        Identifier = "D2E63B47-8904-5039-B1FD-7C3E4F9B6028",
        Purpose = "Test that EscapeNonTablePipes leaves pipe characters within table blocks unchanged",
        PostCondition = "Pipe characters within table blocks remain unescaped")]
        public void TestEscapeNonTablePipes_PreservesTableContent()
        {
            // Arrange
            string input = "Text before table.\n|===\n| Cell 1 | Cell 2\n|===\nText after | table.";
            string expected = "Text before table.\n|===\n| Cell 1 | Cell 2\n|===\nText after \\| table.";
            SLMSPlugin plugin = new SLMSPlugin(fileProviderPlugin);

            // Act
            string result = plugin.PublicEscapeNonTablePipes(input);

            // Assert
            ClassicAssert.AreEqual(expected, result);
        }

        [Test]
        [UnitTestAttribute(
        Identifier = "E3F74C58-9015-614A-C20E-8D4F5A0C7139",
        Purpose = "Test that EscapeNonTablePipes handles multiple tables correctly",
        PostCondition = "Pipe characters outside of multiple table blocks are escaped while those inside remain unchanged")]
        public void TestEscapeNonTablePipes_MultipleTableBlocks()
        {
            // Arrange
            string input = "Text | before.\n|===\n| Table 1\n|===\nMiddle | text.\n|===\n| Table 2\n|===\nAfter | text.";
            string expected = "Text \\| before.\n|===\n| Table 1\n|===\nMiddle \\| text.\n|===\n| Table 2\n|===\nAfter \\| text.";
            SLMSPlugin plugin = new SLMSPlugin(fileProviderPlugin);

            // Act
            string result = plugin.PublicEscapeNonTablePipes(input);

            // Assert
            ClassicAssert.AreEqual(expected, result);
        }

        [Test]
        [UnitTestAttribute(
        Identifier = "F4085D69-A126-725B-D31F-9E5060BD824A",
        Purpose = "Test that EscapeNonTablePipes does not re-escape already escaped pipes",
        PostCondition = "Already escaped pipe characters remain unchanged")]
        public void TestEscapeNonTablePipes_PreservesEscapedPipes()
        {
            // Arrange
            string input = "This has an already escaped \\| pipe.";
            string expected = "This has an already escaped \\| pipe.";
            SLMSPlugin plugin = new SLMSPlugin(fileProviderPlugin);

            // Act
            string result = plugin.PublicEscapeNonTablePipes(input);

            // Assert
            ClassicAssert.AreEqual(expected, result);
        }

        [Test]
        [UnitTestAttribute(
        Identifier = "G5196E7A-B237-836C-E420-AF6171CE935B",
        Purpose = "Test that EscapeNonTablePipes correctly handles input without any pipes",
        PostCondition = "Input without pipe characters is returned unchanged")]
        public void TestEscapeNonTablePipes_NoPipes()
        {
            // Arrange
            string input = "This text has no pipe characters.";
            string expected = "This text has no pipe characters.";
            SLMSPlugin plugin = new SLMSPlugin(fileProviderPlugin);

            // Act
            string result = plugin.PublicEscapeNonTablePipes(input);

            // Assert
            ClassicAssert.AreEqual(expected, result);
        }

        [Test]
        [UnitTestAttribute(
        Identifier = "H62A7F8B-C348-947D-F531-B0728EDF0468",
        Purpose = "Test that EscapeNonTablePipes handles empty input correctly",
        PostCondition = "Empty string is returned unchanged")]
        public void TestEscapeNonTablePipes_EmptyInput()
        {
            // Arrange
            string input = "";
            string expected = "";
            SLMSPlugin plugin = new SLMSPlugin(fileProviderPlugin);

            // Act
            string result = plugin.PublicEscapeNonTablePipes(input);

            // Assert
            ClassicAssert.AreEqual(expected, result);
        }


        [Test]
        [UnitTestAttribute(
        Identifier = "I73B809C-D459-A58E-0642-C183A0F15579",
        Purpose = "Test that ScrubItemContents properly escapes pipe characters in all item collections except DocContent items.",
        PostCondition = "Pipe characters in all item string properties are escaped.")]
        public void TestScrubItemContents_ProcessesAllItemCollections()
        {
            // Arrange
            SLMSPlugin plugin = new SLMSPlugin(fileProviderPlugin);
            plugin.InitializePlugin(config);

            // Clear all items
            plugin.PublicClearAllItems();

            // Add test items with pipe characters in various string properties
            var sysReq = new RequirementItem(RequirementType.SystemRequirement)
            {
                ItemID = "SYS-001",
                ItemTitle = "System | Requirement",
                RequirementDescription = "This is a | description with a table:\n|===\n| Cell 1 | Cell 2\n|==="
            };

            var swReq = new RequirementItem(RequirementType.SoftwareRequirement)
            {
                ItemID = "SW-001",
                ItemTitle = "Software | Requirement",
                RequirementDescription = "Another | description"
            };

            var docReq = new RequirementItem(RequirementType.DocumentationRequirement)
            {
                ItemID = "DOC-001",
                ItemTitle = "Documentation | Requirement",
                RequirementDescription = "Documentation | description"
            };

            var docContent = new DocContentItem
            {
                ItemID = "CONT-001",
                ItemTitle = "Doc | Content",
                DocContent = "Content with | pipe"
            };

            var testCase = new SoftwareSystemTestItem
            {
                ItemID = "TEST-001",
                ItemTitle = "Test | Case",
                TestCaseDescription = "Test case | description"
            };
            testCase.AddTestCaseStep(new TestStep("1", "Action with | pipe", "Expected | result"));

            var anomaly = new AnomalyItem
            {
                ItemID = "ANOM-001",
                ItemTitle = "Anomaly | Item",
                AnomalyDetailedDescription = "Anomaly | description"
            };

            var risk = new RiskItem
            {
                ItemID = "RISK-001",
                ItemTitle = "Risk | Item",
                RiskCauseOfFailure = "Risk | cause"
            };

            var soup = new SOUPItem
            {
                ItemID = "SOUP-001",
                ItemTitle = "SOUP | Item",
                SOUPDetailedDescription = "SOUP | description"
            };

            // Add items to their collections
            plugin.AddSystemRequirement(sysReq);
            plugin.AddSoftwareRequirement(swReq);
            plugin.AddDocumentationRequirement(docReq);
            plugin.AddDocContent(docContent);
            plugin.AddTestCase(testCase);
            plugin.AddAnomaly(anomaly);
            plugin.AddRisk(risk);
            plugin.AddSOUP(soup);

            // Act
            plugin.PublicScrubItemContents();

            // Assert
            // System requirements
            ClassicAssert.AreEqual("System \\| Requirement", plugin.GetSystemRequirements().First().ItemTitle);
            ClassicAssert.AreEqual("This is a \\| description with a table:\n|===\n| Cell 1 | Cell 2\n|===",
                plugin.GetSystemRequirements().First().RequirementDescription);

            // Software requirements
            ClassicAssert.AreEqual("Software \\| Requirement", plugin.GetSoftwareRequirements().First().ItemTitle);
            ClassicAssert.AreEqual("Another \\| description",
                plugin.GetSoftwareRequirements().First().RequirementDescription);

            // Documentation requirements
            ClassicAssert.AreEqual("Documentation \\| Requirement", plugin.GetDocumentationRequirements().First().ItemTitle);
            ClassicAssert.AreEqual("Documentation \\| description",
                plugin.GetDocumentationRequirements().First().RequirementDescription);

            // Doc content should not be escaped since this is typically not displayed in tables
            ClassicAssert.AreEqual("Doc | Content", plugin.GetDocContents().First().ItemTitle);
            ClassicAssert.AreEqual("Content with | pipe", plugin.GetDocContents().First().DocContent);

            // Test cases and steps
            ClassicAssert.AreEqual("Test \\| Case", plugin.GetSoftwareSystemTests().First().ItemTitle);
            ClassicAssert.AreEqual("Test case \\| description", plugin.GetSoftwareSystemTests().First().TestCaseDescription);
            ClassicAssert.AreEqual("Action with \\| pipe", plugin.GetSoftwareSystemTests().First().TestCaseSteps.First().Action);
            ClassicAssert.AreEqual("Expected \\| result", plugin.GetSoftwareSystemTests().First().TestCaseSteps.First().ExpectedResult);

            // Anomalies
            ClassicAssert.AreEqual("Anomaly \\| Item", plugin.GetAnomalies().First().ItemTitle);
            ClassicAssert.AreEqual("Anomaly \\| description", plugin.GetAnomalies().First().AnomalyDetailedDescription);

            // Risks
            ClassicAssert.AreEqual("Risk \\| Item", plugin.GetRisks().First().ItemTitle);
            ClassicAssert.AreEqual("Risk \\| cause", plugin.GetRisks().First().RiskCauseOfFailure);

            // SOUP
            ClassicAssert.AreEqual("SOUP \\| Item", plugin.GetSOUP().First().ItemTitle);
            ClassicAssert.AreEqual("SOUP \\| description", plugin.GetSOUP().First().SOUPDetailedDescription);
        }
    }
}
