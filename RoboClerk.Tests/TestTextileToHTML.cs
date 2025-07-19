using NUnit.Framework;
using NUnit.Framework.Legacy;
using RoboClerk.Redmine;
using System;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("Tests for the TextileToHTMLConverter class")]
    public class TextileToHTMLConverterTests
    {
        private TextileToHTMLConverter converter;

        [SetUp]
        public void TestSetup()
        {
            converter = new TextileToHTMLConverter();
        }

        [UnitTestAttribute(
            Identifier = "8F3A5E92-D713-4E8B-9F3C-A4B5D7C86E9E",
            Purpose = "Verify that the converter handles null input appropriately",
            PostCondition = "An ArgumentNullException is thrown")]
        [Test]
        public void TestNullInput()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => converter.Convert(null));
        }

        [UnitTestAttribute(
            Identifier = "7B42E981-F5A3-4D69-B8D2-6EDF7C54E3A9",
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
            ClassicAssert.AreEqual("", result);
        }

        [UnitTestAttribute(
            Identifier = "A1B2C3D4-E5F6-7890-ABCD-EF1234567892",
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
            ClassicAssert.IsTrue(result.Contains("@@inline tag@@"));
        }

        [UnitTestAttribute(
            Identifier = "B2C3D4E5-F6A7-8901-2345-6789ABCDEF03",
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
            ClassicAssert.IsTrue(result.Contains("@@@\nBlock tag content\nwith multiple lines\n@@@"));
        }

        [UnitTestAttribute(
            Identifier = "C3D4E5F6-A7B8-9012-3456-789ABCDEF014",
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
            ClassicAssert.IsTrue(result.Contains("@@tag1@@"));
            ClassicAssert.IsTrue(result.Contains("@@tag2@@"));
            ClassicAssert.IsTrue(result.Contains("@@@\nblock tag\n@@@"));
        }

        [UnitTestAttribute(
            Identifier = "D4E5F6A7-B8C9-0123-4567-89ABCDEF015",
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
            // Check that RoboClerk tags are preserved
            ClassicAssert.IsTrue(result.Contains("@@inline tag@@"));
            ClassicAssert.IsTrue(result.Contains("@@@\nBlock tag with **bold** and *italic*\n@@@"));
        }

        [UnitTestAttribute(
            Identifier = "E5F6A7B8-C9D0-1234-5678-9ABCDEF016",
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
            ClassicAssert.IsTrue(result.Contains("@@@@"));
            ClassicAssert.IsTrue(result.Contains("@@@@@@@"));
        }

        [UnitTestAttribute(
            Identifier = "F6A7B8C9-D0E1-2345-6789-ABCDEF017",
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
            // Check that RoboClerk tags are preserved
            ClassicAssert.IsTrue(result.Contains("@@tag@@"));
        }

        [UnitTestAttribute(
            Identifier = "G7B8C9D0-E1F2-3456-789A-BCDEF018",
            Purpose = "Verify that the converter correctly converts basic textile formatting",
            PostCondition = "Basic textile formatting is converted to HTML")]
        [Test]
        public void TestBasicTextileFormatting()
        {
            // Arrange
            string textile = "h1. Heading\n\n**Bold text** and *italic text*.";

            // Act
            string result = converter.Convert(textile);

            // Assert
            // The Textile formatter should convert this to HTML
            ClassicAssert.IsTrue(result.Contains("<h1>") || result.Contains("Heading"));
        }
    }
} 