using DocumentFormat.OpenXml.Spreadsheet;
using NSubstitute;
using NUnit.Framework;
using RoboClerk.Configuration;
using RoboClerk.ContentCreators;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("These tests test the RoboClerk Anomaly content creator")]
    internal class TestAnomalyContentCreator
    {
        private IConfiguration config = null;
        private IDataSources dataSources = null;
        private ITraceabilityAnalysis traceAnalysis = null;
        private DocumentConfig documentConfig = null;
        private List<LinkedItem> anomalyItems = new List<LinkedItem>();

        [SetUp]
        public void TestSetup()
        {
            dataSources = Substitute.For<IDataSources>();
            traceAnalysis = Substitute.For<ITraceabilityAnalysis>();
            var te = new TraceEntity("Anomaly", "Anomaly", "spabrrv", TraceEntityType.Truth);
            var teDoc = new TraceEntity("docID", "docTitle", "docAbbr", TraceEntityType.Document);
            traceAnalysis.GetTraceEntityForID("Anomaly").Returns(te);
            traceAnalysis.GetTraceEntityForID("docID").Returns(teDoc);
            traceAnalysis.GetTraceEntityForTitle("docTitle").Returns(teDoc);
            traceAnalysis.GetTraceEntityForAnyProperty("Anomaly").Returns(te);
            documentConfig = new DocumentConfig("BugReport", "docID", "docTitle", "docAbbr", @"c:\in\template.adoc");

            anomalyItems.Clear();
            var anomalyItem = new AnomalyItem();
            anomalyItem.ItemID = "tcid1";
            anomalyItem.ItemRevision = "tcrev1";
            anomalyItem.ItemLastUpdated = new DateTime(1999, 10, 10);
            anomalyItem.ItemTargetVersion = "1";
            anomalyItem.AddLinkedItem(new ItemLink("target1", ItemLinkType.Parent));
            anomalyItem.ItemTitle = "title1";
            anomalyItem.AnomalyState = "deferred";
            anomalyItem.AnomalyAssignee = "tester mc testee";
            anomalyItem.AnomalySeverity = "critical";
            anomalyItem.AnomalyJustification = "it's all good";
            anomalyItems.Add(anomalyItem);

            anomalyItem = new AnomalyItem();
            anomalyItem.ItemID = "tcid2";
            anomalyItem.ItemRevision = "tcrev2";
            anomalyItem.ItemLastUpdated = new DateTime(1999, 12, 12);
            anomalyItem.ItemTargetVersion = "2";
            anomalyItem.AddLinkedItem(new ItemLink("target2", ItemLinkType.Parent));
            anomalyItem.ItemTitle = "title2";
            anomalyItem.Link = new Uri("http://localhost/");
            anomalyItems.Add(anomalyItem);

            dataSources.GetItems(te).Returns(anomalyItems);
            dataSources.GetItem("tcid1").Returns(anomalyItems[0]);
            dataSources.GetItem("tcid2").Returns(anomalyItems[1]);
            dataSources.GetTemplateFile("./ItemTemplates/Anomaly.adoc").Returns(File.ReadAllText("../../../../RoboClerk/ItemTemplates/Anomaly.adoc"));
        }

        [UnitTestAttribute(Purpose = "(TEST) Anomaly content creator is created",
        Identifier = "1C2B7995-DFDF-466B-96D8-B8165EDA28C8",
        PostCondition = "No exception is thrown")]
        [Test]
        public void CreateAnomalyCC()
        {
            var sst = new Anomaly(dataSources, traceAnalysis, config);
        }

        [UnitTestAttribute(
        Identifier = "622F9DDE-F75C-4CD5-9644-E8A8C7BC10BB",
        Purpose = "Anomaly content creator is created, a tag requesting all anomalies is supplied",
        PostCondition = "Expected content is returned and trace is set")]
        [Test]
        public void TestAnomaly1()
        {
            var anomaly = new Anomaly(dataSources, traceAnalysis, config);
            var tag = new RoboClerkTag(0, 18, "@@SLMS:Anomaly()@@", true);

            string result = anomaly.GetContent(tag, documentConfig);
            string expectedResult = "\n|====\n| Anomaly ID: | tcid1\n\n| Anomaly Revision: | tcrev1\n\n| State: | deferred\n\n| Assigned To: | tester mc testee\n\n| Title: | title1\n\n| Severity: | critical\n\n| Description: | MISSING\n\n| Justification: | it's all good\n|====\n\n|====\n| Anomaly ID: | http://localhost/[tcid2]\n\n| Anomaly Revision: | tcrev2\n\n| State: | N/A\n\n| Assigned To: | NOT ASSIGNED\n\n| Title: | title2\n\n| Severity: | N/A\n\n| Description: | MISSING\n\n| Justification: | N/A\n|====\n";

            Assert.That(Regex.Replace(result, @"\r\n", "\n"), Is.EqualTo(expectedResult)); 
            Assert.DoesNotThrow(() => traceAnalysis.Received().AddTrace(Arg.Any<TraceEntity>(), "tcid1", Arg.Any<TraceEntity>(), "tcid1"));
            Assert.DoesNotThrow(() => traceAnalysis.Received().AddTrace(Arg.Any<TraceEntity>(), "tcid2", Arg.Any<TraceEntity>(), "tcid2"));
        }

        [UnitTestAttribute(
        Identifier = "0CCC57E4-FD87-4ED3-86C7-0B14AF2CD6C1",
        Purpose = "Anomaly content creator is created, a tag requesting all anomalies is supplied, one anomaly is closed",
        PostCondition = "Expected content is returned, closed anomaly is ignored and trace is set only for non-closed anomaly.")]
        [Test]
        public void TestAnomaly2()
        {
            var anomaly = new Anomaly(dataSources, traceAnalysis, config);
            var tag = new RoboClerkTag(0, 18, "@@SLMS:Anomaly()@@", true);
            ((AnomalyItem)anomalyItems[0]).AnomalyState = "Closed";

            string result = anomaly.GetContent(tag, documentConfig);
            string expectedResult = "\n|====\n| Anomaly ID: | http://localhost/[tcid2]\n\n| Anomaly Revision: | tcrev2\n\n| State: | N/A\n\n| Assigned To: | NOT ASSIGNED\n\n| Title: | title2\n\n| Severity: | N/A\n\n| Description: | MISSING\n\n| Justification: | N/A\n|====\n";

            Assert.That(Regex.Replace(result, @"\r\n", "\n"), Is.EqualTo(expectedResult));
            Assert.DoesNotThrow(() => traceAnalysis.DidNotReceive().AddTrace(Arg.Any<TraceEntity>(), "tcid1", Arg.Any<TraceEntity>(), "tcid1"));
            Assert.DoesNotThrow(() => traceAnalysis.Received().AddTrace(Arg.Any<TraceEntity>(), "tcid2", Arg.Any<TraceEntity>(), "tcid2"));
        }

        [UnitTestAttribute(
        Identifier = "F9798C27-8063-4575-B7D1-D31513C71A96",
        Purpose = "Anomaly content creator is created, a tag requesting all anomalies is supplied, one anomaly is closed, no other anomalies",
        PostCondition = "Expected content is returned, closed anomaly is ignored and no trace is set")]
        [Test]
        public void TestAnomaly3()
        {
            var anomaly = new Anomaly(dataSources, traceAnalysis, config);
            var tag = new RoboClerkTag(0, 18, "@@SLMS:Anomaly()@@", true);
            ((AnomalyItem)anomalyItems[0]).AnomalyState = "Closed";
            anomalyItems.RemoveAt(1);

            string result = anomaly.GetContent(tag, documentConfig);
            string expectedResult = "No outstanding Anomaly found.";

            Assert.That(Regex.Replace(result, @"\r\n", "\n"), Is.EqualTo(expectedResult));
            Assert.DoesNotThrow(() => traceAnalysis.DidNotReceive().AddTrace(Arg.Any<TraceEntity>(), "tcid1", Arg.Any<TraceEntity>(), "tcid1"));
            Assert.DoesNotThrow(() => traceAnalysis.DidNotReceive().AddTrace(Arg.Any<TraceEntity>(), "tcid2", Arg.Any<TraceEntity>(), "tcid2"));
        }
    }
}
