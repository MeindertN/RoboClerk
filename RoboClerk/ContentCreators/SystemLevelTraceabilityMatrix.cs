namespace RoboClerk.ContentCreators
{
    class SystemLevelTraceabilityMatrix : TraceabilityMatrixBase
    {
        public override string GetContent(RoboClerkTag tag, IDataSources data, ITraceabilityAnalysis analysis, string docTitle)
        {
            truthTarget = analysis.GetTraceEntityForID("SoftwareRequirement");
            truthSource = analysis.GetTraceEntityForID("SystemRequirement");
            return base.GetContent(tag, data, analysis, docTitle);
        }
    }
}
