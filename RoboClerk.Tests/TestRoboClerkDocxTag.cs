using NUnit.Framework;
using NSubstitute;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Packaging;
using RoboClerk.Core;
using RoboClerk.Core.Configuration;
using RoboClerk.Core.DocxSupport;
using System;
using System.IO;
using System.Linq;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("These tests test the RoboClerkDocxTag class and its base class RoboClerkBaseTag for DOCX documents")]
    internal class TestRoboClerkDocxTag
    {
        private IConfiguration config;

        [SetUp]
        public void TestSetup()
        {
            config = Substitute.For<IConfiguration>();
            config.OutputDir.Returns(@"c:\output");
        }

        private SdtElement CreateTestContentControl(string tagValue, string contentText = "")
        {
            var sdt = new SdtBlock(
                new SdtProperties(
                    new SdtId { Val = 12345678 },
                    new Tag { Val = tagValue }
                ),
                new SdtContentBlock(
                    new Paragraph(
                        new Run(
                            new Text(contentText)
                        )
                    )
                )
            );
            return sdt;
        }

        [UnitTestAttribute(
            Identifier = "E2D2D572-99F7-4C71-A2F5-5E0F02003777",
            Purpose = "Create RoboClerkDocxTag with valid content control",
            PostCondition = "Tag is created successfully with properties set")]
        [Test]
        public void CreateRoboClerkDocxTag()
        {
            var sdt = CreateTestContentControl("Config:SoftwareName()");
            var tag = new RoboClerkDocxTag(sdt, config);

            Assert.That(tag, Is.Not.Null);
            Assert.That(tag.ContentControlId, Is.EqualTo("12345678"));
            Assert.That(tag.Source, Is.EqualTo(DataSource.Config));
            Assert.That(tag.ContentCreatorID, Is.EqualTo("SoftwareName"));
        }

        [UnitTestAttribute(
            Identifier = "F4B385B7-F0BD-4D0C-BF5C-55D6C82DA296",
            Purpose = "Create RoboClerkDocxTag with null content control",
            PostCondition = "ArgumentNullException is thrown")]
        [Test]
        public void CreateRoboClerkDocxTagWithNullContentControl()
        {
            Assert.Throws<ArgumentNullException>(() => new RoboClerkDocxTag(null, config));
        }

        [UnitTestAttribute(
            Identifier = "558406DD-146D-4544-9FDB-CC268133031A",
            Purpose = "Parse content control tag with parameters",
            PostCondition = "Parameters are extracted correctly")]
        [Test]
        public void ParseContentControlTagWithParameters()
        {
            var sdt = CreateTestContentControl("Trace:SWR(id=123,name=test)");
            var tag = new RoboClerkDocxTag(sdt, config);

            Assert.That(tag.Source, Is.EqualTo(DataSource.Trace));
            Assert.That(tag.ContentCreatorID, Is.EqualTo("SWR"));
            Assert.That(tag.GetParameterOrDefault("id"), Is.EqualTo("123"));
            Assert.That(tag.GetParameterOrDefault("name"), Is.EqualTo("test"));
        }

        [UnitTestAttribute(
            Identifier = "F9CCB055-A62A-4B9F-84BC-053E3DD0703D",
            Purpose = "Parse content control tag with quoted parameters",
            PostCondition = "Quoted parameters with commas are handled correctly")]
        [Test]
        public void ParseContentControlTagWithQuotedParameters()
        {
            var sdt = CreateTestContentControl("Web:KrokiDiagram(type=plantuml,caption=\"My diagram, version 1\")");
            var tag = new RoboClerkDocxTag(sdt, config);

            Assert.That(tag.Source, Is.EqualTo(DataSource.Web));
            Assert.That(tag.ContentCreatorID, Is.EqualTo("KrokiDiagram"));
            Assert.That(tag.GetParameterOrDefault("type"), Is.EqualTo("plantuml"));
            Assert.That(tag.GetParameterOrDefault("caption"), Is.EqualTo("My diagram, version 1"));
        }

        [UnitTestAttribute(
            Identifier = "62DACA17-DFC5-4CF9-9A00-84919B47C59B",
            Purpose = "Extract text content from content control",
            PostCondition = "Text content is extracted correctly")]
        [Test]
        public void ExtractTextContentFromContentControl()
        {
            var sdt = CreateTestContentControl("Config:SoftwareName()", "Test Content");
            var tag = new RoboClerkDocxTag(sdt, config);

            Assert.That(tag.Contents, Is.EqualTo("Test Content"));
        }

        [UnitTestAttribute(
            Identifier = "787A3893-191F-4028-B156-89952F62CE9F",
            Purpose = "Update content of RoboClerkDocxTag",
            PostCondition = "Content is updated successfully")]
        [Test]
        public void UpdateContentOfRoboClerkDocxTag()
        {
            var sdt = CreateTestContentControl("Config:SoftwareName()");
            var tag = new RoboClerkDocxTag(sdt, config);

            tag.UpdateContent("New Content");
            Assert.That(tag.Contents, Is.EqualTo("New Content"));
        }

        [UnitTestAttribute(
            Identifier = "4C4D2AD4-995C-4750-B899-7CE3495320F8",
            Purpose = "Verify Inline property always returns false",
            PostCondition = "Inline property is false for DOCX tags")]
        [Test]
        public void VerifyInlinePropertyIsFalse()
        {
            var sdt = CreateTestContentControl("Config:SoftwareName()");
            var tag = new RoboClerkDocxTag(sdt, config);

            Assert.That(tag.Inline, Is.False);
        }

        [UnitTestAttribute(
            Identifier = "2B868399-5F94-437F-B293-65794FFDD355",
            Purpose = "Process nested tags with no nested content",
            PostCondition = "Empty collection is returned")]
        [Test]
        public void ProcessNestedTagsWithNoNestedContent()
        {
            var sdt = CreateTestContentControl("Comment:Note()", "Simple text with no tags");
            var tag = new RoboClerkDocxTag(sdt, config);

            var nestedTags = tag.ProcessNestedTags();
            Assert.That(nestedTags, Is.Empty);
        }

        [UnitTestAttribute(
            Identifier = "7A324005-0509-40BD-8949-3E3063E7D207",
            Purpose = "Process nested tags with nested RoboClerk tags",
            PostCondition = "Nested tags are extracted correctly")]
        [Test]
        public void ProcessNestedTagsWithNestedRoboClerkTags()
        {
            var sdt = CreateTestContentControl("Comment:Note()");
            var tag = new RoboClerkDocxTag(sdt, config);
            
            // Update content to include nested tags
            tag.UpdateContent("@@Config:SoftwareName()@@ and @@Config:Version()@@");

            var nestedTags = tag.ProcessNestedTags().ToList();
            Assert.That(nestedTags.Count, Is.EqualTo(2));
            Assert.That(nestedTags[0].Source, Is.EqualTo(DataSource.Config));
            Assert.That(nestedTags[0].ContentCreatorID, Is.EqualTo("SoftwareName"));
            Assert.That(nestedTags[1].Source, Is.EqualTo(DataSource.Config));
            Assert.That(nestedTags[1].ContentCreatorID, Is.EqualTo("Version"));
        }

        [UnitTestAttribute(
            Identifier = "0F6ABF1A-2163-4461-9E0D-A4B6F0CF44A0",
            Purpose = "Parse tag with Base64 parameter",
            PostCondition = "Base64 parameter is preserved correctly")]
        [Test]
        public void ParseTagWithBase64Parameter()
        {
            var sdt = CreateTestContentControl("Web:KrokiDiagram(payload=SGVsbG8gV29ybGQ=)");
            var tag = new RoboClerkDocxTag(sdt, config);

            Assert.That(tag.GetParameterOrDefault("payload"), Is.EqualTo("SGVsbG8gV29ybGQ="));
        }

        [UnitTestAttribute(
            Identifier = "146C70A8-4251-45CD-A39F-6AFFFDC2588B",
            Purpose = "Parse tag with quoted Base64 parameter",
            PostCondition = "Quoted Base64 parameter is handled correctly")]
        [Test]
        public void ParseTagWithQuotedBase64Parameter()
        {
            var sdt = CreateTestContentControl("Web:KrokiDiagram(payload=\"SGVsbG8gV29ybGQ=\")");
            var tag = new RoboClerkDocxTag(sdt, config);

            Assert.That(tag.GetParameterOrDefault("payload"), Is.EqualTo("SGVsbG8gV29ybGQ="));
        }

        [UnitTestAttribute(
            Identifier = "50BC5FD0-284E-4C4D-8400-A8731E51244E",
            Purpose = "Parse tag with multiple parameters including quoted ones",
            PostCondition = "All parameters are parsed correctly")]
        [Test]
        public void ParseTagWithMultipleParameters()
        {
            var sdt = CreateTestContentControl("Web:KrokiDiagram(type=plantuml,format=png,caption=\"Diagram, v1\",xDim=500)");
            var tag = new RoboClerkDocxTag(sdt, config);

            Assert.That(tag.Source, Is.EqualTo(DataSource.Web));
            Assert.That(tag.ContentCreatorID, Is.EqualTo("KrokiDiagram"));
            Assert.That(tag.GetParameterOrDefault("type"), Is.EqualTo("plantuml"));
            Assert.That(tag.GetParameterOrDefault("format"), Is.EqualTo("png"));
            Assert.That(tag.GetParameterOrDefault("caption"), Is.EqualTo("Diagram, v1"));
            Assert.That(tag.GetParameterOrDefault("xDim"), Is.EqualTo("500"));
        }

        [UnitTestAttribute(
            Identifier = "AE38B329-A5EB-4DE2-851E-7A6C5C51B622",
            Purpose = "Parse tag with case-insensitive data source",
            PostCondition = "Data source is recognized regardless of case")]
        [Test]
        public void ParseTagWithCaseInsensitiveSource()
        {
            var sdt = CreateTestContentControl("wEB:KrokiDiagram()");
            var tag = new RoboClerkDocxTag(sdt, config);

            Assert.That(tag.Source, Is.EqualTo(DataSource.Web));
            Assert.That(tag.ContentCreatorID, Is.EqualTo("KrokiDiagram"));
        }

        [UnitTestAttribute(
            Identifier = "2AF2B7C9-B031-47A1-BABF-56EE9E1323AD",
            Purpose = "Verify HasParameter method",
            PostCondition = "HasParameter returns correct values")]
        [Test]
        public void VerifyHasParameterMethod()
        {
            var sdt = CreateTestContentControl("Trace:SWR(id=123)");
            var tag = new RoboClerkDocxTag(sdt, config);

            Assert.That(tag.HasParameter("id"), Is.True);
            Assert.That(tag.HasParameter("nonexistent"), Is.False);
        }

        [UnitTestAttribute(
            Identifier = "F630EE04-E05C-420F-B35D-EFE2A1625B4B",
            Purpose = "Parse invalid tag format",
            PostCondition = "TagInvalidException is thrown")]
        [Test]
        public void ParseInvalidTagFormat()
        {
            var sdt = CreateTestContentControl("InvalidFormat");
            Assert.Throws<TagInvalidException>(() => new RoboClerkDocxTag(sdt, config));
        }
    }
}
