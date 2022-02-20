using System.Collections.Generic;
using System.Text;

namespace RoboClerk.ContentCreators
{
    abstract public class RequirementBase : ContentCreatorBase
    {
        protected List<RequirementItem> requirements = null;
        protected string requirementName = string.Empty;
        protected TraceEntity sourceType = null;

        public RequirementBase()
        {

        }

        public override string GetContent(RoboClerkTag tag, IDataSources sources, ITraceabilityAnalysis analysis, string docTitle)
        {
            bool foundRequirement = false;
            //No selection needed, we return everything
            StringBuilder output = new StringBuilder();
            var properties = typeof(RequirementItem).GetProperties();
            foreach (var requirement in requirements)
            {
                if (ShouldBeIncluded(tag, requirement, properties))
                {
                    foundRequirement = true;
                    output.AppendLine(requirement.ToText());
                    analysis.AddTrace(sourceType, requirement.RequirementID, analysis.GetTraceEntityForTitle(docTitle), requirement.RequirementID);
                }
            }
            if (!foundRequirement)
            {
                return $"Unable to find {requirementName}(s). Check if {requirementName}s of the correct type are provided or if a valid {requirementName} identifier is specified.";
            }
            return output.ToString();
        }
    }
}
