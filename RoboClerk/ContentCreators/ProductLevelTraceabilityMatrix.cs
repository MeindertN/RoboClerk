using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RoboClerk.ContentCreators
{
    class ProductLevelTraceabilityMatrix : TraceabilityMatrixBase
    {
        public ProductLevelTraceabilityMatrix()
        {
            columns = new List<TraceEntityType>()
            {   TraceEntityType.ProductRequirement,
                TraceEntityType.ProductRequirementsSpecification,
                TraceEntityType.SoftwareRequirement,
                TraceEntityType.RiskAssessmentRecord,
                TraceEntityType.ProductValidationPlan
            };
            truthSource = "Product";
            truthTarget = "Software";
            targetTruthEntity = TraceEntityType.SoftwareRequirement;
        }
    }
}
