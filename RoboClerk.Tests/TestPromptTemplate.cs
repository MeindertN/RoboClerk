using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("These tests test the Prompt Template")]
    internal class TestPromptTemplate
    {
        [SetUp]
        public void TestSetup()
        {
        }

        [UnitTestAttribute(
        Identifier = "411435FB-8FD0-42C8-863D-8F80DE758CCB",
        Purpose = "PromptTemplate is created",
        PostCondition = "No exception is thrown")]
        [Test]
        public void TestPrompt()
        {
            string template = "sdsdsd";
            var tpe = new PromptTemplate(template);
        }

        [UnitTestAttribute(
        Identifier = "BAE98938-0991-422A-8670-B7E015A5D3EC",
        Purpose = "PromptTemplate is created with empty string",
        PostCondition = "No exception is thrown and empty string is returned for prompt")]
        [Test]
        public void TestPrompt2()
        {
            string template = "";
            var tpe = new PromptTemplate(template);
            Dictionary<string,string> parameters = new Dictionary<string,string>();
            Assert.IsTrue(tpe.GetPrompt(parameters) == "");
        }

        [UnitTestAttribute(
        Identifier = "4A624A11-9E6F-4B81-AEFE-C2ED1634C958",
        Purpose = "PromptTemplate is created with empty string and parameters are added.",
        PostCondition = "No exception is thrown and empty string is returned for prompt")]
        [Test]
        public void TestPrompt3()
        {
            string template = "";
            var tpe = new PromptTemplate(template);
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters["RequirementDescription"] = "true";
            Assert.IsTrue(tpe.GetPrompt(parameters) == "");
        }

        [UnitTestAttribute(
        Identifier = "E4C108B3-2019-423A-8375-BA827B51D1C7",
        Purpose = "PromptTemplate is created with single parameter and matching parameter is added.",
        PostCondition = "Parameter value is returned with a case insensitive match")]
        [Test]
        public void TestPrompt4()
        {
            string template = "%{parval}%";
            var tpe = new PromptTemplate(template);
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters["pArvAl"] = "true";
            Assert.IsTrue(tpe.GetPrompt(parameters) == "true");
        }

        [UnitTestAttribute(
        Identifier = "CD8EBC49-E844-4755-84FF-983C4DEC9450",
        Purpose = "PromptTemplate is created with single parameter and non-matching parameter is added.",
        PostCondition = "The parameter name is returned.")]
        [Test]
        public void TestPrompt5()
        {
            string template = "%{parval}%";
            var tpe = new PromptTemplate(template);
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters["pArvAl_non_matching"] = "true";
            Assert.IsTrue(tpe.GetPrompt(parameters) == "parval");
        }

        [UnitTestAttribute(
        Identifier = "78AACFCF-0CA6-4335-951B-450C103BD055",
        Purpose = "PromptTemplate is created with single parameter matching the property name of a SystemRequirement item. A SystemRequirement item is passed into the GetPrompt function.",
        PostCondition = "The appropriate property value is returned.")]
        [Test]
        public void TestPrompt6()
        {
            string template = "%{RequirementDescription}%";
            RequirementItem item = new RequirementItem(RequirementType.SystemRequirement);
            item.RequirementDescription = "test string";
            var tpe = new PromptTemplate(template);
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters["pArvAl_non_matching"] = "true";
            Assert.IsTrue(tpe.GetPrompt(parameters,item) == "test string");
        }
    }

}
