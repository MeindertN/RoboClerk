using RoboClerk.Core.Configuration;
using RoboClerk.Core;

namespace RoboClerk.ContentCreators
{
    class RiskTraceabilityMatrix : TraceabilityMatrixBase
    {
        protected override string MatrixTypeName => "Risk";

        public RiskTraceabilityMatrix(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration conf)
            : base(data, analysis, conf)
        {
        }

        /// <summary>
        /// Static metadata for the RiskTraceabilityMatrix content creator
        /// </summary>
        public static ContentCreatorMetadata StaticMetadata { get; } = CreateMatrixMetadata("Risk");

        public override string GetContent(IRoboClerkTag tag, DocumentConfig doc)
        {
            truthSource = analysis.GetTraceEntityForID("Risk");
            return base.GetContent(tag, doc);
        }
    }
}
