using RoboClerk.Configuration;
using System;
using System.Linq;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace RoboClerk.ContentCreators
{
    public class SoftwareSystemTest : ContentCreatorBase
    {
        protected bool automated = true;

        private string GenerateMarkdown(TestCaseItem item, IDataSources data)
        {
            StringBuilder sb = new StringBuilder();
            int[] columnWidths = new int[2] { 25, Math.Max(($"[{item.ItemID}]({item.Link})").Length, 75) };
            string separator = MarkdownTableUtils.GenerateGridTableSeparator(columnWidths);
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "**Test Case ID:**"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, item.HasLink ? $"[{item.ItemID}]({item.Link})" : item.ItemID));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "**Test Case Revision:**"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, item.TestCaseRevision));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "**Parent ID:**"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, GetParentField(item, data)));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "**Title:**"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, item.TestCaseTitle));
            sb.AppendLine(separator);
            sb.AppendLine($"~REMOVEPARAGRAPH\n");
            int maxDescriptionLength = getMaxTestStepElementLength(0, item.TestCaseSteps);
            int maxResultLength = getMaxTestStepElementLength(1, item.TestCaseSteps);
            int[] testStepColumnWidths = null;
            if (item.TestCaseAutomated)
            {
                testStepColumnWidths = new int[4] { 10, Math.Max(maxDescriptionLength + 2, 12), Math.Max(maxResultLength + 2, 21), 17 };
            }
            else
            {
                testStepColumnWidths = new int[5] { 10, Math.Max(maxDescriptionLength + 2, 12), Math.Max(maxResultLength + 2, 21), 21, 17 };
            }
            separator = MarkdownTableUtils.GenerateGridTableSeparator(testStepColumnWidths);
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateTestCaseStepsHeader(testStepColumnWidths, item.TestCaseAutomated));
            sb.AppendLine(separator);
            int stepNr = 1;
            foreach (var step in item.TestCaseSteps)
            {
                sb.Append(MarkdownTableUtils.GenerateTestCaseStepLine(testStepColumnWidths, step, stepNr, item.TestCaseAutomated));
                stepNr++;
                sb.AppendLine(separator);
            }
            if (!item.TestCaseAutomated)
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

        private string GetParentField(TestCaseItem item, IDataSources data)
        {
            StringBuilder parentField = new StringBuilder();
            var parents = item.LinkedItems.Where(x => x.LinkType == ItemLinkType.Parent);
            if (parents.Count() > 0)
            {
                foreach (var parent in parents)
                {
                    if (parentField.Length > 0)
                    {
                        parentField.Append(" / ");
                    }
                    var parentItem = data.GetItem(parent.TargetID) as RequirementItem;
                    if (parentItem != null)
                    {
                        parentField.Append(parentItem.HasLink ? $"[{parentItem.ItemID}]({parentItem.Link})" : parentItem.ItemID);
                        parentField.Append($": \"{parentItem.RequirementTitle}\"");
                    }
                    else
                    {
                        parentField.Append(parent.TargetID);
                    }
                }
                return parentField.ToString();
            }
            return "N/A";
        }

        private int getMaxTestStepElementLength(int v, List<string[]> testCaseSteps)
        {
            int maxLength = 0;
            foreach (var step in testCaseSteps)
            {
                maxLength = Math.Max(maxLength, step[v].Length);
            }
            return maxLength;
        }

        public override string GetContent(RoboClerkTag tag, IDataSources data, ITraceabilityAnalysis analysis, DocumentConfig doc)
        {
            var systemTests = data.GetAllSystemLevelTests();
            StringBuilder output = new StringBuilder();
            bool testCaseFound = false;

            var properties = typeof(TestCaseItem).GetProperties();
            foreach (var test in systemTests)
            {
                if (ShouldBeIncluded(tag, test, properties))
                {
                    testCaseFound = true;
                    try
                    {
                        output.AppendLine(GenerateMarkdown(test,data));
                    }
                    catch
                    {
                        logger.Error($"An error occurred while rendering software system test {test.ItemID} in {doc.DocumentTitle}.");
                        throw;
                    }
                    analysis.AddTrace(analysis.GetTraceEntityForID("SoftwareSystemTest"), test.ItemID, analysis.GetTraceEntityForTitle(doc.DocumentTitle), test.ItemID);

                    var parents = test.LinkedItems.Where(x => x.LinkType == ItemLinkType.Parent);
                    if (parents.Count() == 0)
                    {
                        //in case there are no parents, ensure that the broken trace is included
                        analysis.AddTrace(analysis.GetTraceEntityForID("SoftwareRequirement"), null, analysis.GetTraceEntityForTitle(doc.DocumentTitle), test.ItemID);
                    }
                    else foreach (var parent in parents)
                    {
                        analysis.AddTrace(analysis.GetTraceEntityForID("SoftwareRequirement"), parent.TargetID, analysis.GetTraceEntityForTitle(doc.DocumentTitle), test.ItemID);
                    }
                }
            }
            if (!testCaseFound)
            {
                return $"Unable to find {(automated ? "automated" : "manual")} test case(s). Check if test cases are provided or if a valid test case identifier is specified.";
            }
            return output.ToString();
        }
    }
}
