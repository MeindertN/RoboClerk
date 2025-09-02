using RoboClerk.Core.Configuration;
using RoboClerk.Core;

namespace RoboClerk.ContentCreators
{
    internal class PostLayout : IContentCreator
    {
        public string GetContent(IRoboClerkTag tag, DocumentConfig doc)
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
