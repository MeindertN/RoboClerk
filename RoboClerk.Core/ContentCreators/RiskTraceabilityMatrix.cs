using RoboClerk.Configuration;
using RoboClerk.Core;

namespace RoboClerk.ContentCreators
{
    class RiskTraceabilityMatrix : TraceabilityMatrixBase
    {
        public RiskTraceabilityMatrix(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration conf)
            : base(data, analysis, conf)
        {

        }

        public override string GetContent(IRoboClerkTag tag, DocumentConfig doc)
        {
            truthSource = analysis.GetTraceEntityForID("Risk");
            return base.GetContent(tag, doc);
        }
    }
}
