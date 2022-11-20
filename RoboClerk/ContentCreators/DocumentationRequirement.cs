using RoboClerk.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboClerk.ContentCreators
{    
    public class DocumentationRequirement : RequirementBase
    {
        public DocumentationRequirement(IDataSources data, ITraceabilityAnalysis analysis)
            : base(data, analysis)
        {

        }

        public override string GetContent(RoboClerkTag tag, DocumentConfig doc)
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
