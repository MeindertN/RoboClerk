using RoboClerk.Configuration;

namespace RoboClerk.ContentCreators
{
    class SoftwareLevelTraceabilityMatrix : TraceabilityMatrixBase
    {
        public override string GetContent(RoboClerkTag tag, IDataSources data, ITraceabilityAnalysis analysis, DocumentConfig doc)
        {
            truthSource = analysis.GetTraceEntityForID("SoftwareRequirement");
            truthTarget = analysis.GetTraceEntityForID("SystemRequirement");
            return base.GetContent(tag, data, analysis, doc);
        }
    }
}
