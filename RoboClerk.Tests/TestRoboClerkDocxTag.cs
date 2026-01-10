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

        [UnitTestAttribute(
            Identifier = "f3a8d98f-ff09-467b-a666-a6c5314af79f",
            Purpose = "UpdateTagDefinition updates the tag properties correctly",
            PostCondition = "Tag properties are updated from the new definition")]
        [Test]
        public void UpdateTagDefinition_UpdatesProperties()
        {
            var sdt = CreateTestContentControl("Config:SoftwareName()");
            var tag = new RoboClerkDocxTag(sdt, config);

            tag.UpdateTagDefinition("Trace:SWR(id=123)");

            Assert.That(tag.Source, Is.EqualTo(DataSource.Trace));
            Assert.That(tag.ContentCreatorID, Is.EqualTo("SWR"));
            Assert.That(tag.GetParameterOrDefault("id"), Is.EqualTo("123"));
        }

        [UnitTestAttribute(
            Identifier = "525889e6-d01e-45e9-bece-82799b335c69",
            Purpose = "ConvertContentToOpenXml handles plain text content",
            PostCondition = "Content control is updated with plain text")]
        [Test]
        public void ConvertContentToOpenXml_PlainText()
        {
            var sdt = CreateTestContentControl("Config:SoftwareName()", "Old Content");
            var tag = new RoboClerkDocxTag(sdt, config);
            tag.UpdateContent("New Plain Text Content");

            tag.ConvertContentToOpenXml();

            var content = sdt.Descendants<Text>().Select(t => t.Text).Aggregate((a, b) => a + b);
            Assert.That(content, Is.EqualTo("New Plain Text Content"));
        }

        [UnitTestAttribute(
            Identifier = "dc864b2e-36d3-4790-935d-226803e44213",
            Purpose = "ConvertContentToOpenXml handles OpenXML content",
            PostCondition = "Content control is updated with OpenXML content")]
        [Test]
        public void ConvertContentToOpenXml_OpenXmlContent()
        {
            var sdt = CreateTestContentControl("Config:SoftwareName()", "Old Content");
            var tag = new RoboClerkDocxTag(sdt, config);
            var openXmlContent = "<!--OPENXML_CONTENT--><w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t>OpenXML Content</w:t></w:r></w:p>";
            tag.UpdateContent(openXmlContent);

            tag.ConvertContentToOpenXml();

            var content = sdt.Descendants<Text>().Select(t => t.Text).Aggregate((a, b) => a + b);
            Assert.That(content, Is.EqualTo("OpenXML Content"));
        }

        [UnitTestAttribute(
            Identifier = "d375f6ce-9fe8-4c8c-86b6-890fe4e65e64",
            Purpose = "ConvertContentToOpenXml preserves original formatting",
            PostCondition = "Original formatting is preserved in the new content")]
        [Test]
        public void ConvertContentToOpenXml_PreservesFormatting()
        {
            var sdt = CreateTestContentControl("Config:SoftwareName()", "Old Content");
            
            // Add some formatting to the original content
            var run = sdt.Descendants<Run>().First();
            run.RunProperties = new RunProperties(new Bold());
            var paragraph = sdt.Descendants<Paragraph>().First();
            paragraph.ParagraphProperties = new ParagraphProperties(new Justification { Val = JustificationValues.Center });

            var tag = new RoboClerkDocxTag(sdt, config);
            tag.UpdateContent("New Content");

            tag.ConvertContentToOpenXml();

            var newRun = sdt.Descendants<Run>().First();
            Assert.That(newRun.RunProperties.HasChildren, Is.True);
            Assert.That(newRun.RunProperties.Descendants<Bold>().Any(), Is.True);

            var newParagraph = sdt.Descendants<Paragraph>().First();
            Assert.That(newParagraph.ParagraphProperties.HasChildren, Is.True);
            Assert.That(newParagraph.ParagraphProperties.Descendants<Justification>().Any(), Is.True);
            Assert.That(newParagraph.ParagraphProperties.Descendants<Justification>().First().Val.Value, Is.EqualTo(JustificationValues.Center));
        }

        #region ConvertHtmlToOpenXml and ParseOpenXmlElement Tests

        [UnitTestAttribute(
            Identifier = "2a0d27d3-afdc-4552-9459-3e8746d90f31",
            Purpose = "ConvertContentToOpenXml handles basic HTML content",
            PostCondition = "HTML content is converted to OpenXML and inserted into content control")]
        [Test]
        public void ConvertContentToOpenXml_BasicHtml()
        {
            using var ms = new MemoryStream();
            using var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document);
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());

            var sdt = new SdtBlock(
                new SdtProperties(
                    new SdtId { Val = 12345678 },
                    new Tag { Val = "Config:SoftwareName()" }
                ),
                new SdtContentBlock(
                    new Paragraph(new Run(new Text("Old Content")))
                )
            );
            mainPart.Document.Body.AppendChild(sdt);

            var tag = new RoboClerkDocxTag(sdt, config);
            tag.UpdateContent("<p>Simple HTML paragraph</p>");

            tag.ConvertContentToOpenXml();

            var paragraphs = sdt.Descendants<Paragraph>().ToList();
            Assert.That(paragraphs.Count, Is.GreaterThan(0));
            var allText = string.Join("", sdt.Descendants<Text>().Select(t => t.Text));
            Assert.That(allText, Does.Contain("Simple HTML paragraph"));
        }

        [UnitTestAttribute(
            Identifier = "5831e5c7-5326-4b59-8aa2-4ba6a8e42b59",
            Purpose = "ConvertContentToOpenXml handles HTML with bold and italic formatting",
            PostCondition = "HTML formatting tags are converted to corresponding OpenXML run properties")]
        [Test]
        public void ConvertContentToOpenXml_HtmlWithFormatting()
        {
            using var ms = new MemoryStream();
            using var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document);
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());

            var sdt = new SdtBlock(
                new SdtProperties(
                    new SdtId { Val = 12345678 },
                    new Tag { Val = "Config:SoftwareName()" }
                ),
                new SdtContentBlock(
                    new Paragraph(new Run(new Text("Old")))
                )
            );
            mainPart.Document.Body.AppendChild(sdt);

            var tag = new RoboClerkDocxTag(sdt, config);
            tag.UpdateContent("<p><strong>Bold</strong> and <em>Italic</em></p>");

            tag.ConvertContentToOpenXml();

            var allText = string.Join("", sdt.Descendants<Text>().Select(t => t.Text));
            Assert.That(allText, Does.Contain("Bold"));
            Assert.That(allText, Does.Contain("Italic"));

            var runs = sdt.Descendants<Run>().ToList();
            Assert.That(runs.Any(r => r.RunProperties?.GetFirstChild<Bold>() != null), Is.True);
            Assert.That(runs.Any(r => r.RunProperties?.GetFirstChild<Italic>() != null), Is.True);
        }

        [UnitTestAttribute(
            Identifier = "6c6c8996-3aa4-4a33-a1d9-c3826944b3d2",
            Purpose = "ConvertContentToOpenXml handles HTML list",
            PostCondition = "HTML list is converted to OpenXML paragraphs")]
        [Test]
        public void ConvertContentToOpenXml_HtmlList()
        {
            using var ms = new MemoryStream();
            using var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document);
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());

            var sdt = new SdtBlock(
                new SdtProperties(
                    new SdtId { Val = 12345678 },
                    new Tag { Val = "Config:SoftwareName()" }
                ),
                new SdtContentBlock(
                    new Paragraph(new Run(new Text("Old")))
                )
            );
            mainPart.Document.Body.AppendChild(sdt);

            var tag = new RoboClerkDocxTag(sdt, config);
            tag.UpdateContent("<ul><li>Item 1</li><li>Item 2</li></ul>");

            tag.ConvertContentToOpenXml();

            var allText = string.Join(" ", sdt.Descendants<Text>().Select(t => t.Text));
            Assert.That(allText, Does.Contain("Item 1"));
            Assert.That(allText, Does.Contain("Item 2"));
        }

        [UnitTestAttribute(
            Identifier = "8d885e8c-c7c9-4c46-8bf3-ad6479c747dd",
            Purpose = "ConvertContentToOpenXml falls back to plain text on invalid HTML",
            PostCondition = "Invalid HTML is treated as plain text")]
        [Test]
        public void ConvertContentToOpenXml_InvalidHtmlFallback()
        {
            var sdt = CreateTestContentControl("Config:SoftwareName()", "Old");
            var tag = new RoboClerkDocxTag(sdt, config);
            
            tag.UpdateContent("<p>Some HTML</p>");
            tag.ConvertContentToOpenXml();

            var allText = string.Join("", sdt.Descendants<Text>().Select(t => t.Text));
            Assert.That(allText, Does.Contain("<p>Some HTML</p>"));
        }

        [UnitTestAttribute(
            Identifier = "8667d0e0-2b83-4e73-b3c6-4c976c14c4f7",
            Purpose = "ConvertContentToOpenXml handles OpenXML content with table element",
            PostCondition = "OpenXML table is parsed and inserted correctly")]
        [Test]
        public void ConvertContentToOpenXml_OpenXmlTable()
        {
            var sdt = CreateTestContentControl("Config:SoftwareName()", "Old");
            var tag = new RoboClerkDocxTag(sdt, config);
            
            var openXmlContent = @"<!--OPENXML_CONTENT-->
<w:tbl xmlns:w=""http://schemas.openxmlformats.org/wordprocessingml/2006/main"">
    <w:tr><w:tc><w:p><w:r><w:t>Cell1</w:t></w:r></w:p></w:tc></w:tr>
</w:tbl>";
            tag.UpdateContent(openXmlContent);

            tag.ConvertContentToOpenXml();

            var tables = sdt.Descendants<Table>().ToList();
            Assert.That(tables.Count, Is.EqualTo(1));
            
            var allText = string.Join(" ", sdt.Descendants<Text>().Select(t => t.Text));
            Assert.That(allText, Does.Contain("Cell1"));
        }

        #endregion
    }
}
