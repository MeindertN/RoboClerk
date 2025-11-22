using RoboClerk.Core.Configuration;
using RoboClerk.Core;

namespace RoboClerk.ContentCreators
{
    internal class PostLayout : IContentCreator
    {
        /// <summary>
        /// Static metadata for the PostLayout content creator
        /// </summary>
        public static ContentCreatorMetadata StaticMetadata { get; } = new ContentCreatorMetadata("Post", "Post-Processing Layout", 
            "Inserts markers for post-processing tools to handle table of contents, page breaks, and paragraph removal")
        {
            Category = "Document Formatting",
            Tags = new List<ContentCreatorTag>
            {
                new ContentCreatorTag("TOC", "Inserts a table of contents marker")
                {
                    Category = "Layout Control",
                    Description = "Inserts a marker that post-processing tools will replace with a table of contents",
                    ExampleUsage = "@@Post:TOC()@@"
                },
                new ContentCreatorTag("PageBreak", "Inserts a page break marker")
                {
                    Category = "Layout Control",
                    Description = "Inserts a marker that post-processing tools will convert to a page break",
                    ExampleUsage = "@@Post:PageBreak()@@"
                },
                new ContentCreatorTag("RemoveParagraph", "Marks a paragraph for removal")
                {
                    Category = "Layout Control",
                    Description = "Inserts a marker that post-processing tools will use to remove the containing paragraph",
                    ExampleUsage = "@@Post:RemoveParagraph()@@"
                }
            }
        };

        public ContentCreatorMetadata GetMetadata() => StaticMetadata;

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
