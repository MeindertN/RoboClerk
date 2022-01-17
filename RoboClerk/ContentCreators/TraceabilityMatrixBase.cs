using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk.ContentCreators
{
    abstract class TraceabilityMatrixBase : IContentCreator
    {
        protected TraceEntityType truthSource = TraceEntityType.Unknown;
        protected TraceEntityType truthTarget = TraceEntityType.Unknown;

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

            //determine the columns
            TraceEntityType baseDoc = (truthSource == TraceEntityType.SoftwareRequirement ? 
                TraceEntityType.SoftwareRequirementsSpecification : TraceEntityType.ProductRequirementsSpecification);
            List<TraceEntityType> columns = new List<TraceEntityType>() { truthSource, baseDoc };
            
            foreach(KeyValuePair<TraceEntityType,List<List<Item>>> entry in traceMatrix)
            {
                if(!columns.Contains(entry.Key))
                {
                    columns.Add(entry.Key);
                }
            }

            List<string> columnHeaders = new List<string>();
            foreach(var entry in columns)
            {
                if (entry == TraceEntityType.ProductRequirement || entry == TraceEntityType.SoftwareRequirement)
                {
                    columnHeaders.Add(analysis.GetTitleForTraceEntity(entry));
                }
                else
                {
                    columnHeaders.Add(analysis.GetAbreviationForTraceEntity(entry));
                }
            }

            StringBuilder matrix = new StringBuilder();
            matrix.Append(MarkdownTableUtils.GenerateTraceMatrixHeader(columnHeaders));

            for( int index = 0; index<traceMatrix[truthSource].Count; ++index)
            {
                List<string> line = new List<string>();
                foreach(var entry in columns)
                {
                    if (traceMatrix[entry][index].Count == 0)
                    {
                        line.Add("N/A");
                    }
                    else
                    {
                        StringBuilder combinedString = new StringBuilder();
                        foreach (Item item in traceMatrix[entry][index])
                        {
                            if (item == null)
                            {
                                combinedString.Append("MISSING");
                            }
                            else
                            {
                                combinedString.Append(item.HasLink ? $"[{item.ItemID}]({item.Link})" : item.ItemID);
                            }
                            combinedString.Append(',');
                        }
                        combinedString.Remove(combinedString.Length - 1, 1); //remove extra comma
                        line.Add(combinedString.ToString());
                    }
                }
                matrix.Append(MarkdownTableUtils.GenerateTraceMatrixLine(line));
            }

            matrix.AppendLine("\nTrace issues:");
            bool traceIssuesFound = false;
            //now visualize the trace issues, first the truth
            var truthIssues = analysis.GetTraceIssuesForTruth(truthSource);
            foreach (var issue in truthIssues)
            {
                traceIssuesFound = true;
                Item item = data.GetItem(issue.TraceID);
                matrix.AppendLine($"* {analysis.GetTitleForTraceEntity(truthSource)} {(item.HasLink ? $"[{item.ItemID}]({item.Link})" : item.ItemID)} is potentially missing a corresponding {analysis.GetTitleForTraceEntity(truthTarget)}.");
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
                    Item item = data.GetItem(issue.TraceID);
                    string identifierText = issue.TraceID;
                    if(item != null)
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
            if(!traceIssuesFound)
            {
                matrix.AppendLine($"* No {truthSource} level trace problems detected!");
            }
            return matrix.ToString();
        }
    }
}
