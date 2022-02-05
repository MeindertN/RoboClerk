using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk.ContentCreators
{
    public class SystemRequirements : RequirementBase
    {
        public SystemRequirements()
        {
            requirementName = "Product Requirement";
            sourceType = TraceEntityType.SystemRequirement;
        }

        public override string GetContent(RoboClerkTag tag, DataSources sources, TraceabilityAnalysis analysis, string docTitle)
        {
            requirements = sources.GetAllSystemRequirements();
            return base.GetContent(tag, sources, analysis, docTitle);
        }
    }
}
