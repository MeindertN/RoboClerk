using RoboClerk.Core.Configuration;
using RoboClerk.Core;
using System;

namespace RoboClerk.ContentCreators
{
    public class SoftwareRequirement : RequirementBase
    {
        public SoftwareRequirement(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration conf)
            : base(data, analysis, conf)
        {

        }

        public override string GetContent(IRoboClerkTag tag, DocumentConfig doc)
        {
            var te = analysis.GetTraceEntityForID("SoftwareRequirement");
            if (te == null)
            {
                throw new Exception("SoftwareRequirement trace entity is missing, this trace entity must be present for RoboClerk to function.");
            }
            requirementName = te.Name;
            sourceType = te;
            requirements = data.GetAllSoftwareRequirements();
            return base.GetContent(tag, doc);
        }
    }
}
