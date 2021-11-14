using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk.ContentCreators
{
    public class ProductRequirements : RequirementBase
    {
        public ProductRequirements()
        {
            requirementCategory = "Product Requirement";
            requirementName = "Product Requirement";
            linkType = TraceLinkType.ProductRequirementTrace;
        }

        public override string GetContent(RoboClerkTag tag, DataSources sources, TraceabilityAnalysis analysis, string docTitle)
        {
            requirements = sources.GetAllProductRequirements();
            return base.GetContent(tag, sources, analysis, docTitle);
        }
    }
}
