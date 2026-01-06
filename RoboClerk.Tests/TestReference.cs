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
    [Description("These tests test the Reference Content Creator")]
    internal class TestReference
    {
        private IConfiguration config = null;
        private IDataSources dataSources = null;
        private ITraceabilityAnalysis traceAnalysis = null;
        private Reference creator = null;
        private List<DocumentConfig> documents = null;

        [SetUp]
        public void TestSetup()
        {
            config = Substitute.For<IConfiguration>();
            dataSources = Substitute.For<IDataSources>();
            traceAnalysis = Substitute.For<ITraceabilityAnalysis>();
            
            documents = new List<DocumentConfig>
            {
                new DocumentConfig("RefDocID", "DOC-001", "Referenced Document Title", "REF", "template.adoc"),
                new DocumentConfig("OtherDocID", "DOC-002", "Other Document", "OTH", "other.adoc")
            };
            config.Documents.Returns(documents);

            creator = new Reference(dataSources, traceAnalysis, config);
        }

        [UnitTestAttribute(
        Identifier = "12598dee-11f1-425d-924c-4b962a4f5e84",
        Purpose = "Reference content creator is created",
        PostCondition = "No exception is thrown")]
        [Test]
        public void TestReferenceCreation()
        {
            Assert.That(creator, Is.Not.Null);
        }

        [UnitTestAttribute(
        Identifier = "20677719-c19b-434e-aa40-36176f7112c7",
        Purpose = "GetMetadata returns correct metadata with document tags",
        PostCondition = "Metadata contains tags for configured documents")]
        [Test]
        public void TestGetMetadata()
        {
            var metadata = ((ContentCreatorBase)creator).GetMetadata();
            Assert.That(metadata.Name, Is.EqualTo("Document Reference"));
            Assert.That(metadata.Tags, Has.Count.EqualTo(2));
            Assert.That(metadata.Tags.Any(t => t.TagID == "Referenced Document Title"), Is.True);
            Assert.That(metadata.Tags.Any(t => t.TagID == "Other Document"), Is.True);
        }

        [UnitTestAttribute(
        Identifier = "55ce41cf-0fef-43a1-8f42-521e9fd6285f",
        Purpose = "StaticMetadata returns correct default metadata",
        PostCondition = "Static metadata is returned with expected values")]
        [Test]
        public void TestStaticMetadata()
        {
            var metadata = Reference.StaticMetadata;
            Assert.That(metadata.Name, Is.EqualTo("Document Reference"));
            Assert.That(metadata.Tags, Has.Count.EqualTo(1));
            Assert.That(metadata.Tags[0].TagID, Is.EqualTo("[DocumentID]"));
            Assert.That(metadata.Category, Is.EqualTo("Document Information"));
        }

        [UnitTestAttribute(
        Identifier = "8d066c29-33c7-4d3f-9476-d13fd4540a65",
        Purpose = "GetContent returns title by default",
        PostCondition = "Document title is returned")]
        [Test]
        public void TestGetContent_Default()
        {
            var tag = Substitute.For<IRoboClerkTag>();
            tag.ContentCreatorID.Returns("RefDocID");
            tag.Parameters.Returns(new List<string>());
            
            var doc = new DocumentConfig("CurrentDoc", "DOC-000", "Current Document", "CUR", "template.adoc");
            
            // Mock trace entities for AddTrace
            var currentTraceEntity = new TraceEntity("CurrentDoc", "Current Document", "CUR", TraceEntityType.Document);
            var refTraceEntity = new TraceEntity("RefDocID", "Referenced Document Title", "REF", TraceEntityType.Document);
            traceAnalysis.GetTraceEntityForTitle("Current Document").Returns(currentTraceEntity);
            traceAnalysis.GetTraceEntityForID("RefDocID").Returns(refTraceEntity);

            var result = creator.GetContent(tag, doc);
            
            Assert.That(result, Is.EqualTo("Referenced Document Title"));
            traceAnalysis.Received().AddTrace(currentTraceEntity, "RefDocID", refTraceEntity, "RefDocID");
        }

        [UnitTestAttribute(
        Identifier = "1e0792f5-d4ef-4e5e-a8dc-6f23710bda4d",
        Purpose = "GetContent returns ID when requested",
        PostCondition = "Document ID is returned")]
        [Test]
        public void TestGetContent_ID()
        {
            var tag = Substitute.For<IRoboClerkTag>();
            tag.ContentCreatorID.Returns("RefDocID");
            tag.HasParameter("ID").Returns(true);
            tag.GetParameterOrDefault("ID", string.Empty).Returns("TRUE");
            tag.Parameters.Returns(new List<string> { "ID" });

            var doc = new DocumentConfig("CurrentDoc", "DOC-000", "Current Document", "CUR", "template.adoc");
            
            var result = creator.GetContent(tag, doc);
            
            Assert.That(result, Is.EqualTo("DOC-001"));
        }

        [UnitTestAttribute(
        Identifier = "53bf0fb8-0227-46cc-9517-6741b3f8760f",
        Purpose = "GetContent returns combined properties",
        PostCondition = "Combined string is returned")]
        [Test]
        public void TestGetContent_Combined()
        {
            var tag = Substitute.For<IRoboClerkTag>();
            tag.ContentCreatorID.Returns("RefDocID");
            tag.HasParameter("ID").Returns(true);
            tag.GetParameterOrDefault("ID", string.Empty).Returns("TRUE");
            tag.HasParameter("TITLE").Returns(true);
            tag.GetParameterOrDefault("TITLE", string.Empty).Returns("TRUE");
            tag.HasParameter("ABBR").Returns(true);
            tag.GetParameterOrDefault("ABBR", string.Empty).Returns("TRUE");
            tag.Parameters.Returns(new List<string> { "ID", "TITLE", "ABBR" });

            var doc = new DocumentConfig("CurrentDoc", "DOC-000", "Current Document", "CUR", "template.adoc");
            
            var result = creator.GetContent(tag, doc);
            
            Assert.That(result, Is.EqualTo("DOC-001 Referenced Document Title (REF)"));
        }

        [UnitTestAttribute(
        Identifier = "812a0006-07ba-4319-a00d-7e604b6be353",
        Purpose = "GetContent throws exception for unknown document",
        PostCondition = "TagInvalidException is thrown")]
        [Test]
        public void TestGetContent_UnknownDocument()
        {
            var tag = Substitute.For<IRoboClerkTag>();
            tag.ContentCreatorID.Returns("UnknownID");
            tag.Contents.Returns("@@Reference:UnknownID@@");
            
            var doc = new DocumentConfig("CurrentDoc", "DOC-000", "Current Document", "CUR", "template.adoc");
            
            var ex = Assert.Throws<TagInvalidException>(() => creator.GetContent(tag, doc));
            Assert.That(ex.Message, Does.Contain("unknown document"));
        }

        [UnitTestAttribute(
        Identifier = "0d0e19bb-38b0-471b-8a09-022edf64f346",
        Purpose = "GetContent throws exception for unknown parameter",
        PostCondition = "TagInvalidException is thrown")]
        [Test]
        public void TestGetContent_UnknownParameter()
        {
            var tag = Substitute.For<IRoboClerkTag>();
            tag.ContentCreatorID.Returns("RefDocID");
            tag.Parameters.Returns(new List<string> { "UNKNOWN" });
            tag.Contents.Returns("@@Reference:RefDocID(UNKNOWN=true)@@");
            
            var doc = new DocumentConfig("CurrentDoc", "DOC-000", "Current Document", "CUR", "template.adoc");
            
            var ex = Assert.Throws<TagInvalidException>(() => creator.GetContent(tag, doc));
            Assert.That(ex.Message, Does.Contain("unknown parameter"));
        }
    }
}
