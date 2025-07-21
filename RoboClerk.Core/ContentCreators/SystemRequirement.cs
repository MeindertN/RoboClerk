using RoboClerk.Configuration;
using System;

namespace RoboClerk.ContentCreators
{
    public class SystemRequirement : RequirementBase
    {
        public SystemRequirement(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration conf)
            : base(data, analysis, conf)
        {

        }

        public override string GetContent(RoboClerkTag tag, DocumentConfig doc)
        {
            var te = analysis.GetTraceEntityForID("SystemRequirement");
            if (te == null)
            {
                throw new Exception("SystemRequirement trace entity is missing, this trace entity must be present for RoboClerk to function.");
            }
            requirementName = te.Name;
            sourceType = te;
            requirements = data.GetAllSystemRequirements();
            return base.GetContent(tag, doc);
        }
    }
}
