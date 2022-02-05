using RoboClerk;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;

namespace RoboClerk
{
    public static class RoboClerkMarkdown 
    {
        public static List<RoboClerkTag> ExtractRoboClerkTags(string markdownText)
        {
            int index = 0;
            List<int> containerIndices = new List<int>();
            List<int> inlineIndices = new List<int>();
            while ((index = markdownText.IndexOf("@@",index)) >= 0)
            {
                if(index + 1 == markdownText.Length-1)
                {
                    inlineIndices.Add(index);
                }
                if(markdownText[index+2] != '@')
                {
                    inlineIndices.Add(index);
                    index += 2;
                }
                else
                {
                    containerIndices.Add(index);
                    index += 3;
                }
            }

            if(containerIndices.Count % 2 != 0)
            {
                throw new Exception("Number of @@@ container indices is not even. Template file is invalid.");
            }
            if (inlineIndices.Count % 2 != 0)
            {
                throw new Exception("Number of @@ inline container indices is not even. Template file is invalid.");
            }

            List<RoboClerkTag> tags = new List<RoboClerkTag>();

            for( int i = 0; i < containerIndices.Count; i += 2)
            {
                tags.Add(new RoboClerkTag(containerIndices[i], containerIndices[i + 1], markdownText, false)); 
            }

            for (int i = 0; i < inlineIndices.Count; i += 2)
            {
                //check for newlines in tag which is illegal
                int idx = markdownText.IndexOf('\n', inlineIndices[i], inlineIndices[i + 1] - inlineIndices[i]);
                if(idx >= 0 && idx <=inlineIndices[i + 1])
                {
                    throw new Exception($"Inline Roboclerk containers cannot have newline characters in them. Newline found in tag at {inlineIndices[i]}.");
                }
                tags.Add(new RoboClerkTag(inlineIndices[i], inlineIndices[i + 1], markdownText, true));
            }

            return tags;
        }

        public static string ReInsertRoboClerkTags(string markdownDoc, List<RoboClerkTag> tags)
        {
            if(tags.Count == 0)
            {
                return markdownDoc;
            }
            //first we break apart the original markdownDoc using the original tag locations
            //determine the break points by sorting the tags by the start value and
            //iterating over them
            List<RoboClerkTag> sortedTags = tags.OrderBy(o => o.ContentStart).ToList();
            
            List<string> parts = new List<string>();
            int lastEnd = -1;
            foreach(var tag in sortedTags)
            {
                parts.Add(markdownDoc.Substring(lastEnd + 1,tag.TagStart - (lastEnd + 1)));
                lastEnd = tag.TagEnd;                
            }
            if (lastEnd + 1 < markdownDoc.Length)
            {
                parts.Add(markdownDoc.Substring(lastEnd + 1, markdownDoc.Length - (lastEnd + 1)));
            }
            
            //then we insert the potentially updated tag contents
            int index = 1;
            foreach(var tag in sortedTags)
            {
                if(tag.Inline || tag.Contents == "")
                {
                    parts.Insert(index, tag.Contents);
                }
                else
                {
                    parts.Insert(index, tag.Contents + '\n');
                }
                index += 2;
            }

            //we join the string back together and return
            return string.Join("",parts);
        }
    }
}