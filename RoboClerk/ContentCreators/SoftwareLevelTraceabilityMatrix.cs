using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk.ContentCreators
{
    class SoftwareLevelTraceabilityMatrix : TraceabilityMatrixBase
    {
        public SoftwareLevelTraceabilityMatrix()
        {
            truthSource = TraceEntityType.SoftwareRequirement; 
            truthTarget = TraceEntityType.ProductRequirement; 
        }
    }
}
