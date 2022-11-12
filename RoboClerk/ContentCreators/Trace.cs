using RoboClerk.Configuration;

namespace RoboClerk.ContentCreators
{
    public class Trace : IContentCreator
    {
        public Trace()
        {

        }

        public string GetContent(RoboClerkTag tag, IDataSources data, ITraceabilityAnalysis analysis, DocumentConfig doc)
        {
            if (tag.HasParameter("ID"))
            {
                Item item = data.GetItem(tag.GetParameterOrDefault("ID"));
                string result = $"({tag.GetParameterOrDefault("ID")})";
                if (item != null && item.HasLink) 
                {
                    result = $"({item.Link}[{tag.GetParameterOrDefault("ID")}])";
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
