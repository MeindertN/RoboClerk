using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk
{
    public class TestCaseItem : LinkedItem
    {
        private string testCaseState = "";
        private string testCaseID = "";
        private string testCaseTitle = "";
        private string testCaseDescription = "";
        private string testCaseRevision = "";
        private bool testCaseAutomated = false;
        private List<string[]> testCaseSteps = new List<string[]>();
        public TestCaseItem()
        {
            type = "TestCaseItem";
            id = Guid.NewGuid().ToString();
        }

        public override string ToText()
        {
            StringBuilder sb = new StringBuilder();
            int[] columnWidths = new int[2] { 25, Math.Max(($"[{testCaseID}]({link})").Length, 75) };
            string separator = MarkdownTableUtils.GenerateGridTableSeparator(columnWidths);
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "**Test Case ID:**"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, HasLink ? $"[{testCaseID}]({link})" : testCaseID));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "**Test Case Revision:**"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, testCaseRevision));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "**Parent ID:**"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, GetParentField()));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "**Title:**"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, testCaseTitle));
            sb.AppendLine(separator);
            sb.AppendLine($"~REMOVEPARAGRAPH\n");
            int maxDescriptionLength = getMaxTestStepElementLength(0);
            int maxResultLength = getMaxTestStepElementLength(1);
            int[] testStepColumnWidths = null;
            if (testCaseAutomated)
            {
                testStepColumnWidths = new int[4] { 10, Math.Max(maxDescriptionLength + 2, 12), Math.Max(maxResultLength + 2, 21), 17 };
            }
            else
            {
                testStepColumnWidths = new int[5] { 10, Math.Max(maxDescriptionLength + 2, 12), Math.Max(maxResultLength + 2, 21), 21, 17 };
            }
            separator = MarkdownTableUtils.GenerateGridTableSeparator(testStepColumnWidths);
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateTestCaseStepsHeader(testStepColumnWidths, testCaseAutomated));
            sb.AppendLine(separator);
            int stepNr = 1;
            foreach (var step in testCaseSteps)
            {
                sb.Append(MarkdownTableUtils.GenerateTestCaseStepLine(testStepColumnWidths, step, stepNr, testCaseAutomated));
                stepNr++;
                sb.AppendLine(separator);
            }
            if (!testCaseAutomated)
            {
                sb.AppendLine("~REMOVEPARAGRAPH\n");
                testStepColumnWidths = new int[2] { 40, 40 };
                separator = MarkdownTableUtils.GenerateGridTableSeparator(testStepColumnWidths);
                sb.AppendLine(separator);
                sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(testStepColumnWidths[0], "Initial:"));
                sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(testStepColumnWidths, "Date:"));
                sb.AppendLine(separator);
            }
            return sb.ToString();
        }

        private string GetParentField()
        {
            StringBuilder parentField = new StringBuilder();
            if (parents.Count > 0)
            {
                foreach (var parent in parents)
                {
                    if (parentField.Length > 0)
                    {
                        parentField.Append(", ");
                    }
                    if (parent.Item2 != null)
                    {
                        parentField.Append($"[{parent.Item1}]({parent.Item2})");
                    }
                    else
                    {
                        parentField.Append(parent.Item1);
                    }
                }
                return parentField.ToString();
            }
            return "N/A";
        }

        private int getMaxTestStepElementLength(int v)
        {
            int maxLength = 0;
            foreach (var step in testCaseSteps)
            {
                maxLength = Math.Max(maxLength, step[v].Length);
            }
            return maxLength;
        }

        public string TestCaseState
        {
            get => testCaseState;
            set => testCaseState = value;
        }

        public string TestCaseID
        {
            get => testCaseID;
            set
            {
                id = value;
                testCaseID = value;
            }
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

        public List<string[]> TestCaseSteps
        {
            get => testCaseSteps;
            set => testCaseSteps = value;
        }

        public bool TestCaseAutomated
        {
            get => testCaseAutomated;
            set => testCaseAutomated = value;
        }

    }
}

