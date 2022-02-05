using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RoboClerk
{
    public class Document
    {
        protected string title = "";

        protected string rawText;

        protected List<RoboClerkTag> roboclerkTags = new List<RoboClerkTag>();
                
        public Document(string t)
        {
            title = t;
        }

        public void FromText(string textFile)
        {
            //normalize the line endings in the string
            string normalized = Regex.Replace(textFile, @"\r\n", "\n");
            rawText = normalized;
            try
            {
                roboclerkTags = RoboClerkMarkdown.ExtractRoboClerkTags(rawText);
            }
            catch(TagInvalidException e)
            {
                e.DocumentTitle = title;
                throw e;
            }
        }

        public string ToText()
        {
            //this function can be called at any time, remove the tags and relace them with 
            //the tag contents that could have been updated since the document was parsed. 
            //The document can be updated by replacing the individual tag contents.
            return RoboClerkMarkdown.ReInsertRoboClerkTags(rawText,roboclerkTags);
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
    }
}