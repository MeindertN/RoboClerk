using RoboClerk.Configuration;
using System.Text;

namespace RoboClerk.ContentCreators
{
    class Anomaly : ContentCreatorBase
    {

        private string GenerateMarkdown(AnomalyItem item, TraceEntity tet)
        {
            StringBuilder sb = new StringBuilder();
            int[] columnWidths = new int[2] { 44, 160 };
            string separator = MarkdownTableUtils.GenerateGridTableSeparator(columnWidths);
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], $"{(tet==null?"Anomaly":tet.Name)} ID:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, item.HasLink ? $"[{item.ItemID}]({item.Link})" : item.ItemID));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "Revision:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, item.AnomalyRevision == string.Empty ? "N/A" : item.AnomalyRevision));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "State:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, item.AnomalyState == string.Empty ? "N/A" : item.AnomalyState));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "Assigned To:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, item.AnomalyAssignee == string.Empty ? "NOT ASSIGNED" : item.AnomalyAssignee));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "Title:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, item.AnomalyTitle == string.Empty ? "MISSING" : item.AnomalyTitle));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "Priority:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, item.AnomalyPriority == string.Empty ? "N/A" : item.AnomalyPriority));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "Justification:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, item.AnomalyJustification == string.Empty ? "N/A" : item.AnomalyJustification));
            return sb.ToString();
        }

        public override string GetContent(RoboClerkTag tag, IDataSources data, ITraceabilityAnalysis analysis, DocumentConfig doc)
        {
            var anomalies = data.GetAllAnomalies();
            StringBuilder output = new StringBuilder();
            foreach (var anomaly in anomalies)
            {
                if (anomaly.AnomalyState == "Closed")
                {
                    continue; //skip closed bugs as they are no longer outstanding
                }
                try
                {
                    output.AppendLine(GenerateMarkdown(anomaly,analysis.GetTraceEntityForID("Anomaly")));
                }
                catch
                {
                    logger.Error($"An error occurred while rendering anomaly {anomaly.ItemID} in {doc.DocumentTitle}.");
                    throw;
                }
            }
            if (anomalies.Count == 0)
            {
                return $"No outstanding bugs found.";
            }
            return output.ToString();
        }
    }
}
