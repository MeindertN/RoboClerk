using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk
{
    public class TestCaseItem : TraceItem
    {
        private string testCaseState = "";
        private string testCaseID = "";
        private Uri testCaseLink;
        private string testCaseParentID = "";
        private Uri testCaseParentLink;
        private string testCaseTitle = "";
        private string testCaseDescription = "";
        private string testCaseRevision = "";
        private List<string[]> testCaseSteps = new List<string[]>();
        public TestCaseItem()
        {
            type = "TestCaseItem";
            id = Guid.NewGuid().ToString();
        }

        public override string ToMarkDown()
        {
            StringBuilder sb = new StringBuilder();
            int[] columnWidths = new int[2] { 22, 80 };
            string separator = MarkdownTableUtils.generateTableSeparator(columnWidths);
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.generateLeftMostTableCell(22, "testCase ID:"));
            sb.Append(MarkdownTableUtils.generateRightMostTableCell(columnWidths, testCaseID));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.generateLeftMostTableCell(22, "testCase Revision:"));
            sb.Append(MarkdownTableUtils.generateRightMostTableCell(columnWidths, testCaseRevision));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.generateLeftMostTableCell(22, "Parent ID:"));
            sb.Append(MarkdownTableUtils.generateRightMostTableCell(columnWidths, testCaseParentID));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.generateLeftMostTableCell(22, "Title:"));
            sb.Append(MarkdownTableUtils.generateRightMostTableCell(columnWidths, testCaseTitle));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.generateLeftMostTableCell(22, "*Test Steps*"));
            sb.Append(MarkdownTableUtils.generateRightMostTableCell(columnWidths, testCaseDescription));
            return sb.ToString();
        }

        public string TestCaseState
        {
            get => testCaseState;
            set => testCaseState = value;
        }

        public string TestCaseID
        {
            get => testCaseID;
            set => testCaseID = value;
        }

        public Uri TestCaseLink
        {
            get => testCaseLink;
            set => testCaseLink = value;
        }

        public string TestCaseParentID
        {
            get => testCaseParentID;
            set => testCaseParentID = value;
        }

        public Uri TestCaseParentLink
        {
            get => testCaseParentLink;
            set => testCaseParentLink = value;
        }

        public string TestCaseTitle
        {
            get => testCaseTitle;
            set => testCaseTitle = value;
        }

        public string TestCaseDescription
        {
            get => testCaseDescription;
            set => testCaseDescription = value;
        }

        public string TestCaseRevision
        {
            get => testCaseRevision;
            set => testCaseRevision = value;
        }

    }
}

