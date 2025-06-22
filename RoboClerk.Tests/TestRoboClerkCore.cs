using NUnit.Framework;
using NSubstitute;
using System;
using System.Collections.Generic;
using RoboClerk.Configuration;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using NUnit.Framework.Legacy;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("These tests test the RoboClerk Core")]
    internal class TestRoboClerkCore
    {

        private IConfiguration config = null;
        private IPluginLoader pluginLoader = null;
        private IDataSources dataSources = null;
        private ITraceabilityAnalysis traceAnalysis = null;
        private IFileSystem fs = null;

        [SetUp]
        public void TestSetup()
        {
            config = Substitute.For<IConfiguration>();
            dataSources = Substitute.For<IDataSources>();
            traceAnalysis = Substitute.For<ITraceabilityAnalysis>();
            fs = Substitute.For<IFileSystem>();
            pluginLoader = Substitute.For<IPluginLoader>();
        }

        [UnitTestAttribute(
        Identifier = "9B6BDA21-1C7D-4EE8-8EC6-11838377294A",
        Purpose = "RoboClerk Core is created",
        PostCondition = "No exception is thrown")]
        [Test]
        public void CreateRoboClerkCore()
        {
            var core = new RoboClerkCore(config,dataSources,traceAnalysis,fs,pluginLoader);
        }

        [UnitTestAttribute(
            Identifier = "9A3258CF-F9EE-4A1A-95E6-B49EF25FB200",
            Purpose = "RoboClerk Processes the media directory including subdirs, output media directory exists including subdirs",
            PostCondition = "Media directory is deleted, recreated and files are copied (except .gitignore)")]
        [Test]
        public void CheckMediaDirectory()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\temp\media\illustration.jpeg"), new MockFileData(new byte[] { 0x12, 0x34, 0x56, 0xd2 }) },
                { TestingHelpers.ConvertFileName(@"c:\temp\media\subdir\image.gif"), new MockFileData(new byte[] { 0x12, 0x34, 0x56, 0xd2 }) },
                { TestingHelpers.ConvertFileName(@"c:\temp\media\.gitignore"), new MockFileData("This is a gitignore file") },
                { TestingHelpers.ConvertFileName(@"c:\out\media\junk.jpeg"), new MockFileData(new byte[] { 0x11, 0x33, 0x55, 0xd1 }) },
            });
            config.MediaDir.Returns(TestingHelpers.ConvertFileName(@"c:\temp\media"));
            config.OutputDir.Returns(TestingHelpers.ConvertFileName(@"c:\out\"));
            config.Documents.Returns(new List<DocumentConfig>());
            
            var core = new RoboClerkCore(config, dataSources, traceAnalysis, fileSystem, pluginLoader);
            core.GenerateDocs();
            ClassicAssert.IsFalse(fileSystem.FileExists(TestingHelpers.ConvertFileName(@"c:\out\media\junk.jpeg")));
            ClassicAssert.IsTrue(fileSystem.FileExists(TestingHelpers.ConvertFileName(@"c:\temp\media\illustration.jpeg")));
            ClassicAssert.IsTrue(fileSystem.FileExists(TestingHelpers.ConvertFileName(@"c:\temp\media\subdir\image.gif")));
            ClassicAssert.IsTrue(fileSystem.FileExists(TestingHelpers.ConvertFileName(@"c:\temp\media\.gitignore")));
            ClassicAssert.IsTrue(fileSystem.FileExists(TestingHelpers.ConvertFileName(@"c:\out\media\illustration.jpeg")));
            ClassicAssert.IsTrue(fileSystem.FileExists(TestingHelpers.ConvertFileName(@"c:\out\media\subdir\image.gif")));
            ClassicAssert.IsFalse(fileSystem.FileExists(TestingHelpers.ConvertFileName(@"c:\out\media\.gitignore")));
        }

        [UnitTestAttribute(
            Identifier = "4FFA9AF7-7C4E-4B79-A3B5-67F49664A31B",
            Purpose = "RoboClerk Processes the media directory, output directory does not exist",
            PostCondition = "Media directory is created, files are copied (except .gitignore)")]
        [Test]
        public void CheckMediaDirectory2()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\temp\media\illustration.jpeg"), new MockFileData(new byte[] { 0x12, 0x34, 0x56, 0xd2 }) },
                { TestingHelpers.ConvertFileName(@"c:\temp\media\image.gif"), new MockFileData(new byte[] { 0x12, 0x34, 0x56, 0xd2 }) },
                { TestingHelpers.ConvertFileName(@"c:\temp\media\.gitignore"), new MockFileData("This is a gitignore file") },
                { TestingHelpers.ConvertFileName(@"c:\out\placeholder.bin"), new MockFileData(new byte[] { 0x11, 0x33, 0x55, 0xd1 }) },
            });
            config.MediaDir.Returns(TestingHelpers.ConvertFileName(@"c:\temp\media"));
            config.OutputDir.Returns(TestingHelpers.ConvertFileName(@"c:\out\"));
            config.Documents.Returns(new List<DocumentConfig>());

            var core = new RoboClerkCore(config, dataSources, traceAnalysis, fileSystem, pluginLoader);
            core.GenerateDocs();
            ClassicAssert.IsTrue(fileSystem.FileExists(TestingHelpers.ConvertFileName(@"c:\temp\media\illustration.jpeg")));
            ClassicAssert.IsTrue(fileSystem.FileExists(TestingHelpers.ConvertFileName(@"c:\temp\media\image.gif")));
            ClassicAssert.IsTrue(fileSystem.FileExists(TestingHelpers.ConvertFileName(@"c:\temp\media\.gitignore")));
            ClassicAssert.IsTrue(fileSystem.FileExists(TestingHelpers.ConvertFileName(@"c:\out\media\illustration.jpeg")));
            ClassicAssert.IsTrue(fileSystem.FileExists(TestingHelpers.ConvertFileName(@"c:\out\media\image.gif")));
            ClassicAssert.IsTrue(fileSystem.FileExists(TestingHelpers.ConvertFileName(@"c:\out\placeholder.bin")));
            ClassicAssert.IsFalse(fileSystem.FileExists(TestingHelpers.ConvertFileName(@"c:\out\media\.gitignore")));
        }

        [UnitTestAttribute(
            Identifier = "E566D07E-7A44-4D8E-8999-31F31A3EF833",
            Purpose = "RoboClerk Processes the media directory, media directory does not exist",
            PostCondition = "No changes are made to the filesystem")]
        [Test]
        public void CheckMediaDirectory3()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\out\placeholder.bin"), new MockFileData(new byte[] { 0x11, 0x33, 0x55, 0xd1 }) },
            });
            config.MediaDir.Returns(TestingHelpers.ConvertFileName(@"c:\temp\media"));
            config.OutputDir.Returns(TestingHelpers.ConvertFileName(@"c:\out\"));
            config.Documents.Returns(new List<DocumentConfig>());

            var core = new RoboClerkCore(config, dataSources, traceAnalysis, fs, pluginLoader);
            core.GenerateDocs();
            ClassicAssert.IsTrue(fileSystem.FileExists(TestingHelpers.ConvertFileName(@"c:\out\placeholder.bin")));
            ClassicAssert.IsFalse(fileSystem.FileExists(TestingHelpers.ConvertFileName(@"c:\out\media")));
        }

        [UnitTestAttribute(
            Identifier = "039C8A94-6DA6-4C62-B96E-77040153EB1C",
            Purpose = "RoboClerk processes a document without a template",
            PostCondition = "No output document is produced")]
        [Test]
        public void ProcessDocumentWithoutTemplate1()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\in\template.adoc"), new MockFileData("@@Config:SoftwareName()@@") },
                { TestingHelpers.ConvertFileName(@"c:\out\placeholder.bin"), new MockFileData(new byte[] { 0x11, 0x33, 0x55, 0xd1 }) },
            });
            config.MediaDir.Returns(TestingHelpers.ConvertFileName(@"c:\temp\media"));
            config.OutputDir.Returns(TestingHelpers.ConvertFileName(@"c:\out\"));
            DocumentConfig config2 = new DocumentConfig(
                "roboclerkID", "documentID", "documentTitle", "ABR", string.Empty);
            config.Documents.Returns(new List<DocumentConfig>() { config2 });

            var core = new RoboClerkCore(config, dataSources, traceAnalysis, fs, pluginLoader);
            core.GenerateDocs();
            core.SaveDocumentsToDisk();
            ClassicAssert.IsFalse(fileSystem.FileExists(TestingHelpers.ConvertFileName(@"c:\out\template.adoc")));
        }

        [UnitTestAttribute(
            Identifier = "8B35B5E8-0799-46AD-AD60-512AE625C093",
            Purpose = "RoboClerk processes a document with config value tag",
            PostCondition = "Resulting processed document is as expected")]
        [Test]
        public void ProcessConfigValueDocument1()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\in\template.adoc"), new MockFileData("@@Config:SoftwareName()@@") },
                { TestingHelpers.ConvertFileName(@"c:\out\placeholder.bin"), new MockFileData(new byte[] { 0x11, 0x33, 0x55, 0xd1 }) },
            });
            config.MediaDir.Returns(TestingHelpers.ConvertFileName(@"c:\temp\media"));
            config.OutputDir.Returns(TestingHelpers.ConvertFileName(@"c:\out\"));
            DocumentConfig config2 = new DocumentConfig(
                "roboclerkID", "documentID", "documentTitle", "ABR", TestingHelpers.ConvertFileName(@"c:\in\template.adoc"));
            config.Documents.Returns(new List<DocumentConfig>() { config2 });
            dataSources.GetConfigValue("SoftwareName").Returns("testvalue");

            var core = new RoboClerkCore(config, dataSources, traceAnalysis, fileSystem, pluginLoader);
            core.GenerateDocs();
            core.SaveDocumentsToDisk();
            ClassicAssert.IsTrue(fileSystem.FileExists(TestingHelpers.ConvertFileName(@"c:\out\template.adoc")));
            string content = fileSystem.File.ReadAllText(TestingHelpers.ConvertFileName(@"c:\out\template.adoc"));
            Assert.That(content == "testvalue");
        }

        [UnitTestAttribute(
            Identifier = "22717813-58D3-4676-AFC8-E98608B88B1C",
            Purpose = "RoboClerk processes a document with trace tag",
            PostCondition = "Resulting processed document is as expected")]
        [Test]
        public void ProcessTraceTagDocument1()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\in\template.adoc"), new MockFileData("@@Trace:SWR(id=89)@@ @@traCe:SWR(iD=19)@@") },
                { TestingHelpers.ConvertFileName(@"c:\out\placeholder.bin"), new MockFileData(new byte[] { 0x11, 0x33, 0x55, 0xd1 }) },
            });
            config.MediaDir.Returns(TestingHelpers.ConvertFileName(@"c:\temp\media"));
            config.OutputDir.Returns(TestingHelpers.ConvertFileName(@"c:\out\"));
            DocumentConfig config2 = new DocumentConfig(
                "roboclerkID", "documentID", "documentTitle", "ABR", TestingHelpers.ConvertFileName(@"c:\in\template.adoc"));
            config.Documents.Returns(new List<DocumentConfig>() { config2 });
            var item = new RequirementItem(RequirementType.SoftwareRequirement);
            item.Link = new Uri("http://localhost/");
            dataSources.GetItem("19").Returns(item);

            var core = new RoboClerkCore(config, dataSources, traceAnalysis, fileSystem, pluginLoader);
            core.GenerateDocs();
            core.SaveDocumentsToDisk();
            ClassicAssert.IsTrue(fileSystem.FileExists(TestingHelpers.ConvertFileName(@"c:\out\template.adoc")));
            string content = fileSystem.File.ReadAllText(TestingHelpers.ConvertFileName(@"c:\out\template.adoc"));
            Assert.That(content == "(89) (http://localhost/[19])");

            fileSystem.File.WriteAllText(TestingHelpers.ConvertFileName(@"c:\in\template.adoc"), "@@Trace:SWR()@@");
            core = new RoboClerkCore(config, dataSources, traceAnalysis, fileSystem, pluginLoader);
            Assert.Throws<TagInvalidException>(() => core.GenerateDocs());
        }

        [UnitTestAttribute(
            Identifier = "E45202E9-39CC-47D8-A749-43B5EB2EA28F",
            Purpose = "RoboClerk processes a document with a comment tag",
            PostCondition = "Resulting processed document is as expected")]
        [Test]
        public void ProcessCommentTagDocument1()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\in\template.adoc"), new MockFileData("remainder\n@@@Comment:general()\nthis is the comment\n@@@") },
                { TestingHelpers.ConvertFileName(@"c:\out\placeholder.bin"), new MockFileData(new byte[] { 0x11, 0x33, 0x55, 0xd1 }) },
            });
            config.MediaDir.Returns(TestingHelpers.ConvertFileName(@"c:\temp\media"));
            config.OutputDir.Returns(TestingHelpers.ConvertFileName(@"c:\out\"));
            DocumentConfig config2 = new DocumentConfig(
                "roboclerkID", "documentID", "documentTitle", "ABR", TestingHelpers.ConvertFileName(@"c:\in\template.adoc"));
            config.Documents.Returns(new List<DocumentConfig>() { config2 });

            var core = new RoboClerkCore(config, dataSources, traceAnalysis, fileSystem, pluginLoader);
            core.GenerateDocs();
            core.SaveDocumentsToDisk();
            ClassicAssert.IsTrue(fileSystem.FileExists(TestingHelpers.ConvertFileName(@"c:\out\template.adoc")));
            string content = fileSystem.File.ReadAllText(TestingHelpers.ConvertFileName(@"c:\out\template.adoc"));
            Assert.That(content == "remainder\n");
        }

        [UnitTestAttribute(
            Identifier = "8E4E6058-7606-415C-9191-FCEE6F3A37F7",
            Purpose = "RoboClerk processes a document with a post tag",
            PostCondition = "Resulting processed document is as expected")]
        [Test]
        public void ProcessPostTagDocument1()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\in\template.adoc"), new MockFileData("@@PosT:PageBreak()@@ @@POSt:RemoveParagraph()@@ @@post:Toc()@@ @@post:unknown()@@") },
                { TestingHelpers.ConvertFileName(@"c:\out\placeholder.bin"), new MockFileData(new byte[] { 0x11, 0x33, 0x55, 0xd1 }) },
            });
            config.MediaDir.Returns(TestingHelpers.ConvertFileName(@"c:\temp\media"));
            config.OutputDir.Returns(TestingHelpers.ConvertFileName(@"c:\out\"));
            DocumentConfig config2 = new DocumentConfig(
                "roboclerkID", "documentID", "documentTitle", "ABR", TestingHelpers.ConvertFileName(@"c:\in\template.adoc"));
            config.Documents.Returns(new List<DocumentConfig>() { config2 });

            var core = new RoboClerkCore(config, dataSources, traceAnalysis, fileSystem, pluginLoader);
            core.GenerateDocs();
            core.SaveDocumentsToDisk();
            ClassicAssert.IsTrue(fileSystem.FileExists(TestingHelpers.ConvertFileName(@"c:\out\template.adoc")));
            string content = fileSystem.File.ReadAllText(TestingHelpers.ConvertFileName(@"c:\out\template.adoc"));
            Assert.That(content == "~PAGEBREAK ~REMOVEPARAGRAPH ~TOC UNKNOWN POST PROCESSING TAG: unknown");
        }

        [UnitTestAttribute(
            Identifier = "C8A8666F-6D6D-44BF-A22F-41077F6068E8",
            Purpose = "RoboClerk processes a document with a reference tag and a non-existent reference tag",
            PostCondition = "Resulting processed document is as expected")]
        [Test]
        public void ProcessReferenceTagDocument1()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\in\template.adoc"), new MockFileData("@@Ref:roboclerkID()@@ @@Ref:roboclerkID(id=true,title=true,abbr=true,template=true)@@") } , 
                { TestingHelpers.ConvertFileName(@"c:\out\placeholder.bin"), new MockFileData(new byte[] { 0x11, 0x33, 0x55, 0xd1 }) },
            });
            config.MediaDir.Returns(TestingHelpers.ConvertFileName(@"c:\temp\media"));
            config.OutputDir.Returns(TestingHelpers.ConvertFileName(@"c:\out\"));
            DocumentConfig config2 = new DocumentConfig(
                "roboclerkID", "documentID", "documentTitle", "ABR", TestingHelpers.ConvertFileName(@"c:\in\template.adoc"));
            config.Documents.Returns(new List<DocumentConfig>() { config2 });
            traceAnalysis.GetTraceEntityForID("roboclerkID").Returns(new TraceEntity("roboclerkID","documentTitle","ABR",TraceEntityType.Document));

            var core = new RoboClerkCore(config, dataSources, traceAnalysis, fileSystem, pluginLoader);
            core.GenerateDocs();
            core.SaveDocumentsToDisk();
            ClassicAssert.IsTrue(fileSystem.FileExists(TestingHelpers.ConvertFileName(@"c:\out\template.adoc")));
            string content = fileSystem.File.ReadAllText(TestingHelpers.ConvertFileName(@"c:\out\template.adoc"));
            Assert.That(content == $"{config2.DocumentTitle} {config2.DocumentID} {config2.DocumentTitle} ({config2.DocumentAbbreviation}) {config2.DocumentTemplate}");

            fileSystem.File.WriteAllText(TestingHelpers.ConvertFileName(@"c:\in\template.adoc"), "@@Ref:nonexistentID()@@ @@Ref:nonexistentID(abbr=true)@@");
            core = new RoboClerkCore(config, dataSources, traceAnalysis, fileSystem, pluginLoader);
            Assert.Throws<TagInvalidException>(() => core.GenerateDocs());
        }

        [UnitTestAttribute(
            Identifier = "05E1D254-E5D7-41DA-836B-E86C030F9F7C",
            Purpose = "RoboClerk processes a document with all reference tag parameters and one that has unknown parameters",
            PostCondition = "The tags are processed successfully and then an exception is thrown for the unknown parameter.")]
        [Test]
        public void ProcessReferenceTagDocument2()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\in\template.adoc"), new MockFileData("@@Ref:roboclerkID(title=true)@@ @@Ref:roboclerkID(id=true)@@ @@Ref:roboclerkID(title=true)@@ (@@Ref:roboclerkID(abbr=true)@@) @@Ref:roboclerkID(template=true)@@") } ,
                { TestingHelpers.ConvertFileName(@"c:\out\placeholder.bin"), new MockFileData(new byte[] { 0x11, 0x33, 0x55, 0xd1 }) },
            });
            config.MediaDir.Returns(TestingHelpers.ConvertFileName(@"c:\temp\media"));
            config.OutputDir.Returns(TestingHelpers.ConvertFileName(@"c:\out\"));
            DocumentConfig config2 = new DocumentConfig(
                "roboclerkID", "documentID", "documentTitle", "ABR", TestingHelpers.ConvertFileName(@"c:\in\template.adoc"));
            config.Documents.Returns(new List<DocumentConfig>() { config2 });
            traceAnalysis.GetTraceEntityForID("roboclerkID").Returns(new TraceEntity("roboclerkID", "documentTitle", "ABR", TraceEntityType.Document));

            var core = new RoboClerkCore(config, dataSources, traceAnalysis, fileSystem, pluginLoader);
            core.GenerateDocs();
            core.SaveDocumentsToDisk();
            ClassicAssert.IsTrue(fileSystem.FileExists(TestingHelpers.ConvertFileName(@"c:\out\template.adoc")));
            string content = fileSystem.File.ReadAllText(TestingHelpers.ConvertFileName(@"c:\out\template.adoc"));
            Assert.That(content == $"{config2.DocumentTitle} {config2.DocumentID} {config2.DocumentTitle} ({config2.DocumentAbbreviation}) {config2.DocumentTemplate}");

            fileSystem.File.WriteAllText(TestingHelpers.ConvertFileName(@"c:\in\template.adoc"), "@@Ref:roboclerkID()@@ @@Ref:roboclerkID(doesnotexist=true)@@");
            core = new RoboClerkCore(config, dataSources, traceAnalysis, fileSystem, pluginLoader);
            Assert.Throws<TagInvalidException>(() => core.GenerateDocs());
        }

        [UnitTestAttribute(
            Identifier = "49CD1DAA-CB59-4988-9BFE-1C9FA4B1BEF2",
            Purpose = "RoboClerk processes a document with a document tag",
            PostCondition = "Resulting processed document is as expected")]
        [Test]
        public void ProcessDocumentTagDocument1()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\in\template.adoc"), new MockFileData("@@Document:Title()@@ @@DocuMent:abbreviAtion()@@ @@Document:identifier()@@ @@Document:template()@@ @@docUment:RoboClerkID()@@") } , 
                { TestingHelpers.ConvertFileName(@"c:\out\placeholder.bin"), new MockFileData(new byte[] { 0x11, 0x33, 0x55, 0xd1 }) },
            });
            config.MediaDir.Returns(TestingHelpers.ConvertFileName(@"c:\temp\media"));
            config.OutputDir.Returns(TestingHelpers.ConvertFileName(@"c:\out\"));
            DocumentConfig config2 = new DocumentConfig(
                "roboclerkID", "documentID", "documentTitle", "ABR", TestingHelpers.ConvertFileName(@"c:\in\template.adoc"));
            config.Documents.Returns(new List<DocumentConfig>() { config2 });

            var core = new RoboClerkCore(config, dataSources, traceAnalysis, fileSystem, pluginLoader);
            core.GenerateDocs();
            core.SaveDocumentsToDisk();
            ClassicAssert.IsTrue(fileSystem.FileExists(TestingHelpers.ConvertFileName(@"c:\out\template.adoc")));
            string content = fileSystem.File.ReadAllText(TestingHelpers.ConvertFileName(@"c:\out\template.adoc"));
            Assert.That(content == @"documentTitle ABR documentID "+ TestingHelpers.ConvertFileName(@"c:\in\template.adoc") +" roboclerkID");

            fileSystem.File.WriteAllText(TestingHelpers.ConvertFileName(@"c:\in\template.adoc"), "@@document:nonexistentID()@@");
            core = new RoboClerkCore(config, dataSources, traceAnalysis, fileSystem, pluginLoader);
            Assert.Throws<Exception>(() => core.GenerateDocs());
        }

        [UnitTestAttribute(
            Identifier = "E32049B1-F1EA-4D5E-A6EF-CEB9F1532FE4",
            Purpose = "RoboClerk processes a document with a software requirement tag",
            PostCondition = "Resulting processed document is as expected")]
        [Test]
        public void ProcessSoftwareRequirementTagDocument1()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { TestingHelpers.ConvertFileName(@"c:\in\template.adoc"), new MockFileData("@@SLMS:SWR(ItemID=14)@@") },
                { TestingHelpers.ConvertFileName(@"c:\out\placeholder.bin"), new MockFileData(new byte[] { 0x11, 0x33, 0x55, 0xd1 }) },
            });
            config.MediaDir.Returns(TestingHelpers.ConvertFileName(@"c:\temp\media"));
            config.OutputDir.Returns(TestingHelpers.ConvertFileName(@"c:\out\"));
            DocumentConfig config2 = new DocumentConfig(
                "roboclerkID", "documentID", "documentTitle", "ABR", TestingHelpers.ConvertFileName(@"c:\in\template.adoc"));
            config.Documents.Returns(new List<DocumentConfig>() { config2 });

            var testRequirement = new RequirementItem(RequirementType.SoftwareRequirement);
            testRequirement.ItemTitle = "title";
            testRequirement.RequirementDescription = "description";
            testRequirement.ItemRevision = "rev1";
            testRequirement.ItemID = "14";
            var te = new TraceEntity("SoftwareRequirement", "typename", "SWR", TraceEntityType.Truth);
            dataSources.GetItems(te).Returns(new List<LinkedItem> { testRequirement });
            dataSources.GetTemplateFile(@"./ItemTemplates/Requirement.adoc").Returns(@"[csx:
// this first scripting block can be used to set up any prerequisites
// pre-calculate fields for later use etc.
using RoboClerk;

TraceEntity te = SourceTraceEntity;
RequirementItem item = (RequirementItem)Item;
string pl = GetLinkedField(item, ItemLinkType.Parent);
AddTrace(item.ItemID);
]
|====
| [csx:te.Name] ID: | [csx:GetItemLinkString(item)]
| [csx:te.Name] Revision: | [csx:item.ItemRevision]
| [csx:te.Name] Category: | [csx:item.ItemCategory]
| Parent ID: | [csx:pl]
| Title: | [csx:item.ItemTitle]
| Description: a| [csx:item.RequirementDescription]
|====");
            dataSources.GetItem("14").Returns(testRequirement);
            traceAnalysis.GetTraceEntityForAnyProperty("SWR").Returns(te);
            traceAnalysis.GetTraceEntityForID("SoftwareRequirement").Returns(te);

            var core = new RoboClerkCore(config, dataSources, traceAnalysis, fileSystem, pluginLoader);
            core.GenerateDocs();
            core.SaveDocumentsToDisk();
            ClassicAssert.IsTrue(fileSystem.FileExists(TestingHelpers.ConvertFileName(@"c:\out\template.adoc")));
            string content = fileSystem.File.ReadAllText(TestingHelpers.ConvertFileName(@"c:\out\template.adoc"));
            Assert.That(content == "\n|====\n| typename ID: | 14\n| typename Revision: | rev1\n| typename Category: | \n| Parent ID: | N/A\n| Title: | title\n| Description: a| description\n|====\n");

            fileSystem.File.WriteAllText(TestingHelpers.ConvertFileName(@"c:\in\template.adoc"), "@@SLMS:unknown(ItemID=33)@@");
            core = new RoboClerkCore(config, dataSources, traceAnalysis, fileSystem, pluginLoader);
            core.GenerateDocs();
            core.SaveDocumentsToDisk();
            content = fileSystem.File.ReadAllText(TestingHelpers.ConvertFileName(@"c:\out\template.adoc"));
            Assert.That(content == "UNABLE TO CREATE CONTENT, ENSURE THAT THE CONTENT CREATOR CLASS (unknown) IS KNOWN TO ROBOCLERK.\n");
        }

    }
}
