using RoboClerk.Configuration;

namespace RoboClerk.ContentCreators
{
    class RiskTraceabilityMatrix : TraceabilityMatrixBase
    {
        public RiskTraceabilityMatrix(IDataSources data, ITraceabilityAnalysis analysis)
            : base(data, analysis)
        {

        }

        public override string GetContent(RoboClerkTag tag, DocumentConfig doc)
        {
            truthSource = analysis.GetTraceEntityForID("Risk");
            return base.GetContent(tag, doc);
        }
    }
}
