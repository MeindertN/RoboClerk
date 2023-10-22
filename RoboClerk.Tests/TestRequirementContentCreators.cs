using NSubstitute;
using NUnit.Framework;
using RoboClerk.AISystem;
using RoboClerk.Configuration;
using RoboClerk.ContentCreators;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("These tests test the Requirement content creators")]
    internal class TestRequirementContentCreators
    {
        private IConfiguration config = null;
        private IDataSources dataSources = null;
        private ITraceabilityAnalysis traceAnalysis = null;
        private IFileSystem fs = null;
        private IAISystemPlugin aiPlugin = null;
        private DocumentConfig documentConfig = null;
        private List<LinkedItem> sysReqItems = new List<LinkedItem>();
        private List<LinkedItem> softReqItems = new List<LinkedItem>();
        private List<LinkedItem> docReqItems = new List<LinkedItem>();

        [SetUp]
        public void TestSetup()
        {
            config = Substitute.For<IConfiguration>();
            dataSources = Substitute.For<IDataSources>();
            traceAnalysis = Substitute.For<ITraceabilityAnalysis>();
            aiPlugin = Substitute.For<IAISystemPlugin>();
            fs = Substitute.For<IFileSystem>();
            documentConfig = new DocumentConfig("RequirementsSpecification", "docID", "docTitle", "docAbbr", @"c:\in\template.adoc");
            var sysTE = new TraceEntity("SystemRequirement", "sys", "spabrrv", TraceEntityType.Truth);
            var softTE = new TraceEntity("SoftwareRequirement", "soft", "spabrrv", TraceEntityType.Truth);
            var docTE = new TraceEntity("DocumentationRequirement", "doc", "spabrrv", TraceEntityType.Truth);
            var teDoc = new TraceEntity("docID", "docTitle", "docAbbr", TraceEntityType.Document);

            traceAnalysis.GetTraceEntityForID("SystemRequirement").Returns(sysTE);
            traceAnalysis.GetTraceEntityForID("SoftwareRequirement").Returns(softTE);
            traceAnalysis.GetTraceEntityForID("DocumentationRequirement").Returns(docTE);
            traceAnalysis.GetTraceEntityForID("docID").Returns(teDoc);
            traceAnalysis.GetTraceEntityForAnyProperty("SystemRequirement").Returns(sysTE);
            traceAnalysis.GetTraceEntityForAnyProperty("SoftwareRequirement").Returns(softTE);
            traceAnalysis.GetTraceEntityForAnyProperty("DocumentationRequirement").Returns(docTE);

            List<RequirementItem> sysReqItems2 = new List<RequirementItem>();
            List<RequirementItem> softReqItems2 = new List<RequirementItem>();
            List<RequirementItem> docReqItems2 = new List<RequirementItem>();
            sysReqItems.Clear();
            softReqItems.Clear();
            docReqItems.Clear();

            var sysItem = new RequirementItem(RequirementType.SystemRequirement);
            sysItem.ItemID = "21";
            sysItem.RequirementAssignee = "Johnny_21";
            sysItem.RequirementState = "closed";
            sysItem.RequirementDescription = "Description";
            sysItem.ItemRevision = "latest";
            sysReqItems.Add(sysItem);
            sysReqItems2.Add(sysItem);

            var softItem = new RequirementItem(RequirementType.SoftwareRequirement);
            softItem.ItemID = "55";
            softItem.RequirementAssignee = "Johnny_55";
            softItem.RequirementState = "open";
            softItem.RequirementDescription = "       \nDescription";
            softItem.ItemRevision = "some";
            softReqItems.Add(softItem);
            softReqItems2.Add(softItem);

            var docItem = new RequirementItem(RequirementType.DocumentationRequirement);
            docItem.ItemID = "99";
            docItem.RequirementAssignee = "Johnny_99";
            docItem.RequirementState = "open";
            docItem.RequirementDescription = "--------    Description";
            docItem.ItemRevision = "some";
            docReqItems.Add(docItem);
            docReqItems2.Add(docItem);

            dataSources.GetItems(sysTE).Returns(sysReqItems);
            dataSources.GetItems(softTE).Returns(softReqItems);
            dataSources.GetItems(docTE).Returns(docReqItems);
            dataSources.GetAllSystemRequirements().Returns(sysReqItems2);
            dataSources.GetAllSoftwareRequirements().Returns(softReqItems2);
            dataSources.GetAllDocumentationRequirements().Returns(docReqItems2);
            dataSources.GetTemplateFile("./ItemTemplates/Requirement.adoc").Returns(File.ReadAllText("../../../../RoboClerk/ItemTemplates/Requirement.adoc"));
        }

        [UnitTestAttribute(
        Identifier = "941D7ED0-90EA-4419-B42A-F74F5521990D",
        Purpose = "Requirement content creators are created",
        PostCondition = "No exception is thrown")]
        [Test]
        public void TestRequirementContentCreators1()
        {
            var sysReq = new SystemRequirement(dataSources, traceAnalysis, config);
            var softReq = new SoftwareRequirement(dataSources, traceAnalysis, config);
            var docReq = new DocumentationRequirement(dataSources, traceAnalysis, config);
        }

        [UnitTestAttribute(
        Identifier = "996D184C-E34C-43DD-811A-1FD5C654EE90",
        Purpose = "A requirement tag is set, content is created.",
        PostCondition = "Appropriate content is generated.")]
        [Test]
        public void TestRequirementContentCreators2()
        {
            var sysReq = new SystemRequirement(dataSources, traceAnalysis, config);
            var tag = new RoboClerkTag(0, 37, "@@SLMS:SystemRequirement(ItemID=21)@@", true);
            dataSources.GetItem("21").Returns(sysReqItems[0]);
            string content = sysReq.GetContent(tag, documentConfig);
            Assert.IsTrue(content == "\n|====\n| sys ID: | 21\n\n| sys Revision: | latest\n\n| sys Category: | \n\n| Parent ID: | N/A\n\n| Title: | \n\n| Description: a| Description\n|====\r\n");

            var softReq = new SoftwareRequirement(dataSources, traceAnalysis, config);
            tag = new RoboClerkTag(0, 39, "@@SLMS:SoftwareRequirement(ItemID=55)@@", true);
            dataSources.GetItem("55").Returns(softReqItems[0]);
            content = softReq.GetContent(tag, documentConfig);
            Assert.IsTrue(content == "\n|====\n| soft ID: | 55\n\n| soft Revision: | some\n\n| soft Category: | \n\n| Parent ID: | N/A\n\n| Title: | \n\n| Description: a|        \nDescription\n|====\r\n");

            var docReq = new DocumentationRequirement(dataSources, traceAnalysis, config);
            tag = new RoboClerkTag(0, 44, "@@SLMS:DocumentationRequirement(ItemID=99)@@", true);
            dataSources.GetItem("99").Returns(docReqItems[0]);
            content = docReq.GetContent(tag, documentConfig);
            Assert.IsTrue(content == "\n|====\n| doc ID: | 99\n\n| doc Revision: | some\n\n| doc Category: | \n\n| Parent ID: | N/A\n\n| Title: | \n\n| Description: a| --------    Description\n|====\r\n");
        }

        [UnitTestAttribute(
        Identifier = "AAF04F3C-5577-4EC4-946E-D6DF03BC983A",
        Purpose = "A requirement tag is set, ai plugin set in config.",
        PostCondition = "Appropriate content is generated.")]
        [Test]
        public void TestRequirementContentCreators3()
        {
            config.AIPlugin.Returns("aiplugin");
            var sysReq = new SystemRequirement(dataSources, traceAnalysis, config);
            var tag = new RoboClerkTag(0, 37, "@@SLMS:SystemRequirement(ItemID=21)@@", true);
            dataSources.GetItem("21").Returns(sysReqItems[0]);
            string content = sysReq.GetContent(tag, documentConfig);
            Assert.IsTrue(content == "@@@AI:AIFeedback(entity=SystemRequirement,itemID=21)\r\n\n|====\n| sys ID: | 21\n\n| sys Revision: | latest\n\n| sys Category: | \n\n| Parent ID: | N/A\n\n| Title: | \n\n| Description: a| [[comment_21]]Description\n|====\r\n@@@\r\n");

            var softReq = new SoftwareRequirement(dataSources, traceAnalysis, config);
            tag = new RoboClerkTag(0, 39, "@@SLMS:SoftwareRequirement(ItemID=55)@@", true);
            dataSources.GetItem("55").Returns(softReqItems[0]);
            content = softReq.GetContent(tag, documentConfig);
            Assert.IsTrue(content == "@@@AI:AIFeedback(entity=SoftwareRequirement,itemID=55)\r\n\n|====\n| soft ID: | 55\n\n| soft Revision: | some\n\n| soft Category: | \n\n| Parent ID: | N/A\n\n| Title: | \n\n| Description: a|        \n[[comment_55]]Description\n|====\r\n@@@\r\n");

            var docReq = new DocumentationRequirement(dataSources, traceAnalysis, config);
            tag = new RoboClerkTag(0, 44, "@@SLMS:DocumentationRequirement(ItemID=99)@@", true);
            dataSources.GetItem("99").Returns(docReqItems[0]);
            content = docReq.GetContent(tag, documentConfig);
            Assert.IsTrue(content == "@@@AI:AIFeedback(entity=DocumentationRequirement,itemID=99)\r\n\n|====\n| doc ID: | 99\n\n| doc Revision: | some\n\n| doc Category: | \n\n| Parent ID: | N/A\n\n| Title: | \n\n| Description: a| --------    [[comment_99]]Description\n|====\r\n@@@\r\n");
        }

    }
}
