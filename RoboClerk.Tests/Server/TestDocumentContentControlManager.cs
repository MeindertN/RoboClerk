using NUnit.Framework;
using NSubstitute;
using RoboClerk.Server.Models;
using RoboClerk.Core.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace RoboClerk.Tests.Server
{
    [TestFixture]
    [Description("Tests for the DocumentContentControlManager that manages virtual content controls for documents")]
    public class TestDocumentContentControlManager
    {
        private DocumentContentControlManager manager;
        private IConfiguration mockConfiguration;

        [SetUp]
        public void Setup()
        {
            manager = new DocumentContentControlManager();
            mockConfiguration = Substitute.For<IConfiguration>();
        }

        [UnitTestAttribute(
            Identifier = "D5E63F50-BECA-48C6-AAF5-C9ADD734BE98",
            Purpose = "DocumentContentControlManager is created successfully",
            PostCondition = "Manager is initialized with no virtual tags")]
        [Test]
        public void CreateManager_Success()
        {
            // Arrange & Act
            var newManager = new DocumentContentControlManager();

            // Assert
            Assert.That(newManager, Is.Not.Null);
            Assert.That(newManager.GetTotalVirtualTagCount(), Is.EqualTo(0));
        }

        [UnitTestAttribute(
            Identifier = "132D2D42-1BB2-4E53-BF71-86BE784B1690",
            Purpose = "GetOrCreateContentControl creates a new virtual tag when none exists",
            PostCondition = "Virtual tag is created and returned")]
        [Test]
        public void GetOrCreateContentControl_NewTag_CreatesTag()
        {
            // Arrange
            string documentId = "doc1";
            string contentControlId = "cc1";
            string roboclerkTag = "SLMS:SoftwareRequirement()";

            // Act
            var tag = manager.GetOrCreateContentControl(documentId, contentControlId, roboclerkTag, mockConfiguration);

            // Assert
            Assert.That(tag, Is.Not.Null);
            Assert.That(tag.ContentControlId, Is.EqualTo(contentControlId));
            Assert.That(manager.GetTotalVirtualTagCount(), Is.EqualTo(1));
        }

        [UnitTestAttribute(
            Identifier = "D14EE999-1DF2-4E5B-A488-CD851147BA46",
            Purpose = "GetOrCreateContentControl returns existing tag when it already exists",
            PostCondition = "Same tag instance is returned")]
        [Test]
        public void GetOrCreateContentControl_ExistingTag_ReturnsSameTag()
        {
            // Arrange
            string documentId = "doc1";
            string contentControlId = "cc1";
            string roboclerkTag = "SLMS:SoftwareRequirement()";

            var firstTag = manager.GetOrCreateContentControl(documentId, contentControlId, roboclerkTag, mockConfiguration);

            // Act
            var secondTag = manager.GetOrCreateContentControl(documentId, contentControlId, roboclerkTag, mockConfiguration);

            // Assert
            Assert.That(secondTag, Is.SameAs(firstTag));
            Assert.That(manager.GetTotalVirtualTagCount(), Is.EqualTo(1));
        }

        [UnitTestAttribute(
            Identifier = "A21B9AA2-E7BC-4200-8EC6-239E5F2BF3D6",
            Purpose = "GetOrCreateContentControl creates separate tags for different content control IDs",
            PostCondition = "Two separate tags are created")]
        [Test]
        public void GetOrCreateContentControl_DifferentContentControlIds_CreatesSeparateTags()
        {
            // Arrange
            string documentId = "doc1";
            string roboclerkTag = "SLMS:SoftwareRequirement()";

            // Act
            var tag1 = manager.GetOrCreateContentControl(documentId, "cc1", roboclerkTag, mockConfiguration);
            var tag2 = manager.GetOrCreateContentControl(documentId, "cc2", roboclerkTag, mockConfiguration);

            // Assert
            Assert.That(tag1, Is.Not.SameAs(tag2));
            Assert.That(manager.GetTotalVirtualTagCount(), Is.EqualTo(2));
        }

        [UnitTestAttribute(
            Identifier = "10C7436B-833A-43BE-AD19-03F926CCE860",
            Purpose = "GetOrCreateContentControl creates separate tags for different documents",
            PostCondition = "Tags are created per document")]
        [Test]
        public void GetOrCreateContentControl_DifferentDocuments_CreatesSeparateTags()
        {
            // Arrange
            string contentControlId = "cc1";
            string roboclerkTag = "SLMS:SoftwareRequirement()";

            // Act
            var tag1 = manager.GetOrCreateContentControl("doc1", contentControlId, roboclerkTag, mockConfiguration);
            var tag2 = manager.GetOrCreateContentControl("doc2", contentControlId, roboclerkTag, mockConfiguration);

            // Assert
            Assert.That(tag1, Is.Not.SameAs(tag2));
            Assert.That(manager.GetTotalVirtualTagCount(), Is.EqualTo(2));
        }

        [UnitTestAttribute(
            Identifier = "53F639C4-00A9-44EF-A2D0-075DA4FB34A9",
            Purpose = "GetVirtualTagsForDocument returns all tags for a specific document",
            PostCondition = "List of virtual tags for the document is returned")]
        [Test]
        public void GetVirtualTagsForDocument_ReturnsTagsForDocument()
        {
            // Arrange
            manager.GetOrCreateContentControl("doc1", "cc1", "SLMS:SR()", mockConfiguration);
            manager.GetOrCreateContentControl("doc1", "cc2", "SLMS:SR()", mockConfiguration);
            manager.GetOrCreateContentControl("doc2", "cc3", "SLMS:SR()", mockConfiguration);

            // Act
            var doc1Tags = manager.GetVirtualTagsForDocument("doc1");
            var doc2Tags = manager.GetVirtualTagsForDocument("doc2");

            // Assert
            Assert.That(doc1Tags.Count, Is.EqualTo(2));
            Assert.That(doc2Tags.Count, Is.EqualTo(1));
        }

        [UnitTestAttribute(
            Identifier = "B19EC11A-618D-453A-A63F-0BBC8C73D9FB",
            Purpose = "GetVirtualTagsForDocument returns empty list for non-existent document",
            PostCondition = "Empty list is returned")]
        [Test]
        public void GetVirtualTagsForDocument_NonExistentDocument_ReturnsEmptyList()
        {
            // Arrange
            manager.GetOrCreateContentControl("doc1", "cc1", "SLMS:SR()", mockConfiguration);

            // Act
            var tags = manager.GetVirtualTagsForDocument("nonexistent");

            // Assert
            Assert.That(tags, Is.Not.Null);
            Assert.That(tags.Count, Is.EqualTo(0));
        }

        [UnitTestAttribute(
            Identifier = "6A3C2DDC-1D07-4F89-B315-DF869447D64D",
            Purpose = "ClearVirtualTagsForDocument removes all tags for a specific document",
            PostCondition = "Tags are cleared and count is returned")]
        [Test]
        public void ClearVirtualTagsForDocument_ClearsTags()
        {
            // Arrange
            manager.GetOrCreateContentControl("doc1", "cc1", "SLMS:SR()", mockConfiguration);
            manager.GetOrCreateContentControl("doc1", "cc2", "SLMS:SR()", mockConfiguration);
            manager.GetOrCreateContentControl("doc2", "cc3", "SLMS:SR()", mockConfiguration);

            // Act
            var clearedCount = manager.ClearVirtualTagsForDocument("doc1");

            // Assert
            Assert.That(clearedCount, Is.EqualTo(2));
            Assert.That(manager.GetVirtualTagsForDocument("doc1").Count, Is.EqualTo(0));
            Assert.That(manager.GetVirtualTagsForDocument("doc2").Count, Is.EqualTo(1));
        }

        [UnitTestAttribute(
            Identifier = "46BB963F-E245-44EA-AC69-6F53543D8F7F",
            Purpose = "ClearVirtualTagsForDocument returns zero for non-existent document",
            PostCondition = "Zero is returned")]
        [Test]
        public void ClearVirtualTagsForDocument_NonExistentDocument_ReturnsZero()
        {
            // Arrange
            manager.GetOrCreateContentControl("doc1", "cc1", "SLMS:SR()", mockConfiguration);

            // Act
            var clearedCount = manager.ClearVirtualTagsForDocument("nonexistent");

            // Assert
            Assert.That(clearedCount, Is.EqualTo(0));
        }

        [UnitTestAttribute(
            Identifier = "1F1B5C55-2A56-49B7-BACD-82B8A7D27E3A",
            Purpose = "ClearAllVirtualTags removes all tags from all documents",
            PostCondition = "All tags are cleared and total count is returned")]
        [Test]
        public void ClearAllVirtualTags_ClearsAllTags()
        {
            // Arrange
            manager.GetOrCreateContentControl("doc1", "cc1", "SLMS:SR()", mockConfiguration);
            manager.GetOrCreateContentControl("doc1", "cc2", "SLMS:SR()", mockConfiguration);
            manager.GetOrCreateContentControl("doc2", "cc3", "SLMS:SR()", mockConfiguration);

            // Act
            var clearedCount = manager.ClearAllVirtualTags();

            // Assert
            Assert.That(clearedCount, Is.EqualTo(3));
            Assert.That(manager.GetTotalVirtualTagCount(), Is.EqualTo(0));
        }

        [UnitTestAttribute(
            Identifier = "56426605-6E57-45DD-BDD9-1ABC0CDBF5FE",
            Purpose = "GetTotalVirtualTagCount returns correct total count",
            PostCondition = "Correct total count is returned")]
        [Test]
        public void GetTotalVirtualTagCount_ReturnsCorrectCount()
        {
            // Arrange
            manager.GetOrCreateContentControl("doc1", "cc1", "SLMS:SR()", mockConfiguration);
            manager.GetOrCreateContentControl("doc1", "cc2", "SLMS:SR()", mockConfiguration);
            manager.GetOrCreateContentControl("doc2", "cc3", "SLMS:SR()", mockConfiguration);
            manager.GetOrCreateContentControl("doc3", "cc4", "SLMS:SR()", mockConfiguration);

            // Act
            var totalCount = manager.GetTotalVirtualTagCount();

            // Assert
            Assert.That(totalCount, Is.EqualTo(4));
        }

        [UnitTestAttribute(
            Identifier = "CC0E0BDF-E218-4A38-8265-ED4D3B3A2DCA",
            Purpose = "GetVirtualTagStatistics returns correct statistics per document",
            PostCondition = "Dictionary with document IDs and counts is returned")]
        [Test]
        public void GetVirtualTagStatistics_ReturnsCorrectStats()
        {
            // Arrange
            manager.GetOrCreateContentControl("doc1", "cc1", "SLMS:SR()", mockConfiguration);
            manager.GetOrCreateContentControl("doc1", "cc2", "SLMS:SR()", mockConfiguration);
            manager.GetOrCreateContentControl("doc2", "cc3", "SLMS:SR()", mockConfiguration);

            // Act
            var stats = manager.GetVirtualTagStatistics();

            // Assert
            Assert.That(stats.ContainsKey("doc1"), Is.True);
            Assert.That(stats.ContainsKey("doc2"), Is.True);
            Assert.That(stats["doc1"], Is.EqualTo(2));
            Assert.That(stats["doc2"], Is.EqualTo(1));
        }

        [UnitTestAttribute(
            Identifier = "02C0A91F-CA2C-48EE-8C02-4AAAF82231CD",
            Purpose = "GetVirtualTagStatistics returns empty dictionary when no tags exist",
            PostCondition = "Empty dictionary is returned")]
        [Test]
        public void GetVirtualTagStatistics_NoTags_ReturnsEmptyDictionary()
        {
            // Act
            var stats = manager.GetVirtualTagStatistics();

            // Assert
            Assert.That(stats, Is.Not.Null);
            Assert.That(stats.Count, Is.EqualTo(0));
        }

        [UnitTestAttribute(
            Identifier = "7D569B36-97B5-464E-AB46-2EAF88615D1E",
            Purpose = "Thread safety test for concurrent GetOrCreateContentControl calls",
            PostCondition = "No race conditions or duplicate tags created")]
        [Test]
        public void GetOrCreateContentControl_ConcurrentAccess_ThreadSafe()
        {
            // Arrange
            string documentId = "doc1";
            string contentControlId = "cc1";
            string roboclerkTag = "SLMS:SoftwareRequirement()";
            var tasks = new List<Task<VirtualDocxTag>>();

            // Act - Create multiple concurrent requests for the same tag
            for (int i = 0; i < 100; i++)
            {
                tasks.Add(Task.Run(() => manager.GetOrCreateContentControl(documentId, contentControlId, roboclerkTag, mockConfiguration)));
            }

            Task.WaitAll(tasks.ToArray());

            // Assert - All should return the same tag instance
            var results = tasks.Select(t => t.Result).ToList();
            var firstTag = results[0];
            Assert.That(results.All(t => ReferenceEquals(t, firstTag)), Is.True);
            Assert.That(manager.GetTotalVirtualTagCount(), Is.EqualTo(1));
        }

        [UnitTestAttribute(
            Identifier = "CBD22818-2983-469B-87D4-C8DB88E111AD",
            Purpose = "GetVirtualTagsForDocument returns a copy of the list",
            PostCondition = "Modifying returned list does not affect internal state")]
        [Test]
        public void GetVirtualTagsForDocument_ReturnsCopy()
        {
            // Arrange
            manager.GetOrCreateContentControl("doc1", "cc1", "SLMS:SR()", mockConfiguration);
            manager.GetOrCreateContentControl("doc1", "cc2", "SLMS:SR()", mockConfiguration);

            // Act
            var tags = manager.GetVirtualTagsForDocument("doc1");
            tags.Clear();

            // Assert - Internal state should not be affected
            Assert.That(manager.GetVirtualTagsForDocument("doc1").Count, Is.EqualTo(2));
        }
    }
}
