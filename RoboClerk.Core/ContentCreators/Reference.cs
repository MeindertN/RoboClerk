using RoboClerk.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RoboClerk.ContentCreators
{
    internal class Reference : ContentCreatorBase
    {
 
        public Reference(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration conf) 
            : base(data,analysis, conf)
        {
        }

        public override string GetContent(RoboClerkTag tag, DocumentConfig doc)
        {
            StringBuilder result = new StringBuilder();
            DocumentConfig reference = null;
            foreach(var docConfig in configuration.Documents)
            {
                if (docConfig.RoboClerkID == tag.ContentCreatorID)
                {
                    reference = docConfig;
                    break;
                }
            }

            if (reference == null)
            {
                var ex = new TagInvalidException(tag.Contents, $"Reference tag is referencing an unknown document: {tag.ContentCreatorID}");
                ex.DocumentTitle = doc.DocumentTitle;
                throw ex;
            }
            
            if (tag.HasParameter("ID") && tag.GetParameterOrDefault("ID", string.Empty).ToUpper() == "TRUE")
            {
                result.Append(reference.DocumentID);
            }
            if (tag.HasParameter("TITLE") && tag.GetParameterOrDefault("TITLE", string.Empty).ToUpper() == "TRUE")
            {
                if(result.Length > 0) 
                {
                    result.Append($" {reference.DocumentTitle}");
                }
                else
                {
                    result.Append(reference.DocumentTitle);
                }
            }
            if (tag.HasParameter("ABBR") && tag.GetParameterOrDefault("ABBR", string.Empty).ToUpper() == "TRUE")
            {
                if(result.Length > 0) 
                {
                    result.Append($" ({reference.DocumentAbbreviation})");
                }
                else
                {
                    result.Append(reference.DocumentAbbreviation);
                }
            }
            if (tag.HasParameter("TEMPLATE") && tag.GetParameterOrDefault("TEMPLATE", string.Empty).ToUpper() == "TRUE")
            {
                if (result.Length > 0)
                {
                    result.Append($" {reference.DocumentTemplate}");
                }
                else
                {
                    result.Append(reference.DocumentTemplate);
                }
            }
            if (tag.Parameters.Count() == 0)
            {
                result.Append(reference.DocumentTitle);
            }
            else
            {
                List<string> validParameters = new List<string>() { "TEMPLATE", "ABBR", "TITLE", "ID" };
                foreach (var parameter in tag.Parameters)
                {
                    if (!validParameters.Contains(parameter))
                    {
                        var ex = new TagInvalidException(tag.Contents, $"Reference tag has an unknown parameter: {parameter}");
                        ex.DocumentTitle = doc.DocumentTitle;
                        throw ex;
                    }
                }
            }
            
            analysis.AddTrace(analysis.GetTraceEntityForTitle(doc.DocumentTitle), tag.ContentCreatorID, analysis.GetTraceEntityForID(reference.RoboClerkID), tag.ContentCreatorID);
            return result.ToString();
        }
    }
}
