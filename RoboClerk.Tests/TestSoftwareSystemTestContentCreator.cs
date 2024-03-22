using NUnit.Framework;
using NSubstitute;
using System.Collections.Generic;
using RoboClerk.Configuration;
using System.IO.Abstractions;
using RoboClerk.ContentCreators;
using System;
using System.Text.RegularExpressions;
using System.IO;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("These tests test the RoboClerk Software System Test content creator")]
    internal class TestSoftwareSystemTestContentCreator
    {
        private IConfiguration config = null;
        private IDataSources dataSources = null;
        private ITraceabilityAnalysis traceAnalysis = null;
        private IFileSystem fs = null;
        private DocumentConfig documentConfig = null;
        private List<LinkedItem> testcaseItems = new List<LinkedItem>();

        [SetUp]
        public void TestSetup()
        {
            config = Substitute.For<IConfiguration>();
            dataSources = Substitute.For<IDataSources>();
            traceAnalysis = Substitute.For<ITraceabilityAnalysis>();
            var te = new TraceEntity("TestCase", "Test Case", "TC", TraceEntityType.Truth);
            var teDoc = new TraceEntity("docID", "docTitle", "docAbbr", TraceEntityType.Document);
            traceAnalysis.GetTraceEntityForID("TestCase").Returns(te);
            traceAnalysis.GetTraceEntityForID("docID").Returns(teDoc);
            traceAnalysis.GetTraceEntityForAnyProperty("TC").Returns(te);
            traceAnalysis.GetTraceEntityForTitle("docTitle").Returns(teDoc);
            
            fs = Substitute.For<IFileSystem>();
            documentConfig = new DocumentConfig("SystemLevelTestPlan", "docID", "docTitle", "docAbbr", @"c:\in\template.adoc");

            testcaseItems.Clear();
            var testcaseItem = new SoftwareSystemTestItem();
            testcaseItem.ItemID = "tcid1";
            testcaseItem.ItemRevision = "tcrev1";
            testcaseItem.ItemLastUpdated = new DateTime(1999,10,10);
            testcaseItem.ItemTargetVersion = "1";
            testcaseItem.AddLinkedItem(new ItemLink("target1", ItemLinkType.Parent));
            testcaseItem.TestCaseState = "state1";
            testcaseItem.TestCaseToUnitTest = false;
            testcaseItem.ItemTitle = "title1";
            testcaseItem.TestCaseAutomated = false;
            testcaseItem.TestCaseAutomated = true;
            testcaseItem.TestCaseDescription = "description1";
            testcaseItem.AddTestCaseStep(new TestStep("1", "input11", "expected result11" ));
            testcaseItem.AddTestCaseStep(new TestStep("2", "input12", "expected result12" ));
            testcaseItems.Add(testcaseItem);
            testcaseItem = new SoftwareSystemTestItem();
            testcaseItem.ItemID = "tcid2";
            testcaseItem.ItemRevision = "tcrev2";
            testcaseItem.ItemLastUpdated = new DateTime(1999, 12, 12);
            testcaseItem.ItemTargetVersion = "2";
            testcaseItem.AddLinkedItem(new ItemLink("target2", ItemLinkType.Parent));
            testcaseItem.TestCaseState = "state2";
            testcaseItem.TestCaseToUnitTest = false;
            testcaseItem.ItemTitle = "title2";
            testcaseItem.TestCaseAutomated = false;
            testcaseItem.TestCaseAutomated = false;
            testcaseItem.TestCaseDescription = "description2";
            testcaseItem.AddTestCaseStep(new TestStep("1", "input21", "expected result21" ));
            testcaseItem.AddTestCaseStep(new TestStep("2", "input22", "expected result22" ));
            testcaseItem.Link = new Uri("http://localhost/");
            testcaseItems.Add(testcaseItem);
            dataSources.GetItems(te).Returns(testcaseItems);
            dataSources.GetTemplateFile("./ItemTemplates/SoftwareSystemTest_automated.adoc").Returns(File.ReadAllText("../../../../RoboClerk/ItemTemplates/SoftwareSystemTest_automated.adoc"));
            dataSources.GetTemplateFile("./ItemTemplates/SoftwareSystemTest_manual.adoc").Returns(File.ReadAllText("../../../../RoboClerk/ItemTemplates/SoftwareSystemTest_manual.adoc"));
            dataSources.GetItem("target1").Returns(testcaseItem);
            dataSources.GetItem("target2").Returns(testcaseItem);
        }

        [UnitTestAttribute(
        Identifier = "4A8B13AE-3A62-4722-A89B-E5AD93B85E26",
        Purpose = "Software System Test content creator is created",
        PostCondition = "No exception is thrown")]
        [Test]
        public void CreateSoftwareSystemTestCC()
        {
            var sst = new SoftwareSystemTest(dataSources, traceAnalysis, config);
        }

        [UnitTestAttribute(
        Identifier = "428AB262-E85B-419C-A5C9-D7F9860F5F29",
        Purpose = "Software System Test content creator is provided with a tag that calls in all test cases",
        PostCondition = "Appropriate result string is produced and trace is set")]
        [Test]
        public void SoftwareSystemRenderTest1()
        {
            var sst = new SoftwareSystemTest(dataSources, traceAnalysis, config);
            var tag = new RoboClerkTag(0, 13, "@@SLMS:TC()@@", true);
            string content = sst.GetContent(tag, documentConfig);
            string expectedContent = "\n|====\n| *Test Case ID:* | tcid1\n\n| *Test Case Revision:* | tcrev1\n\n| *Parent ID:* | http://localhost/[tcid2]: \"title2\"\n\n| *Title:* | title1\n|====\n\n@@Post:REMOVEPARAGRAPH()@@\n\n|====\n\n| *Step* | *Action* | *Expected Result* \n\n| 1 | input11 | expected result11 \n\n| 2 | input12 | expected result12 \n\n|====\n\n|====\n| *Test Case ID:* | http://localhost/[tcid2]\n\n| *Test Case Revision:* | tcrev2\n\n| *Parent ID:* | http://localhost/[tcid2]: \"title2\"\n\n| *Title:* | title2\n|====\n\n@@Post:REMOVEPARAGRAPH()@@\n\n|====\n| *Step* | *Action* | *Expected Result* | *Actual Result* | *Test Status*\n\n| 1 | input21 | expected result21  |  | Pass / Fail\n\n| 2 | input22 | expected result22  |  | Pass / Fail\n\n|====\n\n@@Post:REMOVEPARAGRAPH()@@\n\n|====\n| Initial: | Date: | Asset ID: \n|====";

            Assert.That(Regex.Replace(content, @"\r\n", "\n"), Is.EqualTo(expectedContent)); //ensure that we're always comparing the correct string, regardless of newline character for a platform
            Assert.DoesNotThrow(() => traceAnalysis.Received().AddTrace(Arg.Any<TraceEntity>(), "tcid1", Arg.Any<TraceEntity>(), "tcid1"));
            Assert.DoesNotThrow(() => traceAnalysis.Received().AddTrace(Arg.Any<TraceEntity>(), "tcid2", Arg.Any<TraceEntity>(), "tcid2"));
        }

        [UnitTestAttribute(
        Identifier = "96F4F08D-6E45-48CB-A98D-0EBD03B35055",
        Purpose = "Software System Test content creator is provided with a tag that calls in a test case with a test step without expected result",
        PostCondition = "Appropriate result string is produced and trace is set")]
        [Test]
        public void SoftwareSystemRenderTest2()
        {
            var sst = new SoftwareSystemTest(dataSources, traceAnalysis, config);
            var tag = new RoboClerkTag(0, 25, "@@SLMS:TC(itemid=tcid2)@@", true);
            ((SoftwareSystemTestItem)testcaseItems[1]).ClearTestCaseSteps();
            ((SoftwareSystemTestItem)testcaseItems[1]).AddTestCaseStep(new TestStep("1", "input21", "expected result21" ));
            ((SoftwareSystemTestItem)testcaseItems[1]).AddTestCaseStep(new TestStep("2", "input22", "" ));
            string content = sst.GetContent(tag, documentConfig);
            string expectedContent = "\n|====\n| *Test Case ID:* | http://localhost/[tcid2]\n\n| *Test Case Revision:* | tcrev2\n\n| *Parent ID:* | http://localhost/[tcid2]: \"title2\"\n\n| *Title:* | title2\n|====\n\n@@Post:REMOVEPARAGRAPH()@@\n\n|====\n| *Step* | *Action* | *Expected Result* | *Actual Result* | *Test Status*\n\n| 1 | input21 | expected result21  |  | Pass / Fail\n\n| 2 | input22 |  |  | \n\n|====\n\n@@Post:REMOVEPARAGRAPH()@@\n\n|====\n| Initial: | Date: | Asset ID: \n|====";

            Assert.That(Regex.Replace(content, @"\r\n", "\n"), Is.EqualTo(expectedContent)); //ensure that we're always comparing the correct string, regardless of newline character for a platform
            Assert.DoesNotThrow(() => traceAnalysis.Received().AddTrace(Arg.Any<TraceEntity>(), "tcid2", Arg.Any<TraceEntity>(), "tcid2"));
        }

        [UnitTestAttribute(
        Identifier = "35C1AE15-8A2D-4BF8-8068-19C48742CC54",
        Purpose = "Software System Test content creator is provided with a tag that calls in a test case that does not exist.",
        PostCondition = "Appropriate response is produced")]
        [Test]
        public void SoftwareSystemRenderTest4()
        {
            var sst = new SoftwareSystemTest(dataSources, traceAnalysis, config);
            var tag = new RoboClerkTag(0, 25, "@@SLMS:TC(itemid=tcid5)@@", true);
            string content = sst.GetContent(tag, documentConfig);
            string expectedContent = "Unable to find specified Test Case(s). Check if Test Cases are provided or if a valid Test Case identifier is specified.";

            Assert.That(Regex.Replace(content, @"\r\n", "\n"), Is.EqualTo(expectedContent));
        }

    }
}
