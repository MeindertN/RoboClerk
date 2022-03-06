using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboClerk.ContentCreators
{
    internal class Reference : IContentCreator
    {
        public string GetContent(RoboClerkTag tag, IDataSources data, ITraceabilityAnalysis analysis, string docTitle)
        {
            string result = string.Empty;
            var reference = analysis.GetTraceEntityForID(tag.ContentCreatorID);
                
            if(reference == null)
            {
                var ex = new TagInvalidException(tag.Contents, $"Reference tag is referencing an unknown document: {tag.ContentCreatorID}");
                ex.DocumentTitle = docTitle;
                throw ex;
            }

            if (tag.Parameters.ContainsKey("SHORT") && tag.Parameters["SHORT"].ToUpper() == "TRUE")
            {
                result = reference.Abbreviation;
            }
            else
            {
                result = reference.Name;
            }

            analysis.AddTrace(reference, tag.ContentCreatorID, analysis.GetTraceEntityForTitle(docTitle), tag.ContentCreatorID);
            return result;
        }
    }
}
