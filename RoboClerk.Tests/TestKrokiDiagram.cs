using NSubstitute;
using NUnit.Framework;
using RoboClerk.ContentCreators;
using RoboClerk.Core;
using RoboClerk.Core.ASCIIDOCSupport;
using RoboClerk.Core.Configuration;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("These tests test the Kroki Diagram Content Creator")]
    internal class TestKrokiDiagram
    {
        private IConfiguration config = null;
        private IDataSources dataSources = null;
        private ITraceabilityAnalysis traceAnalysis = null;
        private IFileProviderPlugin fs = null;
        private IWebResources webResources = null;

        [SetUp]
        public void TestSetup()
        {
            config = Substitute.For<IConfiguration>();
            dataSources = Substitute.For<IDataSources>();
            traceAnalysis = Substitute.For<ITraceabilityAnalysis>();
            fs = Substitute.For<IFileProviderPlugin>();
            webResources = Substitute.For<IWebResources>();
        }

        [UnitTestAttribute(
        Identifier = "c8879078-bc7c-44f3-929f-4a01742c6e78",
        Purpose = "Kroki content creator is created",
        PostCondition = "No exception is thrown")]
        [Test]
        public void TestKrokiDiagram1()
        {
            var test = new KrokiDiagram(dataSources, traceAnalysis, config, fs, webResources);
        }

        [UnitTestAttribute(
        Identifier = "9a059772-3256-44c1-863d-48321327f174",
        Purpose = "Kroki content creator generates HTML image tag",
        PostCondition = "Expected HTML tag is returned")]
        [Test]
        public void TestKrokiDiagram2()
        {
            config.OutputFormat.Returns("HTML");
            var obj = new KrokiDiagram(dataSources, traceAnalysis, config, fs, webResources);
            
            var tag = Substitute.For<IRoboClerkTag>();
            tag.GetParameterOrDefault("type", "plantuml").Returns("plantuml");
            tag.GetParameterOrDefault("format", "png").Returns("png");
            tag.GetParameterOrDefault("caption", string.Empty).Returns("Test Diagram");
            tag.GetParameterOrDefault("xDim", string.Empty).Returns("");
            tag.GetParameterOrDefault("yDim", string.Empty).Returns("");
            tag.GetParameterOrDefault("payload", string.Empty).Returns("");
            tag.Contents.Returns("@startuml\nA->B\n@enduml");

            var doc = new DocumentConfig("roboclerkID", "documentID", "documentTitle", "documentAbbreviation", "documentTemplate");
            
            byte[] imageBytes = Encoding.UTF8.GetBytes("fake image data");
            webResources.DownloadImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(imageBytes));

            var result = obj.GetContent(tag, doc);
            
            Assert.That(result, Does.Contain("<img src=\"data:image/png;base64,"));
            Assert.That(result, Does.Contain("alt=\"Test Diagram\""));
            Assert.That(result, Does.Contain("<figcaption>Test Diagram</figcaption>"));
        }

        [UnitTestAttribute(
        Identifier = "69505620-6efa-42af-b2eb-c644182fb319",
        Purpose = "Kroki content creator handles download failure gracefully",
        PostCondition = "Error message is returned")]
        [Test]
        public void TestKrokiDiagram3()
        {
            config.OutputFormat.Returns("HTML");
            var obj = new KrokiDiagram(dataSources, traceAnalysis, config, fs, webResources);
            
            var tag = Substitute.For<IRoboClerkTag>();
            tag.GetParameterOrDefault("type", "plantuml").Returns("plantuml");
            tag.GetParameterOrDefault("format", "png").Returns("png");
            tag.GetParameterOrDefault("caption", string.Empty).Returns("");
            tag.GetParameterOrDefault("xDim", string.Empty).Returns("");
            tag.GetParameterOrDefault("yDim", string.Empty).Returns("");
            tag.GetParameterOrDefault("payload", string.Empty).Returns("");
            tag.Contents.Returns("@startuml\nA->B\n@enduml");

            var doc = new DocumentConfig("roboclerkID", "documentID", "documentTitle", "documentAbbreviation", "documentTemplate");
            
            webResources.DownloadImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<byte[]>(new Exception("Network error")));

            var result = obj.GetContent(tag, doc);
            
            Assert.That(result, Does.Contain("Diagram Error:"));
            Assert.That(result, Does.Contain("Network error"));
        }

        [UnitTestAttribute(
        Identifier = "fdb149c3-805e-4c87-8370-4e3fcba43421",
        Purpose = "Kroki content creator saves file for AsciiDoc format",
        PostCondition = "File is saved and image tag is returned")]
        [Test]
        public void TestKrokiDiagram4()
        {
            config.OutputFormat.Returns("ASCIIDOC");
            config.MediaDir.Returns("media");
            config.OutputDir.Returns("output");
            
            // Mock file system behavior
            fs.GetFileName("media").Returns("media");
            fs.Combine("output", "media").Returns("output/media");
            
            var obj = new KrokiDiagram(dataSources, traceAnalysis, config, fs, webResources);
            
            var tag = Substitute.For<IRoboClerkTag>();
            tag.GetParameterOrDefault("type", "plantuml").Returns("plantuml");
            tag.GetParameterOrDefault("format", "png").Returns("png");
            tag.GetParameterOrDefault("caption", string.Empty).Returns("Test Diagram");
            tag.GetParameterOrDefault("xDim", string.Empty).Returns("");
            tag.GetParameterOrDefault("yDim", string.Empty).Returns("");
            tag.GetParameterOrDefault("payload", string.Empty).Returns("");
            tag.Contents.Returns("@startuml\nA->B\n@enduml");

            var doc = new DocumentConfig("roboclerkID", "documentID", "documentTitle", "documentAbbreviation", "documentTemplate");
            
            byte[] imageBytes = Encoding.UTF8.GetBytes("fake image data");
            webResources.DownloadImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(imageBytes));

            var result = obj.GetContent(tag, doc);
            
            fs.Received().WriteAllBytes(Arg.Is<string>(s => s.StartsWith("output/media/") && s.EndsWith(".png")), Arg.Any<byte[]>());
            Assert.That(result, Does.Contain("image::media/"));
            Assert.That(result, Does.Contain(".Test Diagram"));
        }

        [UnitTestAttribute(
        Identifier = "e12c0467-25b9-46c8-a157-733815472a14",
        Purpose = "Kroki content creator metadata is correct",
        PostCondition = "Metadata properties match expected values")]
        [Test]
        public void TestKrokiDiagramMetadata()
        {
            var metadata = KrokiDiagram.StaticMetadata;
            Assert.That(metadata.Name, Is.EqualTo("Kroki Diagram"));
            Assert.That(metadata.Source, Is.EqualTo("Web"));
            Assert.That(metadata.Tags, Has.Count.EqualTo(1));
            Assert.That(metadata.Tags[0].TagID, Is.EqualTo("KrokiDiagram"));
            Assert.That(metadata.Tags[0].Parameters, Has.Count.EqualTo(6));
            Assert.That(metadata.Tags[0].Parameters[0].Name, Is.EqualTo("type"));
            Assert.That(metadata.Tags[0].Parameters[1].Name, Is.EqualTo("format"));
            Assert.That(metadata.Tags[0].Parameters[2].Name, Is.EqualTo("xDim"));
            Assert.That(metadata.Tags[0].Parameters[3].Name, Is.EqualTo("yDim"));
            Assert.That(metadata.Tags[0].Parameters[4].Name, Is.EqualTo("caption"));
            Assert.That(metadata.Tags[0].Parameters[5].Name, Is.EqualTo("payload"));
        }

        [UnitTestAttribute(
        Identifier = "aa213f4b-d196-4c25-9077-b290e71101da",
        Purpose = "Kroki content creator GetMetadata returns StaticMetadata",
        PostCondition = "Returned metadata is the same instance as StaticMetadata")]
        [Test]
        public void TestKrokiDiagramGetMetadata()
        {
            var obj = new KrokiDiagram(dataSources, traceAnalysis, config, fs, webResources);
            Assert.That(obj.GetMetadata(), Is.SameAs(KrokiDiagram.StaticMetadata));
        }
    }
}
