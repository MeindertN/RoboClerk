using RoboClerk.Configuration;

namespace RoboClerk.ContentCreators
{
    internal class Reference : IContentCreator
    {
        public string GetContent(RoboClerkTag tag, IDataSources data, ITraceabilityAnalysis analysis, DocumentConfig doc)
        {
            string result = string.Empty;
            var reference = analysis.GetTraceEntityForID(tag.ContentCreatorID);
                
            if(reference == null)
            {
                var ex = new TagInvalidException(tag.Contents, $"Reference tag is referencing an unknown document: {tag.ContentCreatorID}");
                ex.DocumentTitle = doc.DocumentTitle;
                throw ex;
            }

            if (tag.HasParameter("SHORT") && tag.GetParameterOrDefault("SHORT",string.Empty).ToUpper() == "TRUE")
            {
                result = reference.Abbreviation;
            }
            else
            {
                result = reference.Name;
            }

            analysis.AddTrace(reference, tag.ContentCreatorID, analysis.GetTraceEntityForTitle(doc.DocumentTitle), tag.ContentCreatorID);
            return result;
        }
    }
}
