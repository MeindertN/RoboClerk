using NSubstitute;
using NUnit.Framework;
using RoboClerk.Core.Configuration;
using RoboClerk.ContentCreators;
using RoboClerk.Core;
using RoboClerk.Core.ASCIIDOCSupport;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Text.RegularExpressions;

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
            config.OutputFormat.Returns("ASCIIDOC");
            dataSources = Substitute.For<IDataSources>();
            
            traceAnalysis = Substitute.For<ITraceabilityAnalysis>();
            var te = new TraceEntity("UnitTest", "unittest", "ut", TraceEntityType.Truth);
            var teDoc = new TraceEntity("docID", "docTitle", "docAbbr", TraceEntityType.Document);
            traceAnalysis.GetTraceEntityForID("UnitTest").Returns(te);
            traceAnalysis.GetTraceEntityForID("docID").Returns(teDoc);
            traceAnalysis.GetTraceEntityForTitle("docTitle").Returns(teDoc);
            traceAnalysis.GetTitleForTraceEntity("SoftwareRequirement").Returns("Software Requirement");
            traceAnalysis.GetTitleForTraceEntity("SoftwareSystemTest").Returns("Software System Test");
            fs = Substitute.For<IFileSystem>();
            traceAnalysis.GetTraceEntityForAnyProperty("UnitTest").Returns(te);
            documentConfig = new DocumentConfig("UnitLevelTestPlan", "docID", "docTitle", "docAbbr", @"c:\in\template.adoc");

            results.Clear();
            results.Add(new TestResult("tcid1", TestType.UNIT, TestResultStatus.PASS, "the first test", "all good", DateTime.Now));
            results.Add(new TestResult("unit1", TestType.SYSTEM, TestResultStatus.PASS, "the first unit test", "all bad", DateTime.Now));
            results.Add(new TestResult("tcid2", TestType.SYSTEM, TestResultStatus.FAIL, "the first system test", "none good", DateTime.Now));

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
            unittestItem.UnitTestFileName = "";
            unittestItem.UnitTestFunctionName = "";
            unittestItem.UnitTestPurpose = "";
            unittestItem.UnitTestAcceptanceCriteria = "";
            unittestItem.Link = new Uri("http://localhost/");
            unittestItems.Add(unittestItem);

            dataSources.GetAllTestResults().Returns(results);
            dataSources.GetItems(te).Returns(unittestItems);
            dataSources.GetItem("tcid1").Returns(unittestItems[0]);
            dataSources.GetItem("tcid2").Returns(unittestItems[1]);

            dataSources.GetTemplateFile("./ItemTemplates/ASCIIDOC/UnitTest.adoc").Returns(File.ReadAllText("../../../../RoboClerk.Core/ItemTemplates/ASCIIDOC/UnitTest.adoc"));
            dataSources.GetTemplateFile("./ItemTemplates/ASCIIDOC/UnitTest_brief.adoc").Returns(File.ReadAllText("../../../../RoboClerk.Core/ItemTemplates/ASCIIDOC/UnitTest_brief.adoc"));
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
            var tag = new RoboClerkTextTag(0, 31, "@@SLMS:UnitTest(ItemID=tcid1)@@", true);
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
            var tag = new RoboClerkTextTag(0, 31, "@@SLMS:UnitTest(ItemID=tcid1)@@", true);
            
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
            var tag = new RoboClerkTextTag(0, 31, "@@SLMS:UnitTest(ItemID=tcid2)@@", true);
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
            var tag = new RoboClerkTextTag(0, 31, "@@SLMS:UnitTest(ItemID=tcid3)@@", true);
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
            var tag = new RoboClerkTextTag(0, 29, "@@SLMS:UnitTest(brief=true)@@", true);
            string content = sst.GetContent(tag, documentConfig);
            string expectedContent = "|====\n| *File Name* | *Function Name* | *unittest ID* | *Purpose* | *Acceptance* | *Linked Software Requirements* | *Linked Software System Tests* \n\n\n| filename | functionname | tcid1 | purpose1 | accept1 | N/A | N/A \n\n|  |  | http://localhost/[tcid2] |  |  | N/A | N/A \n\n|====\n";

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
            var tag = new RoboClerkTextTag(0, 34, "@@SLMS:UnitTest(CheckResults=true)@@", true);
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
            var tag = new RoboClerkTextTag(0, 34, "@@SLMS:UnitTest(CheckResults=true)@@", true);
            results.Add(new TestResult("tcid2", TestType.UNIT, TestResultStatus.PASS, "the second unit test", "all good", DateTime.Now));
            string content = sst.GetContent(tag, documentConfig);
            string expectedContent = "All unit tests from the test plan were successfully executed and passed.\n";

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
            var tag = new RoboClerkTextTag(0, 34, "@@SLMS:UnitTest(CheckResults=true)@@", true);
            results.Add(new TestResult("tcid2", TestType.UNIT, TestResultStatus.FAIL, "the second unit test", "all bad", DateTime.Now));
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
            var tag = new RoboClerkTextTag(0, 34, "@@SLMS:UnitTest(CheckResults=true)@@", true);
            results.Add(new TestResult("tcid3", TestType.UNIT, TestResultStatus.FAIL, "the third unit test", "all bad", DateTime.Now));
            results.Add(new TestResult("tcid2", TestType.UNIT, TestResultStatus.PASS, "the second unit test", "all good", DateTime.Now));
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
            var tag = new RoboClerkTextTag(0, 52, "@@SLMS:UnitTest(brief=true,sortby=UnitTestFileName)@@", true);

            // Act
            string content = sst.GetContent(tag, documentConfig);

            // Assert - Should be sorted: ATest.cs, MTest.cs, ZTest.cs
            string expectedContent = "|====\n| *File Name* | *Function Name* | *unittest ID* | *Purpose* | *Acceptance* | *Linked Software Requirements* | *Linked Software System Tests* \n\n\n| ATest.cs | SecondTest | test2 | Second test purpose | Second criteria | N/A | N/A \n\n| MTest.cs | ThirdTest | test3 | Third test purpose | Third criteria | N/A | N/A \n\n| ZTest.cs | FirstTest | test1 | First test purpose | First criteria | N/A | N/A \n\n|====\n";
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
            var tag = new RoboClerkTextTag(0, 66, "@@SLMS:UnitTest(brief=true,sortby=UnitTestFileName,sortorder=desc)@@", true);

            // Act
            string content = sst.GetContent(tag, documentConfig);

            // Assert - Should be sorted: ZTest.cs, MTest.cs, ATest.cs
            string expectedContent = "|====\n| *File Name* | *Function Name* | *unittest ID* | *Purpose* | *Acceptance* | *Linked Software Requirements* | *Linked Software System Tests* \n\n\n| ZTest.cs | FirstTest | test1 | First test purpose | First criteria | N/A | N/A \n\n| MTest.cs | ThirdTest | test3 | Third test purpose | Third criteria | N/A | N/A \n\n| ATest.cs | SecondTest | test2 | Second test purpose | Second criteria | N/A | N/A \n\n|====\n";
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
            var tag = new RoboClerkTextTag(0, 55, "@@SLMS:UnitTest(brief=true,sortby=UnitTestFunctionName)@@", true);

            // Act
            string content = sst.GetContent(tag, documentConfig);

            // Assert - Should be sorted: AFunction, ZFunction
            string expectedContent = "|====\n| *File Name* | *Function Name* | *unittest ID* | *Purpose* | *Acceptance* | *Linked Software Requirements* | *Linked Software System Tests* \n\n\n| TestFile.cs | AFunction | test2 | Second test purpose | Second criteria | N/A | N/A \n\n| TestFile.cs | ZFunction | test1 | First test purpose | First criteria | N/A | N/A \n\n|====\n";
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
            var tag = new RoboClerkTextTag(0, 51, "@@SLMS:UnitTest(brief=true,sortby=InvalidProperty)@@", true);

            // Act
            string content = sst.GetContent(tag, documentConfig);

            // Assert - Should maintain original order when invalid property is specified
            string expectedContent = "|====\n| *File Name* | *Function Name* | *unittest ID* | *Purpose* | *Acceptance* | *Linked Software Requirements* | *Linked Software System Tests* \n\n\n| filename | functionname | tcid1 | purpose1 | accept1 | N/A | N/A \n\n|  |  | http://localhost/[tcid2] |  |  | N/A | N/A \n\n|====\n";
            Assert.That(Regex.Replace(content, @"\r\n", "\n"), Is.EqualTo(expectedContent));
        }

        [UnitTestAttribute(
        Identifier = "c2e6dd60-cede-440f-8ddd-962a61c06133",
        Purpose = "Unit Test content creator uses natural sorting for numeric sequences in filenames",
        PostCondition = "Natural sorting treats numeric parts as numbers rather than strings")]
        [Test]
        public void CreateUnitTestCC_NaturalSortingBasic()
        {
            // Arrange - Create test items with filenames that would sort incorrectly with standard string sorting
            unittestItems.Clear();
            
            var unittest1 = new UnitTestItem();
            unittest1.ItemID = "test1";
            unittest1.UnitTestFileName = "Test10.cs";
            unittest1.UnitTestFunctionName = "Function1";
            unittest1.UnitTestPurpose = "Test purpose 1";
            unittest1.UnitTestAcceptanceCriteria = "Criteria 1";
            unittestItems.Add(unittest1);

            var unittest2 = new UnitTestItem();
            unittest2.ItemID = "test2";
            unittest2.UnitTestFileName = "Test2.cs";
            unittest2.UnitTestFunctionName = "Function2";
            unittest2.UnitTestPurpose = "Test purpose 2";
            unittest2.UnitTestAcceptanceCriteria = "Criteria 2";
            unittestItems.Add(unittest2);

            var unittest3 = new UnitTestItem();
            unittest3.ItemID = "test3";
            unittest3.UnitTestFileName = "Test1.cs";
            unittest3.UnitTestFunctionName = "Function3";
            unittest3.UnitTestPurpose = "Test purpose 3";
            unittest3.UnitTestAcceptanceCriteria = "Criteria 3";
            unittestItems.Add(unittest3);

            // Update the data source mock
            var te = new TraceEntity("UnitTest", "unittest", "ut", TraceEntityType.Truth);
            dataSources.GetItems(te).Returns(unittestItems);
            dataSources.GetItem("test1").Returns(unittest1);
            dataSources.GetItem("test2").Returns(unittest2);
            dataSources.GetItem("test3").Returns(unittest3);

            var sst = new UnitTest(dataSources, traceAnalysis, config);
            var tag = new RoboClerkTextTag(0, 52, "@@SLMS:UnitTest(brief=true,sortby=UnitTestFileName)@@", true);

            // Act
            string content = sst.GetContent(tag, documentConfig);

            // Assert - Natural sorting should give: Test1.cs, Test2.cs, Test10.cs
            // (NOT the string sort order: Test1.cs, Test10.cs, Test2.cs)
            string expectedContent = "|====\n| *File Name* | *Function Name* | *unittest ID* | *Purpose* | *Acceptance* | *Linked Software Requirements* | *Linked Software System Tests* \n\n\n| Test1.cs | Function3 | test3 | Test purpose 3 | Criteria 3 | N/A | N/A \n\n| Test2.cs | Function2 | test2 | Test purpose 2 | Criteria 2 | N/A | N/A \n\n| Test10.cs | Function1 | test1 | Test purpose 1 | Criteria 1 | N/A | N/A \n\n|====\n";
            Assert.That(Regex.Replace(content, @"\r\n", "\n"), Is.EqualTo(expectedContent));
        }

        [UnitTestAttribute(
        Identifier = "8441835e-ff4c-4b52-84b7-7ca24a3e7f0a",
        Purpose = "Unit Test content creator handles complex natural sorting with multiple numeric parts",
        PostCondition = "Complex alphanumeric strings are sorted naturally")]
        [Test]
        public void CreateUnitTestCC_NaturalSortingComplex()
        {
            // Arrange - Create test items with complex alphanumeric patterns
            unittestItems.Clear();
            
            var tests = new[]
            {
                ("test1", "Version1.2.10.cs", "TestVersion1_2_10"),
                ("test2", "Version1.2.2.cs", "TestVersion1_2_2"),
                ("test3", "Version1.10.1.cs", "TestVersion1_10_1"),
                ("test4", "Version1.2.3.cs", "TestVersion1_2_3"),
                ("test5", "Version2.1.1.cs", "TestVersion2_1_1")
            };

            foreach (var (id, fileName, funcName) in tests)
            {
                var unittest = new UnitTestItem();
                unittest.ItemID = id;
                unittest.UnitTestFileName = fileName;
                unittest.UnitTestFunctionName = funcName;
                unittest.UnitTestPurpose = $"Purpose for {id}";
                unittest.UnitTestAcceptanceCriteria = $"Criteria for {id}";
                unittestItems.Add(unittest);
            }

            // Update the data source mock
            var te = new TraceEntity("UnitTest", "unittest", "ut", TraceEntityType.Truth);
            dataSources.GetItems(te).Returns(unittestItems);
            foreach (var (id, _, _) in tests)
            {
                dataSources.GetItem(id).Returns(unittestItems.Find(t => t.ItemID == id));
            }

            var sst = new UnitTest(dataSources, traceAnalysis, config);
            var tag = new RoboClerkTextTag(0, 52, "@@SLMS:UnitTest(brief=true,sortby=UnitTestFileName)@@", true);

            // Act
            string content = sst.GetContent(tag, documentConfig);

            // Assert - Natural sorting should give proper version ordering
            // Expected order: Version1.2.2.cs, Version1.2.3.cs, Version1.2.10.cs, Version1.10.1.cs, Version2.1.1.cs
            Assert.That(content.Contains("Version1.2.2.cs"), Is.True);
            Assert.That(content.Contains("Version1.2.3.cs"), Is.True);
            Assert.That(content.Contains("Version1.2.10.cs"), Is.True);
            Assert.That(content.Contains("Version1.10.1.cs"), Is.True);
            Assert.That(content.Contains("Version2.1.1.cs"), Is.True);
            
            // Verify the order by checking positions
            int pos122 = content.IndexOf("Version1.2.2.cs");
            int pos123 = content.IndexOf("Version1.2.3.cs");
            int pos1210 = content.IndexOf("Version1.2.10.cs");
            int pos1101 = content.IndexOf("Version1.10.1.cs");
            int pos211 = content.IndexOf("Version2.1.1.cs");
            
            Assert.That(pos122 < pos123, "Version1.2.2.cs should come before Version1.2.3.cs");
            Assert.That(pos123 < pos1210, "Version1.2.3.cs should come before Version1.2.10.cs");
            Assert.That(pos1210 < pos1101, "Version1.2.10.cs should come before Version1.10.1.cs");
            Assert.That(pos1101 < pos211, "Version1.10.1.cs should come before Version2.1.1.cs");
        }

        [UnitTestAttribute(
        Identifier = "568cc5c3-ae4d-4216-b0fb-263ecd512773",
        Purpose = "Unit Test content creator handles natural sorting with ItemID field containing numbers",
        PostCondition = "ItemID fields with numeric parts are sorted naturally")]
        [Test]
        public void CreateUnitTestCC_NaturalSortingItemID()
        {
            // Arrange - Create test items with ItemIDs that demonstrate natural sorting
            unittestItems.Clear();
            
            var tests = new[]
            {
                ("TEST_100", "TestFile.cs", "Function100"),
                ("TEST_2", "TestFile.cs", "Function2"),
                ("TEST_10", "TestFile.cs", "Function10"),
                ("TEST_1", "TestFile.cs", "Function1"),
                ("TEST_20", "TestFile.cs", "Function20")
            };

            foreach (var (id, fileName, funcName) in tests)
            {
                var unittest = new UnitTestItem();
                unittest.ItemID = id;
                unittest.UnitTestFileName = fileName;
                unittest.UnitTestFunctionName = funcName;
                unittest.UnitTestPurpose = $"Purpose for {id}";
                unittest.UnitTestAcceptanceCriteria = $"Criteria for {id}";
                unittestItems.Add(unittest);
            }

            // Update the data source mock
            var te = new TraceEntity("UnitTest", "unittest", "ut", TraceEntityType.Truth);
            dataSources.GetItems(te).Returns(unittestItems);
            foreach (var (id, _, _) in tests)
            {
                dataSources.GetItem(id).Returns(unittestItems.Find(t => t.ItemID == id));
            }

            var sst = new UnitTest(dataSources, traceAnalysis, config);
            var tag = new RoboClerkTextTag(0, 43, "@@SLMS:UnitTest(brief=true,sortby=ItemID)@@", true);

            // Act
            string content = sst.GetContent(tag, documentConfig);

            // Assert - Natural sorting should give: TEST_1, TEST_2, TEST_10, TEST_20, TEST_100
            // (NOT string sort: TEST_1, TEST_10, TEST_100, TEST_2, TEST_20)
            int pos1 = content.IndexOf("TEST_1");
            int pos2 = content.IndexOf("TEST_2");
            int pos10 = content.IndexOf("TEST_10");
            int pos20 = content.IndexOf("TEST_20");
            int pos100 = content.IndexOf("TEST_100");
            
            Assert.That(pos1 < pos2, "TEST_1 should come before TEST_2");
            Assert.That(pos2 < pos10, "TEST_2 should come before TEST_10");
            Assert.That(pos10 < pos20, "TEST_10 should come before TEST_20");
            Assert.That(pos20 < pos100, "TEST_20 should come before TEST_100");
        }

        [UnitTestAttribute(
        Identifier = "2a0e51dd-d7e7-44dc-899d-cbe9b5dbf2cd",
        Purpose = "Unit Test content creator handles natural sorting with mixed text and numbers",
        PostCondition = "Text and numeric parts are sorted appropriately")]
        [Test]
        public void CreateUnitTestCC_NaturalSortingMixed()
        {
            // Arrange - Create test items that mix text and numbers
            unittestItems.Clear();
            
            var tests = new[]
            {
                ("test1", "B10File.cs", "FunctionB10"),
                ("test2", "A2File.cs", "FunctionA2"),
                ("test3", "B2File.cs", "FunctionB2"),
                ("test4", "A10File.cs", "FunctionA10"),
                ("test5", "C1File.cs", "FunctionC1")
            };

            foreach (var (id, fileName, funcName) in tests)
            {
                var unittest = new UnitTestItem();
                unittest.ItemID = id;
                unittest.UnitTestFileName = fileName;
                unittest.UnitTestFunctionName = funcName;
                unittest.UnitTestPurpose = $"Purpose for {id}";
                unittest.UnitTestAcceptanceCriteria = $"Criteria for {id}";
                unittestItems.Add(unittest);
            }

            // Update the data source mock
            var te = new TraceEntity("UnitTest", "unittest", "ut", TraceEntityType.Truth);
            dataSources.GetItems(te).Returns(unittestItems);
            foreach (var (id, _, _) in tests)
            {
                dataSources.GetItem(id).Returns(unittestItems.Find(t => t.ItemID == id));
            }

            var sst = new UnitTest(dataSources, traceAnalysis, config);
            var tag = new RoboClerkTextTag(0, 52, "@@SLMS:UnitTest(brief=true,sortby=UnitTestFileName)@@", true);

            // Act
            string content = sst.GetContent(tag, documentConfig);

            // Assert - Natural sorting should give: A2File.cs, A10File.cs, B2File.cs, B10File.cs, C1File.cs
            int posA2 = content.IndexOf("A2File.cs");
            int posA10 = content.IndexOf("A10File.cs");
            int posB2 = content.IndexOf("B2File.cs");
            int posB10 = content.IndexOf("B10File.cs");
            int posC1 = content.IndexOf("C1File.cs");
            
            Assert.That(posA2 < posA10, "A2File.cs should come before A10File.cs");
            Assert.That(posA10 < posB2, "A10File.cs should come before B2File.cs");
            Assert.That(posB2 < posB10, "B2File.cs should come before B10File.cs");
            Assert.That(posB10 < posC1, "B10File.cs should come before C1File.cs");
        }

        [UnitTestAttribute(
        Identifier = "85d3a000-5e6e-4acf-8b55-e75d5b6febf2",
        Purpose = "Unit Test content creator handles natural sorting with leading zeros",
        PostCondition = "Leading zeros in numeric parts are handled correctly")]
        [Test]
        public void CreateUnitTestCC_NaturalSortingLeadingZeros()
        {
            // Arrange - Create test items with leading zeros
            unittestItems.Clear();
            
            var tests = new[]
            {
                ("test1", "File001.cs", "Function001"),
                ("test2", "File010.cs", "Function010"),
                ("test3", "File002.cs", "Function002"),
                ("test4", "File100.cs", "Function100"),
                ("test5", "File020.cs", "Function020")
            };

            foreach (var (id, fileName, funcName) in tests)
            {
                var unittest = new UnitTestItem();
                unittest.ItemID = id;
                unittest.UnitTestFileName = fileName;
                unittest.UnitTestFunctionName = funcName;
                unittest.UnitTestPurpose = $"Purpose for {id}";
                unittest.UnitTestAcceptanceCriteria = $"Criteria for {id}";
                unittestItems.Add(unittest);
            }

            // Update the data source mock
            var te = new TraceEntity("UnitTest", "unittest", "ut", TraceEntityType.Truth);
            dataSources.GetItems(te).Returns(unittestItems);
            foreach (var (id, _, _) in tests)
            {
                dataSources.GetItem(id).Returns(unittestItems.Find(t => t.ItemID == id));
            }

            var sst = new UnitTest(dataSources, traceAnalysis, config);
            var tag = new RoboClerkTextTag(0, 52, "@@SLMS:UnitTest(brief=true,sortby=UnitTestFileName)@@", true);

            // Act
            string content = sst.GetContent(tag, documentConfig);

            // Assert - Natural sorting should treat these as numbers: File001.cs, File002.cs, File010.cs, File020.cs, File100.cs
            int pos001 = content.IndexOf("File001.cs");
            int pos002 = content.IndexOf("File002.cs");
            int pos010 = content.IndexOf("File010.cs");
            int pos020 = content.IndexOf("File020.cs");
            int pos100 = content.IndexOf("File100.cs");
            
            Assert.That(pos001 < pos002, "File001.cs should come before File002.cs");
            Assert.That(pos002 < pos010, "File002.cs should come before File010.cs");
            Assert.That(pos010 < pos020, "File010.cs should come before File020.cs");
            Assert.That(pos020 < pos100, "File020.cs should come before File100.cs");
        }

        [UnitTestAttribute(
        Identifier = "d1636e3e-497a-4646-b02d-fe93a983a6fe",
        Purpose = "Unit Test content creator handles natural sorting with descending order",
        PostCondition = "Natural sorting works correctly in descending order")]
        [Test]
        public void CreateUnitTestCC_NaturalSortingDescending()
        {
            // Arrange - Create test items with numeric filenames
            unittestItems.Clear();
            
            var tests = new[]
            {
                ("test1", "Test1.cs", "Function1"),
                ("test2", "Test10.cs", "Function10"),
                ("test3", "Test2.cs", "Function2"),
                ("test4", "Test20.cs", "Function20")
            };

            foreach (var (id, fileName, funcName) in tests)
            {
                var unittest = new UnitTestItem();
                unittest.ItemID = id;
                unittest.UnitTestFileName = fileName;
                unittest.UnitTestFunctionName = funcName;
                unittest.UnitTestPurpose = $"Purpose for {id}";
                unittest.UnitTestAcceptanceCriteria = $"Criteria for {id}";
                unittestItems.Add(unittest);
            }

            // Update the data source mock
            var te = new TraceEntity("UnitTest", "unittest", "ut", TraceEntityType.Truth);
            dataSources.GetItems(te).Returns(unittestItems);
            foreach (var (id, _, _) in tests)
            {
                dataSources.GetItem(id).Returns(unittestItems.Find(t => t.ItemID == id));
            }

            var sst = new UnitTest(dataSources, traceAnalysis, config);
            var tag = new RoboClerkTextTag(0, 66, "@@SLMS:UnitTest(brief=true,sortby=UnitTestFileName,sortorder=desc)@@", true);

            // Act
            string content = sst.GetContent(tag, documentConfig);

            // Assert - Natural sorting descending should give: Test20.cs, Test10.cs, Test2.cs, Test1.cs
            int pos20 = content.IndexOf("Test20.cs");
            int pos10 = content.IndexOf("Test10.cs");
            int pos2 = content.IndexOf("Test2.cs");
            int pos1 = content.IndexOf("Test1.cs");
            
            Assert.That(pos20 < pos10, "Test20.cs should come before Test10.cs in descending order");
            Assert.That(pos10 < pos2, "Test10.cs should come before Test2.cs in descending order");
            Assert.That(pos2 < pos1, "Test2.cs should come before Test1.cs in descending order");
        }
    }
}
