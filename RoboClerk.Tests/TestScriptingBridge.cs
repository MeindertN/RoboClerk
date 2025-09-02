using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using NSubstitute;
using RoboClerk;
using NUnit.Framework.Legacy;
using RoboClerk.Core.Configuration;

namespace RoboClerk.Tests
{
    /// <summary>
    /// This class tests the functionality of the ScriptingBridge class.
    /// </summary>
    [TestFixture]
    public class ScriptingBridgeTests
    {
        private IDataSources mockDataSources;
        private ITraceabilityAnalysis mockTraceAnalysis;
        private IConfiguration mockConfiguration;
        private TraceEntity mockSourceTraceEntity;
        private ScriptingBridge scriptingBridge;

        [SetUp]
        public void Setup()
        {
            mockDataSources = Substitute.For<IDataSources>();
            mockTraceAnalysis = Substitute.For<ITraceabilityAnalysis>();
            mockConfiguration = Substitute.For<IConfiguration>();
            mockConfiguration.OutputFormat.Returns("ASCIIDOC");
            mockSourceTraceEntity = new TraceEntity("TEST", "Test Entity", "TE", TraceEntityType.Truth);
            scriptingBridge = new ScriptingBridge(mockDataSources, mockTraceAnalysis, mockSourceTraceEntity, mockConfiguration);
        }

        #region EmbedAsciidocTables Tests

