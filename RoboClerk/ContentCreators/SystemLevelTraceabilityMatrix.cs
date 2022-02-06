using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RoboClerk.ContentCreators
{
    class SystemLevelTraceabilityMatrix : TraceabilityMatrixBase
    {
        public override string GetContent(RoboClerkTag tag, DataSources data, TraceabilityAnalysis analysis, string docTitle)
        {
            truthTarget = analysis.GetTraceEntityForID("SoftwareRequirement");
            truthSource = analysis.GetTraceEntityForID("SystemRequirement");
            return base.GetContent(tag, data, analysis, docTitle);
        }
    }
}
