using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk.ContentCreators
{
    abstract public class RequirementBase : IContentCreator
    {
        protected string requirementCategory = string.Empty;
        protected List<RequirementItem> requirements = null;
        protected string requirementName = string.Empty;
        protected TraceLinkType linkType = TraceLinkType.Unknown;

        public RequirementBase()
        {

        }

        public virtual string GetContent(RoboClerkTag tag, DataSources sources, TraceabilityAnalysis analysis, string docTitle)
        {
            bool foundRequirement = false;
            //No selection needed, we return everything
            StringBuilder output = new StringBuilder();
            foreach (var requirement in requirements)
            {
                if (tag.TraceReference != string.Empty && tag.TraceReference != requirement.RequirementID)
                {
                    continue; //if a particular requirement was indicated, we ignore those that do not match
                }
                if(requirement.RequirementCategory != requirementCategory)
                {
                    continue; //ignore items if they are of the wrong category
                }
                foundRequirement = true;
                output.AppendLine(requirement.ToMarkDown());
                analysis.AddTrace(docTitle, new TraceLink(requirement.RequirementID, linkType));
            }
            if (!foundRequirement)
            {
                return $"Unable to find {requirementName}(s). Check if {requirementName}s of the correct type are provided or if a valid {requirementName} identifier is specified.";
            }
            return output.ToString();
        }
    }
}
