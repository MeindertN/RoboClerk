using RoboClerk.Configuration;
using System;

namespace RoboClerk.ContentCreators
{
    public class Document : IContentCreator
    {
        private readonly ITraceabilityAnalysis analysis;
        public Document(ITraceabilityAnalysis analysis) 
        {
            this.analysis = analysis;
        }

        public string GetContent(RoboClerkTag tag, DocumentConfig doc)
        {
            if (tag.ContentCreatorID.ToUpper() == "TITLE")
            {
                return doc.DocumentTitle;
            }
            else if (tag.ContentCreatorID.ToUpper() == "ABBREVIATION")
            {
                return doc.DocumentAbbreviation;
            }
            else if (tag.ContentCreatorID.ToUpper() == "IDENTIFIER")
            {
                return doc.DocumentID;
            }
            else if (tag.ContentCreatorID.ToUpper() == "TEMPLATE")
            {
                return doc.DocumentTemplate;
            }
            else if (tag.ContentCreatorID.ToUpper() == "ROBOCLERKID")
            {
                return doc.RoboClerkID;
            }
            else if (tag.ContentCreatorID.ToUpper() == "GENDATETIME")
            {
                return DateTime.Now.ToString("yyyy/MM/dd HH:mm");
            }
            else if (tag.ContentCreatorID.ToUpper() == "COUNTENTITIES")
            {
                if (tag.HasParameter("entity"))
                {
                    string entityName = tag.GetParameterOrDefault("entity");
                    if (entityName != null) 
                    {
                        string restart = tag.GetParameterOrDefault("restart");
                        TraceEntity te = analysis.GetTraceEntityForAnyProperty(entityName);
                        if (te == null)
                        {
                            throw new Exception($"RoboClerk was unable to find the entity \"{entityName}\" as specified in the document tag: \"{tag.Source}:{tag.ContentCreatorID}\" in \"{doc.RoboClerkID}\".");
                        }
                        if (restart != null && restart.ToUpper() == "TRUE")
                        {
                            //reset the counter and return an empty string
                            doc.ResetEntityCount(te);
                            return string.Empty;
                        }

                        return doc.GetEntityCount(te).ToString();
                    }
                }
            }
            throw new Exception($"RoboClerk did not know how to handle the document tag: \"{tag.Source}:{tag.ContentCreatorID}\" in \"{doc.RoboClerkID}\".");
        }
    }
}
