using NUnit.Framework;
using NSubstitute;
using System.Collections.Generic;
using RoboClerk.Configuration;
using System.IO.Abstractions;
using RoboClerk.ContentCreators;
using System.Text.RegularExpressions;
using NSubstitute.Core.Arguments;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("These tests test the RoboClerk SOUP content creator")]
    internal class TestSOUPContentCreator
    {
        private IConfiguration config = null;
        private IDataSources dataSources = null;
        private ITraceabilityAnalysis traceAnalysis = null;
        private IFileSystem fs = null;
        private DocumentConfig documentConfig = null;
        private List<SOUPItem> soupItems = new List<SOUPItem>();

        [SetUp]
        public void TestSetup()
        {
            config = Substitute.For<IConfiguration>();
            dataSources = Substitute.For<IDataSources>();
            traceAnalysis = Substitute.For<ITraceabilityAnalysis>();
            var te = new TraceEntity("SOUP", "soup", "spabrrv", TraceEntityType.Truth);
            var teDoc = new TraceEntity("docID", "docTitle", "docAbbr", TraceEntityType.Document);
            traceAnalysis.GetTraceEntityForID("SOUP").Returns(te);
            traceAnalysis.GetTraceEntityForID("docID").Returns(teDoc);
            fs = Substitute.For<IFileSystem>();
            documentConfig = new DocumentConfig("SoftwareRequirementsSpecification", "docID", "docTitle", "docAbbr", @"c:\in\template.adoc");

            soupItems.Clear();
            var soupItem = new SOUPItem();
            soupItem.ItemID = "21";
            soupItem.SOUPName = "soupname";
            soupItem.SOUPVersion = "soupversion";
            soupItem.SOUPAnomalyListDescription = "No anomalies were found.";
            soupItem.SOUPCybersecurityCritical = false;
            soupItem.SOUPCybersecurityCriticalText = "cybersecurity text";
            soupItem.SOUPDetailedDescription = "detailed description";
            soupItem.SOUPEnduserTraining = "Not required";
            soupItem.SOUPInstalledByUser = false;
            soupItem.SOUPInstalledByUserText = "user install text";
            soupItem.SOUPPerformanceCritical = false;
            soupItem.SOUPPerformanceCriticalText = "performance text";
            soupItem.ItemRevision = "latest";
            soupItem.SOUPLicense = "license";
            soupItems.Add(soupItem);

            soupItem = new SOUPItem();
            soupItem.ItemID = "22";
            soupItem.SOUPName = "soupname2";
            soupItem.SOUPVersion = "soupversion2";
            soupItem.SOUPAnomalyListDescription = "No anomalies were found.2";
            soupItem.SOUPCybersecurityCritical = false;
            soupItem.SOUPCybersecurityCriticalText = "cybersecurity text2";
            soupItem.SOUPDetailedDescription = "detailed description2";
            soupItem.SOUPEnduserTraining = "Not required2";
            soupItem.SOUPInstalledByUser = false;
            soupItem.SOUPInstalledByUserText = "user install text2";
            soupItem.SOUPPerformanceCritical = false;
            soupItem.SOUPPerformanceCriticalText = "performance text2";
            soupItem.SOUPLinkedLib = true;
            soupItem.ItemRevision = "latest2";
            soupItem.SOUPLicense = "license2";
            soupItems.Add(soupItem);
        }

        [UnitTestAttribute(
        Identifier = "5C0A13ED-92D8-45F7-869C-CA3CDC428086",
        Purpose = "Soup content creator is created",
        PostCondition = "No exception is thrown")]
        [Test]
        public void CreateSOUPCC()
        {
            var soup = new SOUP(dataSources, traceAnalysis);
        }

        [UnitTestAttribute(
        Identifier = "5C0A13ED-92D8-45F7-869C-CA3CDC428086",
        Purpose = "Soup content creator is provided a SOUP item and visualizes it",
        PostCondition = "Expected string content is returned and trace is set")]
        [Test]
        public void GenerateSOUPContent1()
        {
            var soup = new SOUP(dataSources, traceAnalysis);
            var tag = new RoboClerkTag(0, 22, "@@SLMS:SOUP(ItemID=21)@@",true);
            dataSources.GetAllSOUP().Returns(soupItems);
            string content = soup.GetContent(tag, documentConfig);
            string expectedContent = "|====\n| soup ID: | 21\n\n| soup Revision: | latest\n\n| soup Name and Version: | soupname soupversion\n\n| Is soup Critical for Performance: | performance text\n\n| Is soup Critical for Cyber Security: | cybersecurity text\n\n| Is soup Installed by End-User: | user install text\n\n| Detailed Description: a| detailed description\n\n| soup License: | license\n|====\n\n";
            
            Assert.That(Regex.Replace(content, @"\r\n", "\n"), Is.EqualTo(expectedContent)); //ensure that we're always comparing the correct string, regardless of newline character for a platform
            Assert.DoesNotThrow(()=>traceAnalysis.Received().AddTrace(Arg.Any<TraceEntity>(), "21", Arg.Any<TraceEntity>(), "21"));
        }

        [UnitTestAttribute(
        Identifier = "B33C3AB9-B35B-443D-A555-B2146452712A",
        Purpose = "Soup content creator is provided a performance critical SOUP item and visualizes it",
        PostCondition = "Expected string content is returned and trace is set")]
        [Test]
        public void GenerateSOUPContent2()
        {
            var soup = new SOUP(dataSources, traceAnalysis);
            var tag = new RoboClerkTag(0, 22, "@@SLMS:SOUP(ItemID=21)@@", true);
            soupItems[0].SOUPPerformanceCritical = true;
            dataSources.GetAllSOUP().Returns( soupItems );
            string content = soup.GetContent(tag, documentConfig);
            string expectedContent = "|====\n| soup ID: | 21\n\n| soup Revision: | latest\n\n| soup Name and Version: | soupname soupversion\n\n| Is soup Critical for Performance: | performance text\n\n| Is soup Critical for Cyber Security: | cybersecurity text\n\n| Result Anomaly List Examination: | No anomalies were found.\n\n| Is soup Installed by End-User: | user install text\n\n| Detailed Description: a| detailed description\n\n| soup License: | license\n|====\n\n";

            Assert.That(Regex.Replace(content, @"\r\n", "\n"), Is.EqualTo(expectedContent)); //ensure that we're always comparing the correct string, regardless of newline character for a platform
            Assert.DoesNotThrow(() => traceAnalysis.Received().AddTrace(Arg.Any<TraceEntity>(), "21", Arg.Any<TraceEntity>(), "21"));
        }

        [UnitTestAttribute(
        Identifier = "99524E37-3F2D-4E43-BAD2-A20532DF3137",
        Purpose = "Soup content creator is provided a user installed SOUP item and visualizes it",
        PostCondition = "Expected string content is returned and trace is set")]
        [Test]
        public void GenerateSOUPContent3()
        {
            var soup = new SOUP(dataSources, traceAnalysis);
            var tag = new RoboClerkTag(0, 22, "@@SLMS:SOUP(ItemID=21)@@", true);
            soupItems[0].SOUPInstalledByUser = true;
            dataSources.GetAllSOUP().Returns( soupItems );
            string content = soup.GetContent(tag, documentConfig);
            string expectedContent = "|====\n| soup ID: | 21\n\n| soup Revision: | latest\n\n| soup Name and Version: | soupname soupversion\n\n| Is soup Critical for Performance: | performance text\n\n| Is soup Critical for Cyber Security: | cybersecurity text\n\n| Is soup Installed by End-User: | user install text\n\n| Required End-User Training: | Not required\n\n| Detailed Description: a| detailed description\n\n| soup License: | license\n|====\n\n";

            Assert.That(Regex.Replace(content, @"\r\n", "\n"), Is.EqualTo(expectedContent)); //ensure that we're always comparing the correct string, regardless of newline character for a platform
            Assert.DoesNotThrow(() => traceAnalysis.Received().AddTrace(Arg.Any<TraceEntity>(), "21", Arg.Any<TraceEntity>(), "21"));
        }

        [UnitTestAttribute(
        Identifier = "0F5C8CE2-1974-4AF7-9E8B-6D1418744243",
        Purpose = "Soup content creator is configured to provide a brief overview of SOUP",
        PostCondition = "Expected string content is returned and trace is set")]
        [Test]
        public void GenerateSOUPContent4()
        {
            var soup = new SOUP(dataSources, traceAnalysis);
            var tag = new RoboClerkTag(0, 33, "@@SLMS:SOUP(ItemID=21,BrIeF=TruE)@@", true);
            dataSources.GetAllSOUP().Returns( soupItems );
            string content = soup.GetContent(tag, documentConfig);
            string expectedContent = "|====\n| soup ID | soup Name and Version\n\n| 21 | soupname soupversion\n\n|====\n\n";

            Assert.That(Regex.Replace(content, @"\r\n", "\n"), Is.EqualTo(expectedContent)); //ensure that we're always comparing the correct string, regardless of newline character for a platform
            Assert.DoesNotThrow(() => traceAnalysis.Received().AddTrace(Arg.Any<TraceEntity>(), "21", Arg.Any<TraceEntity>(), "21"));
        }

        [UnitTestAttribute(
        Identifier = "4A31DA4C-51F0-4AD1-91BD-66800F9E37C4",
        Purpose = "Soup content creator is provided a tag asking for a non-existent soup item",
        PostCondition = "Expected string content is returned")]
        [Test]
        public void GenerateSOUPContent5()
        {
            var soup = new SOUP(dataSources, traceAnalysis);
            var tag = new RoboClerkTag(0, 22, "@@SLMS:SOUP(itemid=23)@@", true);
            dataSources.GetAllSOUP().Returns( soupItems );
            string content = soup.GetContent(tag, documentConfig);
            string expectedContent = "Unable to find soup(s). Check if soups of the correct type are provided or if a valid soup identifier is specified.";

            Assert.That(Regex.Replace(content, @"\r\n", "\n"), Is.EqualTo(expectedContent)); //ensure that we're always comparing the correct string, regardless of newline character for a platform
        }

        private List<ExternalDependency> GetDependencies()
        {
            var dependencies = new List<ExternalDependency>();

            var externalDep = new ExternalDependency("soupname","soupversion",false);
            var externalDep2 = new ExternalDependency("soupname2", "soupversion2", false);
            dependencies.Add(externalDep);
            dependencies.Add(externalDep2);
            return dependencies;
        }

        [UnitTestAttribute(
        Identifier = "EBA50075-EC42-4D61-AB24-DE12F335764B",
        Purpose = "Soup content creator is asked to compare soup against external dependencies which match the known SOUPS",
        PostCondition = "Expected string content is returned")]
        [Test]
        public void GenerateSOUPContent6()
        {
            var soup = new SOUP(dataSources, traceAnalysis);
            var tag = new RoboClerkTag(0, 37, "@@SLMS:SOUP(itemid=21,checksoup=true)@@", true);
            dataSources.GetAllSOUP().Returns(soupItems);
            dataSources.GetAllExternalDependencies().Returns(GetDependencies());   
            string content = soup.GetContent(tag, documentConfig);
            string expectedContent = "Roboclerk detected the following potential soup issues:\n\n* No soup related issues detected!\n\n";

            Assert.That(Regex.Replace(content, @"\r\n", "\n"), Is.EqualTo(expectedContent)); //ensure that we're always comparing the correct string, regardless of newline character for a platform
        }

        [UnitTestAttribute(
        Identifier = "8E75317C-F672-461E-9CF7-324BD2C3D751",
        Purpose = "Soup content creator is asked to compare a linked in soup against external dependencies which is missing",
        PostCondition = "Expected string content is returned")]
        [Test]
        public void GenerateSOUPContent7()
        {
            var soup = new SOUP(dataSources, traceAnalysis);
            var tag = new RoboClerkTag(0, 37, "@@SLMS:SOUP(itemid=21,checksoup=true)@@", true);
            dataSources.GetAllSOUP().Returns(soupItems);
            var deps = GetDependencies();
            deps.RemoveAt(1);  //remove the external dependency matching soupname2
            dataSources.GetAllExternalDependencies().Returns(deps);
            string content = soup.GetContent(tag, documentConfig);
            string expectedContent = "Roboclerk detected the following potential soup issues:\n\n* A soup item (i.e. \"soupname2 soupversion2\") that is marked as being linked into the software does not have a matching external dependency.\n\n";

            Assert.That(Regex.Replace(content, @"\r\n", "\n"), Is.EqualTo(expectedContent)); //ensure that we're always comparing the correct string, regardless of newline character for a platform
        }

        [UnitTestAttribute(
        Identifier = "F4557E69-9127-4B72-A385-B4D14B36EFC6",
        Purpose = "Soup content creator is provided with an external dependency without a corresponding SOUP item",
        PostCondition = "Expected string content is returned")]
        [Test]
        public void GenerateSOUPContent8()
        {
            var soup = new SOUP(dataSources, traceAnalysis);
            var tag = new RoboClerkTag(0, 37, "@@SLMS:SOUP(itemid=21,checksoup=true)@@", true);
            dataSources.GetAllSOUP().Returns(soupItems);
            var deps = GetDependencies();
            deps.Add(new ExternalDependency("soupname3", "soupversion3", false));
            dataSources.GetAllExternalDependencies().Returns(deps);
            string content = soup.GetContent(tag, documentConfig);
            string expectedContent = "Roboclerk detected the following potential soup issues:\n\n* An external dependency soupname3 soupversion3 does not seem to have a matching soup item.\n\n";

            Assert.That(Regex.Replace(content, @"\r\n", "\n"), Is.EqualTo(expectedContent)); //ensure that we're always comparing the correct string, regardless of newline character for a platform
        }

        [UnitTestAttribute(
        Identifier = "FF67C803-62B7-417C-8FF1-CE06A5698FA8",
        Purpose = "Soup content creator is provided with an external dependency with a corresponding SOUP item with a different version",
        PostCondition = "Expected string content is returned")]
        [Test]
        public void GenerateSOUPContent9()
        {
            var soup = new SOUP(dataSources, traceAnalysis);
            var tag = new RoboClerkTag(0, 37, "@@SLMS:SOUP(itemid=21,checksoup=true)@@", true);
            soupItems[1].SOUPVersion = "wrong";
            dataSources.GetAllSOUP().Returns(soupItems);
            var deps = GetDependencies();
            dataSources.GetAllExternalDependencies().Returns(deps);
            string content = soup.GetContent(tag, documentConfig);
            string expectedContent = "Roboclerk detected the following potential soup issues:\n\n* An external dependency (soupname2) has a matching soup item with a mismatched version (\"wrong\" instead of \"soupversion2\").\n\n";

            Assert.That(Regex.Replace(content, @"\r\n", "\n"), Is.EqualTo(expectedContent)); //ensure that we're always comparing the correct string, regardless of newline character for a platform
        }

    }
}