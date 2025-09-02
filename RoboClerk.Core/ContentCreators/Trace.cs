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
