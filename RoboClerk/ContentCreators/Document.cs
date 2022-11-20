using RoboClerk.Configuration;
using System;

namespace RoboClerk.ContentCreators
{
    internal class Document : IContentCreator
    {
        public string GetContent(RoboClerkTag tag, DocumentConfig doc)
        {
            if(tag.ContentCreatorID.ToUpper() == "TITLE")
            {
                return doc.DocumentTitle;
            }
            else if(tag.ContentCreatorID.ToUpper() == "ABBREVIATION")
            {
                return doc.DocumentAbbreviation;
            }
            else if(tag.ContentCreatorID.ToUpper() == "IDENTIFIER")
            {
                return doc.DocumentID;
            }
            else if(tag.ContentCreatorID.ToUpper() == "TEMPLATE")
            {
                return doc.DocumentTemplate;
            }
            else if(tag.ContentCreatorID.ToUpper() == "ROBOCLERKID")
            {
                return doc.RoboClerkID;
            }
            else if(tag.ContentCreatorID.ToUpper() == "GENDATETIME")
            {
                return DateTime.Now.ToString("yyyy/MM/dd HH:mm");
            }
            throw new Exception($"RoboClerk did not know how to handle the document tag: \"{tag.Source}:{tag.ContentCreatorID}\" in \"{doc.RoboClerkID}\".");
        }
    }
}
