using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk.ContentCreators
{
    public class SoftwareRequirement : RequirementBase
    {
        public override string GetContent(RoboClerkTag tag, DataSources sources, TraceabilityAnalysis analysis, string docTitle)
        {
            var te = analysis.GetTraceEntityForID("SoftwareRequirement");
            if (te == null)
            {
                throw new Exception("SoftwareRequirement trace entity is missing, this trace entity must be present for RoboClerk to function.");
            }
            requirementName = te.Name;
            sourceType = te;
            requirements = sources.GetAllSoftwareRequirements();
            return base.GetContent(tag, sources, analysis, docTitle);
        }
    }
}
