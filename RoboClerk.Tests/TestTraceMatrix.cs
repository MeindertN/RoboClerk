using NSubstitute;
using NUnit.Framework;
using RoboClerk.Core.Configuration;
using RoboClerk.ContentCreators;
using RoboClerk.Core;
using RoboClerk.Core.ASCIIDOCSupport;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

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
            config.OutputFormat.Returns("ASCIIDOC");
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
            var trace = new TraceMatrix(dataSources, traceAnalysis, config);
        }

        [UnitTestAttribute(
        Identifier = "B5F9382A-BC7E-4907-BBE7-C82E95CC49E6",
        Purpose = "TraceabilityMatrixBase is created, a tag is provided and trace is initiated",
        PostCondition = "An appropriate trace matrix is generated")]
        [Test]
        public void TestTraceMatrix1()
        {
            var trace = new TraceMatrix(dataSources, traceAnalysis, config);
            var tag = new RoboClerkTextTag(0, 46, "@@SLMS:TraceMatrix(source=SystemRequirement)@@", true);
            string result = trace.GetContent(tag, documentConfig);
            string expectedValue = "|====\n| Requirements | Specifications | SRS \n| SYS1 | SYS1_SWR1, SYS1_SWR2 | Trace Present \n| SYS2 | SYS2_SWR3 | Trace Present \n|====\n\n\nTrace issues:\n\n. No Requirement level trace problems detected!\n";

            Assert.That(Regex.Replace(result, @"\r\n", "\n"), Is.EqualTo(expectedValue));
        }

        [UnitTestAttribute(
        Identifier = "BDC7A886-BD03-4F22-9A28-DAD42E6A425B",
        Purpose = "TraceabilityMatrixBase is created and a tag with unknown source is provided",
        PostCondition = "Expected exception is thrown")]
        [Test]
        public void TestTraceMatrix2()
        {
            var trace = new TraceMatrix(dataSources, traceAnalysis, config);
            var tag = new RoboClerkTextTag(0, 36, "@@SLMS:TraceMatrix(source=Unknown)@@", true);
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
            var trace = new TraceMatrix(dataSources, traceAnalysis, config);
            var tag = new RoboClerkTextTag(0, 22, "@@SLMS:TraceMatrix()@@", true);
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
            var trace = new TraceMatrix(dataSources, traceAnalysis, config);
            var tag = new RoboClerkTextTag(0, 46, "@@SLMS:TraceMatrix(source=SystemRequirement)@@", true);
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
            var trace = new TraceMatrix(dataSources, traceAnalysis, config);
            var tag = new RoboClerkTextTag(0, 46, "@@SLMS:TraceMatrix(source=SystemRequirement)@@", true);
            var te = traceAnalysis.GetTraceEntityForID("SoftwareRequirement");
            matrix[te][1].Clear();
            string result = trace.GetContent(tag, documentConfig);
            string expectedValue = "|====\n| Requirements | Specifications | SRS \n| SYS1 | SYS1_SWR1, SYS1_SWR2 | Trace Present \n| SYS2 | N/A | Trace Present \n|====\n\n\nTrace issues:\n\n. No Requirement level trace problems detected!\n";

            Assert.That(Regex.Replace(result, @"\r\n", "\n"), Is.EqualTo(expectedValue));
        }

        [UnitTestAttribute(
        Identifier = "CADF9157-77A4-4391-8D79-7C7B0A7D8562",
        Purpose = "TraceabilityMatrixBase is created, a tag is provided and trace is initiated. The trace contains a required missing trace.",
        PostCondition = "The expected output, containing a MISSING entry is produced.")]
        [Test]
        public void TestTraceMatrix6()
        {
            var trace = new TraceMatrix(dataSources, traceAnalysis, config);
            var tag = new RoboClerkTextTag(0, 46, "@@SLMS:TraceMatrix(source=SystemRequirement)@@", true);
            var te = traceAnalysis.GetTraceEntityForID("SoftwareRequirement");
            matrix[te][0].Clear();
            matrix[te][0].Add(null);
            string result = trace.GetContent(tag, documentConfig);
            string expectedValue = "|====\n| Requirements | Specifications | SRS \n| SYS1 | MISSING | Trace Present \n| SYS2 | SYS2_SWR3 | Trace Present \n|====\n\n\nTrace issues:\n\n. No Requirement level trace problems detected!\n";

            Assert.That(Regex.Replace(result, @"\r\n", "\n"), Is.EqualTo(expectedValue));
        }

        [UnitTestAttribute(
        Identifier = "F3A08500-1384-4A30-8E18-890FB5F7624A",
        Purpose = "TraceabilityMatrixBase is created, a tag is provided and trace is initiated. A truth trace issue has been identified.",
        PostCondition = "The expected output, with truth trace issue is produced.")]
        [Test]
        public void TestTraceMatrix7()
        {
            var trace = new TraceMatrix(dataSources, traceAnalysis, config);
            var tag = new RoboClerkTextTag(0, 46, "@@SLMS:TraceMatrix(source=SystemRequirement)@@", true);
            var te = traceAnalysis.GetTraceEntityForID("SystemRequirement");
            var teSWR = traceAnalysis.GetTraceEntityForID("SoftwareRequirement");
            TraceIssue trcissue = new TraceIssue(te, "SYS1", teSWR, "SWR5", TraceIssueType.Missing);
            traceAnalysis.GetTraceIssuesForTruth(te).Returns(new List<TraceIssue>() { trcissue });
            string result = trace.GetContent(tag, documentConfig);
            string expectedValue = "|====\n| Requirements | Specifications | SRS \n| SYS1 | SYS1_SWR1, SYS1_SWR2 | Trace Present \n| SYS2 | SYS2_SWR3 | Trace Present \n|====\n\n\nTrace issues:\n\n. Requirement SYS1 is potentially missing a corresponding Specification.\n";

            Assert.That(Regex.Replace(result, @"\r\n", "\n"), Is.EqualTo(expectedValue));
        }

        [UnitTestAttribute(
        Identifier = "3F2F7A51-A100-4853-B272-6372D442561A",
        Purpose = "TraceabilityMatrixBase is created, a tag is provided and trace is initiated. A number of document trace issues have been identified.",
        PostCondition = "The expected output, with document trace issue is produced.")]
        [Test]
        public void TestTraceMatrix8()
        {
            var trace = new TraceMatrix(dataSources, traceAnalysis, config);
            var tag = new RoboClerkTextTag(0, 46, "@@SLMS:TraceMatrix(source=SystemRequirement)@@", true);
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
            string expectedValue = "|====\n| Requirements | Specifications | SRS \n| SYS1 | SYS1_SWR1, SYS1_SWR2 | Trace Present \n| SYS2 | SYS2_SWR3 | Trace Present \n|====\n\n\nTrace issues:\n\n. An expected trace from SYS1 in Requirement to System Requirements Specification is missing.\n. An item with identifier docID appeared in System Requirements Specification without tracing to Requirement.\n. An incorrect trace was found in System Requirements Specification from docID to SYS1 where SYS1 was expected in Requirement but was not found.\n. A possibly extra item with identifier docID appeared in System Requirements Specification without appearing in Requirement.\n. A possibly expected trace from SYS1 in Requirement to System Requirements Specification is missing.\n. An incorrect trace was found in System Requirements Specification from docID to SYS5 where SYS5 was expected in Requirement but was not a valid identifier.\n. A missing trace was detected in System Requirements Specification. The item with ID docID does not have a parent while it was expected to trace to Requirement.\n";

            Assert.That(Regex.Replace(result, @"\r\n", "\n"), Is.EqualTo(expectedValue));
        }

        [UnitTestAttribute(
        Identifier = "A1B2C3D4-E5F6-7UD0-ABCD-EF1234567890",
        Purpose = "Test project filtering functionality with matching project",
        PostCondition = "Only items matching the specified project are included")]
        [Test]
        public void TestTraceMatrixWithProjectFilter_MatchingProject()
        {
            var trace = new TraceMatrix(dataSources, traceAnalysis, config);
            
            // Set up items with different projects
            var te = traceAnalysis.GetTraceEntityForID("SystemRequirement");
            var teSWR = traceAnalysis.GetTraceEntityForID("SoftwareRequirement");
            var teDoc = traceAnalysis.GetTraceEntityForID("SystemRequirementsSpec");

            var sysItem1 = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "SYS1", ItemProject = "ProjectA" };
            var sysItem2 = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "SYS2", ItemProject = "ProjectB" };
            var swrItem1 = new RequirementItem(RequirementType.SoftwareRequirement) { ItemID = "SYS1_SWR1", ItemProject = "ProjectA" };
            var swrItem2 = new RequirementItem(RequirementType.SoftwareRequirement) { ItemID = "SYS1_SWR2", ItemProject = "ProjectA" };
            var swrItem3 = new RequirementItem(RequirementType.SoftwareRequirement) { ItemID = "SYS2_SWR3", ItemProject = "ProjectB" };
            var docItem1 = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "SYS1", ItemProject = "ProjectA" };
            var docItem2 = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "SYS2", ItemProject = "ProjectB" };
            
            // Update matrix with project information
            matrix[te][0][0] = sysItem1;
            matrix[te][1][0] = sysItem2;
            matrix[teSWR][0][0] = swrItem1;
            matrix[teSWR][0][1] = swrItem2;
            matrix[teSWR][1][0] = swrItem3;
            matrix[teDoc][0][0] = docItem1;
            matrix[teDoc][1][0] = docItem2;
            
            string tagString = "@@SLMS:TraceMatrix(source=SystemRequirement,ItemProject=ProjectA)@@";
            var tag = new RoboClerkTextTag(0, tagString.Length, tagString, true);
            string result = trace.GetContent(tag, documentConfig);
            
            // Should only contain ProjectA items (SYS1 row only)
            Assert.That(result.Contains("SYS1"), Is.True);
            Assert.That(result.Contains("SYS1_SWR1"), Is.True);
            Assert.That(result.Contains("SYS1_SWR2"), Is.True);
            Assert.That(result.Contains("SYS2"), Is.False);
            Assert.That(result.Contains("SYS2_SWR3"), Is.False);
        }

        [UnitTestAttribute(
        Identifier = "1F6CB998-7C78-4E8F-8877-EB3D8E918D19",
        Purpose = "Test project filtering with no matching items",
        PostCondition = "Empty matrix is generated when no items match the project filter")]
        [Test]
        public void TestTraceMatrixWithProjectFilter_NoMatchingItems()
        {
            var trace = new TraceMatrix(dataSources, traceAnalysis, config);
            
            // Set up items with different projects (none matching filter)
            var te = traceAnalysis.GetTraceEntityForID("SystemRequirement");
            matrix[te][0][0].ItemProject = "ProjectB";
            matrix[te][1][0].ItemProject = "ProjectB";
            
            string tagString = "@@SLMS:TraceMatrix(source=SystemRequirement,ItemProject=ProjectA)@@";
            var tag = new RoboClerkTextTag(0, tagString.Length, tagString, true);
            string result = trace.GetContent(tag, documentConfig);
            
            // Should show empty matrix since no items match ProjectA
            var lines = result.Split('\n');
            var dataLines = lines.Where(line => line.StartsWith("|") && !line.Contains("====") && !line.Contains("Requirements")).ToArray();
            Assert.That(dataLines.Length, Is.EqualTo(0), "No data rows should be present when no items match the project filter");
        }

        [UnitTestAttribute(
        Identifier = "EF99D0A2-7A0A-4F95-A55C-AC88D56C3DA2",
        Purpose = "Test project filtering with mixed matching items",
        PostCondition = "N/A entries are shown for filtered out items within included rows")]
        [Test]
        public void TestTraceMatrixWithProjectFilter_MixedMatching()
        {
            var trace = new TraceMatrix(dataSources, traceAnalysis, config);
            
            // Set up mixed project scenario
            var te = traceAnalysis.GetTraceEntityForID("SystemRequirement");
            var teSWR = traceAnalysis.GetTraceEntityForID("SoftwareRequirement");

            // SYS1 is ProjectA (included), but its SWR items are ProjectB (filtered out)
            matrix[te][0][0].ItemProject = "ProjectA";
            matrix[teSWR][0][0].ItemProject = "ProjectB";
            matrix[teSWR][0][1].ItemProject = "ProjectB";
            
            string tagString = "@@SLMS:TraceMatrix(source=SystemRequirement,ItemProject=ProjectA)@@";
            var tag = new RoboClerkTextTag(0, tagString.Length, tagString, true);
            string result = trace.GetContent(tag, documentConfig);
            
            // Should contain SYS1 row but with N/A for software requirements
            Assert.That(result.Contains("SYS1"), Is.True);
            Assert.That(result.Contains("N/A"), Is.True);
        }

        [UnitTestAttribute(
        Identifier = "67F591CA-FB1F-4676-AAA5-E3350A4C1B4B",
        Purpose = "Test project filtering with trace issues",
        PostCondition = "Only trace issues for matching projects are shown")]
        [Test]
        public void TestTraceMatrixWithProjectFilter_TraceIssues()
        {
            var trace = new TraceMatrix(dataSources, traceAnalysis, config);
            
            // Set up items with projects
            var te = traceAnalysis.GetTraceEntityForID("SystemRequirement");
            var teSWR = traceAnalysis.GetTraceEntityForID("SoftwareRequirement");
            
            var sysItem1 = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "SYS1", ItemProject = "ProjectA" };
            var sysItem2 = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "SYS2", ItemProject = "ProjectB" };
            
            matrix[te][0][0] = sysItem1;
            matrix[te][1][0] = sysItem2;
            
            dataSources.GetItem("SYS1").Returns(sysItem1);
            dataSources.GetItem("SYS2").Returns(sysItem2);
            
            // Create trace issues for both projects
            TraceIssue issue1 = new TraceIssue(te, "SYS1", teSWR, "SWR1", TraceIssueType.Missing);
            TraceIssue issue2 = new TraceIssue(te, "SYS2", teSWR, "SWR2", TraceIssueType.Missing);
            
            traceAnalysis.GetTraceIssuesForTruth(te).Returns(new List<TraceIssue>() { issue1, issue2 });
            
            string tagString = "@@SLMS:TraceMatrix(source=SystemRequirement,ItemProject=ProjectA)@@";
            var tag = new RoboClerkTextTag(0, tagString.Length, tagString, true);
            string result = trace.GetContent(tag, documentConfig);
            
            // Should only show trace issue for ProjectA
            Assert.That(result.Contains("SYS1 is potentially missing"), Is.True);
            Assert.That(result.Contains("SYS2 is potentially missing"), Is.False);
        }

        [UnitTestAttribute(
        Identifier = "C640410B-C340-40FF-A467-524985B191F5",
        Purpose = "Test project filtering is case insensitive",
        PostCondition = "Project filtering works regardless of case")]
        [Test]
        public void TestTraceMatrixWithProjectFilter_CaseInsensitive()
        {
            var trace = new TraceMatrix(dataSources, traceAnalysis, config);
            
            // Set up items with mixed case projects
            var te = traceAnalysis.GetTraceEntityForID("SystemRequirement");
            matrix[te][0][0].ItemProject = "projecta";  // lowercase
            matrix[te][1][0].ItemProject = "ProjectB";
            
            string tagString = "@@SLMS:TraceMatrix(source=SystemRequirement,ItemProject=PROJECTA)@@";
            var tag = new RoboClerkTextTag(0, tagString.Length, tagString, true);  // uppercase filter
            string result = trace.GetContent(tag, documentConfig);
            
            // Should match despite case difference
            Assert.That(result.Contains("SYS1"), Is.True);
            Assert.That(result.Contains("SYS2"), Is.False);
        }

        [UnitTestAttribute(
        Identifier = "C42B0EE8-7E84-4B69-803B-BD0D8D6D20C6",
        Purpose = "Test backward compatibility - no project filter specified",
        PostCondition = "All items are included when no project filter is specified")]
        [Test]
        public void TestTraceMatrixWithoutProjectFilter_BackwardCompatibility()
        {
            var trace = new TraceMatrix(dataSources, traceAnalysis, config);
            
            // Set up items with various projects
            var te = traceAnalysis.GetTraceEntityForID("SystemRequirement");
            matrix[te][0][0].ItemProject = "ProjectA";
            matrix[te][1][0].ItemProject = "ProjectB";
            
            // No project filter specified
            var tag = new RoboClerkTextTag(0, 46, "@@SLMS:TraceMatrix(source=SystemRequirement)@@", true);
            string result = trace.GetContent(tag, documentConfig);
            
            // Should include all items regardless of project
            Assert.That(result.Contains("SYS1"), Is.True);
            Assert.That(result.Contains("SYS2"), Is.True);
        }

        [UnitTestAttribute(
        Identifier = "3AF42910-6C1C-4B32-A6A9-5F4A3E7F5223",
        Purpose = "Test project filtering with null ItemProject values",
        PostCondition = "Items with null ItemProject are filtered out when project filter is specified")]
        [Test]
        public void TestTraceMatrixWithProjectFilter_NullItemProject()
        {
            var trace = new TraceMatrix(dataSources, traceAnalysis, config);
            
            // Set up items - one with project, one with null project
            var te = traceAnalysis.GetTraceEntityForID("SystemRequirement");
            matrix[te][0][0].ItemProject = "ProjectA";
            matrix[te][1][0].ItemProject = null;  // null project
            
            string tagString = "@@SLMS:TraceMatrix(source=SystemRequirement,ItemProject=ProjectA)@@";
            var tag = new RoboClerkTextTag(0, tagString.Length, tagString, true);
            string result = trace.GetContent(tag, documentConfig);
            
            // Should only include item with matching project, not null project item
            Assert.That(result.Contains("SYS1"), Is.True);
            Assert.That(result.Contains("SYS2"), Is.False);
        }
    }

    // Test helper class to test custom ShouldIncludeItem override
    internal class TestableTraceMatrix : TraceabilityMatrixBase
    {
        protected override string MatrixTypeName => "System";

        public TestableTraceMatrix(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration configuration)
            : base(data, analysis, configuration)
        {
            truthSource = analysis.GetTraceEntityForID("SystemRequirement");
        }

        protected override bool ShouldIncludeItem(Item item, string projectFilter)
        {
            // Custom filtering: include base filter AND exclude cancelled items
            if (!base.ShouldIncludeItem(item, projectFilter))
                return false;
                
            return item?.ItemStatus != "Cancelled";
        }
    }

    [TestFixture]
    [Description("These tests test the extensibility of TraceabilityMatrixBase ShouldIncludeItem method")]
    internal class TestCustomTraceMatrixFiltering
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
            traceAnalysis.GetTraceEntityForID("SystemRequirement").Returns(te);
            traceAnalysis.GetTraceEntityForID("SoftwareRequirement").Returns(teSWR);
            documentConfig = new DocumentConfig("SoftwareRequirementsSpecification", "docID", "docTitle", "docAbbr", @"c:\in\template.adoc");

            matrix = new RoboClerkOrderedDictionary<TraceEntity, List<List<Item>>>();
            matrix[te] = new List<List<Item>>()
            {
                new List<Item>() { new RequirementItem(RequirementType.SystemRequirement) { ItemID = "SYS1", ItemProject = "ProjectA", ItemStatus = "Active" } },
                new List<Item>() { new RequirementItem(RequirementType.SystemRequirement) { ItemID = "SYS2", ItemProject = "ProjectA", ItemStatus = "Cancelled" } }
            };
            matrix[teSWR] = new List<List<Item>>()
            {
                new List<Item>() { new RequirementItem(RequirementType.SoftwareRequirement) { ItemID = "SWR1", ItemProject = "ProjectA", ItemStatus = "Active" } },
                new List<Item>() { new RequirementItem(RequirementType.SoftwareRequirement) { ItemID = "SWR2", ItemProject = "ProjectA", ItemStatus = "Cancelled" } }
            };

            traceAnalysis.PerformAnalysis(dataSources, te).Returns(matrix);
            traceAnalysis.GetTraceIssuesForTruth(te).Returns(new List<TraceIssue>());
        }

        [UnitTestAttribute(
        Identifier = "76D26330-D423-416B-9752-E6FB66DFE7C5",
        Purpose = "Test custom ShouldIncludeItem override filters by both project and status",
        PostCondition = "Only items matching project AND not cancelled are included")]
        [Test]
        public void TestCustomShouldIncludeItemOverride()
        {
            var customTrace = new TestableTraceMatrix(dataSources, traceAnalysis, config);
            
            string tagString = "@@SLMS:TraceMatrix(source=SystemRequirement,ItemProject=ProjectA)@@";
            var tag = new RoboClerkTextTag(0, tagString.Length, tagString, true);
            string result = customTrace.GetContent(tag, documentConfig);
            
            // Should include SYS1 (Active) but exclude SYS2 (Cancelled) even though both match project
            Assert.That(result.Contains("SYS1"), Is.True);
            Assert.That(result.Contains("SYS2"), Is.False);
        }

        [UnitTestAttribute(
        Identifier = "8F285EDC-EA3F-408F-9A70-870FEA65ECC9",
        Purpose = "Test custom filtering without project filter still applies custom logic",
        PostCondition = "Custom status filtering is applied even without project filter")]
        [Test]
        public void TestCustomFilteringWithoutProjectFilter()
        {
            var customTrace = new TestableTraceMatrix(dataSources, traceAnalysis, config);
            
            var tag = new RoboClerkTextTag(0, 46, "@@SLMS:TraceMatrix(source=SystemRequirement)@@", true);
            string result = customTrace.GetContent(tag, documentConfig);
            
            // Should include SYS1 (Active) but exclude SYS2 (Cancelled)
            Assert.That(result.Contains("SYS1"), Is.True);
            Assert.That(result.Contains("SYS2"), Is.False);
        }
    }
}
