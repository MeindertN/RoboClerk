using NUnit.Framework;
using RoboClerk.Core;
using RoboClerk.Core.ASCIIDOCSupport;
using System;
using System.Linq;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("These tests test the RoboClerkTextTag class and its base class RoboClerkBaseTag")]
    internal class TestRoboClerkTextTag
    {
        [UnitTestAttribute(
            Identifier = "167984DD-B825-4F41-B2F1-C20EBF278D1D",
            Purpose = "Parse a simple inline tag with basic parameters",
            PostCondition = "Tag is parsed correctly with all properties set")]
        [Test]
        public void ParseSimpleInlineTag()
        {
            string document = "Some text @@Config:SoftwareName()@@ more text";
            var tag = new RoboClerkTextTag(10, 33, document, true);

            Assert.That(tag.Inline, Is.True);
            Assert.That(tag.Source, Is.EqualTo(DataSource.Config));
            Assert.That(tag.ContentCreatorID, Is.EqualTo("SoftwareName"));
            Assert.That(tag.Contents, Is.EqualTo("Config:SoftwareName()"));
            Assert.That(tag.TagStart, Is.EqualTo(10));
            Assert.That(tag.TagEnd, Is.EqualTo(34));
        }

        [UnitTestAttribute(
            Identifier = "5AABCCAD-2666-494F-9B8F-44E5B0A5A22A",
            Purpose = "Parse an inline tag with parameters",
            PostCondition = "Tag parameters are extracted correctly")]
        [Test]
        public void ParseInlineTagWithParameters()
        {
            string document = "Text @@Trace:SWR(id=89)@@ more";
            var tag = new RoboClerkTextTag(5, 24, document, true);

            Assert.That(tag.Inline, Is.True);
            Assert.That(tag.Source, Is.EqualTo(DataSource.Trace));
            Assert.That(tag.ContentCreatorID, Is.EqualTo("SWR"));
            Assert.That(tag.GetParameterOrDefault("id"), Is.EqualTo("89"));
            Assert.That(tag.HasParameter("id"), Is.True);
            Assert.That(tag.HasParameter("nonexistent"), Is.False);
        }

        [UnitTestAttribute(
            Identifier = "F24FFF63-A6E1-45B0-BD52-6483A5961A20",
            Purpose = "Parse a container tag without content",
            PostCondition = "Empty container tag is parsed correctly")]
        [Test]
        public void ParseEmptyContainerTag()
        {
            string document = "@@@Comment:Note()\n@@@";
            var tag = new RoboClerkTextTag(0, 18, document, false);

            Assert.That(tag.Inline, Is.False);
            Assert.That(tag.Source, Is.EqualTo(DataSource.Comment));
            Assert.That(tag.ContentCreatorID, Is.EqualTo("Note"));
            Assert.That(tag.Contents, Is.EqualTo(string.Empty));
            Assert.That(tag.TagStart, Is.EqualTo(0));
            Assert.That(tag.TagEnd, Is.EqualTo(20));
        }

        [UnitTestAttribute(
            Identifier = "C8AB33D8-EE4C-4297-9C00-17495638A392",
            Purpose = "Parse a container tag with content",
            PostCondition = "Container tag content is extracted correctly")]
        [Test]
        public void ParseContainerTagWithContent()
        {
            string document = "@@@Comment:Note()\nThis is the content\nMore content\n@@@";
            var tag = new RoboClerkTextTag(0, 51, document, false);

            Assert.That(tag.Inline, Is.False);
            Assert.That(tag.Source, Is.EqualTo(DataSource.Comment));
            Assert.That(tag.ContentCreatorID, Is.EqualTo("Note"));
            Assert.That(tag.Contents, Is.EqualTo("This is the content\nMore content\n"));
            Assert.That(tag.ContentStart, Is.EqualTo(18));
        }

        [UnitTestAttribute(
            Identifier = "2DB67357-EA42-4ABC-9DC1-F3C6B5991573",
            Purpose = "Parse tag with quoted parameters containing commas",
            PostCondition = "Quoted parameters are parsed correctly with commas preserved")]
        [Test]
        public void ParseTagWithQuotedParameters()
        {
            string document = "@@Web:KrokiDiagram(type=plantuml,caption=\"My diagram, version 1\")@@";
            var tag = new RoboClerkTextTag(0, 66, document, true);

            Assert.That(tag.Source, Is.EqualTo(DataSource.Web));
            Assert.That(tag.ContentCreatorID, Is.EqualTo("KrokiDiagram"));
            Assert.That(tag.GetParameterOrDefault("type"), Is.EqualTo("plantuml"));
            Assert.That(tag.GetParameterOrDefault("caption"), Is.EqualTo("My diagram, version 1"));
        }

        [UnitTestAttribute(
            Identifier = "7B06CD15-4330-460E-992F-97EA9A62C995",
            Purpose = "Parse tag with quoted parameters containing escaped quotes",
            PostCondition = "Escaped quotes in parameters are handled correctly")]
        [Test]
        public void ParseTagWithEscapedQuotes()
        {
            string document = "@@Web:KrokiDiagram(caption=\"Diagram \\\"v1\\\"\")@@";
            var tag = new RoboClerkTextTag(0, 46, document, true);

            Assert.That(tag.Source, Is.EqualTo(DataSource.Web));
            Assert.That(tag.GetParameterOrDefault("caption"), Is.EqualTo("Diagram \"v1\""));
        }

        [UnitTestAttribute(
            Identifier = "4EF73CE8-7907-4892-8785-3F2D83A62828",
            Purpose = "Parse tag with Base64 parameter containing equals signs",
            PostCondition = "Base64 parameter with padding is preserved")]
        [Test]
        public void ParseTagWithBase64Parameter()
        {
            string document = "@@Web:KrokiDiagram(payload=SGVsbG8gV29ybGQ=)@@";
            var tag = new RoboClerkTextTag(0, 44, document, true);

            Assert.That(tag.Source, Is.EqualTo(DataSource.Web));
            Assert.That(tag.GetParameterOrDefault("payload"), Is.EqualTo("SGVsbG8gV29ybGQ="));
        }

        [UnitTestAttribute(
            Identifier = "0CD55DBA-1647-4DFF-9BBA-C75E58919362",
            Purpose = "Parse tag with quoted Base64 parameter",
            PostCondition = "Quoted Base64 parameter is handled correctly")]
        [Test]
        public void ParseTagWithQuotedBase64Parameter()
        {
            string document = "@@Web:KrokiDiagram(payload=\"SGVsbG8gV29ybGQ=\")@@";
            var tag = new RoboClerkTextTag(0, 46, document, true);

            Assert.That(tag.Source, Is.EqualTo(DataSource.Web));
            Assert.That(tag.GetParameterOrDefault("payload"), Is.EqualTo("SGVsbG8gV29ybGQ="));
        }

        [UnitTestAttribute(
            Identifier = "8CB3258C-486C-484A-94F6-3A99521BCF7A",
            Purpose = "Parse tag with multiple parameters including quoted ones",
            PostCondition = "All parameters are parsed correctly")]
        [Test]
        public void ParseTagWithMultipleParameters()
        {
            string document = "@@Web:KrokiDiagram(type=plantuml,format=png,caption=\"Diagram, v1\",xDim=500)@@";
            var tag = new RoboClerkTextTag(0, 76, document, true);

            Assert.That(tag.Source, Is.EqualTo(DataSource.Web));
            Assert.That(tag.ContentCreatorID, Is.EqualTo("KrokiDiagram"));
            Assert.That(tag.GetParameterOrDefault("type"), Is.EqualTo("plantuml"));
            Assert.That(tag.GetParameterOrDefault("format"), Is.EqualTo("png"));
            Assert.That(tag.GetParameterOrDefault("caption"), Is.EqualTo("Diagram, v1"));
            Assert.That(tag.GetParameterOrDefault("xDim"), Is.EqualTo("500"));
        }

        [UnitTestAttribute(
            Identifier = "030F74DD-5748-41E7-B793-54F10C03AF46",
            Purpose = "Parse tag with case-insensitive data source",
            PostCondition = "Data source is recognized regardless of case")]
        [Test]
        public void ParseTagWithCaseInsensitiveSource()
        {
            string document = "@@wEB:KrokiDiagram()@@";
            var tag = new RoboClerkTextTag(0, 20, document, true);

            Assert.That(tag.Source, Is.EqualTo(DataSource.Web));
            Assert.That(tag.ContentCreatorID, Is.EqualTo("KrokiDiagram"));
        }

        [UnitTestAttribute(
            Identifier = "D6EA5CA3-2FF6-4917-A5FC-57A9861E8C61",
            Purpose = "Parse tag with case-insensitive parameter names",
            PostCondition = "Parameters are stored with uppercase keys")]
        [Test]
        public void ParseTagWithCaseInsensitiveParameters()
        {
            string document = "@@Web:KrokiDiagram(TyPe=plantuml,FoRmAt=png)@@";
            var tag = new RoboClerkTextTag(0, 44, document, true);

            Assert.That(tag.GetParameterOrDefault("type"), Is.EqualTo("plantuml"));
            Assert.That(tag.GetParameterOrDefault("TYPE"), Is.EqualTo("plantuml"));
            Assert.That(tag.GetParameterOrDefault("format"), Is.EqualTo("png"));
            Assert.That(tag.GetParameterOrDefault("FORMAT"), Is.EqualTo("png"));
        }

        [UnitTestAttribute(
            Identifier = "4A00A34E-AD0E-4E3C-AE6B-E5AA52789C05",
            Purpose = "Parse tag with whitespace in parameters",
            PostCondition = "Whitespace is trimmed from parameter keys and values")]
        [Test]
        public void ParseTagWithWhitespaceInParameters()
        {
            string document = "@@Web:KrokiDiagram( type = plantuml , format = png )@@";
            var tag = new RoboClerkTextTag(0, 53, document, true);

            Assert.That(tag.GetParameterOrDefault("type"), Is.EqualTo("plantuml"));
            Assert.That(tag.GetParameterOrDefault("format"), Is.EqualTo("png"));
        }

        [UnitTestAttribute(
            Identifier = "B8B879CE-252A-454E-B45E-05BD04D8C3A4",
            Purpose = "Parse tag and verify GetParameterOrDefault with default value",
            PostCondition = "Default value is returned for non-existent parameter")]
        [Test]
        public void TestGetParameterOrDefaultWithDefaultValue()
        {
            string document = "@@Web:KrokiDiagram(type=plantuml)@@";
            var tag = new RoboClerkTextTag(0, 33, document, true);

            Assert.That(tag.GetParameterOrDefault("type", "default"), Is.EqualTo("plantuml"));
            Assert.That(tag.GetParameterOrDefault("nonexistent", "default"), Is.EqualTo("default"));
            Assert.That(tag.GetParameterOrDefault("nonexistent"), Is.EqualTo(string.Empty));
        }

        [UnitTestAttribute(
            Identifier = "EE82657D-EEB6-4A55-B95A-C79FC5B5983E",
            Purpose = "Parse tag and verify Parameters property",
            PostCondition = "All parameter keys are returned")]
        [Test]
        public void TestParametersProperty()
        {
            string document = "@@Web:KrokiDiagram(type=plantuml,format=png,xDim=500)@@";
            var tag = new RoboClerkTextTag(0, 53, document, true);

            var parameters = tag.Parameters.ToList();
            Assert.That(parameters.Count, Is.EqualTo(3));
            Assert.That(parameters, Does.Contain("TYPE"));
            Assert.That(parameters, Does.Contain("FORMAT"));
            Assert.That(parameters, Does.Contain("XDIM"));
        }

        [UnitTestAttribute(
            Identifier = "316AFAC7-769B-4543-996A-FEEFDEA85E53",
            Purpose = "Parse invalid tag with malformed structure",
            PostCondition = "TagInvalidException is thrown")]
        [Test]
        public void ParseInvalidTag()
        {
            string document = "@@InvalidTag@@";
            Assert.Throws<TagInvalidException>(() => new RoboClerkTextTag(0, 12, document, true));
        }

        [UnitTestAttribute(
            Identifier = "A8C06B0B-A5EB-4C4A-AF2B-B5F9D1F7825B",
            Purpose = "Parse tag with unknown data source",
            PostCondition = "TagInvalidException is thrown")]
        [Test]
        public void ParseTagWithUnknownSource()
        {
            string document = "@@Unknown:ContentCreator()@@";
            Assert.Throws<TagInvalidException>(() => new RoboClerkTextTag(0, 26, document, true));
        }

        [UnitTestAttribute(
            Identifier = "849B70BE-6D85-4CEC-873E-40386F8CB2EE",
            Purpose = "Test UpdateContent method",
            PostCondition = "Content is updated successfully")]
        [Test]
        public void TestUpdateContent()
        {
            string document = "@@Config:SoftwareName()@@";
            var tag = new RoboClerkTextTag(0, 23, document, true);

            tag.UpdateContent("New Content");
            Assert.That(tag.Contents, Is.EqualTo("New Content"));
        }

        [UnitTestAttribute(
            Identifier = "EBE4D86B-EF18-4641-834E-D83CC51139CD",
            Purpose = "Test ProcessNestedTags with no nested tags",
            PostCondition = "Empty collection is returned")]
        [Test]
        public void TestProcessNestedTagsEmpty()
        {
            string document = "@@@Comment:Note()\nNo nested tags\n@@@";
            // Extract tags using the parser to get correct indices
            var tags = RoboClerkTextParser.ExtractRoboClerkTags(document);
            Assert.That(tags.Count, Is.EqualTo(1));
            
            var tag = tags[0];
            var nestedTags = tag.ProcessNestedTags();
            Assert.That(nestedTags, Is.Empty);
        }

        [UnitTestAttribute(
            Identifier = "7F6B946C-B596-4361-AA83-B82DCA027964",
            Purpose = "Test ProcessNestedTags with nested tags",
            PostCondition = "Nested tags are extracted correctly")]
        [Test]
        public void TestProcessNestedTagsWithContent()
        {
            // Create a simple container tag with text content that contains nested tag syntax
            string document = "@@@Comment:Note()\nSome text with nested syntax\n@@@";
            var tag = new RoboClerkTextTag(0, document.IndexOf("@@@", 3), document, false);
            
            // Update the contents to include actual nested tags
            tag.UpdateContent("@@Config:SoftwareName()@@ and @@Config:Version()@@");
            
            var nestedTags = tag.ProcessNestedTags().ToList();
            Assert.That(nestedTags.Count, Is.EqualTo(2));
            Assert.That(nestedTags[0].Source, Is.EqualTo(DataSource.Config));
            Assert.That(nestedTags[0].ContentCreatorID, Is.EqualTo("SoftwareName"));
            Assert.That(nestedTags[1].Source, Is.EqualTo(DataSource.Config));
            Assert.That(nestedTags[1].ContentCreatorID, Is.EqualTo("Version"));
        }
    }
}
