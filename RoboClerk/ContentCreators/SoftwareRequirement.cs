using RoboClerk.Configuration;
using System;

namespace RoboClerk.ContentCreators
{
    public class SoftwareRequirement : RequirementBase
    {
        public override string GetContent(RoboClerkTag tag, IDataSources sources, ITraceabilityAnalysis analysis, DocumentConfig doc)
        {
            var te = analysis.GetTraceEntityForID("SoftwareRequirement");
            if (te == null)
            {
                throw new Exception("SoftwareRequirement trace entity is missing, this trace entity must be present for RoboClerk to function.");
            }
            requirementName = te.Name;
            sourceType = te;
            requirements = sources.GetAllSoftwareRequirements();
            return base.GetContent(tag, sources, analysis, doc);
        }
    }
}
