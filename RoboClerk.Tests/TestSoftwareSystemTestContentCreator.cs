using NUnit.Framework;
using NSubstitute;
using System.Collections.Generic;
using RoboClerk.Configuration;
using System.IO.Abstractions;
using RoboClerk.ContentCreators;
using System;
using System.Text.RegularExpressions;
using System.IO;
using RoboClerk.Items;

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
        private List<TestResult> results = new List<TestResult>();

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

            results.Clear();
            results.Add(new TestResult("tcid1", TestResultType.SYSTEM, TestResultStatus.PASS, "the first test", "all good", DateTime.Now));
            results.Add(new TestResult("unit1", TestResultType.UNIT, TestResultStatus.FAIL, "the first unit test", "all bad", DateTime.Now));
            
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

            dataSources.GetAllTestResults().Returns(results);
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

        [UnitTestAttribute(
        Identifier = "AA8E3271-8778-4504-8753-04EB6B51EC3C",
        Purpose = "Software System Test content creator is provided with a tag that does a comparison between known results and test cases.",
        PostCondition = "Appropriate response is produced")]
        [Test]
        public void SoftwareSystemRenderTest5()
        {
            var sst = new SoftwareSystemTest(dataSources, traceAnalysis, config);
            var tag = new RoboClerkTag(0, 28, "@@SLMS:TC(CheckResults=true)@@", true);

            results.Add(new TestResult("tcid3", TestResultType.SYSTEM, TestResultStatus.PASS, "the second test", "all good", DateTime.Now));
            SoftwareSystemTestItem testcaseItem = new SoftwareSystemTestItem();
            testcaseItem.ItemID = "tcid3";
            testcaseItem.ItemRevision = "tcrev3";
            testcaseItem.ItemLastUpdated = new DateTime(1993, 10, 13);
            testcaseItem.ItemTargetVersion = "3";
            testcaseItem.AddLinkedItem(new ItemLink("target3", ItemLinkType.Parent));
            testcaseItem.TestCaseState = "state3";
            testcaseItem.TestCaseToUnitTest = false;
            testcaseItem.ItemTitle = "title3";
            testcaseItem.TestCaseAutomated = true;
            testcaseItem.TestCaseDescription = "description3";
            testcaseItem.AddTestCaseStep(new TestStep("1", "input31", "expected result31"));
            testcaseItem.AddTestCaseStep(new TestStep("2", "input32", "expected result32"));
            testcaseItem.Link = new Uri("http://localhost/");
            testcaseItems.Add(testcaseItem);

            string content = sst.GetContent(tag, documentConfig);
            string expectedContent = "All automated tests from the test plan were successfully executed and passed.";

            Assert.That(Regex.Replace(content, @"\r\n", "\n"), Is.EqualTo(expectedContent));
        }

        [UnitTestAttribute(
        Identifier = "67555D6B-72D0-45E6-8B15-A13056BCD380",
        Purpose = "Software System Test content creator is provided with a tag that does a comparison between known results and test cases. There is a result for a missing test case.",
        PostCondition = "Appropriate response is produced")]
        [Test]
        public void SoftwareSystemRenderTest6()
        {
            var sst = new SoftwareSystemTest(dataSources, traceAnalysis, config);
            var tag = new RoboClerkTag(0, 28, "@@SLMS:TC(CheckResults=true)@@", true);

            results.Add(new TestResult("tcid3", TestResultType.SYSTEM, TestResultStatus.PASS, "the second test", "all good", DateTime.Now));

            string content = sst.GetContent(tag, documentConfig);
            string expectedContent = "RoboClerk detected problems with the automated testing:\n\n* Result for test with ID \"tcid3\" found, but test plan does not contain such an automated test.\n\n";

            Assert.That(Regex.Replace(content, @"\r\n", "\n"), Is.EqualTo(expectedContent));
        }

        [UnitTestAttribute(
        Identifier = "4C3B948D-CD26-470C-9A80-4282ECACC6DA",
        Purpose = "Software System Test content creator is provided with a tag that does a comparison between known results and test cases. There is a test case with a missing result.",
        PostCondition = "Appropriate response is produced")]
        [Test]
        public void SoftwareSystemRenderTest7()
        {
            var sst = new SoftwareSystemTest(dataSources, traceAnalysis, config);
            var tag = new RoboClerkTag(0, 28, "@@SLMS:TC(CheckResults=true)@@", true);

            SoftwareSystemTestItem testcaseItem = new SoftwareSystemTestItem();
            testcaseItem.ItemID = "tcid3";
            testcaseItem.ItemRevision = "tcrev3";
            testcaseItem.ItemLastUpdated = new DateTime(1993, 10, 13);
            testcaseItem.ItemTargetVersion = "3";
            testcaseItem.AddLinkedItem(new ItemLink("target3", ItemLinkType.Parent));
            testcaseItem.TestCaseState = "state3";
            testcaseItem.TestCaseToUnitTest = false;
            testcaseItem.ItemTitle = "title3";
            testcaseItem.TestCaseAutomated = true;
            testcaseItem.TestCaseDescription = "description3";
            testcaseItem.AddTestCaseStep(new TestStep("1", "input31", "expected result31"));
            testcaseItem.AddTestCaseStep(new TestStep("2", "input32", "expected result32"));
            testcaseItem.Link = new Uri("http://localhost/");
            testcaseItems.Add(testcaseItem);

            string content = sst.GetContent(tag, documentConfig);
            string expectedContent = "RoboClerk detected problems with the automated testing:\n\n* Result for test with ID \"tcid3\" not found in results.\n\n";

            Assert.That(Regex.Replace(content, @"\r\n", "\n"), Is.EqualTo(expectedContent));
        }

        [UnitTestAttribute(
        Identifier = "3DE923FB-0C8A-4F5D-95EC-053037E9D792",
        Purpose = "Software System Test content creator is provided with a tag that does a comparison between known results and test cases. There is a test case with a missing result and a failed test case.",
        PostCondition = "Appropriate response is produced")]
        [Test]
        public void SoftwareSystemRenderTest8()
        {
            var sst = new SoftwareSystemTest(dataSources, traceAnalysis, config);
            var tag = new RoboClerkTag(0, 28, "@@SLMS:TC(CheckResults=true)@@", true);

            // Replace the first result with a failed test result instead of trying to modify the read-only property
            results[0] = new TestResult("tcid1", TestResultType.SYSTEM, TestResultStatus.FAIL, "the first test", "all bad", DateTime.Now);
            SoftwareSystemTestItem testcaseItem = new SoftwareSystemTestItem();
            testcaseItem.ItemID = "tcid3";
            testcaseItem.ItemRevision = "tcrev3";
            testcaseItem.ItemLastUpdated = new DateTime(1993, 10, 13);
            testcaseItem.ItemTargetVersion = "3";
            testcaseItem.AddLinkedItem(new ItemLink("target3", ItemLinkType.Parent));
            testcaseItem.TestCaseState = "state3";
            testcaseItem.TestCaseToUnitTest = false;
            testcaseItem.ItemTitle = "title3";
            testcaseItem.TestCaseAutomated = true;
            testcaseItem.TestCaseDescription = "description3";
            testcaseItem.AddTestCaseStep(new TestStep("1", "input31", "expected result31"));
            testcaseItem.AddTestCaseStep(new TestStep("2", "input32", "expected result32"));
            testcaseItem.Link = new Uri("http://localhost/");
            testcaseItems.Add(testcaseItem);

            string content = sst.GetContent(tag, documentConfig);
            string expectedContent = "RoboClerk detected problems with the automated testing:\n\n* Test with ID \"tcid1\" has failed.\n* Result for test with ID \"tcid3\" not found in results.\n\n";

            Assert.That(Regex.Replace(content, @"\r\n", "\n"), Is.EqualTo(expectedContent));
        }

    }
}
