using NSubstitute;
using NUnit.Framework;
using RoboClerk.Configuration;
using RoboClerk.ContentCreators;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("These tests test the Traceability Matrix")]
    internal class TestTraceMatrix
    {
        private IConfiguration config = null;
        private IDataSources dataSources = null;
        private ITraceabilityAnalysis traceAnalysis = null;
        private DocumentConfig documentConfig = null;
        private RoboClerkOrderedDictionary<TraceEntity, List<List<Item>>> matrix = null;

        [SetUp]
        public void TestSetup()
        {
            config = Substitute.For<IConfiguration>();
            dataSources = Substitute.For<IDataSources>();
            traceAnalysis = Substitute.For<ITraceabilityAnalysis>();
            var te = new TraceEntity("SystemRequirement", "Requirement", "SYS", TraceEntityType.Truth);
            var teSWR = new TraceEntity("SoftwareRequirement", "Specification", "SWR", TraceEntityType.Truth);
            var teDoc = new TraceEntity("SystemRequirementsSpec", "System Requirements Specification", "SRS", TraceEntityType.Document);
            traceAnalysis.GetTraceEntityForID("SystemRequirement").Returns(te);
            traceAnalysis.GetTraceEntityForID("SoftwareRequirement").Returns(teSWR);
            traceAnalysis.GetTraceEntityForID("SystemRequirementsSpec").Returns(teDoc);
            documentConfig = new DocumentConfig("SoftwareRequirementsSpecification", "docID", "docTitle", "docAbbr", @"c:\in\template.adoc");

            matrix = new RoboClerkOrderedDictionary<TraceEntity, List<List<Item>>>();
            matrix[te] = new List<List<Item>>()
            {
                new List<Item>() { new RequirementItem(RequirementType.SystemRequirement) { ItemID = "SYS1" } },
                new List<Item>() { new RequirementItem(RequirementType.SystemRequirement) { ItemID = "SYS2" } }
            };
            dataSources.GetItem("SYS1").Returns(matrix[te][0][0]);
            dataSources.GetItem("SYS2").Returns(matrix[te][1][0]);
            matrix[teSWR] = new List<List<Item>>()
            {
                new List<Item>()
                {
                    new RequirementItem(RequirementType.SoftwareRequirement) { ItemID = "SYS1_SWR1" },
                    new RequirementItem(RequirementType.SoftwareRequirement) { ItemID = "SYS1_SWR2" }
                },
                new List<Item>() { new RequirementItem(RequirementType.SoftwareRequirement) { ItemID = "SYS2_SWR3" } }
            };
            matrix[teDoc] = new List<List<Item>>()
            {
                new List<Item>()
                {
                    new RequirementItem(RequirementType.SystemRequirement) { ItemID = "SYS1" },
                },
                new List<Item>()
                {
                    new RequirementItem(RequirementType.SystemRequirement) { ItemID = "SYS2" }
                }
            };

            traceAnalysis.PerformAnalysis(dataSources, te).Returns(matrix);
        }

        [UnitTestAttribute(
        Identifier = "C273346D-DC82-4E3E-A5C6-C3D228320A55",
        Purpose = "TraceabilityMatrixBase is created",
        PostCondition = "No exception is thrown")]
        [Test]
        public void CreateTraceMatrix()
        {
            var trace = new TraceMatrix(dataSources, traceAnalysis);
        }

        [UnitTestAttribute(
        Identifier = "B5F9382A-BC7E-4907-BBE7-C82E95CC49E6",
        Purpose = "TraceabilityMatrixBase is created, a tag is provided and trace is initiated",
        PostCondition = "An appropriate trace matrix is generated")]
        [Test]
        public void TestTraceMatrix1()
        {
            var trace = new TraceMatrix(dataSources, traceAnalysis);
            var tag = new RoboClerkTag(0, 46, "@@SLMS:TraceMatrix(source=SystemRequirement)@@", true);
            string result = trace.GetContent(tag, documentConfig);
            string expectedValue = "|====\n| Requirements | Specifications | SRS \n| SYS1 | SYS1_SWR1, SYS1_SWR2 | SYS1 \n| SYS2 | SYS2_SWR3 | SYS2 \n|====\n\n\nTrace issues:\n\n* No Requirement level trace problems detected!\n";

            Assert.That(Regex.Replace(result, @"\r\n", "\n"), Is.EqualTo(expectedValue));
        }

        [UnitTestAttribute(
        Identifier = "BDC7A886-BD03-4F22-9A28-DAD42E6A425B",
        Purpose = "TraceabilityMatrixBase is created and a tag with unknown source is provided",
        PostCondition = "Expected exception is thrown")]
        [Test]
        public void TestTraceMatrix2()
        {
            var trace = new TraceMatrix(dataSources, traceAnalysis);
            var tag = new RoboClerkTag(0, 36, "@@SLMS:TraceMatrix(source=Unknown)@@", true);
            var ex = Assert.Throws<Exception>(()=>trace.GetContent(tag, documentConfig));
            Assert.That(ex.Message.Contains("Truth source is null"));
        }

        [UnitTestAttribute(
        Identifier = "AB96DA53-B253-4282-80E5-BA6F201703B2",
        Purpose = "TraceabilityMatrixBase is created and a tag without source is provided",
        PostCondition = "Expected exception is thrown")]
        [Test]
        public void TestTraceMatrix3()
        {
            var trace = new TraceMatrix(dataSources, traceAnalysis);
            var tag = new RoboClerkTag(0, 22, "@@SLMS:TraceMatrix()@@", true);
            var ex = Assert.Throws<Exception>(() => trace.GetContent(tag, documentConfig));
            Assert.That(ex.Message.Contains("Unable to find trace source"));
        }

        [UnitTestAttribute(
        Identifier = "9B26C944-4500-4E02-8F3D-73F29E9F974E",
        Purpose = "TraceabilityMatrixBase is created, a tag is provided and trace is initiated. The analysis returns an empty trace.",
        PostCondition = "The expected exception is thrown.")]
        [Test]
        public void TestTraceMatrix4()
        {
            var trace = new TraceMatrix(dataSources, traceAnalysis);
            var tag = new RoboClerkTag(0, 46, "@@SLMS:TraceMatrix(source=SystemRequirement)@@", true);
            matrix.Clear();
            var ex = Assert.Throws<Exception>(() => trace.GetContent(tag, documentConfig));
            Assert.That(ex.Message.Contains("Requirement level trace matrix is empty"));
        }

        [UnitTestAttribute(
        Identifier = "11AD688E-6BF6-4791-BE1A-AC2E2C71A398",
        Purpose = "TraceabilityMatrixBase is created, a tag is provided and trace is initiated. The trace contains a non-required missing trace.",
        PostCondition = "The expected output, containing a N/A entry is produced.")]
        [Test]
        public void TestTraceMatrix5()
        {
            var trace = new TraceMatrix(dataSources, traceAnalysis);
            var tag = new RoboClerkTag(0, 46, "@@SLMS:TraceMatrix(source=SystemRequirement)@@", true);
            var te = traceAnalysis.GetTraceEntityForID("SoftwareRequirement");
            matrix[te][1].Clear();
            string result = trace.GetContent(tag, documentConfig);
            string expectedValue = "|====\n| Requirements | Specifications | SRS \n| SYS1 | SYS1_SWR1, SYS1_SWR2 | SYS1 \n| SYS2 | N/A | SYS2 \n|====\n\n\nTrace issues:\n\n* No Requirement level trace problems detected!\n";

            Assert.That(Regex.Replace(result, @"\r\n", "\n"), Is.EqualTo(expectedValue));
        }

        [UnitTestAttribute(
        Identifier = "CADF9157-77A4-4391-8D79-7C7B0A7D8562",
        Purpose = "TraceabilityMatrixBase is created, a tag is provided and trace is initiated. The trace contains a required missing trace.",
        PostCondition = "The expected output, containing a MISSING entry is produced.")]
        [Test]
        public void TestTraceMatrix6()
        {
            var trace = new TraceMatrix(dataSources, traceAnalysis);
            var tag = new RoboClerkTag(0, 46, "@@SLMS:TraceMatrix(source=SystemRequirement)@@", true);
            var te = traceAnalysis.GetTraceEntityForID("SoftwareRequirement");
            matrix[te][0].Clear();
            matrix[te][0].Add(null);
            string result = trace.GetContent(tag, documentConfig);
            string expectedValue = "|====\n| Requirements | Specifications | SRS \n| SYS1 | MISSING | SYS1 \n| SYS2 | SYS2_SWR3 | SYS2 \n|====\n\n\nTrace issues:\n\n* No Requirement level trace problems detected!\n";

            Assert.That(Regex.Replace(result, @"\r\n", "\n"), Is.EqualTo(expectedValue));
        }

        [UnitTestAttribute(
        Identifier = "F3A08500-1384-4A30-8E18-890FB5F7624A",
        Purpose = "TraceabilityMatrixBase is created, a tag is provided and trace is initiated. A truth trace issue has been identified.",
        PostCondition = "The expected output, with truth trace issue is produced.")]
        [Test]
        public void TestTraceMatrix7()
        {
            var trace = new TraceMatrix(dataSources, traceAnalysis);
            var tag = new RoboClerkTag(0, 46, "@@SLMS:TraceMatrix(source=SystemRequirement)@@", true);
            var te = traceAnalysis.GetTraceEntityForID("SystemRequirement");
            var teSWR = traceAnalysis.GetTraceEntityForID("SoftwareRequirement");
            TraceIssue trcissue = new TraceIssue(te, "SYS1", teSWR, "SWR5", TraceIssueType.Missing);
            traceAnalysis.GetTraceIssuesForTruth(te).Returns(new List<TraceIssue>() { trcissue });
            string result = trace.GetContent(tag, documentConfig);
            string expectedValue = "|====\n| Requirements | Specifications | SRS \n| SYS1 | SYS1_SWR1, SYS1_SWR2 | SYS1 \n| SYS2 | SYS2_SWR3 | SYS2 \n|====\n\n\nTrace issues:\n\n. Requirement SYS1 is potentially missing a corresponding Specification.\n";

            Assert.That(Regex.Replace(result, @"\r\n", "\n"), Is.EqualTo(expectedValue));
        }

        [UnitTestAttribute(
        Identifier = "3F2F7A51-A100-4853-B272-6372D442561A",
        Purpose = "TraceabilityMatrixBase is created, a tag is provided and trace is initiated. A number of document trace issues have been identified.",
        PostCondition = "The expected output, with document trace issue is produced.")]
        [Test]
        public void TestTraceMatrix8()
        {
            var trace = new TraceMatrix(dataSources, traceAnalysis);
            var tag = new RoboClerkTag(0, 46, "@@SLMS:TraceMatrix(source=SystemRequirement)@@", true);
            var te = traceAnalysis.GetTraceEntityForID("SystemRequirement");
            var teSWR = traceAnalysis.GetTraceEntityForID("SoftwareRequirement");
            var teDOC = traceAnalysis.GetTraceEntityForID("SystemRequirementsSpec");
            TraceIssue trcissue1 = new TraceIssue(te, "SYS1", teDOC, "docID", TraceIssueType.Missing);
            TraceIssue trcissue5 = new TraceIssue(te, "SYS1", teDOC, "docID", TraceIssueType.PossiblyMissing);
            TraceIssue trcissue2 = new TraceIssue(teDOC, "docID", te, "SYS1", TraceIssueType.Extra);
            TraceIssue trcissue4 = new TraceIssue(teDOC, "docID", te, "SYS1", TraceIssueType.PossiblyExtra);
            TraceIssue trcissue3 = new TraceIssue(teDOC, "docID", te, "SYS1", TraceIssueType.Incorrect);
            TraceIssue trcissue6 = new TraceIssue(teDOC, "docID", te, "SYS5", TraceIssueType.Incorrect);
            TraceIssue trcissue7 = new TraceIssue(teDOC, "docID", te, null, TraceIssueType.Incorrect);

            traceAnalysis.GetTraceIssuesForDocument(teDOC).Returns(new List<TraceIssue>() { trcissue1, trcissue2, trcissue3, trcissue4, trcissue5, trcissue6, trcissue7 });
            string result = trace.GetContent(tag, documentConfig);
            string expectedValue = "|====\n| Requirements | Specifications | SRS \n| SYS1 | SYS1_SWR1, SYS1_SWR2 | SYS1 \n| SYS2 | SYS2_SWR3 | SYS2 \n|====\n\n\nTrace issues:\n\n. An expected trace from SYS1 in Requirement to System Requirements Specification is missing.\n. An item with identifier docID appeared in System Requirements Specification without tracing to Requirement.\n. An incorrect trace was found in System Requirements Specification from docID to SYS1 where SYS1 was expected in Requirement but was not found.\n. A possibly extra item with identifier docID appeared in System Requirements Specification without appearing in Requirement.\n. A possibly expected trace from SYS1 in Requirement to System Requirements Specification is missing.\n. An incorrect trace was found in System Requirements Specification from docID to SYS5 where SYS5 was expected in Requirement but was not a valid identifier.\n. A missing trace was detected in System Requirements Specification. The item with ID docID does not have a parent while it was expected to trace to Requirement.\n";

            Assert.That(Regex.Replace(result, @"\r\n", "\n"), Is.EqualTo(expectedValue));
        }



    }
}
