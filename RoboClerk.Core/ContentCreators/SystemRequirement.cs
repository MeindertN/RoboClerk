using RoboClerk.Core.Configuration;
using RoboClerk.Core;
using System;

namespace RoboClerk.ContentCreators
{
    public class SystemRequirement : RequirementBase
    {
        protected override string RequirementTypeName => "System";

        public SystemRequirement(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration conf)
            : base(data, analysis, conf)
        {
        }

        /// <summary>
        /// Static metadata for the SystemRequirement content creator
        /// </summary>
        public static ContentCreatorMetadata StaticMetadata { get; } = CreateRequirementMetadata("System");

        public override string GetContent(IRoboClerkTag tag, DocumentConfig doc)
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
