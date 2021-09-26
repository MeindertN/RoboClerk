using Markdig;
using Markdig.Syntax;
using Markdig.Extensions.RoboClerk;
using RoboClerk;
using System.Collections.Generic;
using System.Linq;

namespace RoboClerk
{
    public static class RoboClerkMarkdown 
    {
        public static List<RoboClerkTag> ExtractRoboClerkTags(string markdownText)
        {
            var pipelineBuilder = new MarkdownPipelineBuilder();
            pipelineBuilder.PreciseSourceLocation = true;
            pipelineBuilder.Extensions.AddIfNotAlready<RoboClerkContainerExtension>();
            
            var pipeline = pipelineBuilder.Build();
            var markdownDocument = Markdown.Parse(markdownText, pipeline, null);

            List<RoboClerkTag> tags = new List<RoboClerkTag>();
            
            foreach(var obj in markdownDocument.Descendants())
            {
                if(obj is RoboClerkContainer cc)
                {
                    tags.Add(new RoboClerkTag(cc,markdownText));             
                }
                if(obj is RoboClerkContainerInline cci)
                {
                    tags.Add(new RoboClerkTag(cci,markdownText));             
                }
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
            List<RoboClerkTag> sortedTags = tags.OrderBy(o => o.Start).ToList();
            
            List<string> parts = new List<string>();//[sortedTags.Count+1];
            int lastEnd = -1;
            foreach(var tag in sortedTags)
            {
                parts.Add(markdownDoc.Substring(lastEnd + 1,tag.Start - (lastEnd + 1)));
                if (tag.Start == tag.End)
                {
                    lastEnd = tag.End-1; //need to correct for the fact there is nothing in this tag
                }
                else
                {
                    lastEnd = tag.End;
                }
                
            }
            parts.Add(markdownDoc.Substring(lastEnd + 1,markdownDoc.Length-(lastEnd + 1)));
            
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

        public static string ConvertMarkdownToHTML(string markdown)
        {
            var pipelineBuilder = new MarkdownPipelineBuilder();
            pipelineBuilder.PreciseSourceLocation = true;
            pipelineBuilder = pipelineBuilder.UsePipeTables();
            pipelineBuilder = pipelineBuilder.UseGridTables();
            pipelineBuilder.Extensions.AddIfNotAlready<RoboClerkContainerExtension>();
            
            var pipeline = pipelineBuilder.Build();
            return Markdown.ToHtml(markdown, pipeline);
        }
    }
}