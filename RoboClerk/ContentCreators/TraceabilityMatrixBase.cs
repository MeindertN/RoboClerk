using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk.ContentCreators
{
    abstract class TraceabilityMatrixBase : IContentCreator
    {
        protected List<TraceEntityType> columns = new List<TraceEntityType>();
        protected string truthSource = string.Empty;
        protected string truthTarget = string.Empty;
        protected TraceEntityType targetTruthEntity = TraceEntityType.Unknown;

        public TraceabilityMatrixBase()
        {

        }

        public virtual string GetContent(RoboClerkTag tag, DataSources data, TraceabilityAnalysis analysis, string docTitle)
        {
            var traceMatrix = analysis.PerformAnalysis(data, columns);

            if (traceMatrix.Count == 0)
            {
                throw new Exception($"{truthSource} level trace matrix is empty.");
            }

            StringBuilder matrix = new StringBuilder();
            matrix.Append(MarkdownTableUtils.GenerateTraceMatrixHeader(traceMatrix[0]));

            for (int i = 1; i < traceMatrix.Count; ++i)
            {
                matrix.Append(MarkdownTableUtils.GenerateTraceMatrixLine(traceMatrix[i]));
            }
            matrix.AppendLine("\nTrace issues:");
            bool traceIssuesFound = false;
            //now visualize the trace issues, first the truth
            var truthIssues = analysis.GetTraceIssuesForTruth(targetTruthEntity);
            foreach (var issue in truthIssues)
            {
                traceIssuesFound = true;
                matrix.AppendLine($"* {truthSource} requirement {issue.TraceID} is potentially missing a corresponding {truthTarget} requirement.");
            }

            foreach (var tet in columns)
            {
                if (tet == TraceEntityType.ProductRequirement || tet == TraceEntityType.SoftwareRequirement || tet == TraceEntityType.TestCase) //skip the truth entity types
                {
                    continue;
                }

                var traceIssues = analysis.GetTraceIssuesForDocument(tet);
                foreach (var issue in traceIssues)
                {
                    traceIssuesFound = true;
                    string sourceTitle = analysis.GetTitleForTraceEntity(issue.Source);
                    string targetTitle = analysis.GetTitleForTraceEntity(issue.Target);
                    if (issue.IssueType == TraceIssueType.Extra)
                    {
                        matrix.AppendLine($"* An extra item with identifier \"{issue.TraceID}\" appeared in \"{sourceTitle}\" without appearing in \"{targetTitle}\".");
                    }
                    else if (issue.IssueType == TraceIssueType.Missing)
                    {
                        matrix.AppendLine($"* An expected trace from \"{issue.TraceID}\" in \"{sourceTitle}\" to \"{targetTitle}\" is missing.");
                    }
                    else if (issue.IssueType == TraceIssueType.PossiblyExtra)
                    {
                        matrix.AppendLine($"* A possibly extra item with identifier \"{issue.TraceID}\" appeared in \"{sourceTitle}\" without appearing in {targetTitle}.");
                    }
                    else if (issue.IssueType == TraceIssueType.PossiblyMissing)
                    {
                        matrix.AppendLine($"* A possibly expected trace from \"{issue.TraceID}\" in \"{sourceTitle}\" to \"{targetTitle}\" is missing.");
                    }
                }
            }
            if(!traceIssuesFound)
            {
                matrix.AppendLine($"* No {truthSource} level trace problems detected!");
            }
            return matrix.ToString();
        }
    }
}
