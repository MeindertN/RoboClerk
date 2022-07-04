using NUnit.Framework;
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("These tests are for work item 9")]
    internal class TestRoboClerkADoc
    {
        private string validText = @"This is a line of text.
@@@SLMS:TheFirstInfo()
This is the content
@@@
another line of text
@@Source:testinfo()@@ @@Config:testinfo2()@@
@@@OTS:empty()
@@@
@@@Comment:huff()
this is some contents
it is even multiline
# it contains a *header*
@@@
There is some text @@Foo:inline()@@ in this line.
@@Trace:SWR(id=1234, name  =test name    )@@
 ";

        [SetUp]
        public void TestSetup()
        {
            validText = Regex.Replace(validText, @"\r\n", "\n");
        }

        [Test]
        public void Valid_Text_Can_Be_Parsed_VERIFIES_No_Exception_Thrown_When_Input_Valid()
        {
            Assert.DoesNotThrow(() => { RoboClerkAsciiDoc.ExtractRoboClerkTags(validText); });
        }

        [Test]
        public void Test_Tag_Extraction_VERIFIES_The_Expected_Nr_Of_Tags_Is_Extracted()
        {
            var tags = RoboClerkAsciiDoc.ExtractRoboClerkTags(validText);
            Assert.AreEqual(7, tags.Count);
        }

        [Test]
        public void Mismatched_Tag_Detection_VERIFIES_Exception_Thrown_When_Initial_Container_Tags_Do_Not_Match()
        {
            StringBuilder sb = new StringBuilder(validText);
            sb[24] = ' ';
            sb[25] = ' ';
            sb[26] = ' ';
            Assert.Throws<Exception>(() => RoboClerkAsciiDoc.ExtractRoboClerkTags(sb.ToString()));
        }

        [Test]
        public void Mismatched_Tag_Detection_VERIFIES_Exception_Thrown_When_Final_Container_Tags_Do_Not_Match()
        {
            StringBuilder sb = new StringBuilder(validText);
            sb[65] = ' ';
            sb[66] = ' ';
            sb[67] = ' ';
            Assert.Throws<Exception>(() => RoboClerkAsciiDoc.ExtractRoboClerkTags(sb.ToString()));
        }

        [Test]
        public void Mismatched_Tag_Detection_VERIFIES_Exception_Thrown_When_Initial_Inline_Tags_Do_Not_Match()
        {
            StringBuilder sb = new StringBuilder(validText);
            sb[92] = ' ';
            sb[93] = ' ';
            Assert.Throws<Exception>(() => RoboClerkAsciiDoc.ExtractRoboClerkTags(sb.ToString()));
        }

        [Test]
        public void Mismatched_Tag_Detection_VERIFIES_Exception_Thrown_When_Final_Inline_Tags_Do_Not_Match()
        {
            StringBuilder sb = new StringBuilder(validText);
            sb[134] = ' ';
            sb[135] = ' ';
            Assert.Throws<Exception>(() => RoboClerkAsciiDoc.ExtractRoboClerkTags(sb.ToString()));
        }

        [Test]
        public void Invalid_Tag_Detection_VERIFIES_Exception_Thrown_When_Newline_In_Inline_Tag_Start()
        {
            StringBuilder sb = new StringBuilder(validText);
            sb[92] = '\n';
            Assert.Throws<Exception>(() => RoboClerkAsciiDoc.ExtractRoboClerkTags(sb.ToString()));
        }

        [Test]
        public void Invalid_Inline_Tag_Detection_VERIFIES_Exception_Thrown_When_Newline_In_Inline_Tag_End()
        {
            StringBuilder sb = new StringBuilder(validText);
            sb[124] = '\n';
            Assert.Throws<Exception>(() => RoboClerkAsciiDoc.ExtractRoboClerkTags(sb.ToString()));
        }

        [Test]
        public void Parameters_Are_Successfully_Extracted_VERIFIES_Parameter_Values_Match_Expected_Values()
        {
            var tags = RoboClerkAsciiDoc.ExtractRoboClerkTags(validText);
            Assert.IsTrue(tags[6].Parameters.Count == 2);
            Assert.AreEqual("1234", tags[6].Parameters["ID"]);
            Assert.AreEqual("test name", tags[6].Parameters["NAME"]);
        }

        [Test]
        public void Parameter_Syntax_Errors_Are_Detected_VERIFIES_Exception_Thrown_When_Equal_Sign_Missing()
        {
            StringBuilder sb = new StringBuilder(validText);
            sb.AppendLine("text@@Info:SWR(huff)@@ test");
            Assert.Throws<TagInvalidException>(() => RoboClerkAsciiDoc.ExtractRoboClerkTags(sb.ToString()));
        }

        [Test]
        public void Parameter_Syntax_Errors_Are_Detected_VERIFIES_Exception_Thrown_When_Comma_Mismatched()
        {
            StringBuilder sb = new StringBuilder(validText);
            sb.AppendLine("text@@Info:SWR(,huff=puff)@@ test");
            Assert.Throws<TagInvalidException>(() => RoboClerkAsciiDoc.ExtractRoboClerkTags(sb.ToString()));
        }

        [Test]
        public void Parameter_Syntax_Errors_Are_Detected_VERIFIES_Exception_Thrown_When_Equal_Sign_Missing_And_Comma()
        {
            StringBuilder sb = new StringBuilder(validText);
            sb.AppendLine("text@@Info:SWR(foo,huff=puff)@@ test");
            Assert.Throws<TagInvalidException>(() => RoboClerkAsciiDoc.ExtractRoboClerkTags(sb.ToString()));
        }

        [Test]
        public void Parameter_Syntax_Can_Be_Parsed_VERIFIES_No_Exception_Thrown_When_Not_Malformed_Parameters()
        {
            StringBuilder sb = new StringBuilder(validText);
            sb.AppendLine("text@@Info:SWR(foo= barr,   huff=puff     )@@ test");
            Assert.DoesNotThrow(() => RoboClerkAsciiDoc.ExtractRoboClerkTags(sb.ToString()));
        }

        [Test]
        public void Content_Fields_Are_Parsed_Correctly_VERIFIES_The_Correct_Content_Fields_Are_Extracted()
        {
            var tags = RoboClerkAsciiDoc.ExtractRoboClerkTags(validText);
            Assert.AreEqual("This is the content\n", tags[0].Contents);
            Assert.AreEqual("Source:testinfo()", tags[3].Contents);
            Assert.AreEqual("Config:testinfo2()", tags[4].Contents);
            Assert.AreEqual("", tags[1].Contents);
            Assert.AreEqual("this is some contents\nit is even multiline\n# it contains a *header*\n", tags[2].Contents);
            Assert.AreEqual("Foo:inline()", tags[5].Contents);
            Assert.AreEqual("Trace:SWR(id=1234, name  =test name    )", tags[6].Contents);
        }

        [Test]
        public void Info_Fields_Are_Parsed_Correctly_VERIFIES_The_Correct_Info_Fields_Are_Extracted()
        {
            var tags = RoboClerkAsciiDoc.ExtractRoboClerkTags(validText);
            Assert.AreEqual("TheFirstInfo", tags[0].ContentCreatorID);
            Assert.AreEqual("testinfo", tags[3].ContentCreatorID);
            Assert.AreEqual("testinfo2", tags[4].ContentCreatorID);
            Assert.AreEqual("empty", tags[1].ContentCreatorID);
            Assert.AreEqual("huff", tags[2].ContentCreatorID);
            Assert.AreEqual("inline", tags[5].ContentCreatorID);
            Assert.AreEqual("SWR", tags[6].ContentCreatorID);
        }

        [Test]
        public void Source_Fields_Are_Parsed_Correctly_VERIFIES_The_Correct_Source_Fields_Are_Extracted()
        {
            var tags = RoboClerkAsciiDoc.ExtractRoboClerkTags(validText);
            Assert.AreEqual(DataSource.SLMS, tags[0].Source);
            Assert.AreEqual(DataSource.Source, tags[3].Source);
            Assert.AreEqual(DataSource.Config, tags[4].Source);
            Assert.AreEqual(DataSource.OTS, tags[1].Source);
            Assert.AreEqual(DataSource.Comment, tags[2].Source);
            Assert.AreEqual(DataSource.Unknown, tags[5].Source);
            Assert.AreEqual(DataSource.Trace, tags[6].Source);
        }

        [Test]
        public void Tag_Content_Replacement_Behavior_VERIFIES_Content_Is_Inserted_In_The_Correct_Place_Without_Tags()
        {
            var tags = RoboClerkAsciiDoc.ExtractRoboClerkTags(validText);
            tags[0].Contents = "item1";
            tags[3].Contents = "item2";
            tags[4].Contents = "";
            tags[1].Contents = "item4";
            tags[2].Contents = "";
            tags[5].Contents = "item6";
            tags[6].Contents = "D";

            string expectedResult = @"This is a line of text.
item1
another line of text
item2 
item4
There is some text item6 in this line.
D
 ";
            expectedResult = Regex.Replace(expectedResult, @"\r\n", "\n");

            string finalResult = RoboClerkAsciiDoc.ReInsertRoboClerkTags(validText, tags);
            Assert.AreEqual(expectedResult, finalResult);
        }
    }
}