using System;

namespace RoboClerk.ContentCreators
{
    public class SystemRequirement : RequirementBase
    {
        public override string GetContent(RoboClerkTag tag, IDataSources sources, ITraceabilityAnalysis analysis, string docTitle)
        {
            var te = analysis.GetTraceEntityForID("SystemRequirement");
            if (te == null)
            {
                throw new Exception("SystemRequirement trace entity is missing, this trace entity must be present for RoboClerk to function.");
            }
            requirementName = te.Name;
            sourceType = te;
            requirements = sources.GetAllSystemRequirements();
            return base.GetContent(tag, sources, analysis, docTitle);
        }
    }
}
