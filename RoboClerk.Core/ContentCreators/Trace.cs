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

        public ContentCreatorMetadata GetMetadata()
        {
            var metadata = new ContentCreatorMetadata("Trace", "Traceability Link", 
                "Creates a traceability link to a specific item and tracks the relationship");
            
            metadata.Category = "Requirements & Traceability";

            var traceTag = new ContentCreatorTag("Trace", "Creates a clickable link to an item and records the trace relationship");
            traceTag.Category = "Traceability Management";
            traceTag.Description = "Creates a link to a specific item by ID and records the traceability relationship. " +
                "The link will be clickable if the item has an associated URL. " +
                "This tag is used to establish and track traceability between documents and items.";
            
            traceTag.Parameters.Add(new ContentCreatorParameter("ID", 
                "The identifier of the item to trace to", 
                ParameterValueType.ItemID, required: true)
            {
                ExampleValue = "REQ-001"
            });
            
            traceTag.ExampleUsage = "@@Trace:Trace(ID=REQ-001)@@";
            metadata.Tags.Add(traceTag);

            return metadata;
        }

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
