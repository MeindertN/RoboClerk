using System;
using System.Text;

namespace RoboClerk
{
    public class AnomalyItem : Item
    {
        private string anomalyState = string.Empty;
        private string anomalyID = string.Empty;
        private string anomalyTitle = string.Empty;
        private string anomalyRevision = string.Empty;
        private string anomalyAssignee = string.Empty;
        private string anomalyPriority = string.Empty;
        private string anomalyJustification = string.Empty;

        public AnomalyItem()
        {
            type = "AnomalyItem";
            id = Guid.NewGuid().ToString();
        }

        public override string ToText()
        {
            StringBuilder sb = new StringBuilder();
            int[] columnWidths = new int[2] { 44, 160 };
            string separator = MarkdownTableUtils.GenerateGridTableSeparator(columnWidths);
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "Anomaly ID:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, HasLink ? $"[{anomalyID}]({link})" : anomalyID));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "Revision:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, anomalyRevision == string.Empty ? "N/A" : anomalyRevision));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "State:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, anomalyState == string.Empty ? "N/A" : anomalyState));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "Assigned To:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, anomalyAssignee == string.Empty ? "NOT ASSIGNED" : anomalyAssignee));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "Title:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, anomalyTitle == string.Empty ? "MISSING" : anomalyTitle));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "Priority:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, anomalyPriority == string.Empty ? "N/A" : anomalyPriority));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "Justification:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, anomalyJustification == string.Empty ? "N/A" : anomalyJustification));
            return sb.ToString();
        }

        public string AnomalyState
        {
            get => anomalyState;
            set => anomalyState = value;
        }

        public string AnomalyID
        {
            get => anomalyID;
            set
            {
                id = value;
                anomalyID = value;
            }
        }

        public string AnomalyTitle
        {
            get => anomalyTitle;
            set => anomalyTitle = value;
        }

        public string AnomalyRevision
        {
            get => anomalyRevision;
            set => anomalyRevision = value;
        }

        public string AnomalyAssignee
        {
            get => anomalyAssignee;
            set => anomalyAssignee = value;
        }

        public string AnomalyPriority
        {
            get => anomalyPriority;
            set => anomalyPriority = value;
        }

        public string AnomalyJustification
        {
            get => anomalyJustification;
            set => anomalyJustification = value;
        }
    }
}
