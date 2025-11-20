using RoboClerk.Core.Configuration;
using RoboClerk.Core;

namespace RoboClerk.ContentCreators
{
    internal class PostLayout : IContentCreator
    {
        public ContentCreatorMetadata GetMetadata()
        {
            var metadata = new ContentCreatorMetadata("Post", "Post-Processing Layout", 
                "Inserts markers for post-processing tools to handle table of contents, page breaks, and paragraph removal. Note that these are only relevant for text based formats like ASCIIDOC or HTML.");
            
            metadata.Category = "Document Formatting";

            // TOC tag
            var tocTag = new ContentCreatorTag("TOC", "Inserts a table of contents marker");
            tocTag.Category = "Layout Control";
            tocTag.Description = "Inserts a marker that post-processing tools will replace with a table of contents";
            tocTag.ExampleUsage = "@@Post:TOC()@@";
            metadata.Tags.Add(tocTag);

            // PageBreak tag
            var pageBreakTag = new ContentCreatorTag("PageBreak", "Inserts a page break marker");
            pageBreakTag.Category = "Layout Control";
            pageBreakTag.Description = "Inserts a marker that post-processing tools will convert to a page break";
            pageBreakTag.ExampleUsage = "@@Post:PageBreak()@@";
            metadata.Tags.Add(pageBreakTag);

            // RemoveParagraph tag
            var removeParagraphTag = new ContentCreatorTag("RemoveParagraph", "Marks a paragraph for removal");
            removeParagraphTag.Category = "Layout Control";
            removeParagraphTag.Description = "Inserts a marker that post-processing tools will use to remove the containing paragraph";
            removeParagraphTag.ExampleUsage = "@@Post:RemoveParagraph()@@";
            metadata.Tags.Add(removeParagraphTag);

            return metadata;
        }

        public string GetContent(IRoboClerkTag tag, DocumentConfig doc)
        {
            if (tag.ContentCreatorID.ToUpper() == "TOC")
            {
                return "~TOC";
            }
            else if (tag.ContentCreatorID.ToUpper() == "REMOVEPARAGRAPH")
            {
                return "~REMOVEPARAGRAPH";
            }
            else if (tag.ContentCreatorID.ToUpper() == "PAGEBREAK")
            {
                return "~PAGEBREAK";
            }
            else
            {
                return $"UNKNOWN POST PROCESSING TAG: {tag.ContentCreatorID}";
            }
        }
    }
}
