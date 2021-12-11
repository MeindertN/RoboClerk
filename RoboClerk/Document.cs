using System;
using System.Collections.Generic;
using RoboClerk;
using System.Text.RegularExpressions;

namespace RoboClerk
{
    public class Document
    {
        protected string title = "";

        protected string rawMarkdown;

        protected List<RoboClerkTag> roboclerkTags = new List<RoboClerkTag>();
                
        public Document(string t)
        {
            title = t;
        }

        public void FromMarkDown(string markdown)
        {
            //normalize the line endings in the string
            string normalized = Regex.Replace(markdown, @"\r\n", "\n");
            rawMarkdown = normalized;
            roboclerkTags = RoboClerkMarkdown.ExtractRoboClerkTags(rawMarkdown);
        }

        public string ToMarkDown()
        {
            //this function can be called at any time, it will reconstruct the markdown document
            //based on the tag contents that could have been updated since the document was parsed. 
            //The document can be updated by replacing the individual tag contents.
            return RoboClerkMarkdown.ReInsertRoboClerkTags(rawMarkdown,roboclerkTags,false);
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