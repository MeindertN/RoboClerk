using RoboClerk.Configuration;

namespace RoboClerk.ContentCreators
{
    class SystemLevelTraceabilityMatrix : TraceabilityMatrixBase
    {
        public SystemLevelTraceabilityMatrix(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration conf)
            : base(data, analysis, conf)
        {

        }

        public override string GetContent(RoboClerkTag tag, DocumentConfig doc)
        {
            truthSource = analysis.GetTraceEntityForID("SystemRequirement");
            return base.GetContent(tag, doc);
        }
    }
}
