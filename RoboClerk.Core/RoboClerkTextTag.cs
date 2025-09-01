using System;
using System.Collections.Generic;
using System.Linq;

namespace RoboClerk.Core
{
    /// <summary>
    /// Text-based implementation of IRoboClerkTag for text documents (AsciiDoc, Markdown, etc.)
    /// </summary>
    public class RoboClerkTextTag : RoboClerkBaseTag
    {
        private int contentStart = -1; //stores the start location of the content in the *original* asciidoc string
        private int contentEnd = -1; //stores the end location of the content similar to the content start location
        private int tagStart = -1;
        private int tagEnd = -1;
        private bool inline; //true if this tag was found inline
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public RoboClerkTextTag(int startIndex, int endIndex, string rawDocument, bool inline)
        {
            this.inline = inline;
            if (inline)
            {
                logger.Debug("Processing inline RoboClerk tag.");
                ProcessRoboClerkContainerInlineTag(startIndex, endIndex, rawDocument);
            }
            else
            {
                logger.Debug("Processing RoboClerk container tag.");
                ProcessRoboClerkContainerTag(startIndex, endIndex, rawDocument);
            }
        }

        public override bool Inline => inline;
        
        public int ContentStart => contentStart;
        public int ContentEnd => contentEnd;
        public int TagStart => tagStart;
        public int TagEnd => tagEnd;

        public override IEnumerable<IRoboClerkTag> ProcessNestedTags()
        {
            if (string.IsNullOrEmpty(Contents))
                return Enumerable.Empty<IRoboClerkTag>();

            // Parse nested text-based RoboClerk tags within the content
            var nestedRoboClerkTags = RoboClerkTextParser.ExtractRoboClerkTags(Contents);
            return nestedRoboClerkTags.Cast<IRoboClerkTag>();
        }

        private void ProcessRoboClerkContainerInlineTag(int startIndex, int endIndex, string rawDocument)
        {
            tagStart = startIndex;
            tagEnd = endIndex + 1;
            contentStart = startIndex + 2; //remove starting tag
            contentEnd = endIndex; //remove ending tag
            contents = rawDocument.Substring(contentStart, contentEnd - contentStart); 
            try
            {
                ParseCompleteTag(contents);
            }
            catch (TagInvalidException e)
            {
                e.SetLocation(tagStart, rawDocument);
                throw;
            }
        }

        private void ProcessRoboClerkContainerTag(int startIndex, int endIndex, string rawDocument)
        {
            tagStart = startIndex;
            tagEnd = endIndex + 2;
            //parse the tagInfo, items are separated by :
            string tagContents = rawDocument.Substring(startIndex + 3, endIndex - startIndex).Split('\n')[0];
            try
            {
                ParseCompleteTag(tagContents);
            }
            catch (TagInvalidException e)
            {
                e.SetLocation(tagStart, rawDocument);
                throw;
            }
            var prelimTagContents = rawDocument.Substring(startIndex, endIndex - startIndex + 1);
            contentStart = startIndex + prelimTagContents.IndexOf('\n') + 1; //ensure to skip linebreak
            if (prelimTagContents.IndexOf('\n') == prelimTagContents.LastIndexOf('\n'))
            {
                //this tag is empty
                contentEnd = contentStart - 1;
                contents = "";
            }
            else
            {
                contentEnd = startIndex + prelimTagContents.LastIndexOf('\n');
                contents = rawDocument.Substring(contentStart, contentEnd - contentStart + 1);
            }
        }
    }
}
