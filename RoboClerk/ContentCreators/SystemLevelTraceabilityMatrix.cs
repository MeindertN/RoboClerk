using RoboClerk.Configuration;

namespace RoboClerk.ContentCreators
{
    class SystemLevelTraceabilityMatrix : TraceabilityMatrixBase
    {
        public override string GetContent(RoboClerkTag tag, IDataSources data, ITraceabilityAnalysis analysis, DocumentConfig doc)
        {
            truthSource = analysis.GetTraceEntityForID("SystemRequirement");
            return base.GetContent(tag, data, analysis, doc);
        }
    }
}
