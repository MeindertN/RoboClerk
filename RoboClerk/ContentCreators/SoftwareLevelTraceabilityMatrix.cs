using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk.ContentCreators
{
    class SoftwareLevelTraceabilityMatrix : TraceabilityMatrixBase
    {
        public SoftwareLevelTraceabilityMatrix()
        {
            columns = new List<TraceEntityType>()
            {   TraceEntityType.SoftwareRequirement,
                TraceEntityType.SoftwareRequirementsSpecification,
                TraceEntityType.ProductRequirement,
                TraceEntityType.SoftwareDesignSpecification,
                TraceEntityType.RiskAssessmentRecord,
                TraceEntityType.SystemLevelTestPlan,
                TraceEntityType.IntegrationLevelTestPlan,
                TraceEntityType.UnitLevelTestPlan
            };
            truthSource = "Software";
            truthTarget = "Product";
            targetTruthEntity = TraceEntityType.ProductRequirement;
        }
    }
}
