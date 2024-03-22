using NUnit.Framework;
using RoboClerk.Configuration;
using System.IO.Abstractions;
using RoboClerk.ContentCreators;
using RoboClerk.AISystem;
using NSubstitute;
using System.Collections.Generic;
using System;
using System.IO.Abstractions.TestingHelpers;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("These tests test the AI Content Creator")]
    internal class TestAIContentCreator
    {
        private IConfiguration config = null;
        private IDataSources dataSources = null;
        private ITraceabilityAnalysis traceAnalysis = null;
        private IFileSystem fs = null;
        private IAISystemPlugin aiSystemPlugin = null;

        [SetUp]
        public void TestSetup()
        {
            config = Substitute.For<IConfiguration>();
            dataSources = Substitute.For<IDataSources>();
            traceAnalysis = Substitute.For<ITraceabilityAnalysis>();
            fs = Substitute.For<IFileSystem>();
            aiSystemPlugin = Substitute.For<IAISystemPlugin>();
            aiSystemPlugin.GetFeedback(Arg.Any<TraceEntity>(),Arg.Any<Item>()).Returns("This is an AI test comment!");
        }


        [UnitTestAttribute(
        Identifier = "44CC375C-2B1B-4600-BF22-C102FEE59BD1",
        Purpose = "AI content creator is created",
        PostCondition = "No exception is thrown")]
        [Test]
        public void TestAIContentCreator1()
        {
            var test = new AIContentCreator(dataSources,traceAnalysis,config,aiSystemPlugin,fs);
        }

        [UnitTestAttribute(
        Identifier = "F6BFD1BC-21E8-4F27-8043-66B25D02F1CB",
        Purpose = "AI content creator is created with a null AI plugin",
        PostCondition = "Appropriate exception is thrown")]
        [Test]
        public void TestAIContentCreator2()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new AIContentCreator(dataSources, traceAnalysis, config, null, fs));
            Assert.That(ex.Message.Contains("AI System Plugin is null while AI system feedback was requested."));
        }

        [UnitTestAttribute(
        Identifier = "9A3B3520-3197-423B-BCCD-08159C2DB65E",
        Purpose = "AI content creator is created. Get content is called with empty tag.",
        PostCondition = "Appropriate exception is thrown")]
        [Test]
        public void TestAIContentCreator3()
        {
            var obj = new AIContentCreator(dataSources, traceAnalysis, config, aiSystemPlugin, fs);
            var tag = new RoboClerkTag(0, 22, "@@SLMS:TraceMatrix()@@", true);
            var doc = new DocumentConfig("roboclerkID", "documentID", "documentTitle", "documentAbbreviation", "documentTemplate");
            var ex = Assert.Throws<Exception>(() => obj.GetContent(tag,doc));
            Assert.That(ex.Message.Contains("One or both of the required AI parameters are not present in the AI tag."));
        }

        [UnitTestAttribute(
        Identifier = "3D4FE8DE-8327-4D6D-BD28-EBB7D0396536",
        Purpose = "AI content creator is created. Get content is called with appopriate tag.",
        PostCondition = "Expected content is produced and file is written.")]
        [Test]
        public void TestAIContentCreator4()
        {
            fs = new MockFileSystem();
            fs.Directory.CreateDirectory(TestingHelpers.ConvertFileName(@"c:\out"));
            var obj = new AIContentCreator(dataSources, traceAnalysis, config, aiSystemPlugin, fs);
            var tag = new RoboClerkTag(0, 57, "@@SLMS:TraceMatrix(entity=SystemRequirement,itemID=101)@@", true);
            var doc = new DocumentConfig("roboclerkID", "documentID", "documentTitle", "documentAbbreviation", "documentTemplate");
            RequirementItem item = new RequirementItem(RequirementType.SoftwareRequirement);
            dataSources.GetItem(Arg.Any<string>()).Returns(item);
            config.OutputDir.Returns(TestingHelpers.ConvertFileName(@"c:\out"));

            var result = obj.GetContent(tag,doc);
            Assert.That(result == "SLMS:TraceMatrix(entity=SystemRequirement,itemID=101)@@");
            Assert.That(fs.File.Exists(TestingHelpers.ConvertFileName(@"c:\out\documentTemplate_AIComments.json")));
            Assert.That(fs.File.ReadAllText(TestingHelpers.ConvertFileName(@"c:\out\documentTemplate_AIComments.json")).Contains("This is an AI test comment!"));
        }

    }
}
