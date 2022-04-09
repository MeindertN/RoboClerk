using RoboClerk.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RoboClerk.ContentCreators
{
    abstract public class RequirementBase : ContentCreatorBase
    {
        protected List<RequirementItem> requirements = null;
        protected string requirementName = string.Empty;
        protected TraceEntity sourceType = null;

        public RequirementBase()
        {

        }

        private string GenerateMarkdown(RequirementItem item, IDataSources sources)
        {
            StringBuilder sb = new StringBuilder();
            int[] columnWidths = new int[2] { 44, 160 };
            string separator = MarkdownTableUtils.GenerateGridTableSeparator(columnWidths);
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], $"{sourceType.Name} ID:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, item.HasLink ? $"[{item.ItemID}]({item.Link})" : item.ItemID));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], $"{sourceType.Name} Revision:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, item.RequirementRevision));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], $"{sourceType.Name} Category:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, item.ItemCategory));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "Parent ID:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, GetParentField(item,sources)));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "Title:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, item.RequirementTitle));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "Description:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, item.RequirementDescription));
            return sb.ToString();
        }

        private string GetParentField(RequirementItem item, IDataSources data)
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

        public override string GetContent(RoboClerkTag tag, IDataSources sources, ITraceabilityAnalysis analysis, DocumentConfig doc)
        {
            bool foundRequirement = false;
            //No selection needed, we return everything
            StringBuilder output = new StringBuilder();
            var properties = typeof(RequirementItem).GetProperties();
            foreach (var requirement in requirements)
            {
                if (ShouldBeIncluded(tag, requirement, properties))
                {
                    foundRequirement = true;
                    try
                    {
                        output.AppendLine(GenerateMarkdown(requirement,sources));
                    }
                    catch
                    {
                        logger.Error($"An error occurred while rendering requirement {requirement.ItemID} in {doc.DocumentTitle}.");
                        throw;
                    }
                    analysis.AddTrace(sourceType, requirement.ItemID, analysis.GetTraceEntityForTitle(doc.DocumentTitle), requirement.ItemID);
                }
            }
            if (!foundRequirement)
            {
                return $"Unable to find {requirementName}(s). Check if {requirementName}s of the correct type are provided or if a valid {requirementName} identifier is specified.";
            }
            return output.ToString();
        }
    }
}
