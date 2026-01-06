using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using NSubstitute;
using NUnit.Framework;
using RoboClerk.ContentCreators;
using RoboClerk.Core;
using RoboClerk.Core.Configuration;
using RoboClerk.Core.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("These tests test the TemplateSection Content Creator")]
    internal class TestTemplateSection
    {
        private IConfiguration config = null;
        private IDataSources dataSources = null;
        private ITraceabilityAnalysis traceAnalysis = null;
        private IFileProviderPlugin fileProvider = null;

        [SetUp]
        public void TestSetup()
        {
            config = Substitute.For<IConfiguration>();
            dataSources = Substitute.For<IDataSources>();
            traceAnalysis = Substitute.For<ITraceabilityAnalysis>();
            fileProvider = Substitute.For<IFileProviderPlugin>();
        }

        [UnitTestAttribute(
        Identifier = "638be59f-c802-4796-b048-6593964a63ee",
        Purpose = "TemplateSection content creator is created",
        PostCondition = "No exception is thrown")]
        [Test]
        public void TestTemplateSectionCreation()
        {
            var creator = new TemplateSection(dataSources, traceAnalysis, config);
            Assert.That(creator, Is.Not.Null);
        }

        [UnitTestAttribute(
        Identifier = "ec85521a-d001-475c-ae30-251357ae8cd5",
        Purpose = "GetMetadata returns correct metadata without config",
        PostCondition = "Metadata is returned with default values")]
        [Test]
        public void TestGetMetadata_NoConfig()
        {
            var metadata = TemplateSection.GetMetadata();
            Assert.That(metadata.Name, Is.EqualTo("Template Section"));
            Assert.That(metadata.Source, Is.EqualTo("FILE"));
            Assert.That(metadata.Tags, Has.Count.EqualTo(1));
            Assert.That(metadata.Tags[0].TagID, Is.EqualTo("TemplateSection"));
            Assert.That(metadata.Tags[0].Parameters, Has.Count.EqualTo(1));
            Assert.That(metadata.Tags[0].Parameters[0].Name, Is.EqualTo("fileName"));
            Assert.That(metadata.Tags[0].Parameters[0].AllowedValues, Is.Null);
        }

        [UnitTestAttribute(
        Identifier = "93ea69e0-6639-4032-bd0b-b1800b1e0273",
        Purpose = "GetMetadata returns correct metadata with config and file provider",
        PostCondition = "Metadata is returned with allowed values populated")]
        [Test]
        public void TestGetMetadata_WithConfig()
        {
            config.OutputFormat.Returns("DOCX");
            config.TemplateDir.Returns("templates");
            fileProvider.DirectoryExists("templates").Returns(true);
            fileProvider.GetFiles("templates", "*.docx", SearchOption.TopDirectoryOnly).Returns(new[] { "templates/test.docx" });
            fileProvider.GetFileName("templates/test.docx").Returns("test.docx");

            var metadata = TemplateSection.GetMetadata(config, fileProvider);
            
            Assert.That(metadata.Tags[0].Parameters[0].AllowedValues, Is.Not.Null);
            Assert.That(metadata.Tags[0].Parameters[0].AllowedValues, Contains.Item("test.docx"));
            Assert.That(metadata.Tags[0].Parameters[0].ExampleValue, Is.EqualTo("test.docx"));
        }

        [UnitTestAttribute(
        Identifier = "ae187361-c13a-4e05-b66d-242c528e3302",
        Purpose = "GetContent throws TagInvalidException when filename parameter is missing",
        PostCondition = "TagInvalidException is thrown")]
        [Test]
        public void TestGetContent_MissingFilename()
        {
            var creator = new TemplateSection(dataSources, traceAnalysis, config);
            var tag = Substitute.For<IRoboClerkTag>();
            tag.GetParameterOrDefault("FILENAME", string.Empty).Returns(string.Empty);
            tag.Contents.Returns("@@FILE:TemplateSection()@@");
            var doc = new DocumentConfig("roboclerkID", "documentID", "documentTitle", "documentAbbreviation", "documentTemplate");

            Assert.Throws<TagInvalidException>(() => creator.GetContent(tag, doc));
        }

        [UnitTestAttribute(
        Identifier = "ab183966-7d92-4e3c-b277-719d1bd79eab",
        Purpose = "GetContent returns text content for non-DOCX file",
        PostCondition = "Content is returned from data source")]
        [Test]
        public void TestGetContent_TextFile()
        {
            var creator = new TemplateSection(dataSources, traceAnalysis, config);
            var tag = Substitute.For<IRoboClerkTag>();
            tag.GetParameterOrDefault("FILENAME", string.Empty).Returns("test.adoc");
            var doc = new DocumentConfig("roboclerkID", "documentID", "documentTitle", "documentAbbreviation", "documentTemplate");
            
            dataSources.GetTemplateFile("test.adoc").Returns("Some content");

            var result = creator.GetContent(tag, doc);
            Assert.That(result, Is.EqualTo("Some content"));
        }

        [UnitTestAttribute(
        Identifier = "9914b850-72a2-45b6-af03-c5dba7459430",
        Purpose = "GetContent throws TagInvalidException when inserting DOCX into non-DOCX output",
        PostCondition = "TagInvalidException is thrown")]
        [Test]
        public void TestGetContent_DocxInNonDocx()
        {
            config.OutputFormat.Returns("ASCIIDOC");
            var creator = new TemplateSection(dataSources, traceAnalysis, config);
            var tag = Substitute.For<IRoboClerkTag>();
            tag.GetParameterOrDefault("FILENAME", string.Empty).Returns("test.docx");
            tag.Contents.Returns("@@FILE:TemplateSection(fileName=test.docx)@@");
            var doc = new DocumentConfig("roboclerkID", "documentID", "documentTitle", "documentAbbreviation", "documentTemplate");

            Assert.Throws<TagInvalidException>(() => creator.GetContent(tag, doc));
        }

        [UnitTestAttribute(
        Identifier = "c07b9129-1c85-4293-8aab-71d02aa73687",
        Purpose = "GetContent extracts OpenXML content from DOCX file when output format is DOCX",
        PostCondition = "OpenXML content is returned")]
        [Test]
        public void TestGetContent_DocxInDocx()
        {
            config.OutputFormat.Returns("DOCX");
            var creator = new TemplateSection(dataSources, traceAnalysis, config);
            var tag = Substitute.For<IRoboClerkTag>();
            tag.GetParameterOrDefault("FILENAME", string.Empty).Returns("test.docx");
            var doc = new DocumentConfig("roboclerkID", "documentID", "documentTitle", "documentAbbreviation", "documentTemplate");

            // Create a valid DOCX in memory
            using (var ms = new MemoryStream())
            {
                using (var wordDoc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
                {
                    var mainPart = wordDoc.AddMainDocumentPart();
                    mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
                    var body = mainPart.Document.AppendChild(new Body());
                    var para = body.AppendChild(new Paragraph());
                    var run = para.AppendChild(new Run());
                    run.AppendChild(new Text("Hello World"));
                }
                
                // Reset stream position for reading
                var docBytes = ms.ToArray();
                var readStream = new MemoryStream(docBytes);
                
                dataSources.GetFileStreamFromTemplateDir("test.docx").Returns(readStream);

                var result = creator.GetContent(tag, doc);
                
                Assert.That(result, Does.Contain("<!--OPENXML_CONTENT-->"));
                Assert.That(result, Does.Contain("<w:t>Hello World</w:t>"));
            }
        }

        [UnitTestAttribute(
        Identifier = "53064846-3309-4a51-a1e2-1eb0b752b092",
        Purpose = "GetContent handles exception when loading text file",
        PostCondition = "Exception is rethrown")]
        [Test]
        public void TestGetContent_TextFileError()
        {
            var creator = new TemplateSection(dataSources, traceAnalysis, config);
            var tag = Substitute.For<IRoboClerkTag>();
            tag.GetParameterOrDefault("FILENAME", string.Empty).Returns("test.adoc");
            var doc = new DocumentConfig("roboclerkID", "documentID", "documentTitle", "documentAbbreviation", "documentTemplate");
            
            dataSources.When(x => x.GetTemplateFile("test.adoc")).Do(x => { throw new FileNotFoundException(); });

            Assert.Throws<FileNotFoundException>(() => creator.GetContent(tag, doc));
        }

        [UnitTestAttribute(
        Identifier = "f863ceb3-76a2-416f-a426-efe3d87fd900",
        Purpose = "GetContent handles exception when extracting OpenXML",
        PostCondition = "Exception is rethrown")]
        [Test]
        public void TestGetContent_DocxError()
        {
            config.OutputFormat.Returns("DOCX");
            var creator = new TemplateSection(dataSources, traceAnalysis, config);
            var tag = Substitute.For<IRoboClerkTag>();
            tag.GetParameterOrDefault("FILENAME", string.Empty).Returns("test.docx");
            var doc = new DocumentConfig("roboclerkID", "documentID", "documentTitle", "documentAbbreviation", "documentTemplate");

            dataSources.When(x => x.GetFileStreamFromTemplateDir("test.docx")).Do(x => { throw new FileNotFoundException(); });

            Assert.Throws<FileNotFoundException>(() => creator.GetContent(tag, doc));
        }
    }
}
