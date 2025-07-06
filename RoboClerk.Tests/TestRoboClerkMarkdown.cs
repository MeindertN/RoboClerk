using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Linq;
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
@@Trace:SWR(id=1234, name  =test name    )@@";

        [SetUp]
        public void TestSetup()
        {
            validText = Regex.Replace(validText, @"\r\n", "\n");
        }

        [Test]
        public void Valid_Text_Can_Be_Parsed_VERIFIES_No_Exception_Thrown_When_Input_Valid()
        {
            Assert.DoesNotThrow(() => { RoboClerkParser.ExtractRoboClerkTags(validText); });
        }

        [Test]
        public void Test_Tag_Extraction_VERIFIES_The_Expected_Nr_Of_Tags_Is_Extracted()
        {
            var tags = RoboClerkParser.ExtractRoboClerkTags(validText);
            ClassicAssert.AreEqual(7, tags.Count);
        }

        [Test]
        public void Mismatched_Tag_Detection_VERIFIES_Exception_Thrown_When_Initial_Container_Tags_Do_Not_Match()
        {
            StringBuilder sb = new StringBuilder(validText);
            sb[24] = ' ';
            sb[25] = ' ';
            sb[26] = ' ';
            Assert.Throws<Exception>(() => RoboClerkParser.ExtractRoboClerkTags(sb.ToString()));
        }

        [Test]
        public void Mismatched_Tag_Detection_VERIFIES_Exception_Thrown_When_Final_Container_Tags_Do_Not_Match()
        {
            StringBuilder sb = new StringBuilder(validText);
            sb[67] = ' ';
            sb[68] = ' ';
            sb[69] = ' ';
            Assert.Throws<Exception>(() => RoboClerkParser.ExtractRoboClerkTags(sb.ToString()));
        }

        [Test]
        public void Mismatched_Tag_Detection_VERIFIES_Exception_Thrown_When_Initial_Inline_Tags_Do_Not_Match()
        {
            StringBuilder sb = new StringBuilder(validText);
            sb[92] = ' ';
            sb[93] = ' ';
            Assert.Throws<Exception>(() => RoboClerkParser.ExtractRoboClerkTags(sb.ToString()));
        }

        [Test]
        public void Mismatched_Tag_Detection_VERIFIES_Exception_Thrown_When_Final_Inline_Tags_Do_Not_Match()
        {
            StringBuilder sb = new StringBuilder(validText);
            sb[134] = ' ';
            sb[135] = ' ';
            Assert.Throws<Exception>(() => RoboClerkParser.ExtractRoboClerkTags(sb.ToString()));
        }

        [Test]
        public void Invalid_Tag_Detection_VERIFIES_Exception_Thrown_When_Newline_In_Inline_Tag_Start()
        {
            StringBuilder sb = new StringBuilder(validText);
            sb[92] = '\n';
            Assert.Throws<Exception>(() => RoboClerkParser.ExtractRoboClerkTags(sb.ToString()));
        }

        [Test]
        public void Invalid_Inline_Tag_Detection_VERIFIES_Exception_Thrown_When_Newline_In_Inline_Tag_End()
        {
            StringBuilder sb = new StringBuilder(validText);
            sb[124] = '\n';
            Assert.Throws<Exception>(() => RoboClerkParser.ExtractRoboClerkTags(sb.ToString()));
        }

        [Test]
        public void Parameters_Are_Successfully_Extracted_VERIFIES_Parameter_Values_Match_Expected_Values()
        {
            var tags = RoboClerkParser.ExtractRoboClerkTags(validText);
            ClassicAssert.IsTrue(tags[6].Parameters.Count() == 2);
            ClassicAssert.AreEqual("1234", tags[6].GetParameterOrDefault("ID"));
            ClassicAssert.AreEqual("test name", tags[6].GetParameterOrDefault("NAME"));
        }

        [Test]
        public void Parameter_Syntax_Errors_Are_Detected_VERIFIES_Exception_Thrown_When_Equal_Sign_Missing()
        {
            StringBuilder sb = new StringBuilder(validText);
            sb.AppendLine("text@@Info:SWR(huff)@@ test");
            Assert.Throws<TagInvalidException>(() => RoboClerkParser.ExtractRoboClerkTags(sb.ToString()));
        }

        [Test]
        public void Parameter_Syntax_Errors_Are_Detected_VERIFIES_Exception_Thrown_When_Comma_Mismatched()
        {
            StringBuilder sb = new StringBuilder(validText);
            sb.AppendLine("text@@Info:SWR(,huff=puff)@@ test");
            Assert.Throws<TagInvalidException>(() => RoboClerkParser.ExtractRoboClerkTags(sb.ToString()));
        }

        [Test]
        public void Parameter_Syntax_Errors_Are_Detected_VERIFIES_Exception_Thrown_When_Equal_Sign_Missing_And_Comma()
        {
            StringBuilder sb = new StringBuilder(validText);
            sb.AppendLine("text@@Info:SWR(foo,huff=puff)@@ test");
            Assert.Throws<TagInvalidException>(() => RoboClerkParser.ExtractRoboClerkTags(sb.ToString()));
        }

        [Test]
        public void Parameter_Syntax_Can_Be_Parsed_VERIFIES_No_Exception_Thrown_When_Not_Malformed_Parameters()
        {
            StringBuilder sb = new StringBuilder(validText);
            sb.AppendLine("text@@Info:SWR(foo= barr,   huff=puff     )@@ test");
            Assert.DoesNotThrow(() => RoboClerkParser.ExtractRoboClerkTags(sb.ToString()));
        }

        [Test]
        public void Content_Fields_Are_Parsed_Correctly_VERIFIES_The_Correct_Content_Fields_Are_Extracted()
        {
            var tags = RoboClerkParser.ExtractRoboClerkTags(validText);
            ClassicAssert.AreEqual("This is the content\n", tags[0].Contents);
            ClassicAssert.AreEqual("Source:testinfo()", tags[1].Contents);
            ClassicAssert.AreEqual("Config:testinfo2()", tags[2].Contents);
            ClassicAssert.AreEqual("", tags[3].Contents);
            ClassicAssert.AreEqual("this is some contents\nit is even multiline\n# it contains a *header*\n", tags[4].Contents);
            ClassicAssert.AreEqual("Foo:inline()", tags[5].Contents);
            ClassicAssert.AreEqual("Trace:SWR(id=1234, name  =test name    )", tags[6].Contents);
        }

        [Test]
        public void Info_Fields_Are_Parsed_Correctly_VERIFIES_The_Correct_Info_Fields_Are_Extracted()
        {
            var tags = RoboClerkParser.ExtractRoboClerkTags(validText);
            ClassicAssert.AreEqual("TheFirstInfo", tags[0].ContentCreatorID);
            ClassicAssert.AreEqual("testinfo", tags[1].ContentCreatorID);
            ClassicAssert.AreEqual("testinfo2", tags[2].ContentCreatorID);
            ClassicAssert.AreEqual("empty", tags[3].ContentCreatorID);
            ClassicAssert.AreEqual("huff", tags[4].ContentCreatorID);
            ClassicAssert.AreEqual("inline", tags[5].ContentCreatorID);
            ClassicAssert.AreEqual("SWR", tags[6].ContentCreatorID);
        }

        [Test]
        public void Source_Fields_Are_Parsed_Correctly_VERIFIES_The_Correct_Source_Fields_Are_Extracted()
        {
            var tags = RoboClerkParser.ExtractRoboClerkTags(validText);
            ClassicAssert.AreEqual(DataSource.SLMS, tags[0].Source);
            ClassicAssert.AreEqual(DataSource.Source, tags[1].Source);
            ClassicAssert.AreEqual(DataSource.Config, tags[2].Source);
            ClassicAssert.AreEqual(DataSource.OTS, tags[3].Source);
            ClassicAssert.AreEqual(DataSource.Comment, tags[4].Source);
            ClassicAssert.AreEqual(DataSource.Unknown, tags[5].Source);
            ClassicAssert.AreEqual(DataSource.Trace, tags[6].Source);
        }

        [Test]
        public void Tag_Content_Replacement_Behavior_VERIFIES_Content_Is_Inserted_In_The_Correct_Place_Without_Tags()
        {
            var tags = RoboClerkParser.ExtractRoboClerkTags(validText);
            tags[0].Contents = "item1";
            tags[1].Contents = "item2";
            tags[2].Contents = "";
            tags[3].Contents = "item4";
            tags[4].Contents = "";
            tags[5].Contents = "item6";
            tags[6].Contents = "D";

            string expectedResult = @"This is a line of text.
item1
another line of text
item2 
item4
There is some text item6 in this line.
D";
            expectedResult = Regex.Replace(expectedResult, @"\r\n", "\n");

            string finalResult = RoboClerkParser.ReInsertRoboClerkTags(validText, tags);
            ClassicAssert.AreEqual(expectedResult, finalResult);
        }

        [Test]
        public void Tag_Invalid_Format_Brackets1_Inline_VERIFIES_Inline_RoboClerk_Tag_Verifies_Tag_Validity()
        {
            StringBuilder sb = new StringBuilder(validText);
            sb.AppendLine("\n@@SLMS:testfunc)(@@");

            var ex = Assert.Throws<TagInvalidException>(() => RoboClerkParser.ExtractRoboClerkTags(sb.ToString()));
            Assert.That(ex.Message, Is.EqualTo("RoboClerk tag is not formatted correctly at (16:1). Tag contents: SLMS:testfunc)("));
        }

        [Test]
        public void Tag_Invalid_Format_Brackets2_Inline_VERIFIES_Inline_RoboClerk_Tag_Verifies_Tag_Validity()
        {
            StringBuilder sb = new StringBuilder(validText);
            sb.AppendLine("\n@@SLMS:testfunc(()@@");

            var ex = Assert.Throws<TagInvalidException>(() => RoboClerkParser.ExtractRoboClerkTags(sb.ToString()));
            Assert.That(ex.Message, Is.EqualTo("RoboClerk tag is not formatted correctly at (16:1). Tag contents: SLMS:testfunc(()"));
        }

        [Test]
        public void Tag_Invalid_Format_Brackets3_Inline_VERIFIES_Inline_RoboClerk_Tag_Verifies_Tag_Validity()
        {
            StringBuilder sb = new StringBuilder(validText);
            sb.AppendLine("\n@@SLMS:testfunc())@@");

            var ex = Assert.Throws<TagInvalidException>(() => RoboClerkParser.ExtractRoboClerkTags(sb.ToString()));
            Assert.That(ex.Message, Is.EqualTo("RoboClerk tag is not formatted correctly at (16:1). Tag contents: SLMS:testfunc())"));
        }

        [Test]
        public void Tag_Invalid_Format_Brackets_Missing_Inline_VERIFIES_Inline_RoboClerk_Tag_Verifies_Tag_Validity()
        {
            StringBuilder sb = new StringBuilder(validText);
            sb.AppendLine("\n@@SLMS:testfunc@@");

            var ex = Assert.Throws<TagInvalidException>(() => RoboClerkParser.ExtractRoboClerkTags(sb.ToString()));
            Assert.That(ex.Message, Is.EqualTo("RoboClerk tag is not formatted correctly at (16:1). Tag contents: SLMS:testfunc"));
        }

        [Test]
        public void Tag_Invalid_Format_Colon_Position_Inline_VERIFIES_Inline_RoboClerk_Tag_Verifies_Tag_Validity()
        {
            StringBuilder sb = new StringBuilder(validText);
            sb.AppendLine("\n@@SLMStestfunc(:)@@");

            var ex = Assert.Throws<TagInvalidException>(() => RoboClerkParser.ExtractRoboClerkTags(sb.ToString()));
            Assert.That(ex.Message, Is.EqualTo("RoboClerk tag is not formatted correctly at (16:1). Tag contents: SLMStestfunc(:)"));
        }

        [Test]
        public void Tag_Invalid_Format_Colon_Missing_Inline_VERIFIES_Inline_RoboClerk_Tag_Verifies_Tag_Validity()
        {
            StringBuilder sb = new StringBuilder(validText);
            sb.AppendLine("\n@@SLMStestfunc()@@");

            var ex = Assert.Throws<TagInvalidException>(() => RoboClerkParser.ExtractRoboClerkTags(sb.ToString()));
            Assert.That(ex.Message, Is.EqualTo("RoboClerk tag is not formatted correctly at (16:1). Tag contents: SLMStestfunc()"));
        }

        [Test]
        public void Tag_Valid_Format_Double_Colon_Inline_VERIFIES_Inline_RoboClerk_Tag_Verifies_Tag_Validity()
        {
            StringBuilder sb = new StringBuilder(validText);
            sb.AppendLine("\n@@SLMS:testfunc(test=A:B)@@");

            Assert.DoesNotThrow(() => RoboClerkParser.ExtractRoboClerkTags(sb.ToString()));
        }

        [Test]  //we will only test one instance of the block code because the tag validation is the same
        public void Tag_Invalid_Format_Colon_Missing_Block_VERIFIES_Block_RoboClerk_Tag_Verifies_Tag_Validity()
        {
            StringBuilder sb = new StringBuilder(validText);
            sb.Append("\n@@@SLMStestfunc()\n");
            sb.Append("\n");
            sb.Append("@@@\n");

            var ex = Assert.Throws<TagInvalidException>(() => RoboClerkParser.ExtractRoboClerkTags(sb.ToString()));
            Assert.That(ex.Message, Is.EqualTo("RoboClerk tag is not formatted correctly at (16:1). Tag contents: SLMStestfunc()"));
        }

        [Test]  
        public void Preamble_Invalid_Format_Block_VERIFIES_Block_RoboClerk_Tag_Verifies_Preamble_Validity1()
        {
            StringBuilder sb = new StringBuilder(validText);
            sb.Append("\n@@@:testfunc()\n");
            sb.Append("\n");
            sb.Append("@@@\n");

            var ex = Assert.Throws<TagInvalidException>(() => RoboClerkParser.ExtractRoboClerkTags(sb.ToString()));
            Assert.That(ex.Message, Is.EqualTo("Preamble section in RoboClerk tag not formatted correctly at (16:1). Tag contents: :testfunc()"));
        }

        [Test]
        public void Preamble_Invalid_Format_Block_VERIFIES_Block_RoboClerk_Tag_Verifies_Preamble_Validity2()
        {
            StringBuilder sb = new StringBuilder(validText);
            sb.Append("\n@@@SLMS:()\n");
            sb.Append("\n");
            sb.Append("@@@\n");

            var ex = Assert.Throws<TagInvalidException>(() => RoboClerkParser.ExtractRoboClerkTags(sb.ToString()));
            Assert.That(ex.Message, Is.EqualTo("Preamble section in RoboClerk tag not formatted correctly at (16:1). Tag contents: SLMS:()"));
        }

        [Test]
        public void Parameters_Invalid_Format_Block_VERIFIES_Block_RoboClerk_Tag_Verifies_Parameter_Validity1()
        {
            StringBuilder sb = new StringBuilder(validText);
            sb.Append("\n@@@SLMS:testfunc(test==4)\n");
            sb.Append("\n");
            sb.Append("@@@\n");

            var ex = Assert.Throws<TagInvalidException>(() => RoboClerkParser.ExtractRoboClerkTags(sb.ToString()));
            Assert.That(ex.Message, Is.EqualTo("Parameter section in RoboClerk tag not formatted correctly at (16:1). Tag contents: SLMS:testfunc(test==4)"));
        }

        [Test]
        public void Parameters_Invalid_Format_Block_VERIFIES_Block_RoboClerk_Tag_Verifies_Parameter_Validity2()
        {
            StringBuilder sb = new StringBuilder(validText);
            sb.Append("\n@@@SLMS:testfunc(test==4,testing)\n");
            sb.Append("\n");
            sb.Append("@@@\n");

            var ex = Assert.Throws<TagInvalidException>(() => RoboClerkParser.ExtractRoboClerkTags(sb.ToString()));
            Assert.That(ex.Message, Is.EqualTo("Malformed element in parameter section of RoboClerk tag at (16:1). Tag contents: SLMS:testfunc(test==4,testing)"));
        }

        [Test]
        public void Parameters_Invalid_Format_Block_VERIFIES_Block_RoboClerk_Tag_Verifies_Parameter_Validity3()
        {
            StringBuilder sb = new StringBuilder(validText);
            sb.Append("\n@@@SLMS:testfunc(test=4,testing=)\n");
            sb.Append("\n");
            sb.Append("@@@\n");

            var ex = Assert.Throws<TagInvalidException>(() => RoboClerkParser.ExtractRoboClerkTags(sb.ToString()));
            Assert.That(ex.Message, Is.EqualTo("Malformed element in parameter section of RoboClerk tag at (16:1). Tag contents: SLMS:testfunc(test=4,testing=)"));
        }

        [Test]
        public void Parameters_Retrieval_Functions_VERIFIES_Parameter_Value_Can_Be_Retrieved()
        {
            StringBuilder sb = new StringBuilder(validText);
            sb.Append("\n@@@SLMS:testfunc(test=4,testing=teststring)\n");
            sb.Append("\n");
            sb.Append("@@@\n");

            var tags = RoboClerkParser.ExtractRoboClerkTags(sb.ToString());

            foreach(var tag in tags)
            {
                if(tag.ContentCreatorID == "testfunc")
                {
                    Assert.That(tag.GetParameterOrDefault("test", "default"), Is.EqualTo("4"));
                    Assert.That(tag.GetParameterOrDefault("testing", "default"), Is.EqualTo("teststring"));
                    Assert.That(tag.GetParameterOrDefault("testrrrr", "default"), Is.EqualTo("default"));
                    return;
                }
            }
            Assert.Fail("Could not find test tag in parsed tags.");
        }

        [Test]
        public void TagInvalidException_Message_Retrieval_VERIFIES_Correct_Message_Is_Returned1()
        {
            TagInvalidException ex = new TagInvalidException("test contents", "test reason");
            Assert.That(ex.Message, Is.EqualTo("test reason. Tag contents: test contents"));
            string teststring = "\nabcdeThis is the test text.";
            ex.SetLocation(5, teststring);
            Assert.That(ex.Message, Is.EqualTo("test reason at (2:5). Tag contents: test contents"));

            ex = new TagInvalidException("test contents", "test reason");
            ex.DocumentTitle = "test title";
            Assert.That(ex.Message, Is.EqualTo("test reason in test title template. Tag contents: test contents"));
            ex.SetLocation(5, teststring);
            Assert.That(ex.Message, Is.EqualTo("test reason in test title template at (2:5). Tag contents: test contents"));
        }
    }
}