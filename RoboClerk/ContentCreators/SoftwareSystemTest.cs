using RoboClerk.Configuration;
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace RoboClerk.ContentCreators
{
    public class SoftwareSystemTest : ContentCreatorBase
    {
        protected bool automated = true;

        private string GenerateTestCaseStepsHeader(bool automated)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("| *Step* | *Action* | *Expected Result* ");
            if (!automated)
            {
                sb.Append("| *Actual Result* ");
                sb.AppendLine("| *Test Status*");
            }
            else
            {
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private string GenerateTestCaseStepLine(string[] step, int stepNr, bool automated)
        {
            if (step.Length < 2)
            {
                throw new ArgumentException("Not enough information to build step line.");
            }

            StringBuilder sb = new StringBuilder();
            sb.Append($"| {stepNr.ToString()} ");
            sb.Append($"| {step[0].Replace("\n", "").Replace("\r", "")} ");
            sb.Append($"| {step[1].Replace("\n", "").Replace("\r", "")} ");
            if (!automated)
            {
                sb.Append("|  | ");
            }
            return sb.ToString();
        }

        private string GenerateADOC(TestCaseItem item, IDataSources data)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("|====");
            sb.Append("| *Test Case ID:* ");
            sb.AppendLine(item.HasLink ? $"| {item.Link}[{item.ItemID}]" : $"| {item.ItemID}");
            sb.AppendLine();

            sb.Append("| *Test Case Revision:* ");
            sb.AppendLine($"| {(item.TestCaseRevision==string.Empty ? "N/A" : item.TestCaseRevision)}");
            sb.AppendLine();
            
            sb.Append("| *Parent ID:* ");
            sb.AppendLine($"| {GetParentField(item, data)}");
            sb.AppendLine();

            sb.Append("| *Title:* ");
            sb.AppendLine($"| {(item.TestCaseTitle==string.Empty ? "N/A" : item.TestCaseTitle)}");
            sb.AppendLine("|====");
            sb.AppendLine();
            sb.AppendLine($"@@Post:REMOVEPARAGRAPH()@@");
            sb.AppendLine();
            
            sb.AppendLine("|====");
            sb.AppendLine(GenerateTestCaseStepsHeader(item.TestCaseAutomated));
            sb.AppendLine();
            int stepNr = 1;
            foreach (var step in item.TestCaseSteps)
            {
                sb.AppendLine(GenerateTestCaseStepLine(step, stepNr, item.TestCaseAutomated));
                sb.AppendLine();
                stepNr++;
            }
            sb.AppendLine("|====");
            
            if (!item.TestCaseAutomated)
            {
                sb.AppendLine();
                sb.AppendLine($"@@Post:REMOVEPARAGRAPH()@@");
                sb.AppendLine();
                sb.AppendLine("|====");
                sb.Append("| Initial: ");
                sb.AppendLine("| Date: ");
                sb.AppendLine("|====");
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
                        parentField.Append(parentItem.HasLink ? $"{parentItem.Link}[{parentItem.ItemID}]" : parentItem.ItemID);
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
                        output.AppendLine(GenerateADOC(test,data));
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
