namespace RoboClerk.ContentCreators
{
    class SoftwareLevelTraceabilityMatrix : TraceabilityMatrixBase
    {
        public override string GetContent(RoboClerkTag tag, IDataSources data, ITraceabilityAnalysis analysis, string docTitle)
        {
            truthSource = analysis.GetTraceEntityForID("SoftwareRequirement");
            truthTarget = analysis.GetTraceEntityForID("SystemRequirement");
            return base.GetContent(tag, data, analysis, docTitle);
        }
    }
}
