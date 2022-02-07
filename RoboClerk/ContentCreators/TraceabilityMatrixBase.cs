using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk.ContentCreators
{
    abstract class TraceabilityMatrixBase : IContentCreator
    {
        protected TraceEntity truthSource = null;
        protected TraceEntity truthTarget = null;

        public TraceabilityMatrixBase()
        {

        }

        public virtual string GetContent(RoboClerkTag tag, DataSources data, TraceabilityAnalysis analysis, string docTitle)
        {
            var traceMatrix = analysis.PerformAnalysis(data, truthSource);

            if (traceMatrix.Count == 0)
            {
                throw new Exception($"{truthSource} level trace matrix is empty.");
            }

            List<string> columnHeaders = new List<string>();
            foreach (var entry in traceMatrix)
            {
                if (entry.Key.ID == "SystemRequirement" || entry.Key.ID == "SoftwareRequirement")
                {
                    columnHeaders.Add($"{entry.Key.Name}s");
                }
                else
                {
                    columnHeaders.Add(entry.Key.Abbreviation);
                }
            }

            StringBuilder matrix = new StringBuilder();
            matrix.Append(MarkdownTableUtils.GenerateTraceMatrixHeader(columnHeaders));

            for (int index = 0; index < traceMatrix[truthSource].Count; ++index)
            {
                List<string> line = new List<string>();
                foreach (var entry in traceMatrix)
                {
                    if (entry.Value[index].Count == 0)
                    {
                        line.Add("N/A");
                    }
                    else
                    {
                        StringBuilder combinedString = new StringBuilder();
                        foreach (Item item in entry.Value[index])
                        {
                            if (item == null)
                            {
                                combinedString.Append("MISSING");
                            }
                            else
                            {
                                combinedString.Append(item.HasLink ? $"[{item.ItemID}]({item.Link})" : item.ItemID);
                            }
                            combinedString.Append(", ");
                        }
                        combinedString.Remove(combinedString.Length - 2, 2); //remove extra comma and space
                        line.Add(combinedString.ToString());
                    }
                }
                matrix.Append(MarkdownTableUtils.GenerateTraceMatrixLine(line));
            }

            matrix.AppendLine("\nTrace issues:\n");
            bool traceIssuesFound = false;
            //now visualize the trace issues, first the truth
            var truthIssues = analysis.GetTraceIssuesForTruth(truthSource);
            foreach (var issue in truthIssues)
            {
                traceIssuesFound = true;
                Item item = data.GetItem(issue.TraceID);
                matrix.AppendLine($"* {truthSource.Name} {(item.HasLink ? $"[{item.ItemID}]({item.Link})" : item.ItemID)} is potentially missing a corresponding {truthTarget.Name}.");
            }

            foreach (var tet in traceMatrix)
            {
                if (tet.Key.ID == "SystemRequirement" || tet.Key.ID == "SoftwareRequirement" ||
                    tet.Key.ID == "SoftwareSystemTest" || tet.Key.ID == "SoftwareUnitTest" ||
                    tet.Key.ID == "Anomaly") //skip the truth entity types
                {
                    continue;
                }

                var traceIssues = analysis.GetTraceIssuesForDocument(tet.Key.Name);
                foreach (var issue in traceIssues)
                {
                    traceIssuesFound = true;
                    string sourceTitle = issue.Source.Name;
                    string targetTitle = issue.Target.Name;
                    Item item = data.GetItem(issue.TraceID);
                    string identifierText = issue.TraceID;
                    if (item != null)
                    {
                        identifierText = (item.HasLink ? $"[{item.ItemID}]({item.Link})" : item.ItemID);
                    }
                    if (issue.IssueType == TraceIssueType.Extra)
                    {
                        matrix.AppendLine($"* An extra item with identifier {identifierText} appeared in {sourceTitle} without appearing in {targetTitle}.");
                    }
                    else if (issue.IssueType == TraceIssueType.Missing)
                    {
                        matrix.AppendLine($"* An expected trace from {identifierText} in {sourceTitle} to {targetTitle} is missing.");
                    }
                    else if (issue.IssueType == TraceIssueType.PossiblyExtra)
                    {
                        matrix.AppendLine($"* A possibly extra item with identifier {identifierText} appeared in {sourceTitle} without appearing in {targetTitle}.");
                    }
                    else if (issue.IssueType == TraceIssueType.PossiblyMissing)
                    {
                        matrix.AppendLine($"* A possibly expected trace from {identifierText} in {sourceTitle} to {targetTitle} is missing.");
                    }
                }
            }
            if (!traceIssuesFound)
            {
                matrix.AppendLine($"* No {truthSource.Name} level trace problems detected!");
            }
            return matrix.ToString();
        }
    }
}
