using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk
{
    public class BugItem : Item
    {
        private string bugState = string.Empty;
        private string bugID = string.Empty;
        private string bugTitle = string.Empty;
        private string bugRevision = string.Empty;
        private string bugAssignee = string.Empty;
        private string bugPriority = string.Empty;
        private string bugJustification = string.Empty;

        public BugItem()
        {
            type = "BugItem";
            id = Guid.NewGuid().ToString();
        }

        public override string ToText()
        {
            StringBuilder sb = new StringBuilder();
            int[] columnWidths = new int[2] { 44, 160 };
            string separator = MarkdownTableUtils.GenerateGridTableSeparator(columnWidths);
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "Bug ID:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, HasLink ? $"[{bugID}]({link})" : bugID));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "Bug Revision:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, bugRevision == string.Empty ? "N/A": bugRevision));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "Bug State:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, bugState == string.Empty ? "N/A": bugState));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "Assigned To:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, bugAssignee == string.Empty ? "NOT ASSIGNED": bugAssignee));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "Bug Title:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, bugTitle == string.Empty ? "MISSING": bugTitle));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "Bug Priority:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, bugPriority == string.Empty ? "N/A" : bugPriority));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "Bug Justification:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, bugJustification == string.Empty ? "N/A" : bugJustification));
            return sb.ToString();
        }

        public string BugState
        {
            get => bugState;
            set => bugState = value;
        }

        public string BugID
        {
            get => bugID;
            set
            {
                id = value;
                bugID = value;
            }
        }

        public string BugTitle
        {
            get => bugTitle;
            set => bugTitle = value;
        }

        public string BugRevision
        {
            get => bugRevision;
            set => bugRevision = value;
        }

        public string BugAssignee
        {
            get => bugAssignee;
            set => bugAssignee = value;
        }

        public string BugPriority
        {
            get => bugPriority;
            set => bugPriority = value;
        }

        public string BugJustification
        {
            get => bugJustification;
            set => bugJustification = value;
        }
    }
}
