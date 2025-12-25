using RoboClerk.Core.Configuration;
using RoboClerk.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk.ContentCreators
{
    public abstract class TraceabilityMatrixBase : IContentCreator
    {
        protected TraceEntity? truthSource = null;
        protected readonly IDataSources data;
        protected readonly ITraceabilityAnalysis analysis;
        protected readonly IConfiguration configuration;

        /// <summary>
        /// Gets the matrix type name for this specific traceability matrix (e.g., "System Level", "Software Level", "Risk")
        /// </summary>
        protected abstract string MatrixTypeName { get; }

        public TraceabilityMatrixBase(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration configuration)
        {
            this.data = data;
            this.analysis = analysis;
            this.configuration = configuration;
        }

        /// <summary>
        /// Creates static metadata for a traceability matrix content creator
        /// </summary>
        protected static ContentCreatorMetadata CreateMatrixMetadata(string matrixType)
        {
            var metadata = new ContentCreatorMetadata("SLMS", $"{matrixType} Traceability Matrix", 
                $"Generates a traceability matrix showing relationships between {matrixType.ToLower()} items and other entities")
            {
                Category = "Requirements & Traceability",
                Tags = new List<ContentCreatorTag>
                {
                    new ContentCreatorTag($"{matrixType.Replace(" ", "")}TraceabilityMatrix", $"Displays {matrixType.ToLower()} traceability matrix and trace issues", $"{matrixType.Replace(" ", "")}TraceabilityMatrix")
                    {
                        Category = "Traceability Analysis",
                        Description = $"Creates a comprehensive traceability matrix for {matrixType.ToLower()} showing all relationships and traces. " +
                            "The matrix displays trace relationships between different entity types and identifies any trace issues such as missing, extra, or incorrect traces. " +
                            "Can be filtered by project to focus on specific project items.",
                        Parameters = new List<ContentCreatorParameter>
                        {
                            new ContentCreatorParameter("ItemProject", 
                                "Filter items by project identifier", 
                                ParameterValueType.String, required: false)
                            {
                                ExampleValue = "MyProject",
                                Description = "Only include items from the specified project in the matrix. " +
                                    "Filtering is case-insensitive and applies to both rows and trace relationships."
                            }
                        },
                        ExampleUsage = $"@@SLMS:{matrixType.Replace(" ", "")}TraceabilityMatrix()@@"
                    }
                }
            };
            return metadata;
        }

        public virtual ContentCreatorMetadata GetMetadata()
        {
            // Use the matrix type name from the derived class
            return CreateMatrixMetadata(MatrixTypeName);
        }

        protected virtual bool ShouldIncludeItem(Item item, string projectFilter)
        {
            if (string.IsNullOrEmpty(projectFilter))
                return true;

            return string.Equals(item?.ItemProject, projectFilter, StringComparison.OrdinalIgnoreCase);
        }

        private string GetLinkString(Item item)
        {
            string format = configuration.OutputFormat.ToUpper();
            string result = item.ItemID;
            if (item.HasLink)
            {
                if (format == "HTML" || format == "DOCX")
                {
                    result = $"<a href=\"{item.Link}\">{item.ItemID}</a>";
                }
                else if (format == "ASCIIDOC")
                {
                    result = $"{item.Link}[{item.ItemID}]";
                }
            }
            return result;
        }

        public virtual string GetContent(IRoboClerkTag tag, DocumentConfig doc)
        {
            if (truthSource == null)
            {
                throw new Exception("Truth source is null, unclear where to start tracing.");
            }

            // Extract project filter parameter
            string projectFilter = string.Empty;
            if (tag.HasParameter("ItemProject"))
                projectFilter = tag.GetParameterOrDefault("ItemProject");

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
                // Check if the truth source item should be included based on project filter
                var truthItem = traceMatrix[truthSource][index].FirstOrDefault();
                if (!ShouldIncludeItem(truthItem, projectFilter))
                {
                    continue; // Skip this entire row if the truth item doesn't match the project filter
                }
                foreach (var entry in traceMatrix)
                {
                    if (entry.Value[index].Count == 0)
                    {
                        row.Add("N/A");
                    }
                    else
                    {
                        StringBuilder combinedString = new StringBuilder();
                        var filteredItems = entry.Value[index].Where(item => ShouldIncludeItem(item, projectFilter)).ToList();

                        if (filteredItems.Count == 0 && !string.IsNullOrEmpty(projectFilter))
                        {
                            row.Add("N/A");
                        }
                        else
                        {
                            foreach (Item item in filteredItems.Count > 0 ? filteredItems : entry.Value[index])
                            {
                                if (item == null)
                                {
                                    combinedString.Append("MISSING");
                                }
                                else
                                {
                                    if (entry.Key.EntityType == TraceEntityType.Document)
                                    {
                                        combinedString.Append("Trace Present");
                                    }
                                    else
                                    {
                                        combinedString.Append(GetLinkString(item));
                                    }
                                }
                                combinedString.Append(", ");
                            }
                            combinedString.Remove(combinedString.Length - 2, 2); //remove extra comma and space
                            row.Add(combinedString.ToString());
                        }
                    }
                }
                matrixData.Add(row);
            }

            // Collect trace issues (format-agnostic logic)
            var traceIssues = new List<string>();

            
            //now visualize the trace issues, first the truth
            var truthTraceIssues = analysis.GetTraceIssuesForTruth(truthSource);
            foreach (var issue in truthTraceIssues)
            {
                Item item = data.GetItem(issue.SourceID);
                // Only include trace issues for items that match the project filter
                if (ShouldIncludeItem(item, projectFilter))
                {

                    traceIssues.Add($"{truthSource.Name} {GetLinkString(item)} is potentially missing a corresponding {issue.Target.Name}.");
                }
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
                    Item item = data.GetItem(issue.SourceID);
                    // Only include trace issues for items that match the project filter
                    if (ShouldIncludeItem(item, projectFilter))
                    {
                        string sourceTitle = issue.Source.Name;
                        string targetTitle = issue.Target.Name;
                        string sourceID = issue.SourceID;
                        string targetID = issue.TargetID;
                        if (item != null)
                        {
                            sourceID = GetLinkString(item);
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
                                targetID = GetLinkString(targetItem);
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
            }
            
            // Generate format-specific output
            if (configuration.OutputFormat.ToUpper() == "HTML" || configuration.OutputFormat.ToUpper() == "DOCX")
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
            if (traceIssues.Count > 0)
            {
                matrix.AppendLine("\nTrace issues:\n");
                foreach (var issue in traceIssues)
                {
                    matrix.AppendLine($". {issue}");
                }
            }
            
            return matrix.ToString();
        }

        private string GenerateHTMLTraceabilityMatrix(List<string> headers, List<List<string>> matrixData, List<string> traceIssues)
        {
            StringBuilder matrix = new StringBuilder();
            matrix.AppendLine("<div>");
            matrix.AppendLine("    <table summary=\"Style:RoboClerk Table;SpaceAfter:0;SpaceBefore:0;FontName:Calibri;FontSize:11;TableSpacing:5;\" border=\"1\" cellspacing=\"0\" cellpadding=\"4\" style=\"width: 100%;\">");
            
            // Add headers
            matrix.AppendLine("        <tr>");
            foreach (var header in headers)
            {
                matrix.AppendLine($"            <td>{header}</td>");
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
            if (traceIssues.Count > 0)
            {
                //matrix.AppendLine("<p style=\"font-family:Calibri; font-size:12pt; margin:0;\">&nbsp;</p>");
                matrix.AppendLine("<div>");
                matrix.AppendLine("<table summary=\"SpaceAfter:0;SpaceBefore:0\" border=\"0\" cellspacing=\"0\" cellpadding=\"0\" style=\"border-collapse:collapse;width=100%\">");
                matrix.AppendLine("<tr><td colspan=\"2\"><p style=\"font-family:Calibri; font-size:12pt; margin:0;\">&nbsp;</p><strong>Trace issues:</strong></td></tr>");
                foreach (var issue in traceIssues)
                {
                    matrix.AppendLine("<tr>");
                    // Bullet Cell
                    matrix.AppendLine("<td valign=\"top\" width=\"30\" style=\"width:25pt; padding:0; padding-bottom:4pt; font-family:Arial;text-align: center;\">&#8226;</td>");
                    // Content Cell
                    matrix.AppendLine($"<td valign=\"top\" style=\"padding:0; padding-bottom:4pt; font-family:Calibri; font-size:11pt;\">{issue}</td>");
                    matrix.AppendLine("</tr>");
                }
                matrix.AppendLine("</table></div>");
            }
            return matrix.ToString();
        }
    }
}
