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
            truthSource = TraceEntityType.SystemRequirement;
            truthTarget = TraceEntityType.SoftwareRequirement;
        }
    }
}
