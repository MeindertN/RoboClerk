using NSubstitute;
using NUnit.Framework;
using RoboClerk.Configuration;
using System.Collections.Generic;
using System.IO.Abstractions;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("These tests test the RoboClerk Eliminated Item content creator")]
    internal class TestEliminatedContentCreator
    {
        private IConfiguration config = null;
        private IDataSources dataSources = null;
        private ITraceabilityAnalysis traceAnalysis = null;
        private IFileSystem fs = null;
        private DocumentConfig documentConfig = null;
        private List<EliminatedLinkedItem> eliminatedItems = new List<EliminatedLinkedItem>();

        [SetUp]
        public void TestSetup()
        {
            config = Substitute.For<IConfiguration>();
            dataSources = Substitute.For<IDataSources>();
            /*
            dataSources.GetAllEliminatedRisks());
            eliminatedItems.AddRange(data.GetAllEliminatedSystemRequirements());
            eliminatedItems.AddRange(data.GetAllEliminatedSoftwareRequirements());
            eliminatedItems.AddRange(data.GetAllEliminatedDocumentationRequirements());
            eliminatedItems.AddRange(data.GetAllEliminatedSoftwareSystemTests());
            eliminatedItems.AddRange(data.GetAllEliminatedDocContents());
            eliminatedItems.AddRange(data.GetAllEliminatedAnomalies());
            eliminatedItems.AddRange(data.GetAllEliminatedSOUP());

            fs = Substitute.For<IFileSystem>();
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
            dataSources.GetTemplateFile("./ItemTemplates/UnitTest_brief.adoc").Returns(File.ReadAllText("../../../../RoboClerk/ItemTemplates/UnitTest_brief.adoc"));*/
        }



    }
}
