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
        private List<TestResult> results = new List<TestResult>();

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

            results.Clear();
            results.Add(new TestResult("tcid1", TestResultType.UNIT, TestResultStatus.PASS, "the first test", "all good", DateTime.Now));
            results.Add(new TestResult("unit1", TestResultType.SYSTEM, TestResultStatus.PASS, "the first unit test", "all bad", DateTime.Now));
            results.Add(new TestResult("tcid2", TestResultType.SYSTEM, TestResultStatus.FAIL, "the first system test", "none good", DateTime.Now));

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

            dataSources.GetAllTestResults().Returns(results);
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

        [UnitTestAttribute(
        Identifier = "E65F01B6-800C-4851-A0FA-F31107A9AC42",
        Purpose = "Unit Test content creator is created and is supplied with a tag requesting a comparison with results. One unit test's result is missing.",
        PostCondition = "Appropriate result string is produced")]
        [Test]
        public void CreateUnitTestCC7()
        {
            var sst = new UnitTest(dataSources, traceAnalysis, config);
            var tag = new RoboClerkTag(0, 34, "@@SLMS:UnitTest(CheckResults=true)@@", true);
            string content = sst.GetContent(tag, documentConfig);
            string expectedContent = "RoboClerk detected problems with the unit testing:\n\n* Result for unit test with ID \"tcid2\" not found in results.\n\n";

            Assert.That(Regex.Replace(content, @"\r\n", "\n"), Is.EqualTo(expectedContent)); //ensure that we're always comparing the correct string, regardless of newline character for a platform
        }

        [UnitTestAttribute(
        Identifier = "8FCFEC1E-2895-4361-8911-7172AE910735",
        Purpose = "Unit Test content creator is created and is supplied with a tag requesting a comparison with results. All unit tests results pass and are present.",
        PostCondition = "Appropriate result string is produced")]
        [Test]
        public void CreateUnitTestCC8()
        {
            var sst = new UnitTest(dataSources, traceAnalysis, config);
            var tag = new RoboClerkTag(0, 34, "@@SLMS:UnitTest(CheckResults=true)@@", true);
            results.Add(new TestResult("tcid2", TestResultType.UNIT, TestResultStatus.PASS, "the second unit test", "all good", DateTime.Now));
            string content = sst.GetContent(tag, documentConfig);
            string expectedContent = "All unit tests from the test plan were successfully executed and passed.";

            Assert.That(Regex.Replace(content, @"\r\n", "\n"), Is.EqualTo(expectedContent)); //ensure that we're always comparing the correct string, regardless of newline character for a platform
        }

        [UnitTestAttribute(
        Identifier = "81D18BAB-F9E7-40BC-9C4F-CD8325D0B25D",
        Purpose = "Unit Test content creator is created and is supplied with a tag requesting a comparison with results. One unit test failed.",
        PostCondition = "Appropriate result string is produced")]
        [Test]
        public void CreateUnitTestCC9()
        {
            var sst = new UnitTest(dataSources, traceAnalysis, config);
            var tag = new RoboClerkTag(0, 34, "@@SLMS:UnitTest(CheckResults=true)@@", true);
            results.Add(new TestResult("tcid2", TestResultType.UNIT, TestResultStatus.FAIL, "the second unit test", "all bad", DateTime.Now));
            string content = sst.GetContent(tag, documentConfig);
            string expectedContent = "RoboClerk detected problems with the unit testing:\n\n* Unit test with ID \"tcid2\" has failed.\n\n";

            Assert.That(Regex.Replace(content, @"\r\n", "\n"), Is.EqualTo(expectedContent)); //ensure that we're always comparing the correct string, regardless of newline character for a platform
        }

        [UnitTestAttribute(
        Identifier = "0105BA97-B7DE-4A8E-91F0-B93162F10B6B",
        Purpose = "Unit Test content creator is created and is supplied with a tag requesting a comparison with results. Results provided for unknown test.",
        PostCondition = "Appropriate result string is produced")]
        [Test]
        public void CreateUnitTestCC10()
        {
            var sst = new UnitTest(dataSources, traceAnalysis, config);
            var tag = new RoboClerkTag(0, 34, "@@SLMS:UnitTest(CheckResults=true)@@", true);
            results.Add(new TestResult("tcid3", TestResultType.UNIT, TestResultStatus.FAIL, "the third unit test", "all bad", DateTime.Now));
            results.Add(new TestResult("tcid2", TestResultType.UNIT, TestResultStatus.PASS, "the second unit test", "all good", DateTime.Now));
            string content = sst.GetContent(tag, documentConfig);
            string expectedContent = "RoboClerk detected problems with the unit testing:\n\n* Result for unit test with ID \"tcid3\" found, but test plan does not contain such a unit test.\n\n";

            Assert.That(Regex.Replace(content, @"\r\n", "\n"), Is.EqualTo(expectedContent)); //ensure that we're always comparing the correct string, regardless of newline character for a platform
        }

        [UnitTestAttribute(
        Identifier = "2A1C86FD-43CD-4EA1-8A25-8C961F6DB433",
        Purpose = "Unit Test content creator handles sorting by UnitTestFileName in ascending order",
        PostCondition = "Unit tests are sorted alphabetically by UnitTestFileName")]
        [Test]
        public void CreateUnitTestCC_SortByFilenameAscending()
        {
            // Arrange
            unittestItems.Clear();
            
            var unittest1 = new UnitTestItem();
            unittest1.ItemID = "test1";
            unittest1.UnitTestFileName = "ZTest.cs";
            unittest1.UnitTestFunctionName = "FirstTest";
            unittest1.UnitTestPurpose = "First test purpose";
            unittest1.UnitTestAcceptanceCriteria = "First criteria";
            unittestItems.Add(unittest1);

            var unittest2 = new UnitTestItem();
            unittest2.ItemID = "test2";
            unittest2.UnitTestFileName = "ATest.cs";
            unittest2.UnitTestFunctionName = "SecondTest";
            unittest2.UnitTestPurpose = "Second test purpose";
            unittest2.UnitTestAcceptanceCriteria = "Second criteria";
            unittestItems.Add(unittest2);

            var unittest3 = new UnitTestItem();
            unittest3.ItemID = "test3";
            unittest3.UnitTestFileName = "MTest.cs";
            unittest3.UnitTestFunctionName = "ThirdTest";
            unittest3.UnitTestPurpose = "Third test purpose";
            unittest3.UnitTestAcceptanceCriteria = "Third criteria";
            unittestItems.Add(unittest3);

            // Update the data source mock
            var te = new TraceEntity("UnitTest", "unittest", "ut", TraceEntityType.Truth);
            dataSources.GetItems(te).Returns(unittestItems);
            dataSources.GetItem("test1").Returns(unittest1);
            dataSources.GetItem("test2").Returns(unittest2);
            dataSources.GetItem("test3").Returns(unittest3);

            var sst = new UnitTest(dataSources, traceAnalysis, config);
            // "@@SLMS:UnitTest(brief=true,sortby=UnitTestFileName)@@" = 53 chars, endIndex = 52
            var tag = new RoboClerkTag(0, 52, "@@SLMS:UnitTest(brief=true,sortby=UnitTestFileName)@@", true);

            // Act
            string content = sst.GetContent(tag, documentConfig);

            // Assert
            // Should be sorted: ATest.cs, MTest.cs, ZTest.cs
            string expectedContent = "|====\n| unittest ID | Function / File Name | unittest Purpose | Acceptance Criteria\n\n| test2 | SecondTest / ATest.cs | Second test purpose | Second criteria \n\n| test3 | ThirdTest / MTest.cs | Third test purpose | Third criteria \n\n| test1 | FirstTest / ZTest.cs | First test purpose | First criteria \n\n|====\n";
            Assert.That(Regex.Replace(content, @"\r\n", "\n"), Is.EqualTo(expectedContent));
        }

        [UnitTestAttribute(
        Identifier = "38957F45-EE0C-4FEB-88C0-E796E694AA1B",
        Purpose = "Unit Test content creator handles sorting by UnitTestFileName in descending order",
        PostCondition = "Unit tests are sorted reverse alphabetically by UnitTestFileName")]
        [Test]
        public void CreateUnitTestCC_SortByFilenameDescending()
        {
            // Arrange
            unittestItems.Clear();
            
            var unittest1 = new UnitTestItem();
            unittest1.ItemID = "test1";
            unittest1.UnitTestFileName = "ZTest.cs";
            unittest1.UnitTestFunctionName = "FirstTest";
            unittest1.UnitTestPurpose = "First test purpose";
            unittest1.UnitTestAcceptanceCriteria = "First criteria";
            unittestItems.Add(unittest1);

            var unittest2 = new UnitTestItem();
            unittest2.ItemID = "test2";
            unittest2.UnitTestFileName = "ATest.cs";
            unittest2.UnitTestFunctionName = "SecondTest";
            unittest2.UnitTestPurpose = "Second test purpose";
            unittest2.UnitTestAcceptanceCriteria = "Second criteria";
            unittestItems.Add(unittest2);

            var unittest3 = new UnitTestItem();
            unittest3.ItemID = "test3";
            unittest3.UnitTestFileName = "MTest.cs";
            unittest3.UnitTestFunctionName = "ThirdTest";
            unittest3.UnitTestPurpose = "Third test purpose";
            unittest3.UnitTestAcceptanceCriteria = "Third criteria";
            unittestItems.Add(unittest3);

            // Update the data source mock
            var te = new TraceEntity("UnitTest", "unittest", "ut", TraceEntityType.Truth);
            dataSources.GetItems(te).Returns(unittestItems);
            dataSources.GetItem("test1").Returns(unittest1);
            dataSources.GetItem("test2").Returns(unittest2);
            dataSources.GetItem("test3").Returns(unittest3);

            var sst = new UnitTest(dataSources, traceAnalysis, config);
            // "@@SLMS:UnitTest(brief=true,sortby=UnitTestFileName,sortorder=desc)@@" = 67 chars, endIndex = 66
            var tag = new RoboClerkTag(0, 66, "@@SLMS:UnitTest(brief=true,sortby=UnitTestFileName,sortorder=desc)@@", true);

            // Act
            string content = sst.GetContent(tag, documentConfig);

            // Assert
            // Should be sorted: ZTest.cs, MTest.cs, ATest.cs
            string expectedContent = "|====\n| unittest ID | Function / File Name | unittest Purpose | Acceptance Criteria\n\n| test1 | FirstTest / ZTest.cs | First test purpose | First criteria \n\n| test3 | ThirdTest / MTest.cs | Third test purpose | Third criteria \n\n| test2 | SecondTest / ATest.cs | Second test purpose | Second criteria \n\n|====\n";
            Assert.That(Regex.Replace(content, @"\r\n", "\n"), Is.EqualTo(expectedContent));
        }

        [UnitTestAttribute(
        Identifier = "B810F3CF-EC2D-43F9-9AF3-DD446E033D3F",
        Purpose = "Unit Test content creator handles sorting by UnitTestFunctionName property",
        PostCondition = "Unit tests are sorted by function name")]
        [Test]
        public void CreateUnitTestCC_SortByFunctionName()
        {
            // Arrange
            unittestItems.Clear();
            
            var unittest1 = new UnitTestItem();
            unittest1.ItemID = "test1";
            unittest1.UnitTestFileName = "TestFile.cs";
            unittest1.UnitTestFunctionName = "ZFunction";
            unittest1.UnitTestPurpose = "First test purpose";
            unittest1.UnitTestAcceptanceCriteria = "First criteria";
            unittestItems.Add(unittest1);

            var unittest2 = new UnitTestItem();
            unittest2.ItemID = "test2";
            unittest2.UnitTestFileName = "TestFile.cs";
            unittest2.UnitTestFunctionName = "AFunction";
            unittest2.UnitTestPurpose = "Second test purpose";
            unittest2.UnitTestAcceptanceCriteria = "Second criteria";
            unittestItems.Add(unittest2);

            // Update the data source mock
            var te = new TraceEntity("UnitTest", "unittest", "ut", TraceEntityType.Truth);
            dataSources.GetItems(te).Returns(unittestItems);
            dataSources.GetItem("test1").Returns(unittest1);
            dataSources.GetItem("test2").Returns(unittest2);

            var sst = new UnitTest(dataSources, traceAnalysis, config);
            var tag = new RoboClerkTag(0, 55, "@@SLMS:UnitTest(brief=true,sortby=UnitTestFunctionName)@@", true);

            // Act
            string content = sst.GetContent(tag, documentConfig);

            // Assert
            // Should be sorted: AFunction, ZFunction
            string expectedContent = "|====\n| unittest ID | Function / File Name | unittest Purpose | Acceptance Criteria\n\n| test2 | AFunction / TestFile.cs | Second test purpose | Second criteria \n\n| test1 | ZFunction / TestFile.cs | First test purpose | First criteria \n\n|====\n";
            Assert.That(Regex.Replace(content, @"\r\n", "\n"), Is.EqualTo(expectedContent));
        }

        [UnitTestAttribute(
        Identifier = "A2FF1996-D9A1-4169-B9B7-AF5E724B2876",
        Purpose = "Unit Test content creator handles invalid sort property gracefully",
        PostCondition = "No sorting is applied and original order is maintained")]
        [Test]
        public void CreateUnitTestCC_SortByInvalidProperty()
        {
            // Arrange
            var sst = new UnitTest(dataSources, traceAnalysis, config);
            // "@@SLMS:UnitTest(brief=true,sortby=InvalidProperty)@@" = 52 chars, endIndex = 51
            var tag = new RoboClerkTag(0, 51, "@@SLMS:UnitTest(brief=true,sortby=InvalidProperty)@@", true);

            // Act
            string content = sst.GetContent(tag, documentConfig);

            // Assert
            // Should maintain original order when invalid property is specified
            string expectedContent = "|====\n| unittest ID | Function / File Name | unittest Purpose | Acceptance Criteria\n\n| tcid1 | functionname / filename | purpose1 | accept1 \n\n| http://localhost/[tcid2] |  /  |  |  \n\n|====\n";
            Assert.That(Regex.Replace(content, @"\r\n", "\n"), Is.EqualTo(expectedContent));
        }
    }
}
