using NUnit.Framework;
using RoboClerk;
using System.Text.RegularExpressions;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("These tests are for work item 9")]
    public class TestRoboClerkMarkdown
    {
        private string validText = @"This is a line of text.
@@@TheFirstInfo:SLMS
This is the content
@@@
another line of text
@@some other stuff(testinfo:Source)@@ @@some other stuff2(testinfo2:Config)@@
@@@empty:OTS
@@@
@@@huff:Info
this is some contents
it is even multiline
# it contains a *header*
@@@
There is some text @@inlinec(inline:Foo)@@ in this line.
@@M(SR:Trace)@@
 ";

        [SetUp]
        public void TestSetup()
        {
            validText = Regex.Replace(validText, @"\r\n", "\n");
        }

        [Test]
        public void Valid_Text_Can_Be_Parsed()
        {
            Assert.DoesNotThrow( () => { RoboClerkMarkdown.ExtractRoboClerkTags(validText); });
        }

        [Test]
        public void The_Correct_Nr_Of_Tags_Is_Extracted()
        {
            var tags = RoboClerkMarkdown.ExtractRoboClerkTags(validText);
            Assert.AreEqual(7, tags.Count);
        }

        [Test]
        public void The_Correct_Content_Fields_Are_Extracted()
        {
            var tags = RoboClerkMarkdown.ExtractRoboClerkTags(validText);
            Assert.AreEqual("This is the content\n", tags[0].Contents);
            Assert.AreEqual("some other stuff", tags[1].Contents);
            Assert.AreEqual("some other stuff2", tags[2].Contents);
            Assert.AreEqual("", tags[3].Contents);
            Assert.AreEqual("this is some contents\nit is even multiline\n# it contains a *header*\n", tags[4].Contents);
            Assert.AreEqual("inlinec", tags[5].Contents);
            Assert.AreEqual("M", tags[6].Contents);
        }

        [Test]
        public void The_Correct_Info_Fields_Are_Extracted()
        {
            var tags = RoboClerkMarkdown.ExtractRoboClerkTags(validText);
            Assert.AreEqual("TheFirstInfo", tags[0].ID);
            Assert.AreEqual("testinfo", tags[1].ID);
            Assert.AreEqual("testinfo2", tags[2].ID);
            Assert.AreEqual("empty", tags[3].ID);
            Assert.AreEqual("huff", tags[4].ID);
            Assert.AreEqual("inline", tags[5].ID);
            Assert.AreEqual("SR", tags[6].ID);
        }

        [Test]
        public void The_Correct_Source_Fields_Are_Extracted()
        {
            var tags = RoboClerkMarkdown.ExtractRoboClerkTags(validText);
            Assert.AreEqual(DataSource.SLMS, tags[0].Source);
            Assert.AreEqual(DataSource.Source, tags[1].Source);
            Assert.AreEqual(DataSource.Config, tags[2].Source);
            Assert.AreEqual(DataSource.OTS, tags[3].Source);
            Assert.AreEqual(DataSource.Info, tags[4].Source);
            Assert.AreEqual(DataSource.Unknown, tags[5].Source);
            Assert.AreEqual(DataSource.Trace, tags[6].Source);
        }

        [Test]
        public void Content_Is_Inserted_In_The_Correct_Place()
        {
            var tags = RoboClerkMarkdown.ExtractRoboClerkTags(validText);
            tags[0].Contents = "item1";
            tags[1].Contents = "item2";
            tags[2].Contents = "";
            tags[3].Contents = "item4";
            tags[4].Contents = "";
            tags[5].Contents = "item6";
            tags[6].Contents = "D";

            string expectedResult = @"This is a line of text.
@@@TheFirstInfo:SLMS
item1
@@@
another line of text
@@item2(testinfo:Source)@@ @@(testinfo2:Config)@@
@@@empty:OTS
item4
@@@
@@@huff:Info
@@@
There is some text @@item6(inline:Foo)@@ in this line.
@@D(SR:Trace)@@
 ";
            expectedResult = Regex.Replace(expectedResult, @"\r\n", "\n");

            string finalResult = RoboClerkMarkdown.ReInsertRoboClerkTags(validText,tags);
            Assert.AreEqual(expectedResult, finalResult);
        }
    }
}