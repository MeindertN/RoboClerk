using NSubstitute;
using NUnit.Framework;
using RoboClerk.ContentCreators;
using RoboClerk.Core;
using RoboClerk.Core.Configuration;
using RoboClerk.Core.FileProviders;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("These tests test the ContentCreatorMetadataRegistry")]
    internal class TestContentCreatorMetadataRegistry
    {
        private IConfiguration config = null;
        private IFileProviderPlugin fileProvider = null;

        [SetUp]
        public void TestSetup()
        {
            config = Substitute.For<IConfiguration>();
            config.TruthEntities.Returns(new List<TraceEntity>()); // Ensure TruthEntities is not null
            config.TraceConfig.Returns(new List<TraceConfig>()); // Ensure TraceConfig is not null
            fileProvider = Substitute.For<IFileProviderPlugin>();
            ContentCreatorMetadataRegistry.Reset();
        }

        [TearDown]
        public void TestTeardown()
        {
            ContentCreatorMetadataRegistry.Reset();
        }

        [UnitTestAttribute(
        Identifier = "21867c74-ae91-4717-a301-8d5a80273463",
        Purpose = "Registry initializes and registers default content creators",
        PostCondition = "Default content creators are registered")]
        [Test]
        public void TestRegistryInitialization()
        {
            var metadata = ContentCreatorMetadataRegistry.GetAllMetadata(config, fileProvider).ToList();
            
            Assert.That(metadata, Is.Not.Empty);
            Assert.That(metadata.Any(m => m.Name == "Document Properties"), Is.True);
            Assert.That(metadata.Any(m => m.Name == "System Requirement"), Is.True);
            Assert.That(metadata.Any(m => m.Name == "Generic Traceability Matrix"), Is.True);
        }

        [UnitTestAttribute(
        Identifier = "19ab6d07-e00f-4688-8de6-3aa942396b98",
        Purpose = "Register adds a new metadata provider",
        PostCondition = "New provider is registered and retrievable")]
        [Test]
        public void TestRegister()
        {
            var testMetadata = new ContentCreatorMetadata("Test", "Test Creator", "Description");
            ContentCreatorMetadataRegistry.Register("TestCreator", (c, f) => testMetadata);

            var retrieved = ContentCreatorMetadataRegistry.GetMetadata("TestCreator", config, fileProvider);
            Assert.That(retrieved, Is.SameAs(testMetadata));
        }

        [UnitTestAttribute(
        Identifier = "d18b388f-4e58-47a3-9d57-771911cfa856",
        Purpose = "GetMetadata retrieves by key",
        PostCondition = "Correct metadata is returned")]
        [Test]
        public void TestGetMetadataByKey()
        {
            var metadata = ContentCreatorMetadataRegistry.GetMetadata("Document", config, fileProvider);
            Assert.That(metadata, Is.Not.Null);
            Assert.That(metadata.Name, Is.EqualTo("Document Properties"));
        }

        [UnitTestAttribute(
        Identifier = "4ce6c436-657a-44e8-bc6e-c9f3fd6388c7",
        Purpose = "GetMetadata retrieves by source",
        PostCondition = "Correct metadata is returned")]
        [Test]
        public void TestGetMetadataBySource()
        {          
            // Let's register a custom one to be sure
            var testMetadata = new ContentCreatorMetadata("CustomSource", "Custom Name", "Desc");
            ContentCreatorMetadataRegistry.Register("CustomKey", (c, f) => testMetadata);

            // Retrieve by source, not key
            var retrieved = ContentCreatorMetadataRegistry.GetMetadata("CustomSource", config, fileProvider);
            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved.Name, Is.EqualTo("Custom Name"));
        }

        [UnitTestAttribute(
        Identifier = "73083645-337a-4966-b8c1-c334de1160dc",
        Purpose = "GetMetadata returns null for unknown key/source",
        PostCondition = "Null is returned")]
        [Test]
        public void TestGetMetadataUnknown()
        {
            var retrieved = ContentCreatorMetadataRegistry.GetMetadata("NonExistent", config, fileProvider);
            Assert.That(retrieved, Is.Null);
        }

        [UnitTestAttribute(
        Identifier = "aae49b7c-c621-45cb-b70f-ec2ba7f1cc3a",
        Purpose = "Refresh forces re-initialization",
        PostCondition = "Registry is re-initialized on next access")]
        [Test]
        public void TestRefresh()
        {
            // Ensure initialized
            var count1 = ContentCreatorMetadataRegistry.GetAllMetadata(config, fileProvider).Count();
            
            // Add a custom one
            ContentCreatorMetadataRegistry.Register("Temp", (c, f) => new ContentCreatorMetadata("T", "T", "T"));
            var count2 = ContentCreatorMetadataRegistry.GetAllMetadata(config, fileProvider).Count();
            Assert.That(count2, Is.EqualTo(count1 + 1));
            
            ContentCreatorMetadataRegistry.Refresh();
            
            // After refresh, "Temp" should still be there because _metadataProviders wasn't cleared.
            // But defaults are re-registered (overwriting if keys match).
            var count3 = ContentCreatorMetadataRegistry.GetAllMetadata(config, fileProvider).Count();
            Assert.That(count3, Is.EqualTo(count2)); 
        }

        [UnitTestAttribute(
        Identifier = "aa394173-9a09-4e8b-88f7-67e4dcba7674",
        Purpose = "Reset clears the registry",
        PostCondition = "Registry is empty until re-initialized")]
        [Test]
        public void TestReset()
        {
            ContentCreatorMetadataRegistry.GetAllMetadata(config, fileProvider).ToList(); // Initialize
            ContentCreatorMetadataRegistry.Reset();
            
            // We can't easily check internal state, but we can check that a custom registered item is gone
            // However, calling GetAllMetadata will re-initialize defaults.
            
            // So let's register custom, Reset, then check if custom is gone after re-init.
            ContentCreatorMetadataRegistry.Register("Custom", (c, f) => new ContentCreatorMetadata("C", "C", "C"));
            Assert.That(ContentCreatorMetadataRegistry.GetMetadata("Custom", config, fileProvider), Is.Not.Null);
            
            ContentCreatorMetadataRegistry.Reset();
            
            // This will trigger re-init of defaults, but "Custom" should be gone
            var metadata = ContentCreatorMetadataRegistry.GetMetadata("Custom", config, fileProvider);
            Assert.That(metadata, Is.Null);
            
            // Defaults should be back
            Assert.That(ContentCreatorMetadataRegistry.GetMetadata("Document", config, fileProvider), Is.Not.Null);
        }

        [UnitTestAttribute(
        Identifier = "c6b0294e-106f-4e9e-a27c-4ca84c27b25e",
        Purpose = "GetAllMetadata passes config and file provider to providers",
        PostCondition = "Providers receive the context")]
        [Test]
        public void TestContextPassing()
        {
            IConfiguration receivedConfig = null;
            IFileProviderPlugin receivedProvider = null;

            ContentCreatorMetadataRegistry.Register("ContextTest", (c, f) => 
            {
                receivedConfig = c;
                receivedProvider = f;
                return new ContentCreatorMetadata("Ctx", "Ctx", "Ctx");
            });

            ContentCreatorMetadataRegistry.GetMetadata("ContextTest", config, fileProvider);

            Assert.That(receivedConfig, Is.SameAs(config));
            Assert.That(receivedProvider, Is.SameAs(fileProvider));
        }
    }
}
