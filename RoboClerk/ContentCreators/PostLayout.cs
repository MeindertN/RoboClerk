using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboClerk.ContentCreators
{
    internal class PostLayout : IContentCreator
    {
        public string GetContent(RoboClerkTag tag, IDataSources data, ITraceabilityAnalysis analysis, string docTitle)
        {
            if(tag.ContentCreatorID.ToUpper() == "TOC")
            {
                return "~TOC";
            }
            else if(tag.ContentCreatorID.ToUpper() == "REMOVEPARAGRAPH")
            {
                return "~REMOVEPARAGRAPH";
            }
            else if(tag.ContentCreatorID.ToUpper() == "PAGEBREAK")
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
