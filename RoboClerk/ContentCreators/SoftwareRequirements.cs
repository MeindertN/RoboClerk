using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk.ContentCreators
{
    public class SoftwareRequirements : RequirementBase
    {
        public SoftwareRequirements()
        {
            requirementName = "Software Requirement";
            sourceType = TraceEntityType.SoftwareRequirement;
        }

        public override string GetContent(RoboClerkTag tag, DataSources sources, TraceabilityAnalysis analysis, string docTitle)
        {
            requirementCategory = tag.Target;
            requirements = sources.GetAllSoftwareRequirements();
            return base.GetContent(tag, sources, analysis, docTitle);
        }
    }
}
