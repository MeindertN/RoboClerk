using RoboClerk.Core.Configuration;
using RoboClerk.Core;
using System;

namespace RoboClerk.ContentCreators
{
    public class DocumentationRequirement : RequirementBase
    {
        public DocumentationRequirement(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration conf)
            : base(data, analysis, conf)
        {

        }

        public override string GetContent(IRoboClerkTag tag, DocumentConfig doc)
        {
            var te = analysis.GetTraceEntityForID("DocumentationRequirement");
            if (te == null)
            {
                throw new Exception("DocumentationRequirement trace entity is missing, this trace entity must be present for RoboClerk to function.");
            }
            requirementName = te.Name;
            sourceType = te;
            requirements = data.GetAllDocumentationRequirements();
            return base.GetContent(tag, doc);
        }
    }
}
