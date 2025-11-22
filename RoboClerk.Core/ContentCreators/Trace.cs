using RoboClerk.Core.Configuration;
using RoboClerk.Core;

namespace RoboClerk.ContentCreators
{
    public class Trace : IContentCreator
    {
        private readonly ITraceabilityAnalysis analysis;
        private readonly IDataSources data;
        private readonly IConfiguration config;

        public Trace(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration config)
        {
            this.analysis = analysis;
            this.data = data;
            this.config = config;
        }

        /// <summary>
        /// Static metadata for the Trace content creator
        /// </summary>
        public static ContentCreatorMetadata StaticMetadata { get; } = new ContentCreatorMetadata("Trace", "Traceability Link", 
            "Creates a traceability link to a specific item and tracks the relationship")
        {
            Category = "Requirements & Traceability",
            Tags = new List<ContentCreatorTag>
            {
                new ContentCreatorTag("Trace", "Creates a clickable link to an item and records the trace relationship")
                {
                    Category = "Traceability Management",
                    Description = "Creates a link to a specific item by ID and records the traceability relationship. " +
                        "The link will be clickable if the item has an associated URL. " +
                        "This tag is used to establish and track traceability between documents and items.",
                    Parameters = new List<ContentCreatorParameter>
                    {
                        new ContentCreatorParameter("ID", 
                            "The identifier of the item to trace to", 
                            ParameterValueType.ItemID, required: true)
                        {
                            ExampleValue = "REQ-001"
                        }
                    },
                    ExampleUsage = "@@Trace:Trace(ID=REQ-001)@@"
                }
            }
        };

        public ContentCreatorMetadata GetMetadata() => StaticMetadata;

        public string GetContent(IRoboClerkTag tag, DocumentConfig doc)
        {
            if (tag.HasParameter("ID"))
            {
                Item item = data.GetItem(tag.GetParameterOrDefault("ID"));
                string result = $"({tag.GetParameterOrDefault("ID")})";
                if (item != null && item.HasLink)
                {
                    if (config.OutputFormat.ToUpper() == "HTML")
                    {
                        result = $"<a href=\"{item.Link}\">{item.ItemID}</a>";
                    }
                    else if (config.OutputFormat.ToUpper() == "ASCIIDOC")
                    {
                        result = $"({item.Link}[{tag.GetParameterOrDefault("ID")}])";
                    }
                }
                analysis.AddTraceTag(doc.DocumentTitle, tag);
                return result;
            }
            else
            {
                var ex = new TagInvalidException(tag.Contents, "Trace tag is missing \"ID\" parameter");
                ex.DocumentTitle = doc.DocumentTitle;
                throw ex;
            }
        }
    }
}
