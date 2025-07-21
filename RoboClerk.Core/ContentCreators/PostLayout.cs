using RoboClerk.Configuration;

namespace RoboClerk.ContentCreators
{
    internal class PostLayout : IContentCreator
    {
        public string GetContent(RoboClerkTag tag, DocumentConfig doc)
        {
            if (tag.ContentCreatorID.ToUpper() == "TOC")
            {
                return "~TOC";
            }
            else if (tag.ContentCreatorID.ToUpper() == "REMOVEPARAGRAPH")
            {
                return "~REMOVEPARAGRAPH";
            }
            else if (tag.ContentCreatorID.ToUpper() == "PAGEBREAK")
            {
                return "~PAGEBREAK";
            }
            else
            {
                return $"UNKNOWN POST PROCESSING TAG: {tag.ContentCreatorID}";
            }
        }
    }
}
