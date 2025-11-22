using RoboClerk.Core.Configuration;
using RoboClerk.Core;

namespace RoboClerk.ContentCreators
{
    class SoftwareLevelTraceabilityMatrix : TraceabilityMatrixBase
    {
        protected override string MatrixTypeName => "Software Level";

        public SoftwareLevelTraceabilityMatrix(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration conf)
            : base(data, analysis, conf)
        {
        }

        /// <summary>
        /// Static metadata for the SoftwareLevelTraceabilityMatrix content creator
        /// </summary>
        public static ContentCreatorMetadata StaticMetadata { get; } = CreateMatrixMetadata("Software Level");

        public override string GetContent(IRoboClerkTag tag, DocumentConfig doc)
        {
            truthSource = analysis.GetTraceEntityForID("SoftwareRequirement");
            return base.GetContent(tag, doc);
        }
    }
}
