using System;
using System.Collections.Generic;
using System.Linq;

namespace RoboClerk
{
    public static class RoboClerkParser
    {
        public static List<RoboClerkTag> ExtractRoboClerkTags(string docText)
        {
            int index = 0;
            List<ProtoTag> indices = new List<ProtoTag>();
            while ((index = docText.IndexOf("@@", index)) >= 0)
            {
                if (index + 1 == docText.Length - 1)
                {
                    AddIndex(indices, index, false);
                    break;
                }
                if (docText[index + 2] != '@')
                {
                    AddIndex(indices, index, false);
                    index += 2;
                }
                else
                {
                    AddIndex(indices, index, true);
                    index += 3;
                }
            }

            CheckForUnbalancedTags(indices);

            List<RoboClerkTag> tags = new List<RoboClerkTag>();

            foreach (ProtoTag tag in indices)
            {
                if (tag.ContainerTag)
                {
                    tags.Add(new RoboClerkTag(tag.StartIndex, tag.EndIndex, docText, false));
                }
                else
                {
                    //check for newlines in tag which is illegal
                    int idx = docText.IndexOf('\n', tag.StartIndex, tag.EndIndex - tag.StartIndex);
                    if (idx >= 0 && idx <= tag.EndIndex)
                    {
                        throw new Exception($"Inline Roboclerk containers cannot have newline characters in them. Newline found in tag at {tag.StartIndex}.");
                    }
                    //check if this inline tag is within a container tag, if so, do not add the tag. The outer container tag will be resolved first
                    if (tags.Count(t => (!t.Inline && t.ContentStart < tag.StartIndex && t.ContentEnd > tag.EndIndex)) == 0)
                        tags.Add(new RoboClerkTag(tag.StartIndex, tag.EndIndex, docText, true));
                }
            }

            return tags;
        }

        private static void AddIndex(List<ProtoTag> tags, int index, bool containerIndex)
        {
            if (tags.Count > 0 && !tags.Last().hasEndIndex())
            {
                tags.Last().EndIndex=index;
                return;
            }
            tags.Add(new ProtoTag(index, containerIndex));
        }

        private static void CheckForUnbalancedTags(List<ProtoTag> tags)
        {
            foreach (ProtoTag tag in tags)
            {
                if (!tag.hasEndIndex() && tag.ContainerTag)
                {
                    throw new Exception("Number of @@@ container indices is not even. Template file is invalid.");
                }
                if (!tag.hasEndIndex() && !tag.ContainerTag)
                {
                    throw new Exception("Number of @@ inline container indices is not even. Template file is invalid.");
                }
            }
        }

        public static string ReInsertRoboClerkTags(string asciiDoc, List<RoboClerkTag> tags)
        {
            if (tags.Count == 0)
            {
                return asciiDoc;
            }
            //first we break apart the original asciiDoc using the original tag locations
            //determine the break points by sorting the tags by the start value and
            //iterating over them
            List<RoboClerkTag> sortedTags = tags.OrderBy(o => o.ContentStart).ToList();

            List<string> parts = new List<string>();
            int lastEnd = -1;
            foreach (var tag in sortedTags)
            {
                parts.Add(asciiDoc.Substring(lastEnd + 1, tag.TagStart - (lastEnd + 1)));
                lastEnd = tag.TagEnd;
            }
            if (lastEnd + 1 < asciiDoc.Length)
            {
                parts.Add(asciiDoc.Substring(lastEnd + 1, asciiDoc.Length - (lastEnd + 1)));
            }

            //then we insert the potentially updated tag contents
            int index = 1;
            foreach (var tag in sortedTags)
            {
                if (tag.Inline || tag.Contents == "")
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
            return string.Join("", parts);
        }
    }
}