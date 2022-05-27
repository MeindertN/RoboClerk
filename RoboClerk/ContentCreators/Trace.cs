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
            if (tag.Parameters.ContainsKey("ID"))
            {
                Item item = data.GetItem(tag.Parameters["ID"]);
                string result = $"({tag.Parameters["ID"]})";
                if (item != null && item.HasLink) 
                {
                    result = $"({item.Link}[{tag.Parameters["ID"]}])";
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
