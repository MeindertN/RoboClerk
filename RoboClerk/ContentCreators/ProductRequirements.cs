using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk.ContentCreators
{
    public class ProductRequirements : RequirementBase
    {
        public ProductRequirements()
        {
            requirementName = "Product Requirement";
            sourceType = TraceEntityType.ProductRequirement;
        }

        public override string GetContent(RoboClerkTag tag, DataSources sources, TraceabilityAnalysis analysis, string docTitle)
        {
            requirementCategory = tag.Target;
            requirements = sources.GetAllProductRequirements();
            return base.GetContent(tag, sources, analysis, docTitle);
        }
    }
}
