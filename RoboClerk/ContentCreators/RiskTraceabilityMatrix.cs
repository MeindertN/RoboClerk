using RoboClerk.Configuration;

namespace RoboClerk.ContentCreators
{
    class RiskTraceabilityMatrix : TraceabilityMatrixBase
    {
        public override string GetContent(RoboClerkTag tag, IDataSources data, ITraceabilityAnalysis analysis, DocumentConfig doc)
        {
            truthSource = analysis.GetTraceEntityForID("Risk");
            return base.GetContent(tag, data, analysis, doc);
        }
    }
}
