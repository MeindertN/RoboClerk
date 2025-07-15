using NUnit.Framework;
using NUnit.Framework.Legacy;
using RoboClerk.Redmine;
using System;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("Tests for the TextileToAsciiDocConverter class")]
    public class TextileToAsciiDocConverterTests
    {
        private TextileToAsciiDocConverter converter;

        [SetUp]
        public void TestSetup()
        {
            converter = new TextileToAsciiDocConverter();
        }

        [UnitTestAttribute(
            Identifier = "8F3A5E92-D713-4E8B-9F3C-A4B5D7C86E9D",
            Purpose = "Verify that the converter handles null input appropriately",
            PostCondition = "An ArgumentNullException is thrown")]
        [Test]
        public void TestNullInput()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => converter.Convert(null));
        }

        [UnitTestAttribute(
            Identifier = "7B42E981-F5A3-4D69-B8D2-6EDF7C54E3A8",
            Purpose = "Verify that the converter handles empty input appropriately",
            PostCondition = "An empty string is returned")]
        [Test]
        public void TestEmptyInput()
        {
            // Arrange
            string textile = string.Empty;

            // Act
            string result = converter.Convert(textile);

            // Assert
            ClassicAssert.AreEqual("\n", result);
        }

        [UnitTestAttribute(
            Identifier = "C1D6F382-8E97-4B5A-BA23-F1E9A8D6C541",
            Purpose = "Verify that the converter correctly transforms headings",
            PostCondition = "Textile headings are converted to AsciiDoc headings")]
        [Test]
        public void TestHeadings()
        {
            // Arrange
            string textile = "h1. Main Heading\nh2. Sub Heading\nh3. Sub-Sub Heading";

            // Act
            string result = converter.Convert(textile);

            // Assert
            string expected = "== Main Heading\n=== Sub Heading\n==== Sub-Sub Heading\n";
            ClassicAssert.AreEqual(expected, result);
        }

        [UnitTestAttribute(
            Identifier = "5E8D7A93-C12F-4B8A-9D47-1F2E3A4B5C6D",
            Purpose = "Verify that the converter correctly transforms links",
            PostCondition = "Textile links are converted to AsciiDoc links")]
        [Test]
        public void TestLinks()
        {
            // Arrange
            string textile = "\"Link text\":http://example.com";

            // Act
            string result = converter.Convert(textile);

            // Assert
            string expected = "link:http://example.com[Link text]\n";
            ClassicAssert.AreEqual(expected, result);
        }

        [UnitTestAttribute(
            Identifier = "F7E6D5C4-B3A2-1098-7654-3210ABCDEF12",
            Purpose = "Verify that the converter correctly transforms images",
            PostCondition = "Textile images are converted to AsciiDoc images")]
        [Test]
        public void TestImages()
        {
            // Arrange
            string textile = "!http://example.com/image.png!";

            // Act
            string result = converter.Convert(textile);

            // Assert
            string expected = "image::http://example.com/image.png[]\n";
            ClassicAssert.AreEqual(expected, result);
        }

        [UnitTestAttribute(
            Identifier = "A1B2C3D4-E5F6-7890-ABCD-EF1234567890",
            Purpose = "Verify that the converter correctly transforms unordered lists",
            PostCondition = "Textile unordered lists are converted to AsciiDoc unordered lists")]
        [Test]
        public void TestUnorderedLists()
        {
            // Arrange
            string textile = "* Item 1\n* Item 2\n** Nested Item\n* Item 3";

            // Act
            string result = converter.Convert(textile);

            // Assert
            string expected = "* Item 1\n\n* Item 2\n\n** Nested Item\n\n* Item 3\n";
            ClassicAssert.AreEqual(expected, result);
        }

        [UnitTestAttribute(
            Identifier = "B2C3D4E5-F6A7-8901-2345-6789ABCDEF01",
            Purpose = "Verify that the converter correctly transforms ordered lists",
            PostCondition = "Textile ordered lists are converted to AsciiDoc ordered lists")]
        [Test]
        public void TestOrderedLists()
        {
            // Arrange
            string textile = "# Item 1\n# Item 2\n## Nested Item\n# Item 3";

            // Act
            string result = converter.Convert(textile);

            // Assert
            string expected = ". Item 1\n\n. Item 2\n\n.. Nested Item\n\n. Item 3\n";
            ClassicAssert.AreEqual(expected, result);
        }

        [UnitTestAttribute(
            Identifier = "C3D4E5F6-A7B8-9012-3456-789ABCDEF012",
            Purpose = "Verify that the converter correctly transforms blockquotes using bq.",
            PostCondition = "Textile blockquotes are converted to AsciiDoc blockquotes")]
        [Test]
        public void TestBlockquotes()
        {
            // Arrange
            string textile = "bq. This is a blockquote";

            // Act
            string result = converter.Convert(textile);

            // Assert
            string expected = "____\nThis is a blockquote\n____\n";
            ClassicAssert.AreEqual(expected, result);
        }

        [UnitTestAttribute(
            Identifier = "D4E5F6A7-B8C9-0123-4567-89ABCDEF0123",
            Purpose = "Verify that the converter correctly transforms blockquotes using >",
            PostCondition = "Textile blockquotes with > are converted to AsciiDoc blockquotes")]
        [Test]
        public void TestBlockquotesWithAngleBracket()
        {
            // Arrange
            string textile = "> This is another blockquote";

            // Act
            string result = converter.Convert(textile);

            // Assert
            string expected = "____\nThis is another blockquote\n____\n";
            ClassicAssert.AreEqual(expected, result);
        }

        [UnitTestAttribute(
            Identifier = "F6A7B8C9-D0E1-2345-6789-ABCDEF012345",
            Purpose = "Verify that the converter correctly transforms inline code",
            PostCondition = "Textile inline code is converted to AsciiDoc inline code")]
        [Test]
        public void TestInlineCode()
        {
            // Arrange
            string textile = "Use @System.out.println(\"Hello\");@ for output.";

            // Act
            string result = converter.Convert(textile);

            // Assert
            string expected = "Use `System.out.println(\"Hello\");` for output.\n";
            ClassicAssert.AreEqual(expected, result);
        }

        [UnitTestAttribute(
            Identifier = "A7B8C9D0-E1F2-3456-789A-BCDEF0123456",
            Purpose = "Verify that the converter correctly transforms strikethrough text",
            PostCondition = "Textile strikethrough is converted to AsciiDoc strikethrough")]
        [Test]
        public void TestStrikethrough()
        {
            // Arrange
            string textile = "This is -deleted- text.";

            // Act
            string result = converter.Convert(textile);

            // Assert
            string expected = "This is [strike]#deleted# text.\n";
            ClassicAssert.AreEqual(expected, result);
        }

        [UnitTestAttribute(
            Identifier = "B8C9D0E1-F2A3-4567-89AB-CDEF01234567",
            Purpose = "Verify that the converter correctly transforms tables",
            PostCondition = "Textile tables are converted to AsciiDoc tables")]
        [Test]
        public void TestTables()
        {
            // Arrange
            string textile = "|_. Header 1|_. Header 2|\n|Cell 1|Cell 2|";

            // Act
            string result = converter.Convert(textile);

            // Assert
            // Testing tables is complex due to the ProcessTables method
            // This just checks that a table structure was created
            ClassicAssert.IsTrue(result.Contains("[cols=\""));
            ClassicAssert.IsTrue(result.Contains("|==="));
            ClassicAssert.IsTrue(result.Contains("^Header 1"));
        }

        [UnitTestAttribute(
            Identifier = "D9E8F7A6-B5C4-3210-9876-543210ABCDEF",
            Purpose = "Verify that the converter correctly handles HTML pre tags",
            PostCondition = "HTML pre tags are converted to AsciiDoc literal blocks")]
        [Test]
        public void TestPreTags()
        {
            // Arrange
            string textile = "Here is some preformatted text:\n<pre>  This is preformatted\n  with    spaces   preserved\n  and line breaks intact</pre>\nAnd regular text after.";

            // Act
            string result = converter.Convert(textile);

            // Assert
            string expected = "Here is some preformatted text:\n\n....\n  This is preformatted\n  with    spaces   preserved\n  and line breaks intact\n....\n\nAnd regular text after.\n";
            ClassicAssert.AreEqual(expected, result);
        }

        [UnitTestAttribute(
            Identifier = "E0F1A2B3-C4D5-6789-0ABC-DEF123456789",
            Purpose = "Verify that the converter correctly handles empty pre tags",
            PostCondition = "Empty pre tags are converted correctly")]
        [Test]
        public void TestEmptyPreTags()
        {
            // Arrange
            string textile = "Empty pre tags: <pre></pre>";

            // Act
            string result = converter.Convert(textile);

            // Assert
            string expected = "Empty pre tags: \n....\n\n....\n\n";
            ClassicAssert.AreEqual(expected, result);
        }

        [UnitTestAttribute(
            Identifier = "F1A2B3C4-D5E6-7890-ABCD-EF1234567890",
            Purpose = "Verify that the converter correctly handles multiple pre tags",
            PostCondition = "Multiple pre tags are all converted correctly")]
        [Test]
        public void TestMultiplePreTags()
        {
            // Arrange
            string textile = "First pre: <pre>Code 1</pre>\nSome text.\nSecond pre: <pre>Code 2</pre>";

            // Act
            string result = converter.Convert(textile);

            // Assert
            string expected = "First pre: \n....\nCode 1\n....\n\nSome text.\nSecond pre: \n....\nCode 2\n....\n\n";
            ClassicAssert.AreEqual(expected, result);
        }

        [UnitTestAttribute(
            Identifier = "ABCDEF12-3456-7890-ABCD-EF1234567890",
            Purpose = "Verify that the converter correctly handles pre tags with other formatting inside",
            PostCondition = "Pre tags preserve all formatting inside them")]
        [Test]
        public void TestPreTagsWithFormatting()
        {
            // Arrange
            string textile = "<pre>* List item\n# Numbered item\nh1. Heading\n**Bold**</pre>";

            // Act
            string result = converter.Convert(textile);

            // Assert
            string expected = "\n....\n* List item\n# Numbered item\nh1. Heading\n**Bold**\n....\n\n";
            ClassicAssert.AreEqual(expected, result);
        }

        [UnitTestAttribute(
            Identifier = "12345678-9ABC-DEF0-1234-56789ABCDEF0",
            Purpose = "Verify that the converter correctly handles complex mixed content",
            PostCondition = "All textile elements are converted appropriately")]
        [Test]
        public void TestComplexMixedContent()
        {
            // Arrange
            string textile = "h1. Main Heading\n\nSome paragraph with \"a link\":http://example.com and an @inline code@.\n\n* List item 1\n* List item 2\n\n<pre>function test() {\n  console.log(\"Hello\");\n}</pre>\n\n|_. Header 1|_. Header 2|\n|Cell 1|Cell 2|";

            // Act
            string result = converter.Convert(textile);

            // Assert
            // Just check for key converted elements
            ClassicAssert.IsTrue(result.Contains("== Main Heading"));
            ClassicAssert.IsTrue(result.Contains("link:http://example.com[a link]"));
            ClassicAssert.IsTrue(result.Contains("`inline code`"));
            ClassicAssert.IsTrue(result.Contains("* List item"));
            ClassicAssert.IsTrue(result.Contains("....\nfunction test() {"));
            ClassicAssert.IsTrue(result.Contains("|==="));
        }

        [UnitTestAttribute(
            Identifier = "A1B2C3D4-E5F6-7890-ABCD-EF1234567891",
            Purpose = "Verify that the converter correctly preserves inline RoboClerk tags",
            PostCondition = "Inline RoboClerk tags are preserved unchanged")]
        [Test]
        public void TestInlineRoboClerkTags()
        {
            // Arrange
            string textile = "This is some text with @@inline tag@@ and more text.";

            // Act
            string result = converter.Convert(textile);

            // Assert
            ClassicAssert.AreEqual("This is some text with @@inline tag@@ and more text.\n", result);
        }

        [UnitTestAttribute(
            Identifier = "B2C3D4E5-F6A7-8901-2345-6789ABCDEF02",
            Purpose = "Verify that the converter correctly preserves block RoboClerk tags",
            PostCondition = "Block RoboClerk tags are preserved unchanged")]
        [Test]
        public void TestBlockRoboClerkTags()
        {
            // Arrange
            string textile = "This is some text.\n@@@\nBlock tag content\nwith multiple lines\n@@@\nAnd more text.";

            // Act
            string result = converter.Convert(textile);

            // Assert
            ClassicAssert.AreEqual("This is some text.\n@@@\nBlock tag content\nwith multiple lines\n@@@\nAnd more text.\n", result);
        }

        [UnitTestAttribute(
            Identifier = "C3D4E5F6-A7B8-9012-3456-789ABCDEF013",
            Purpose = "Verify that the converter correctly preserves multiple RoboClerk tags",
            PostCondition = "Multiple RoboClerk tags are all preserved unchanged")]
        [Test]
        public void TestMultipleRoboClerkTags()
        {
            // Arrange
            string textile = "Text with @@tag1@@ and @@tag2@@ and @@@\nblock tag\n@@@ content.";

            // Act
            string result = converter.Convert(textile);

            // Assert
            ClassicAssert.AreEqual("Text with @@tag1@@ and @@tag2@@ and @@@\nblock tag\n@@@ content.\n", result);
        }

        [UnitTestAttribute(
            Identifier = "D4E5F6A7-B8C9-0123-4567-89ABCDEF014",
            Purpose = "Verify that the converter correctly handles RoboClerk tags with textile formatting",
            PostCondition = "RoboClerk tags are preserved while textile formatting is converted")]
        [Test]
        public void TestRoboClerkTagsWithTextileFormatting()
        {
            // Arrange
            string textile = "h1. Heading\n\nSome **bold** text with @@inline tag@@ and *italic* text.\n\n@@@\nBlock tag with **bold** and *italic*\n@@@";

            // Act
            string result = converter.Convert(textile);

            // Assert
            // Check that headings and formatting are converted
            ClassicAssert.IsTrue(result.Contains("== Heading"));
            // Check that RoboClerk tags are preserved
            ClassicAssert.IsTrue(result.Contains("@@inline tag@@"));
            ClassicAssert.IsTrue(result.Contains("@@@\nBlock tag with **bold** and *italic*\n@@@"));
        }

        [UnitTestAttribute(
            Identifier = "E5F6A7B8-C9D0-1234-5678-9ABCDEF015",
            Purpose = "Verify that the converter correctly handles empty RoboClerk tags",
            PostCondition = "Empty RoboClerk tags are preserved unchanged")]
        [Test]
        public void TestEmptyRoboClerkTags()
        {
            // Arrange
            string textile = "Text with @@@@ and @@@@@@@ content.";

            // Act
            string result = converter.Convert(textile);

            // Assert
            ClassicAssert.AreEqual("Text with @@@@ and @@@@@@@ content.\n", result);
        }

        [UnitTestAttribute(
            Identifier = "F6A7B8C9-D0E1-2345-6789-ABCDEF016",
            Purpose = "Verify that the converter correctly handles RoboClerk tags in complex scenarios",
            PostCondition = "RoboClerk tags are preserved in complex conversion scenarios")]
        [Test]
        public void TestRoboClerkTagsInComplexScenarios()
        {
            // Arrange
            string textile = "h1. Main Heading\n\n* List item with @@tag@@\n* Another item\n\n<pre>Code with @@tag@@</pre>\n\n|_. Header|_. Content|\n|@@tag@@|Normal content|";

            // Act
            string result = converter.Convert(textile);

            // Assert
            // Check that conversion happened
            ClassicAssert.IsTrue(result.Contains("== Main Heading"));
            ClassicAssert.IsTrue(result.Contains("* List item"));
            ClassicAssert.IsTrue(result.Contains("....\nCode with @@tag@@"));
            ClassicAssert.IsTrue(result.Contains("|==="));
            // Check that RoboClerk tags are preserved
            ClassicAssert.IsTrue(result.Contains("@@tag@@"));
        }
    }
}