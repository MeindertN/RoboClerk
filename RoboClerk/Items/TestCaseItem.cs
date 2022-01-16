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
            int[] columnWidths = new int[2] { 22, 80 };
            string separator = MarkdownTableUtils.GenerateTableSeparator(columnWidths);
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(22, "testCase ID:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, HasLink? $"[{testCaseID}]({link})":testCaseID));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(22, "testCase Revision:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, testCaseRevision));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(22, "Parent ID:"));
            string parentField = "N/A";
            if (parents.Count > 0)
            {
                if (parents[0].Item2 != null)
                {
                    parentField = $"[{parents[0].Item1}]({parents[0].Item2})";
                }
                else
                {
                    parentField = parents[0].Item1;
                }
            }
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, parentField));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(22, "Title:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, testCaseTitle));
            sb.AppendLine(separator);
            //sb.AppendLine($"*Test Steps for testcase: {testCaseID}*");
            int maxDescriptionLength = getMaxTestStepElementLength(0);
            int maxResultLength = getMaxTestStepElementLength(1);
            int[] testStepColumnWidth = new int[4] { 6, Math.Max(maxDescriptionLength+2,8), Math.Max(maxResultLength+2, 17), 10 };
            sb.Append(MarkdownTableUtils.GenerateTestCaseStepsHeader(testStepColumnWidth));
            int stepNr = 0;
            foreach (var step in testCaseSteps)
            {
                sb.Append(MarkdownTableUtils.GenerateTestCaseStepLine(testStepColumnWidth, step, stepNr));
                stepNr++;
            }
            
            return sb.ToString();
        }

        private int getMaxTestStepElementLength(int v)
        {
            int maxLength = 0;
            foreach( var step in testCaseSteps )
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
            set => testCaseID = value;
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

