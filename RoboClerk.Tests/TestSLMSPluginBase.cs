﻿using Microsoft.CodeAnalysis.CSharp.Syntax;
using NSubstitute;
using NSubstitute.Routing.Handlers;
using NUnit.Framework;
using RoboClerk.Configuration;
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

        public SLMSPlugin(IFileSystem fileSystem) : base(fileSystem) 
        {
            name = "testplugin";
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

    }

    internal class TestSLMSPluginBase
    {
        private SLMSPlugin basePlugin;
        private IFileSystem fileSystem;
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
            basePlugin = new SLMSPlugin(fileSystem);

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
            config.PluginConfigDir.Returns(TestingHelpers.ConvertFileName(@"c:\temp"));
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
            var ex = Assert.Throws<Exception>(()=> basePlugin.Initialize(config));
            Assert.That(ex.Message.Contains("The testplugin could not read its configuration"));
        }

        [UnitTestAttribute(
        Identifier = "A3A765BA-EEDF-4A70-972D-11BC595AA8B1",
        Purpose = "A new SLMS Plugin based class is created and initialized with appropriate config.",
        PostCondition = "No exception is thrown")]
        [Test]
        public void CreateSLMSPlugin2()
        {
            basePlugin.Initialize(config);
        }

        [UnitTestAttribute(
        Identifier = "7FC0DC2C-1BF6-428B-993A-82D951793010",
        Purpose = "A new SLMS Plugin based class is created and initialized with appropriate config. Its data is requested.",
        PostCondition = "The correct data is returned")]
        [Test]
        public void CreateSLMSPlugin3()
        {
            basePlugin.Initialize(config);
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
            var ex = Assert.Throws<Exception>(() => basePlugin.Initialize(config));
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
            var ex = Assert.Throws<Exception>(() => basePlugin.Initialize(config));
            Assert.That(ex.Message.Contains("The testplugin could not read its configuration"));
        }

        [UnitTestAttribute(
        Identifier = "2780C4F4-D9A6-4C27-B350-486B7144C333",
        Purpose = "Only one item is retrieved, one link exists. Trim the links",
        PostCondition = "One items links removed and one remains")]
        [Test]
        public void TestTrimLinkedItems1()
        {
            basePlugin.Initialize(config);
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
            basePlugin.Initialize(config);

            Assert.That(basePlugin.TestInclusionFilter("ReleaseRegion", new HashSet<string>() { "EU" }),Is.True);
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
            basePlugin.Initialize(config);

            Assert.That(basePlugin.TestExclusionFilter("TestVersion", new HashSet<string>() { "0.5.0" }), Is.True);
            Assert.That(basePlugin.TestExclusionFilter("TestVersion", new HashSet<string>() { "0.4.0" }), Is.False);
            Assert.That(basePlugin.TestExclusionFilter("DifferentVersion", new HashSet<string>() { "BU" }), Is.False);
        }


    }
}
