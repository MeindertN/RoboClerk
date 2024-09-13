using NSubstitute;
using NUnit.Framework;
using RoboClerk.Configuration;
using RoboClerk.ContentCreators;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using System.IO;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("These tests test the RoboClerk Unit Test content creator")]
    internal class TestUnitTestContentCreator
    {
        private IConfiguration config = null;
        private IDataSources dataSources = null;
        private ITraceabilityAnalysis traceAnalysis = null;
        private IFileSystem fs = null;
        private DocumentConfig documentConfig = null;
        private List<LinkedItem> unittestItems = new List<LinkedItem>();

        [SetUp]
        public void TestSetup()
        {
            config = Substitute.For<IConfiguration>();
            dataSources = Substitute.For<IDataSources>();
            
            traceAnalysis = Substitute.For<ITraceabilityAnalysis>();
            var te = new TraceEntity("UnitTest", "unittest", "ut", TraceEntityType.Truth);
            var teDoc = new TraceEntity("docID", "docTitle", "docAbbr", TraceEntityType.Document);
            traceAnalysis.GetTraceEntityForID("UnitTest").Returns(te);
            traceAnalysis.GetTraceEntityForID("docID").Returns(teDoc);
            traceAnalysis.GetTraceEntityForTitle("docTitle").Returns(teDoc);
            fs = Substitute.For<IFileSystem>();
            traceAnalysis.GetTraceEntityForAnyProperty("UnitTest").Returns(te);
            documentConfig = new DocumentConfig("UnitLevelTestPlan", "docID", "docTitle", "docAbbr", @"c:\in\template.adoc");

            unittestItems.Clear();
            var unittestItem = new UnitTestItem();
            unittestItem.ItemID = "tcid1";
            unittestItem.ItemRevision = "tcrev1";
            unittestItem.ItemLastUpdated = new DateTime(1999, 10, 10);
            unittestItem.ItemTargetVersion = "1";
            unittestItem.AddLinkedItem(new ItemLink("target1", ItemLinkType.Related));
            unittestItem.ItemTitle = "title1";
            unittestItem.UnitTestPurpose = "purpose1";
            unittestItem.UnitTestAcceptanceCriteria = "accept1";
            unittestItem.UnitTestFileName = "filename";
            unittestItem.UnitTestFunctionName = "functionname";
            unittestItems.Add(unittestItem);
            unittestItem = new UnitTestItem();
            unittestItem.ItemID = "tcid2";
            unittestItem.ItemTargetVersion = "2";
            unittestItem.ItemTitle = "title2";
            unittestItem.Link = new Uri("http://localhost/");
            unittestItems.Add(unittestItem);
            dataSources.GetItems(te).Returns(unittestItems);
            dataSources.GetItem("tcid1").Returns(unittestItems[0]);
            dataSources.GetItem("tcid2").Returns(unittestItems[1]);

            dataSources.GetTemplateFile("./ItemTemplates/UnitTest.adoc").Returns(File.ReadAllText("../../../../RoboClerk/ItemTemplates/UnitTest.adoc"));
            dataSources.GetTemplateFile("./ItemTemplates/UnitTest_brief.adoc").Returns(File.ReadAllText("../../../../RoboClerk/ItemTemplates/UnitTest_brief.adoc"));
        }

        [UnitTestAttribute(
        Identifier = "2FC82C9C-0968-4580-9BD7-BBC35C6F6933",
        Purpose = "Unit Test content creator is created",
        PostCondition = "No exception is thrown")]
        [Test]
        public void CreateUnitTestCC1()
        {
            var sst = new UnitTest(dataSources,traceAnalysis, config);
        }

        [UnitTestAttribute(
        Identifier = "DD6E97D3-78E6-4AA5-8075-64CE91ECE831",
        Purpose = "Unit Test content creator is created and is supplied with a tag for a single unit test",
        PostCondition = "Appropriate result string is produced and trace is set")]
        [Test]
        public void CreateUnitTestCC2()
        {
            var sst = new UnitTest(dataSources, traceAnalysis, config);
            var tag = new RoboClerkTag(0, 31, "@@SLMS:UnitTest(ItemID=tcid1)@@", true);
            //make sure we can find the item linked to this test
            dataSources.GetItem("target1").Returns(new RequirementItem(RequirementType.SystemRequirement) { ItemID = "target1" });
            string content = sst.GetContent(tag, documentConfig);
            string expectedContent = "\n|====\n| *unittest ID:* | tcid1\n\n| *Function / File Name:* | functionname / filename\n\n| *Revision:* | tcrev1\n\n| *Last Updated:* | 1999/10/10 00:00:00\n| *Trace Link:* | target1\n\n| *Purpose:* | purpose1\n\n| *Acceptance Criteria:* | accept1\n\n|====";

            Assert.That(Regex.Replace(content, @"\r\n", "\n"), Is.EqualTo(expectedContent)); //ensure that we're always comparing the correct string, regardless of newline character for a platform
            Assert.DoesNotThrow(() => traceAnalysis.Received().AddTrace(Arg.Any<TraceEntity>(), "tcid1", Arg.Any<TraceEntity>(), "tcid1"));
        }

        [UnitTestAttribute(
        Identifier = "73A8E1F6-4031-423F-9DE4-CCDFDAC2B585",
        Purpose = "Unit Test content creator is created and is supplied with a tag for a single unit test which is linked to a non-existent item",
        PostCondition = "Exception is thrown")]
        [Test]
        public void CreateUnitTestCC5()
        {
            var sst = new UnitTest(dataSources, traceAnalysis, config);
            var tag = new RoboClerkTag(0, 31, "@@SLMS:UnitTest(ItemID=tcid1)@@", true);
            
            Assert.Throws<AggregateException>(()=>sst.GetContent(tag, documentConfig));
        }

        [UnitTestAttribute(
        Identifier = "473FEA42-E37C-4CD0-9F51-6E8FE59EB360",
        Purpose = "Unit Test content creator is created and is supplied with a tag for a single unit test without a linked item",
        PostCondition = "The appropriate string response is provided and trace is set.")]
        [Test]
        public void CreateUnitTestCC3()
        {
            var sst = new UnitTest(dataSources, traceAnalysis, config);
            var tag = new RoboClerkTag(0, 31, "@@SLMS:UnitTest(ItemID=tcid2)@@", true);
            string content = sst.GetContent(tag, documentConfig);
            string expectedContent = "\n|====\n| *unittest ID:* | http://localhost/[tcid2]\n\n| *Function / File Name:* | N/A / N/A\n\n| *Revision:* | \n\n\n| *Trace Link:* | N/A\n\n| *Purpose:* | N/A\n\n| *Acceptance Criteria:* | N/A\n\n|====";

            Assert.That(Regex.Replace(content, @"\r\n", "\n"), Is.EqualTo(expectedContent)); //ensure that we're always comparing the correct string, regardless of newline character for a platform
            Assert.DoesNotThrow(() => traceAnalysis.Received().AddTrace(Arg.Any<TraceEntity>(), "tcid2", Arg.Any<TraceEntity>(), "tcid2"));
        }

        [UnitTestAttribute(
        Identifier = "A931FCA0-2F8A-446A-95DF-66870F2138AB",
        Purpose = "Unit Test content creator is created and is supplied with a tag for a single unit test that does not exist",
        PostCondition = "The appropriate string response is provided.")]
        [Test]
        public void CreateUnitTestCC4()
        {
            var sst = new UnitTest(dataSources, traceAnalysis, config);
            var tag = new RoboClerkTag(0, 31, "@@SLMS:UnitTest(ItemID=tcid3)@@", true);
            string content = sst.GetContent(tag, documentConfig);
            string expectedContent = "Unable to find specified unittest(s). Check if unittests are provided or if a valid unittest identifier is specified.";

            Assert.That(Regex.Replace(content, @"\r\n", "\n"), Is.EqualTo(expectedContent)); //ensure that we're always comparing the correct string, regardless of newline character for a platform
        }

        [UnitTestAttribute(
        Identifier = "439DA613-EF71-4589-9F3B-8314CB8A11E5",
        Purpose = "Unit Test content creator is created and is supplied with a tag requesting a table view of the unit tests",
        PostCondition = "Appropriate result string is produced and trace is set")]
        [Test]
        public void CreateUnitTestCC6()
        {
            var sst = new UnitTest(dataSources, traceAnalysis, config);
            var tag = new RoboClerkTag(0, 29, "@@SLMS:UnitTest(brief=true)@@", true);
            string content = sst.GetContent(tag, documentConfig);
            string expectedContent = "|====\n| unittest ID | Function / File Name | unittest Purpose | Acceptance Criteria\n\n| tcid1 | functionname / filename | purpose1 | accept1 \n\n| http://localhost/[tcid2] |  /  |  |  \n\n|====\n";

            Assert.That(Regex.Replace(content, @"\r\n", "\n"), Is.EqualTo(expectedContent)); //ensure that we're always comparing the correct string, regardless of newline character for a platform
            Assert.DoesNotThrow(() => traceAnalysis.Received().AddTrace(Arg.Any<TraceEntity>(), "tcid1", Arg.Any<TraceEntity>(), "tcid1"));
            Assert.DoesNotThrow(() => traceAnalysis.Received().AddTrace(Arg.Any<TraceEntity>(), "tcid2", Arg.Any<TraceEntity>(), "tcid2"));
        }
    }
}
