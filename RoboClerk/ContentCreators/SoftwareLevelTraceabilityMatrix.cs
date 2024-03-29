﻿using RoboClerk.Configuration;

namespace RoboClerk.ContentCreators
{
    class SoftwareLevelTraceabilityMatrix : TraceabilityMatrixBase
    {
        public SoftwareLevelTraceabilityMatrix(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration conf)
            : base(data, analysis, conf)
        {

        }

        public override string GetContent(RoboClerkTag tag, DocumentConfig doc)
        {
            truthSource = analysis.GetTraceEntityForID("SoftwareRequirement");
            return base.GetContent(tag, doc);
        }
    }
}
