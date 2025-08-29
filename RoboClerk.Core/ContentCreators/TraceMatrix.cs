using RoboClerk.Configuration;
using RoboClerk.Core;

namespace RoboClerk.ContentCreators
{
    public class TraceMatrix : TraceabilityMatrixBase
    {
        public TraceMatrix(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration conf)
            : base(data, analysis, conf)
        {

        }

        public override string GetContent(IRoboClerkTag tag, DocumentConfig doc)
        {
            string ts = tag.GetParameterOrDefault("source", "not_found");
            if (ts == "not_found")
            {
                throw new System.Exception($"Unable to find trace source. Ensure that the trace source is specified in all the \"TraceMatrix\" calls in {doc.DocumentTitle}.");
            }
            truthSource = analysis.GetTraceEntityForID(ts);

            return base.GetContent(tag, doc);
        }
    }
}
