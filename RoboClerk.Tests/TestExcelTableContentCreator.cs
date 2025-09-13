using ClosedXML.Excel;
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
using System.IO.Abstractions.TestingHelpers;
using System.Text.RegularExpressions;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("These tests test the Excel Table content creator")]
    internal class TestExcelTableContentCreator
    {
        private IConfiguration config = null;
        private IDataSources dataSources = null;
        private ITraceabilityAnalysis traceAnalysis = null;
        private IFileSystem fs = null;
        private DocumentConfig documentConfig = null;

        [SetUp]
        public void TestSetup()
        {
            config = Substitute.For<IConfiguration>();
            config.OutputFormat.Returns("ASCIIDOC");  // Add this line
            dataSources = Substitute.For<IDataSources>();
            traceAnalysis = Substitute.For<ITraceabilityAnalysis>();
            var te = new TraceEntity("SOUP", "soup", "spabrrv", TraceEntityType.Truth);
            var teDoc = new TraceEntity("docID", "docTitle", "docAbbr", TraceEntityType.Document);
            traceAnalysis.GetTraceEntityForID("docID").Returns(teDoc);
            documentConfig = new DocumentConfig("SoftwareRequirementsSpecification", "docID", "docTitle", "docAbbr", @"c:\in\template.adoc");

            fs = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"c:\out\placeholder.bin", new MockFileData(new byte[] { 0x11, 0x33, 0x55, 0xd1 }) },
            });

            //create an excel file from scratch and save it to the mocked filesystem
            var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("testworksheet");
            ws.Cell("B2").SetValue("testvalueb2").Style.Font.Bold = true;
            ws.Cell("C2").SetValue("testvaluec3").Style.Font.Italic = true;
            ws.Cell("B4").SetValue("testvalueb4");
            ws.Cell("C4").SetValue("testvaluec4").SetHyperlink(new XLHyperlink(new Uri("http://localhost/")));

            //var stream = fs.FileStream.Create(@"C:\temp\test.xlsx",FileMode.Create);
            var ms = new MemoryStream();
            wb.SaveAs(ms);
            ms.Position = 0;

            //stream = fs.FileStream.Create(@"C:\temp\test.xlsx", FileMode.Open);
            dataSources.GetFileStreamFromTemplateDir(@"test.xlsx").Returns(ms);
        }

        [UnitTestAttribute(
        Identifier = "168376B3-4824-48FB-A1D0-C347A487A2D7",
        Purpose = "Excel Table content creator is created",
        PostCondition = "No exception is thrown")]
        [Test]
        public void CreateExcelTableCC()
        {
            var et = new ExcelTable(dataSources, traceAnalysis, config);
        }

        [UnitTestAttribute(
        Identifier = "B250826E-83D8-4053-9287-A8F84CECC9D3",
        Purpose = "Excel Table content creator is created, a tag is provided",
        PostCondition = "Appropriate table is returned")]
        [Test]
        public void TestExcelTableCC1()
        {
            var et = new ExcelTable(dataSources, traceAnalysis, config);
            var tag = new RoboClerkTextTag(0, 75, "@@FILE:exceltable(fileName=test.xlsx,range=B2:C4,workSheet=testworksheet)@@", true);

            string result = et.GetContent(tag, documentConfig);
            string expectedResult = "|===\n| *testvalueb2* | _testvaluec3_ \n\n|  |  \n\n| testvalueb4 | http://localhost/[testvaluec4] \n\n|===\n";

            Assert.That(Regex.Replace(result, @"\r\n", "\n"), Is.EqualTo(expectedResult));
        }

        [UnitTestAttribute(
        Identifier = "815E171E-A6F0-4658-A78E-D22656164C33",
        Purpose = "Excel Table content creator is created, a tag with invalid filename is provided",
        PostCondition = "Exception is thrown")]
        [Test]
        public void TestExcelTableCC2()
        {
            var et = new ExcelTable(dataSources, traceAnalysis, config);
            var tag = new RoboClerkTextTag(0, 78, "@@FILE:exceltable(fileName=unknown.xlsx,range=B2:C4,workSheet=testworksheet)@@", true);
            dataSources.GetFileStreamFromTemplateDir(@"unknown.xlsx").Returns(x => throw new Exception("Can't find file"));

            Assert.Throws<Exception>(()=>et.GetContent(tag, documentConfig));
        }

    }
}
