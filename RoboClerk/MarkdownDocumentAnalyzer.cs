using Markdig;
using Markdig.Syntax;
using Markdig.Extensions.CustomContainers;
using RoboClerk;
using System.Collections.Generic;

namespace RoboClerk
{
    class MarkdownDocumentAnalyzer //TODO: turn into static class?
    {
        private string rawDocument;
        private MarkdownDocument markdownDocument;
        public MarkdownDocumentAnalyzer(string markdownDoc)
        {
            rawDocument = markdownDoc;
            //parse the markdown
            ParseMarkdown();
        }
        private void ParseMarkdown()
        {
            var pipelineBuilder = new MarkdownPipelineBuilder();
            pipelineBuilder.PreciseSourceLocation = true;
            var pipeline = pipelineBuilder.UseCustomContainers().Build();
            markdownDocument = Markdown.Parse(rawDocument, pipeline, null);

            foreach(var obj in markdownDocument.Descendants())
            {
                if(obj.GetType().Equals(typeof(CustomContainer)))
                {
                    //found a custom container indicating the presence of a
                    //request tag
                    var cc = obj as CustomContainer;
                    //Console.WriteLine($"Found a block variable: {cc.Info}");
                }
            }
        }

        public List<RequestTag> GetRequestTags()
        {
            return null;
        }




    }
}