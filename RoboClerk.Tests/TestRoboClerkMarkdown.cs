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
@@some other stuff(testinfo:Source)@@ @@some other stuff(testinfo2:Config)@@
@@@empty:OTS
@@@
@@@huff:Info
this is some contents
it is even multiline
# it contains a *header*
@@@
There is some text @@inline(inline:Foo)@@ in this line.
 ";

        [SetUp]
        public void TestSetup()
        {
            validText = Regex.Replace(validText, @"\r\n", "\n");
        }

        [Test]
        public void Valid_Text_Can_Be_Parsed()
        {
            Assert.DoesNotThrow( () => { RoboClerkTagMarkdown.ExtractRoboClerkTags(validText); });
        }

        [Test]
        public void The_Correct_Nr_Of_Tags_Is_Extracted()
        {
            var tags = RoboClerkTagMarkdown.ExtractRoboClerkTags(validText);
            Assert.AreEqual(6, tags.Count);
        }

        [Test]
        public void The_Correct_Info_Fields_Are_Extracted()
        {
            var tags = RoboClerkTagMarkdown.ExtractRoboClerkTags(validText);
            Assert.AreEqual("TheFirstInfo", tags[0].ID);
            Assert.AreEqual("testinfo", tags[1].ID);
            Assert.AreEqual("testinfo2", tags[2].ID);
            Assert.AreEqual("empty", tags[3].ID);
            Assert.AreEqual("huff", tags[4].ID);
            Assert.AreEqual("inline", tags[5].ID);
        }

        [Test]
        public void The_Correct_Source_Fields_Are_Extracted()
        {
            var tags = RoboClerkTagMarkdown.ExtractRoboClerkTags(validText);
            Assert.AreEqual(DataSource.SLMS, tags[0].Source);
            Assert.AreEqual(DataSource.Source, tags[1].Source);
            Assert.AreEqual(DataSource.Config, tags[2].Source);
            Assert.AreEqual(DataSource.OTS, tags[3].Source);
            Assert.AreEqual(DataSource.Info, tags[4].Source);
            Assert.AreEqual(DataSource.Unknown, tags[5].Source);
        }

        [Test]
        public void Content_Is_Inserted_In_The_Correct_Place()
        {
            var tags = RoboClerkTagMarkdown.ExtractRoboClerkTags(validText);
            tags[0].Contents = "item1";
            tags[1].Contents = "item2";
            tags[2].Contents = "item3";
            tags[3].Contents = "item4";
            tags[4].Contents = "";
            tags[5].Contents = "item6";

            string expectedResult = @"This is a line of text.
@@@TheFirstInfo:SLMS
item1
@@@
another line of text
@@item2(testinfo:Source)@@ @@item3(testinfo2:Config)@@
@@@empty:OTS
item4
@@@
@@@huff:Info
@@@
There is some text @@item6(inline:Foo)@@ in this line.
 ";
            expectedResult = Regex.Replace(expectedResult, @"\r\n", "\n");

            string finalResult = RoboClerkTagMarkdown.ReInsertRoboClerkTags(validText,tags);
            Assert.AreEqual(expectedResult, finalResult);
        }
    }
}