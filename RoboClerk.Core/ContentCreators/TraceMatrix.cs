using RoboClerk.Core.Configuration;
using RoboClerk.Core;

namespace RoboClerk.ContentCreators
{
    public class TraceMatrix : TraceabilityMatrixBase
    {
        // Dynamic matrix type based on the truth source
        protected override string MatrixTypeName => truthSource?.Name ?? "Traceability";

        public TraceMatrix(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration conf)
            : base(data, analysis, conf)
        {
        }

        /// <summary>
        /// Static metadata for the generic TraceMatrix content creator
        /// </summary>
        public static ContentCreatorMetadata StaticMetadata { get; } = new ContentCreatorMetadata(
            "SLMS",
            "Generic Traceability Matrix",
            "Generates a traceability matrix for any truth source specified via the 'source' parameter")
        {
            Category = "Requirements & Traceability",
            Tags = new List<ContentCreatorTag>
            {
                new ContentCreatorTag("TraceMatrix", "Displays traceability matrix for a specified source")
                {
                    Category = "Traceability Analysis",
                    Description = "Creates a comprehensive traceability matrix for any entity type by specifying the source parameter. " +
                        "The matrix displays trace relationships between different entity types and identifies any trace issues such as missing, extra, or incorrect traces. " +
                        "Can be filtered by project to focus on specific project items.",
                    Parameters = new List<ContentCreatorParameter>
                    {
                        new ContentCreatorParameter("source", 
                            "The trace entity to use as the truth source (e.g., 'SystemRequirement', 'SoftwareRequirement', 'Risk')", 
                            ParameterValueType.String, required: true)
                        {
                            ExampleValue = "SystemRequirement",
                            Description = "Specifies which entity type to use as the basis for the traceability matrix"
                        },
                        new ContentCreatorParameter("ItemProject", 
                            "Filter items by project identifier", 
                            ParameterValueType.String, required: false)
                        {
                            ExampleValue = "MyProject",
                            Description = "Only include items from the specified project in the matrix. " +
                                "Filtering is case-insensitive and applies to both rows and trace relationships."
                        }
                    },
                    ExampleUsage = "@@SLMS:TraceMatrix(source=SystemRequirement)@@"
                }
            }
        };

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
