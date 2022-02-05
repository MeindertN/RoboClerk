using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk.ContentCreators
{
    public class Trace : IContentCreator
    {
        public Trace()
        {

        }

        public string GetContent(RoboClerkTag tag, DataSources data, TraceabilityAnalysis analysis, string docTitle)
        {
            if (tag.Parameters.ContainsKey("ID"))
            {
                Item item = data.GetItem(tag.Parameters["ID"]);
                string result = string.Empty;
                if(item == null) //the item was not found, we'll still add the trace
                {
                    result = $"({tag.Parameters["ID"]})";
                }
                else
                {
                    result = (item.HasLink ? $"[{tag.Parameters["ID"]}]({item.Link})" : tag.Parameters["ID"]);
                }
                analysis.AddTraceTag(docTitle, tag);
                return result;
            }
            else
            {
                var ex = new TagInvalidException(tag.Contents, "Trace tag is missing \"ID\" parameter");
                ex.DocumentTitle = docTitle;
                throw ex;
            }
        }
    }
}
