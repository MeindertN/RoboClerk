using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RoboClerk.ContentCreators
{
    public class ProductLevelTraceabilityMatrix : IContentCreator
    {
        public ProductLevelTraceabilityMatrix()
        {

        }

        public string GetContent(DataSources data, TraceabilityAnalysis analysis, string docTitle)
        {
            List<TraceEntityType> columns = new List<TraceEntityType>()
            {   TraceEntityType.ProductRequirement,
                TraceEntityType.ProductRequirementsSpecification,
                TraceEntityType.SoftwareRequirement,
                TraceEntityType.RiskAssessmentRecord,
                TraceEntityType.ProductValidationPlan
            };
            var traceMatrix = analysis.PerformAnalysis(data, columns);

            if(traceMatrix.Count == 0)
            {
                throw new Exception("Product level trace matrix is empty.");
            }

            StringBuilder matrix = new StringBuilder();
            matrix.Append(MarkdownTableUtils.GenerateTraceMatrixHeader(traceMatrix[0]));

            for (int i = 1; i < traceMatrix.Count; ++i)
            {
                matrix.Append(MarkdownTableUtils.GenerateTraceMatrixLine(traceMatrix[i]));
            }
            matrix.AppendLine("\nTrace issues:");
            //now visualize the trace issues, first the truth
            var truthIssues = analysis.GetTraceIssuesForTruth(TraceEntityType.SoftwareRequirement);
            foreach( var issue in truthIssues)
            {
                matrix.AppendLine($"* Product requirement {issue.TraceID} is potentially missing a corresponding software requirement.");
            }
            
            foreach( var tet in columns )
            {
                if(tet == TraceEntityType.ProductRequirement || tet == TraceEntityType.SoftwareRequirement || tet == TraceEntityType.TestCase) //skip the truth entity types
                {
                    continue;
                }

                var traceIssues = analysis.GetTraceIssuesForDocument(tet);
                foreach( var issue in traceIssues)
                {
                    string sourceTitle = analysis.GetTitleForTraceEntity(issue.Source);
                    string targetTitle = analysis.GetTitleForTraceEntity(issue.Target);
                    if( issue.IssueType == TraceIssueType.Extra )
                    {
                        matrix.AppendLine($"* An extra item with identifier {issue.TraceID} appeared in {sourceTitle} without appearing in {targetTitle}.");
                    }
                    else if( issue.IssueType == TraceIssueType.Missing )
                    {
                        matrix.AppendLine($"* An expected trace from {issue.TraceID} in {sourceTitle} to {targetTitle} is missing.");
                    }
                    else if( issue.IssueType == TraceIssueType.PossiblyExtra )
                    {
                        matrix.AppendLine($"* A possibly extra item with identifier {issue.TraceID} appeared in {sourceTitle} without appearing in {targetTitle}.");
                    }
                    else if( issue.IssueType == TraceIssueType.PossiblyMissing )
                    {
                        matrix.AppendLine($"* A possibly expected trace from {issue.TraceID} in {sourceTitle} to {targetTitle} is missing.");
                    }
                }
            }

            return matrix.ToString();
        }
    }
}
