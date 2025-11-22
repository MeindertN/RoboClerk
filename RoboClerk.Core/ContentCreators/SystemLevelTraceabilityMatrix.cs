using RoboClerk.Core.Configuration;
using RoboClerk.Core;

namespace RoboClerk.ContentCreators
{
    class SystemLevelTraceabilityMatrix : TraceabilityMatrixBase
    {
        protected override string MatrixTypeName => "System Level";

        public SystemLevelTraceabilityMatrix(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration conf)
            : base(data, analysis, conf)
        {
        }

        /// <summary>
        /// Static metadata for the SystemLevelTraceabilityMatrix content creator
        /// </summary>
        public static ContentCreatorMetadata StaticMetadata { get; } = CreateMatrixMetadata("System Level");

        public override string GetContent(IRoboClerkTag tag, DocumentConfig doc)
        {
            truthSource = analysis.GetTraceEntityForID("SystemRequirement");
            return base.GetContent(tag, doc);
        }
    }
}
