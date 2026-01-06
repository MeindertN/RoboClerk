using NSubstitute;
using NUnit.Framework;
using RoboClerk.ContentCreators;
using RoboClerk.Core;
using RoboClerk.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("These tests test the TraceabilityMatrixBase Content Creator")]
    internal class TestTraceabilityMatrixBase
    {
        private IConfiguration config = null;
        private IDataSources dataSources = null;
        private ITraceabilityAnalysis traceAnalysis = null;
        private TraceabilityMatrixBaseTestable creator = null;

        // Testable subclass to expose protected members and implement abstract class
        private class TraceabilityMatrixBaseTestable : TraceabilityMatrixBase
        {
            public TraceabilityMatrixBaseTestable(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration conf)
                : base(data, analysis, conf)
            {
            }

            protected override string MatrixTypeName => "Test";

            public void SetTruthSource(TraceEntity entity)
            {
                truthSource = entity;
            }
        }

        [SetUp]
        public void TestSetup()
        {
            config = Substitute.For<IConfiguration>();
            dataSources = Substitute.For<IDataSources>();
            traceAnalysis = Substitute.For<ITraceabilityAnalysis>();
            creator = new TraceabilityMatrixBaseTestable(dataSources, traceAnalysis, config);
        }

        [UnitTestAttribute(
        Identifier = "161b89b0-c236-4394-a6bd-4b4f447af52f",
        Purpose = "TraceabilityMatrixBase content creator is created",
        PostCondition = "No exception is thrown")]
        [Test]
        public void TestTraceabilityMatrixBaseCreation()
        {
            Assert.That(creator, Is.Not.Null);
        }

        [UnitTestAttribute(
        Identifier = "e6189469-93be-4b84-8177-ad5c6430864d",
        Purpose = "GetMetadata returns correct metadata",
        PostCondition = "Metadata is returned with expected values")]
        [Test]
        public void TestGetMetadata()
        {
            var metadata = creator.GetMetadata();
            Assert.That(metadata.Name, Is.EqualTo("Test Traceability Matrix"));
            Assert.That(metadata.Source, Is.EqualTo("SLMS"));
            Assert.That(metadata.Tags, Has.Count.EqualTo(1));
            Assert.That(metadata.Tags[0].TagID, Is.EqualTo("TestTraceabilityMatrix"));
        }

        [UnitTestAttribute(
        Identifier = "90306b74-e314-47c9-ac16-369f295e5f37",
        Purpose = "GetContent throws exception when truth source is null",
        PostCondition = "Exception is thrown")]
        [Test]
        public void TestGetContent_NullTruthSource()
        {
            var tag = Substitute.For<IRoboClerkTag>();
            var doc = new DocumentConfig("roboclerkID", "documentID", "documentTitle", "documentAbbreviation", "documentTemplate");

            var ex = Assert.Throws<Exception>(() => creator.GetContent(tag, doc));
            Assert.That(ex.Message, Is.EqualTo("Truth source is null, unclear where to start tracing."));
        }

        [UnitTestAttribute(
        Identifier = "bc4bb50f-2df7-4ade-8a11-c893c7464345",
        Purpose = "GetContent throws exception when trace matrix is empty",
        PostCondition = "Exception is thrown")]
        [Test]
        public void TestGetContent_EmptyMatrix()
        {
            var tag = Substitute.For<IRoboClerkTag>();
            var doc = new DocumentConfig("roboclerkID", "documentID", "documentTitle", "documentAbbreviation", "documentTemplate");
            var truthEntity = new TraceEntity("SystemRequirement", "System Requirement", "SYS", TraceEntityType.Truth);
            
            creator.SetTruthSource(truthEntity);
            traceAnalysis.PerformAnalysis(dataSources, truthEntity).Returns(new RoboClerkOrderedDictionary<TraceEntity, List<List<Item>>>());

            var ex = Assert.Throws<Exception>(() => creator.GetContent(tag, doc));
            Assert.That(ex.Message, Is.EqualTo("System Requirement level trace matrix is empty."));
        }

        [UnitTestAttribute(
        Identifier = "39671c33-0d54-40b9-a2fc-32a6793da327",
        Purpose = "GetContent generates HTML matrix correctly with links",
        PostCondition = "HTML content is returned with links")]
        [Test]
        public void TestGetContent_HTML_WithLinks()
        {
            config.OutputFormat.Returns("HTML");
            var tag = Substitute.For<IRoboClerkTag>();
            var doc = new DocumentConfig("roboclerkID", "documentID", "documentTitle", "documentAbbreviation", "documentTemplate");
            var truthEntity = new TraceEntity("SystemRequirement", "System Requirement", "SYS", TraceEntityType.Truth);
            var targetEntity = new TraceEntity("SoftwareRequirement", "Software Requirement", "SWR", TraceEntityType.Truth);
            
            creator.SetTruthSource(truthEntity);

            var truthItem = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "SYS1", Link = new Uri("http://sys1") };
            var targetItem = new RequirementItem(RequirementType.SoftwareRequirement) { ItemID = "SWR1", Link = new Uri("http://swr1") };

            var matrix = new RoboClerkOrderedDictionary<TraceEntity, List<List<Item>>>
            {
                { truthEntity, new List<List<Item>> { new List<Item> { truthItem } } },
                { targetEntity, new List<List<Item>> { new List<Item> { targetItem } } }
            };

            traceAnalysis.PerformAnalysis(dataSources, truthEntity).Returns(matrix);
            traceAnalysis.GetTraceIssuesForTruth(truthEntity).Returns(new List<TraceIssue>());
            traceAnalysis.GetTraceIssuesForDocument(targetEntity).Returns(new List<TraceIssue>());

            var result = creator.GetContent(tag, doc);

            Assert.That(result, Does.Contain("<table"));
            Assert.That(result, Does.Contain("System Requirements")); // Header check
            Assert.That(result, Does.Contain("Software Requirements")); // Header check
            Assert.That(result, Does.Contain("<a href=\"http://sys1/\">SYS1</a>"));
            Assert.That(result, Does.Contain("<a href=\"http://swr1/\">SWR1</a>"));
        }

        [UnitTestAttribute(
        Identifier = "490aabc4-dd0e-4bad-b1cd-965cbefc750e",
        Purpose = "GetContent generates AsciiDoc matrix correctly with links",
        PostCondition = "AsciiDoc content is returned with links")]
        [Test]
        public void TestGetContent_AsciiDoc_WithLinks()
        {
            config.OutputFormat.Returns("ASCIIDOC");
            var tag = Substitute.For<IRoboClerkTag>();
            var doc = new DocumentConfig("roboclerkID", "documentID", "documentTitle", "documentAbbreviation", "documentTemplate");
            var truthEntity = new TraceEntity("SystemRequirement", "System Requirement", "SYS", TraceEntityType.Truth);
            var targetEntity = new TraceEntity("SoftwareRequirement", "Software Requirement", "SWR", TraceEntityType.Truth);
            
            creator.SetTruthSource(truthEntity);

            var truthItem = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "SYS1", Link = new Uri("http://sys1") };
            var targetItem = new RequirementItem(RequirementType.SoftwareRequirement) { ItemID = "SWR1", Link = new Uri("http://swr1") };

            var matrix = new RoboClerkOrderedDictionary<TraceEntity, List<List<Item>>>
            {
                { truthEntity, new List<List<Item>> { new List<Item> { truthItem } } },
                { targetEntity, new List<List<Item>> { new List<Item> { targetItem } } }
            };

            traceAnalysis.PerformAnalysis(dataSources, truthEntity).Returns(matrix);
            traceAnalysis.GetTraceIssuesForTruth(truthEntity).Returns(new List<TraceIssue>());
            traceAnalysis.GetTraceIssuesForDocument(targetEntity).Returns(new List<TraceIssue>());

            var result = creator.GetContent(tag, doc);

            Assert.That(result, Does.Contain("|===="));
            Assert.That(result, Does.Contain("| System Requirements")); // Header check
            Assert.That(result, Does.Contain("| Software Requirements")); // Header check
            Assert.That(result, Does.Contain("http://sys1/[SYS1]"));
            Assert.That(result, Does.Contain("http://swr1/[SWR1]"));
        }

        [UnitTestAttribute(
        Identifier = "581a8951-6821-40ba-a878-a33ae3c6c86a",
        Purpose = "GetContent filters items by project",
        PostCondition = "Only items matching project are included")]
        [Test]
        public void TestGetContent_ProjectFilter()
        {
            config.OutputFormat.Returns("ASCIIDOC");
            var tag = Substitute.For<IRoboClerkTag>();
            tag.HasParameter("ItemProject").Returns(true);
            tag.GetParameterOrDefault("ItemProject").Returns("ProjectA");

            var doc = new DocumentConfig("roboclerkID", "documentID", "documentTitle", "documentAbbreviation", "documentTemplate");
            var truthEntity = new TraceEntity("SystemRequirement", "System Requirement", "SYS", TraceEntityType.Truth);
            
            creator.SetTruthSource(truthEntity);

            var itemA = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "SYS1", ItemProject = "ProjectA" };
            var itemB = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "SYS2", ItemProject = "ProjectB" };

            var matrix = new RoboClerkOrderedDictionary<TraceEntity, List<List<Item>>>
            {
                { truthEntity, new List<List<Item>> { new List<Item> { itemA }, new List<Item> { itemB } } }
            };

            traceAnalysis.PerformAnalysis(dataSources, truthEntity).Returns(matrix);
            traceAnalysis.GetTraceIssuesForTruth(truthEntity).Returns(new List<TraceIssue>());

            var result = creator.GetContent(tag, doc);

            Assert.That(result, Does.Contain("SYS1"));
            Assert.That(result, Does.Not.Contain("SYS2"));
        }

        [UnitTestAttribute(
        Identifier = "90190bc5-1190-492e-9a34-f9b83f90af62",
        Purpose = "GetContent handles trace issues correctly",
        PostCondition = "Trace issues are included in output")]
        [Test]
        public void TestGetContent_TraceIssues()
        {
            config.OutputFormat.Returns("ASCIIDOC");
            var tag = Substitute.For<IRoboClerkTag>();
            var doc = new DocumentConfig("roboclerkID", "documentID", "documentTitle", "documentAbbreviation", "documentTemplate");
            var truthEntity = new TraceEntity("SystemRequirement", "System Requirement", "SYS", TraceEntityType.Truth);
            var targetEntity = new TraceEntity("SoftwareRequirement", "Software Requirement", "SWR", TraceEntityType.Truth);
            
            creator.SetTruthSource(truthEntity);

            var truthItem = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "SYS1" };
            var targetItem = new RequirementItem(RequirementType.SoftwareRequirement) { ItemID = "SWR1" };

            var matrix = new RoboClerkOrderedDictionary<TraceEntity, List<List<Item>>>
            {
                { truthEntity, new List<List<Item>> { new List<Item> { truthItem } } },
                { targetEntity, new List<List<Item>> { new List<Item> { targetItem } } }
            };

            traceAnalysis.PerformAnalysis(dataSources, truthEntity).Returns(matrix);
            
            var issue = new TraceIssue(truthEntity, "SYS1", targetEntity, "SWR1", TraceIssueType.Missing);
            traceAnalysis.GetTraceIssuesForTruth(truthEntity).Returns(new List<TraceIssue> { issue });
            traceAnalysis.GetTraceIssuesForDocument(targetEntity).Returns(new List<TraceIssue>());
            
            dataSources.GetItem("SYS1").Returns(truthItem);

            var result = creator.GetContent(tag, doc);

            Assert.That(result, Does.Contain("Trace issues:"));
            Assert.That(result, Does.Contain("System Requirement SYS1 is potentially missing a corresponding Software Requirement."));
        }

        [UnitTestAttribute(
        Identifier = "2aae5f25-1f0d-4ef6-874f-2f034ce74794",
        Purpose = "GetContent handles missing items in matrix",
        PostCondition = "MISSING is displayed for null items")]
        [Test]
        public void TestGetContent_MissingItems()
        {
            config.OutputFormat.Returns("ASCIIDOC");
            var tag = Substitute.For<IRoboClerkTag>();
            var doc = new DocumentConfig("roboclerkID", "documentID", "documentTitle", "documentAbbreviation", "documentTemplate");
            var truthEntity = new TraceEntity("SystemRequirement", "System Requirement", "SYS", TraceEntityType.Truth);
            var targetEntity = new TraceEntity("SoftwareRequirement", "Software Requirement", "SWR", TraceEntityType.Truth);
            
            creator.SetTruthSource(truthEntity);

            var truthItem = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "SYS1" };

            var matrix = new RoboClerkOrderedDictionary<TraceEntity, List<List<Item>>>
            {
                { truthEntity, new List<List<Item>> { new List<Item> { truthItem } } },
                { targetEntity, new List<List<Item>> { new List<Item> { null } } } // Null item indicates missing
            };

            traceAnalysis.PerformAnalysis(dataSources, truthEntity).Returns(matrix);
            traceAnalysis.GetTraceIssuesForTruth(truthEntity).Returns(new List<TraceIssue>());
            traceAnalysis.GetTraceIssuesForDocument(targetEntity).Returns(new List<TraceIssue>());

            var result = creator.GetContent(tag, doc);

            Assert.That(result, Does.Contain("MISSING"));
        }

        [UnitTestAttribute(
        Identifier = "0988b2c9-31ef-40c9-a092-59c88b320d3a",
        Purpose = "GetContent handles document trace presence",
        PostCondition = "Trace Present is displayed for document entities")]
        [Test]
        public void TestGetContent_DocumentTrace()
        {
            config.OutputFormat.Returns("ASCIIDOC");
            var tag = Substitute.For<IRoboClerkTag>();
            var doc = new DocumentConfig("roboclerkID", "documentID", "documentTitle", "documentAbbreviation", "documentTemplate");
            var truthEntity = new TraceEntity("SystemRequirement", "System Requirement", "SYS", TraceEntityType.Truth);
            var docEntity = new TraceEntity("Document", "Document", "DOC", TraceEntityType.Document);
            
            creator.SetTruthSource(truthEntity);

            var truthItem = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "SYS1" };
            var docItem = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "DOC1" }; // Generic item for document

            var matrix = new RoboClerkOrderedDictionary<TraceEntity, List<List<Item>>>
            {
                { truthEntity, new List<List<Item>> { new List<Item> { truthItem } } },
                { docEntity, new List<List<Item>> { new List<Item> { docItem } } }
            };

            traceAnalysis.PerformAnalysis(dataSources, truthEntity).Returns(matrix);
            traceAnalysis.GetTraceIssuesForTruth(truthEntity).Returns(new List<TraceIssue>());
            traceAnalysis.GetTraceIssuesForDocument(docEntity).Returns(new List<TraceIssue>());

            var result = creator.GetContent(tag, doc);

            Assert.That(result, Does.Contain("Trace Present"));
        }
    }
}