        [Test]
        [UnitTestAttribute(
            Identifier = "A5F2BD17-E5C6-4DB9-8B7E-56AC28CD9142",
            Purpose = "Test that basic table syntax with pipes is correctly transformed into embedded table syntax with exclamation marks",
            PostCondition = "Table delimiter and cell separators are converted from pipes to exclamation marks")]
        public void EmbedAsciidocTables_SimpleTableConversion()
        {
            // Arrange
            var input = "|===\n| Cell 1 | Cell 2\n| Cell 3 | Cell 4\n|===";
            var expected = "!===\n! Cell 1 ! Cell 2\n! Cell 3 ! Cell 4\n!===";

            // Act
            var result = scriptingBridge.EmbedAsciidocTables(input);

            // Assert
            ClassicAssert.AreEqual(expected, result);
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "B6D3CE08-2A71-4F19-9A45-8B7F1DA42E9F",
            Purpose = "Test that multiple tables in a single input are all correctly converted",
            PostCondition = "All tables in the input are properly converted while preserving non-table text")]
        public void EmbedAsciidocTables_MultipleTablesInText()
        {
            // Arrange
            var input = "Text before table\n|===\n| Table 1 Cell 1 | Table 1 Cell 2\n|===\nText between tables\n|===\n| Table 2 Cell 1 | Table 2 Cell 2\n|===\nText after table";
            var expected = "Text before table\n!===\n! Table 1 Cell 1 ! Table 1 Cell 2\n!===\nText between tables\n!===\n! Table 2 Cell 1 ! Table 2 Cell 2\n!===\nText after table";

            // Act
            var result = scriptingBridge.EmbedAsciidocTables(input);

            // Assert
            ClassicAssert.AreEqual(expected, result);
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "C7E4DF19-3B82-5E2A-AB56-9C8E2EB53F50",
            Purpose = "Test that escaped pipes within tables remain as escaped pipes",
            PostCondition = "Escaped pipes are preserved and not converted to exclamation marks")]
        public void EmbedAsciidocTables_EscapedPipesArePreserved()
        {
            // Arrange
            var input = "|===\n| Cell with \\| escaped pipe | Normal cell\n|===";
            var expected = "!===\n! Cell with \\| escaped pipe ! Normal cell\n!===";

            // Act
            var result = scriptingBridge.EmbedAsciidocTables(input);

            // Assert
            ClassicAssert.AreEqual(expected, result);
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "D8F5EG20-4C93-6F3B-BC67-AD9F3FC64G61",
            Purpose = "Test that leading whitespace before pipes in table cells is preserved",
            PostCondition = "Leading whitespace is maintained in the converted output")]
        public void EmbedAsciidocTables_LeadingWhitespaceIsPreserved()
        {
            // Arrange
            var input = "|===\n  | Cell with leading space | Normal cell\n|===";
            var expected = "!===\n  ! Cell with leading space ! Normal cell\n!===";

            // Act
            var result = scriptingBridge.EmbedAsciidocTables(input);

            // Assert
            ClassicAssert.AreEqual(expected, result);
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "E9G6FH21-5D04-7G4C-CD78-BE0G4GD75H72",
            Purpose = "Test that non-table content with pipes is not modified",
            PostCondition = "Text outside of table blocks remains unchanged even if it contains pipe characters")]
        public void EmbedAsciidocTables_NonTableContentUnchanged()
        {
            // Arrange
            var input = "Text with no tables\nJust plain text\nWith | some | pipes";

            // Act
            var result = scriptingBridge.EmbedAsciidocTables(input);

            // Assert
            ClassicAssert.AreEqual(input, result);
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "F10GHI22-6E15-8H5D-DE89-CF1H5HE86I83",
            Purpose = "Test that nested cell separators within tables are properly converted",
            PostCondition = "Nested cell separators are converted from pipes to exclamation marks")]
        public void EmbedAsciidocTables_NestedTableFormat()
        {
            // Arrange
            var input = "|===\n| Cell 1 | Cell 2 | Cell that contains |a pipe| character\n|===";
            var expected = "!===\n! Cell 1 ! Cell 2 ! Cell that contains !a pipe! character\n!===";

            // Act
            var result = scriptingBridge.EmbedAsciidocTables(input);

            // Assert
            ClassicAssert.AreEqual(expected, result);
        }

        #endregion

        #region AddTrace Tests

        [Test]
        [UnitTestAttribute(
            Identifier = "G21HIJ33-7F26-9I6E-EF90-DG2I6IF97J94",
            Purpose = "Test that AddTrace properly adds trace items to the traces collection",
            PostCondition = "The trace ID is added to the Traces collection")]
        public void AddTrace_SingleTrace_TraceAdded()
        {
            // Arrange
            string traceId = "TEST-123";

            // Act
            scriptingBridge.AddTrace(traceId);

            // Assert
            CollectionAssert.Contains(scriptingBridge.Traces, traceId);
        }

        [Test]
        [UnitTestAttribute(
        Identifier = "H32IJK44-8G37-0J7F-FG01-EH3J7JG08K05",
        Purpose = "Test that AddTrace adds the same trace ID multiple times when called repeatedly",
        PostCondition = "The trace ID appears multiple times in the Traces collection")]
        public void AddTrace_DuplicateTrace_AddedMultipleTimes()
        {
            // Arrange
            string traceId = "TEST-123";

            // Act
            scriptingBridge.AddTrace(traceId);
            scriptingBridge.AddTrace(traceId);

            // Assert
            ClassicAssert.AreEqual(2, scriptingBridge.Traces.Count(id => id == traceId));
        }

        #endregion

        #region GetLinkedItems Tests

        [Test]
        [UnitTestAttribute(
            Identifier = "I43JKL55-9H48-1K8G-GH12-FI4K8KH19L16",
            Purpose = "Test that GetLinkedItems retrieves linked items with the specified link type",
            PostCondition = "Only linked items with the specified link type are returned")]
        public void GetLinkedItems_WithSpecifiedLinkType_ReturnsCorrectItems()
        {
            // Arrange
            var linkedItem = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "REQ-123" };
            var parentItem = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "PARENT-123" };
            var childItem = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "CHILD-123" };
            var relatedItem = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "RELATED-123" };

            linkedItem.AddLinkedItem(new ItemLink(parentItem.ItemID, ItemLinkType.Parent));
            linkedItem.AddLinkedItem(new ItemLink(childItem.ItemID, ItemLinkType.Child));
            linkedItem.AddLinkedItem(new ItemLink(relatedItem.ItemID, ItemLinkType.Related));

            mockDataSources.GetItem(parentItem.ItemID).Returns(parentItem);
            mockDataSources.GetItem(childItem.ItemID).Returns(childItem);
            mockDataSources.GetItem(relatedItem.ItemID).Returns(relatedItem);

            // Act
            var result = scriptingBridge.GetLinkedItems(linkedItem, ItemLinkType.Parent);

            // Assert
            ClassicAssert.AreEqual(1, result.Count());
            ClassicAssert.AreEqual(parentItem.ItemID, result.First().ItemID);
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "J54KLM66-0I59-2L9H-HI23-GJ5L9LI20M27",
            Purpose = "Test that GetLinkedItems returns empty collection when no items with the specified link type exist",
            PostCondition = "An empty collection is returned when no matching linked items exist")]
        public void GetLinkedItems_NoMatchingLinkType_ReturnsEmptyCollection()
        {
            // Arrange
            var linkedItem = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "REQ-123" };
            var parentItem = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "PARENT-123" };

            linkedItem.AddLinkedItem(new ItemLink(parentItem.ItemID, ItemLinkType.Parent));

            mockDataSources.GetItem(parentItem.ItemID).Returns(parentItem);

            // Act
            var result = scriptingBridge.GetLinkedItems(linkedItem, ItemLinkType.Child);

            // Assert
            ClassicAssert.IsEmpty(result);
        }

        #endregion

        #region GetLinkedField Tests

        [Test]
        [UnitTestAttribute(
            Identifier = "K65LMN77-1J60-3M0I-IJ34-HK6M0MJ31N38",
            Purpose = "Test that GetLinkedField formats linked items correctly with titles",
            PostCondition = "A properly formatted string with links and titles is returned")]
        public void GetLinkedField_WithTitles_FormatsCorrectly()
        {
            // Arrange
            var linkedItem = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "REQ-123" };
            var parentItem = new RequirementItem(RequirementType.SystemRequirement)
            {
                ItemID = "PARENT-123",
                ItemTitle = "Parent Requirement",
                Link = new Uri("http://example.com/PARENT-123")
            };

            linkedItem.AddLinkedItem(new ItemLink(parentItem.ItemID, ItemLinkType.Parent));

            mockDataSources.GetItem(parentItem.ItemID).Returns(parentItem);

            // Act
            var result = scriptingBridge.GetLinkedField(linkedItem, ItemLinkType.Parent);

            // Assert
            ClassicAssert.AreEqual("http://example.com/PARENT-123[PARENT-123]: \"Parent Requirement\"", result);
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "L76MNO88-2K71-4N1J-JK45-IL7N1NK42O49",
            Purpose = "Test that GetLinkedField formats linked items correctly without titles",
            PostCondition = "A properly formatted string with links but no titles is returned")]
        public void GetLinkedField_WithoutTitles_FormatsCorrectly()
        {
            // Arrange
            var linkedItem = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "REQ-123" };
            var parentItem = new RequirementItem(RequirementType.SystemRequirement)
            {
                ItemID = "PARENT-123",
                ItemTitle = "Parent Requirement",
                Link = new Uri("http://example.com/PARENT-123")
            };

            linkedItem.AddLinkedItem(new ItemLink(parentItem.ItemID, ItemLinkType.Parent));

            mockDataSources.GetItem(parentItem.ItemID).Returns(parentItem);

            // Act
            var result = scriptingBridge.GetLinkedField(linkedItem, ItemLinkType.Parent, false);

            // Assert
            ClassicAssert.AreEqual("http://example.com/PARENT-123[PARENT-123]", result);
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "M87NOP99-3L82-5O2K-KL56-JM8O2OL53P50",
            Purpose = "Test that GetLinkedField handles multiple linked items correctly",
            PostCondition = "A properly formatted string with multiple linked items is returned")]
        public void GetLinkedField_MultipleItems_FormatsCorrectly()
        {
            // Arrange
            var linkedItem = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "REQ-123" };
            var relatedItem1 = new RequirementItem(RequirementType.SystemRequirement)
            {
                ItemID = "RELATED-123",
                ItemTitle = "Related Requirement 1",
                Link = new Uri("http://example.com/RELATED-123")
            };
            var relatedItem2 = new RequirementItem(RequirementType.SystemRequirement)
            {
                ItemID = "RELATED-456",
                ItemTitle = "Related Requirement 2",
                Link = new Uri("http://example.com/RELATED-456")
            };

            linkedItem.AddLinkedItem(new ItemLink(relatedItem1.ItemID, ItemLinkType.Related));
            linkedItem.AddLinkedItem(new ItemLink(relatedItem2.ItemID, ItemLinkType.Related));

            mockDataSources.GetItem(relatedItem1.ItemID).Returns(relatedItem1);
            mockDataSources.GetItem(relatedItem2.ItemID).Returns(relatedItem2);

            // Act
            var result = scriptingBridge.GetLinkedField(linkedItem, ItemLinkType.Related);

            // Assert
            ClassicAssert.IsTrue(result.Contains("http://example.com/RELATED-123[RELATED-123]: \"Related Requirement 1\""));
            ClassicAssert.IsTrue(result.Contains("http://example.com/RELATED-456[RELATED-456]: \"Related Requirement 2\""));
            ClassicAssert.IsTrue(result.Contains(" / ")); // Separator between items
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "N98OPQ00-4M93-6P3L-LM67-KN9P3PM64Q61",
            Purpose = "Test that GetLinkedField returns 'N/A' when no linked items of the specified type exist",
            PostCondition = "'N/A' is returned when no matching linked items exist")]
        public void GetLinkedField_NoMatchingItems_ReturnsNA()
        {
            // Arrange
            var linkedItem = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "REQ-123" };

            // Act
            var result = scriptingBridge.GetLinkedField(linkedItem, ItemLinkType.Related);

            // Assert
            ClassicAssert.AreEqual("N/A", result);
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "O09PQR11-5N04-7Q4M-MN78-LO0Q4QN75R72",
            Purpose = "Test that GetLinkedField throws exception when linked item does not exist",
            PostCondition = "An exception is thrown when a linked item is not found")]
        public void GetLinkedField_LinkedItemNotFound_ThrowsException()
        {
            // Arrange
            var linkedItem = new RequirementItem(RequirementType.SystemRequirement) { ItemID = "REQ-123" };
            linkedItem.AddLinkedItem(new ItemLink("NONEXISTENT-ID", ItemLinkType.Related));

            mockDataSources.GetItem("NONEXISTENT-ID").Returns((Item)null);

            // Act & Assert
            ClassicAssert.Throws<Exception>(() => scriptingBridge.GetLinkedField(linkedItem, ItemLinkType.Related));
        }

        #endregion

        #region GetItemLinkString Tests

        [Test]
        [UnitTestAttribute(
            Identifier = "P10QRS22-6O15-8R5N-NO89-MP1R5RO86S83",
            Purpose = "Test that GetItemLinkString formats item with link correctly",
            PostCondition = "A properly formatted link string is returned for items with links")]
        public void GetItemLinkString_ItemWithLink_FormatsCorrectly()
        {
            // Arrange
            var item = new RequirementItem(RequirementType.SystemRequirement)
            {
                ItemID = "REQ-123",
                Link = new Uri("http://example.com/REQ-123")
            };

            // Act
            var result = scriptingBridge.GetItemLinkString(item);

            // Assert
            ClassicAssert.AreEqual("http://example.com/REQ-123[REQ-123]", result);
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "Q21RST33-7P26-9S6O-OP90-NQ2S6SP97T94",
            Purpose = "Test that GetItemLinkString formats item without link correctly",
            PostCondition = "Just the item ID is returned for items without links")]
        public void GetItemLinkString_ItemWithoutLink_FormatsCorrectly()
        {
            // Arrange
            var item = new RequirementItem(RequirementType.SystemRequirement)
            {
                ItemID = "REQ-123"
            };

            // Act
            var result = scriptingBridge.GetItemLinkString(item);

            // Assert
            ClassicAssert.AreEqual("REQ-123", result);
        }

        #endregion

        #region GetValOrDef Tests

        [Test]
        [UnitTestAttribute(
            Identifier = "R32STU44-8Q37-0T7P-PQ01-OR3T7TQ08U05",
            Purpose = "Test that GetValOrDef returns value when not empty",
            PostCondition = "The original value is returned when it is not empty")]
        public void GetValOrDef_NonEmptyValue_ReturnsValue()
        {
            // Arrange
            string value = "TestValue";
            string defaultValue = "DefaultValue";

            // Act
            var result = scriptingBridge.GetValOrDef(value, defaultValue);

            // Assert
            ClassicAssert.AreEqual(value, result);
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "S43TUV55-9R48-1U8Q-QR12-PS4U8UR19V16",
            Purpose = "Test that GetValOrDef returns default value when value is empty",
            PostCondition = "The default value is returned when the original value is empty")]
        public void GetValOrDef_EmptyValue_ReturnsDefaultValue()
        {
            // Arrange
            string value = string.Empty;
            string defaultValue = "DefaultValue";

            // Act
            var result = scriptingBridge.GetValOrDef(value, defaultValue);

            // Assert
            ClassicAssert.AreEqual(defaultValue, result);
        }

        #endregion

        #region Insert Tests

        [Test]
        [UnitTestAttribute(
            Identifier = "T54UVW66-0S59-2V9R-RS23-QT5V9VS20W27",
            Purpose = "Test that Insert converts input to string",
            PostCondition = "The input object is properly converted to a string")]
        public void Insert_Object_ConvertsToString()
        {
            // Arrange
            object input = 123;

            // Act
            var result = scriptingBridge.Insert(input);

            // Assert
            ClassicAssert.AreEqual("123", result);
        }

        [Test]
        [UnitTestAttribute(
            Identifier = "U65VWX77-1T60-3W0S-ST34-RU6W0WT31X38",
            Purpose = "Test that Insert handles complex objects correctly",
            PostCondition = "Complex objects are properly converted to their string representation")]
        public void Insert_ComplexObject_ConvertsToString()
        {
            // Arrange
            var input = new DateTime(2023, 1, 1);

            // Act
            var result = scriptingBridge.Insert(input);

            // Assert
            ClassicAssert.AreEqual(input.ToString(), result);
        }

        [UnitTestAttribute(
        Identifier = "CF1A2D78-E341-4B9C-8B6D-F0D3A12E5B97",
        Purpose = "Test conversion of level 1 and 2 headings to bold text",
        PostCondition = "Headings are converted to bold text")]
        [Test]
        public void TestHeadingConversion_Level1And2()
        {
            // Level 1 heading
            string input = "= Document Title\nSome content";
            string expected = "*Document Title*\n\nSome content";
            string result = scriptingBridge.ConvertHeadingsForASCIIDOCTableCell(input);
            ClassicAssert.AreEqual(expected, result);

            // Level 2 heading
            input = "== Section Title\nSome content";
            expected = "*Section Title*\n\nSome content";
            result = scriptingBridge.ConvertHeadingsForASCIIDOCTableCell(input);
            ClassicAssert.AreEqual(expected, result);
        }

        [UnitTestAttribute(
        Identifier = "B5D34F12-8A1E-47EA-A72C-9D4F5BE31C89",
        Purpose = "Test conversion of level 3 headings to italic text",
        PostCondition = "Headings are converted to italic text")]
        [Test]
        public void TestHeadingConversion_Level3()
        {
            string input = "=== Subsection Title\nSome content";
            string expected = "_Subsection Title_\n\nSome content";
            string result = scriptingBridge.ConvertHeadingsForASCIIDOCTableCell(input);
            ClassicAssert.AreEqual(expected, result);
        }

        [UnitTestAttribute(
        Identifier = "3E29A867-F6CB-4D91-B8D5-D2E39F8A61C0",
        Purpose = "Test conversion of level 4 headings to indented bold text",
        PostCondition = "Headings are converted to indented bold text")]
        [Test]
        public void TestHeadingConversion_Level4()
        {
            string input = "==== Subsubsection Title\nSome content";
            string expected = "&#160;&#160; _Subsubsection Title_\n\nSome content";
            string result = scriptingBridge.ConvertHeadingsForASCIIDOCTableCell(input);
            ClassicAssert.AreEqual(expected, result);
        }

        [UnitTestAttribute(
        Identifier = "7F8E6D90-1C53-4A9B-BF92-8D15E47CAF2A",
        Purpose = "Test conversion of level 5 and 6 headings to indented monospace text",
        PostCondition = "Headings are converted to indented monospace text")]
        [Test]
        public void TestHeadingConversion_Level5And6()
        {
            // Level 5 heading
            string input = "===== Deep Level Title\nSome content";
            string expected = "&#160;&#160;&#160;&#160; `Deep Level Title`\n\nSome content";
            string result = scriptingBridge.ConvertHeadingsForASCIIDOCTableCell(input);
            ClassicAssert.AreEqual(expected, result);

            // Level 6 heading
            input = "====== Deeper Level Title\nSome content";
            expected = "&#160;&#160;&#160;&#160; `Deeper Level Title`\n\nSome content";
            result = scriptingBridge.ConvertHeadingsForASCIIDOCTableCell(input);
            ClassicAssert.AreEqual(expected, result);
        }

        [UnitTestAttribute(
        Identifier = "9D187B33-A42E-4DF7-9E91-F5C72A4EB5D3",
        Purpose = "Test conversion of multiple headings in a single text",
        PostCondition = "All headings are correctly converted")]
        [Test]
        public void TestHeadingConversion_MultipleHeadings()
        {
            string input = "== Main Section\nSome text here\n=== Subsection\nMore text\n==== Sub-subsection\nEven more text";
            string expected = "*Main Section*\n\nSome text here\n_Subsection_\n\nMore text\n&#160;&#160; _Sub-subsection_\n\nEven more text";
            string result = scriptingBridge.ConvertHeadingsForASCIIDOCTableCell(input);
            ClassicAssert.AreEqual(expected, result);
        }

        [UnitTestAttribute(
        Identifier = "0A4E7D1C-B5FB-4E28-9B2C-D83E79F6A452",
        Purpose = "Test handling of null and empty input",
        PostCondition = "Null and empty inputs are handled gracefully")]
        [Test]
        public void TestHeadingConversion_NullAndEmpty()
        {
            // Test null input
            string result = scriptingBridge.ConvertHeadingsForASCIIDOCTableCell(null);
            ClassicAssert.IsNull(result);

            // Test empty input
            result = scriptingBridge.ConvertHeadingsForASCIIDOCTableCell(string.Empty);
            ClassicAssert.AreEqual(string.Empty, result);
        }

        [UnitTestAttribute(
        Identifier = "6FC8AB5D-3E94-4C1A-BDC3-E2D47F5A1E89",
        Purpose = "Test that non-heading content is unaffected",
        PostCondition = "Regular text remains unchanged")]
        [Test]
        public void TestHeadingConversion_NonHeadingContent()
        {
            string input = "This is regular text\nNo headings here\n* A list item\n* Another list item";
            string result = scriptingBridge.ConvertHeadingsForASCIIDOCTableCell(input);
            ClassicAssert.AreEqual(input, result);
        }

        [UnitTestAttribute(
        Identifier = "D12E7B90-5A67-4F8E-BC32-A9E15D76F4C3",
        Purpose = "Test that heading followed by blank line works correctly",
        PostCondition = "No additional blank line is added if one already exists")]
        [Test]
        public void TestHeadingConversion_HeadingWithBlankLine()
        {
            string input = "== Section Title\n\nSome content after a blank line";
            string expected = "*Section Title*\n\nSome content after a blank line";
            string result = scriptingBridge.ConvertHeadingsForASCIIDOCTableCell(input);
            ClassicAssert.AreEqual(expected, result);
        }

        #endregion
    }
}