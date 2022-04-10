using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace RoboClerk
{
    public class Document
    {
        protected string title = string.Empty;
        protected string rawText = string.Empty;
        protected string templateFile = string.Empty;

        protected List<RoboClerkTag> roboclerkTags = new List<RoboClerkTag>();

        public Document(string title)
        {
            this.title = title;
        }

        public void FromString(string text)
        {
            //normalize the line endings in the string
            rawText = Regex.Replace(text, @"\r\n", "\n");
            
            try
            {
                roboclerkTags.Clear();
                roboclerkTags = RoboClerkMarkdown.ExtractRoboClerkTags(rawText);
            }
            catch (TagInvalidException e)
            {
                e.DocumentTitle = title;
                throw e;
            }
        }

        public void FromFile(string textFile)
        {
            var fileText = File.ReadAllText(textFile);
            templateFile = textFile;
            FromString(fileText);
        }

        public string ToText()
        {
            //this function can be called at any time, remove the tags and relace them with 
            //the tag contents that could have been updated since the document was parsed. 
            //The document can be updated by replacing the individual tag contents.
            return RoboClerkMarkdown.ReInsertRoboClerkTags(rawText, roboclerkTags);
        }

        public IEnumerable<RoboClerkTag> RoboClerkTags
        {
            get => roboclerkTags;
        }

        public string Title
        {
            get => title;
            set => title = value;
        }

        public string TemplateFile
        {
            get => templateFile;
        }
    }
}