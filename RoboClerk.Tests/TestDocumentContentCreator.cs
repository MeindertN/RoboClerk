using NUnit.Framework;
using NSubstitute;
using System.Collections.Generic;
using RoboClerk.Configuration;
using System.IO.Abstractions;
using RoboClerk.ContentCreators;
using System;
using System.Text.RegularExpressions;
using System.IO;
using RoboClerk.Core;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("These tests test the RoboClerk Document content creator")]
    internal class TestDocumentContentCreator
    {
        //private IConfiguration config = null;
        //private IDataSources dataSources = null;
        private ITraceabilityAnalysis traceAnalysis = null;
        //private IFileSystem fs = null;
        private DocumentConfig documentConfig = null;
        //private List<LinkedItem> testcaseItems = new List<LinkedItem>();
        private TraceEntity te = null;

        [SetUp]
        public void TestSetup()
        {
            te = new TraceEntity("TestCase", "Test Case", "TC", TraceEntityType.Truth);
            documentConfig = new DocumentConfig("SystemLevelTestPlan", "docID", "docTitle", "docAbbr", @"c:\in\template.adoc");
            traceAnalysis = Substitute.For<ITraceabilityAnalysis>();
            traceAnalysis.GetTraceEntityForID("TestCase").Returns(te);
            traceAnalysis.GetTraceEntityForAnyProperty("TC").Returns(te);
        }

        [UnitTestAttribute(
        Identifier = "72A3CD11-2E49-4B02-B71F-767CF3B1EA4F",
        Purpose = "Document content creator is created",
        PostCondition = "No exception is thrown")]
        [Test]
        public void CreateDocumentCC()
        {
            var sst = new ContentCreators.Document(traceAnalysis);
        }

        [UnitTestAttribute(
        Identifier = "CDA4BF30-FDEF-42FC-B82C-0DA644636DA9",
        Purpose = "Document content creator is fed an appropriate tag and documentConfig.",
        PostCondition = "The expected strings are returned")]
        [Test]
        public void TestDocumentCC1()
        {
            var sst = new ContentCreators.Document(traceAnalysis);
            string tagString = "@@Document:title()@@";
            IRoboClerkTag tag = new RoboClerkTextTag(0, tagString.Length, tagString, true);
            string result = sst.GetContent(tag,documentConfig);
            Assert.That(result, Is.EqualTo("docTitle"));

            tagString = "@@Document:aBbreviation()@@";
            tag = new RoboClerkTextTag(0, tagString.Length, tagString, true);
            result = sst.GetContent(tag, documentConfig);
            Assert.That(result, Is.EqualTo("docAbbr"));

            tagString = "@@Document:IDENTIFIER()@@";
            tag = new RoboClerkTextTag(0, tagString.Length, tagString, true);
            result = sst.GetContent(tag, documentConfig);
            Assert.That(result, Is.EqualTo("docID"));

            tagString = "@@Document:template()@@";
            tag = new RoboClerkTextTag(0, tagString.Length, tagString, true);
            result = sst.GetContent(tag, documentConfig);
            Assert.That(result, Is.EqualTo(@"c:\in\template.adoc"));

            tagString = "@@Document:RoboClerkID()@@";
            tag = new RoboClerkTextTag(0, tagString.Length, tagString, true);
            result = sst.GetContent(tag, documentConfig);
            Assert.That(result, Is.EqualTo("SystemLevelTestPlan"));

            tagString = "@@Document:GenDateTime()@@";
            tag = new RoboClerkTextTag(0, tagString.Length, tagString, true);
            result = sst.GetContent(tag, documentConfig);
            DateTime now = DateTime.Now;
            DateTime dateTime = DateTime.Parse(result);
            TimeSpan diff = now - dateTime;
            Assert.That(diff.Minutes, Is.LessThanOrEqualTo(1));
        }

        [UnitTestAttribute(
        Identifier = "991A810C-3C44-411F-9EAB-58D52E990240",
        Purpose = "Document content creator is fed an appropriate tag and documentConfig that has a certain entity count.",
        PostCondition = "The expected count is returned")]
        [Test]
        public void TestDocumentCC2()
        {
            documentConfig.AddEntityCount(te, 3);
            var sst = new ContentCreators.Document(traceAnalysis);
            string tagString = "@@Document:countentities(entity=TC)@@";
            RoboClerkTextTag tag = new RoboClerkTextTag(0, tagString.Length, tagString, true);
            string result = sst.GetContent(tag, documentConfig);
            Assert.That(result, Is.EqualTo("3"));

            tagString = "@@Document:countentities(entity=TC, restart=true)@@";
            tag = new RoboClerkTextTag(0, tagString.Length, tagString, true);
            result = sst.GetContent(tag, documentConfig);
            Assert.That(result, Is.EqualTo(""));

            documentConfig.AddEntityCount(te, 1);
            documentConfig.AddEntityCount(te, 1);
            tagString = "@@Document:countentities(entity=TC)@@";
            tag = new RoboClerkTextTag(0, tagString.Length, tagString, true);
            result = sst.GetContent(tag, documentConfig);
            Assert.That(result, Is.EqualTo("2"));
        }

        [UnitTestAttribute(
        Identifier = "296BDCB4-F8B8-4F4C-8491-E848188B8F99",
        Purpose = "Document content creator is fed an appropriate tag and documentConfig. A valid trace entity count is requested but trace entity not in document config.",
        PostCondition = "The expected count (0) is returned")]
        [Test]
        public void TestDocumentCC3()
        {
            var sst = new ContentCreators.Document(traceAnalysis);
            string tagString = "@@Document:countentities(entity=TC)@@";
            RoboClerkTextTag tag = new RoboClerkTextTag(0, tagString.Length, tagString, true);
            string result = sst.GetContent(tag, documentConfig);
            Assert.That(result, Is.EqualTo("0"));
        }

        [UnitTestAttribute(
        Identifier = "A8C2E267-7103-4273-A3BC-FF58FA3F5CBE",
        Purpose = "Document content creator is fed an appropriate tag and documentConfig. An invalid trace entity count is requested.",
        PostCondition = "The expected exception is thrown")]
        [Test]
        public void TestDocumentCC4()
        {
            var sst = new ContentCreators.Document(traceAnalysis);
            string tagString = "@@Document:countentities(entity=TCC)@@";
            RoboClerkTextTag tag = new RoboClerkTextTag(0, tagString.Length, tagString, true);
            Assert.Throws<Exception>(() => sst.GetContent(tag, documentConfig));
        }

        [UnitTestAttribute(
        Identifier = "53000310-1951-4809-89DA-769FCDFCBC06",
        Purpose = "Document content creator is fed an inappropriate tag.",
        PostCondition = "The expected exception is thrown")]
        [Test]
        public void TestDocumentCC5()
        {
            var sst = new ContentCreators.Document(traceAnalysis);
            string tagString = "@@Document:nonsense(entity=TCC)@@";
            RoboClerkTextTag tag = new RoboClerkTextTag(0, tagString.Length, tagString, true);
            Assert.Throws<Exception>(() => sst.GetContent(tag, documentConfig));
        }
    }
}
