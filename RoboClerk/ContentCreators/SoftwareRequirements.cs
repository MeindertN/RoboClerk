using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk.ContentCreators
{
    public class SoftwareRequirements : RequirementBase
    {
        public SoftwareRequirements()
        {
            requirementCategory = "Software Requirement";
            requirementName = "Software Requirement";
            linkType = TraceLinkType.SoftwareRequirementTrace;
        }

        public override string GetContent(RoboClerkTag tag, DataSources sources, TraceabilityAnalysis analysis, string docTitle)
        {
            requirements = sources.GetAllSoftwareRequirements();
            return base.GetContent(tag, sources, analysis, docTitle);
        }
    }
}
