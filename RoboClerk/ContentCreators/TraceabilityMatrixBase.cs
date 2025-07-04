using RoboClerk.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk.ContentCreators
{
    public abstract class TraceabilityMatrixBase : IContentCreator
    {
        protected TraceEntity truthSource = null;
        protected IDataSources data = null;
        protected ITraceabilityAnalysis analysis = null;
        protected IConfiguration configuration = null;

        public TraceabilityMatrixBase(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration configuration)
        {
            this.data = data;
            this.analysis = analysis;
            this.configuration = configuration;
        }

        public virtual string GetContent(RoboClerkTag tag, DocumentConfig doc)
        {
            if (truthSource == null)
            {
                throw new Exception("Truth source is null, unclear where to start tracing.");
            }

            var traceMatrix = analysis.PerformAnalysis(data, truthSource);

            if (traceMatrix.Count == 0)
            {
                throw new Exception($"{truthSource.Name} level trace matrix is empty.");
            }

            // Collect matrix data (format-agnostic logic)
            var matrixData = new List<List<string>>();
            var headers = new List<string>();
            
            foreach (var entry in traceMatrix)
            {
                if (entry.Key.ID == "SystemRequirement" || entry.Key.ID == "SoftwareRequirement" || entry.Key.ID == "Risk")
                {
                    headers.Add($"{entry.Key.Name}s");
                }
                else
                {
                    headers.Add(entry.Key.Abbreviation);
                }
            }

            for (int index = 0; index < traceMatrix[truthSource].Count; ++index)
            {
                var row = new List<string>();
                foreach (var entry in traceMatrix)
                {
                    if (entry.Value[index].Count == 0)
                    {
                        row.Add("N/A");
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
                                if(entry.Key.EntityType == TraceEntityType.Document)
                                {
                                    combinedString.Append("Trace Present");
                                }
                                else
                                {
                                    combinedString.Append(item.HasLink ? $"{item.Link}[{item.ItemID}]" : item.ItemID);
                                }                                
                            }
                            combinedString.Append(", ");
                        }
                        combinedString.Remove(combinedString.Length - 2, 2); //remove extra comma and space
                        row.Add(combinedString.ToString());
                    }
                }
                matrixData.Add(row);
            }

            // Collect trace issues (format-agnostic logic)
            var traceIssues = new List<string>();
            bool traceIssuesFound = false;
            
            //now visualize the trace issues, first the truth
            var truthTraceIssues = analysis.GetTraceIssuesForTruth(truthSource);
            foreach (var issue in truthTraceIssues)
            {
                traceIssuesFound = true;
                Item item = data.GetItem(issue.SourceID);
                traceIssues.Add($"{truthSource.Name} {(item.HasLink ? $"{item.Link}[{item.ItemID}]" : item.ItemID)} is potentially missing a corresponding {issue.Target.Name}.");
            }

            foreach (var tet in traceMatrix)
            {
                if (tet.Key.ID == "SystemRequirement" || tet.Key.ID == "SoftwareRequirement" ||
                    tet.Key.ID == "SoftwareSystemTest" || tet.Key.ID == "UnitTest" ||
                    tet.Key.ID == "Anomaly" || tet.Key.ID == "Risk" ||
                    tet.Key.ID == "DocumentationRequirement" || tet.Key.ID == "DocContent" ||
                    tet.Key.ID == "SOUP") //skip the truth entity types
                {
                    continue;
                }

                var documentTraceIssues = analysis.GetTraceIssuesForDocument(tet.Key);
                foreach (var issue in documentTraceIssues)
                {
                    traceIssuesFound = true;
                    string sourceTitle = issue.Source.Name;
                    string targetTitle = issue.Target.Name;
                    Item item = data.GetItem(issue.SourceID);
                    string sourceID = issue.SourceID;
                    string targetID = issue.TargetID;
                    if (item != null)
                    {
                        sourceID = (item.HasLink ? $"{item.Link}[{item.ItemID}]" : item.ItemID);
                    }
                    if (issue.IssueType == TraceIssueType.Extra)
                    {
                        traceIssues.Add($"An item with identifier {sourceID} appeared in {sourceTitle} without tracing to {targetTitle}.");
                    }
                    else if (issue.IssueType == TraceIssueType.Missing)
                    {
                        traceIssues.Add($"An expected trace from {sourceID} in {sourceTitle} to {targetTitle} is missing.");
                    }
                    else if (issue.IssueType == TraceIssueType.PossiblyExtra)
                    {
                        traceIssues.Add($"A possibly extra item with identifier {sourceID} appeared in {sourceTitle} without appearing in {targetTitle}.");
                    }
                    else if (issue.IssueType == TraceIssueType.PossiblyMissing)
                    {
                        traceIssues.Add($"A possibly expected trace from {sourceID} in {sourceTitle} to {targetTitle} is missing.");
                    }
                    else if (issue.IssueType == TraceIssueType.Incorrect)
                    {
                        var targetItem = data.GetItem(targetID);
                        if (targetItem != null)
                        {
                            targetID = (targetItem.HasLink ? $"{targetItem.Link}[{targetItem.ItemID}]" : targetItem.ItemID);
                            traceIssues.Add($"An incorrect trace was found in {sourceTitle} from {sourceID} to {targetID} where {targetID} was expected in {targetTitle} but was not found.");
                        }
                        else if (targetID != null)
                        {
                            traceIssues.Add($"An incorrect trace was found in {sourceTitle} from {sourceID} to {targetID} where {targetID} was expected in {targetTitle} but was not a valid identifier.");
                        }
                        else
                        {
                            traceIssues.Add($"A missing trace was detected in {sourceTitle}. The item with ID {sourceID} does not have a parent while it was expected to trace to {targetTitle}.");
                        }
                    }
                }
            }
            
            if (!traceIssuesFound)
            {
                traceIssues.Add($"No {truthSource.Name} level trace problems detected!");
            }

            // Generate format-specific output
            if (configuration.OutputFormat.ToUpper() == "HTML")
            {
                return GenerateHTMLTraceabilityMatrix(headers, matrixData, traceIssues);
            }
            else
            {
                return GenerateASCIIDocTraceabilityMatrix(headers, matrixData, traceIssues);
            }
        }

        private string GenerateASCIIDocTraceabilityMatrix(List<string> headers, List<List<string>> matrixData, List<string> traceIssues)
        {
            StringBuilder matrix = new StringBuilder();
            matrix.AppendLine("|====");
            
            // Add headers
            foreach (var header in headers)
            {
                matrix.Append($"| {header} ");
            }
            matrix.AppendLine();

            // Add matrix data
            foreach (var row in matrixData)
            {
                foreach (var cell in row)
                {
                    matrix.Append($"| {cell} ");
                }
                matrix.AppendLine();
            }
            matrix.AppendLine("|====");
            matrix.AppendLine();

            // Add trace issues
            matrix.AppendLine("\nTrace issues:\n");
            foreach (var issue in traceIssues)
            {
                matrix.AppendLine($". {issue}");
            }
            
            return matrix.ToString();
        }

        private string GenerateHTMLTraceabilityMatrix(List<string> headers, List<List<string>> matrixData, List<string> traceIssues)
        {
            StringBuilder matrix = new StringBuilder();
            matrix.AppendLine("<div>");
            matrix.AppendLine("    <table border=\"1\" cellspacing=\"0\" cellpadding=\"4\">");
            
            // Add headers
            matrix.AppendLine("        <tr>");
            foreach (var header in headers)
            {
                matrix.AppendLine($"            <th>{header}</th>");
            }
            matrix.AppendLine("        </tr>");

            // Add matrix data
            foreach (var row in matrixData)
            {
                matrix.AppendLine("        <tr>");
                foreach (var cell in row)
                {
                    matrix.AppendLine($"            <td>{cell}</td>");
                }
                matrix.AppendLine("        </tr>");
            }
            matrix.AppendLine("    </table>");
            matrix.AppendLine("</div>");
            matrix.AppendLine();

            // Add trace issues
            matrix.AppendLine("<div>");
            matrix.AppendLine("    <h3>Trace issues:</h3>");
            matrix.AppendLine("    <ul>");
            foreach (var issue in traceIssues)
            {
                matrix.AppendLine($"        <li>{issue}</li>");
            }
            matrix.AppendLine("    </ul>");
            matrix.AppendLine("</div>");
            
            return matrix.ToString();
        }
    }
}
