using System;
using System.Text;

namespace RoboClerk
{
    public enum RequirementType
    {
        SystemRequirement,
        SoftwareRequirement
    };
    public class RequirementItem : LinkedItem
    {
        private RequirementType requirementType;
        private string requirementState = string.Empty;
        private string requirementTitle = string.Empty;
        private string requirementDescription = string.Empty;
        private string requirementRevision = string.Empty;
        public RequirementItem(RequirementType reqType)
        {
            TypeOfRequirement = reqType;
            id = Guid.NewGuid().ToString();
        }

        public override string ToText()
        {
            StringBuilder sb = new StringBuilder();
            int[] columnWidths = new int[2] { 44, 160 };
            string separator = MarkdownTableUtils.GenerateGridTableSeparator(columnWidths);
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "Requirement ID:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, HasLink ? $"[{base.ItemID}]({link})" : base.ItemID));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "Requirement Revision:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, requirementRevision));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "Requirement Category:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, base.ItemCategory));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "Parent ID:"));
            string parentField = "N/A";
            if (linkedItems.Count > 0)
            {
                foreach(var item in linkedItems)
                {
                    if(item.LinkType == ItemLinkType.Parent)
                    {
                        parentField = item.TargetID;
                    }
                }
            }
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, parentField));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "Title:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, requirementTitle));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "Description:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, requirementDescription));
            return sb.ToString();
        }

        public RequirementType TypeOfRequirement
        {
            get => requirementType;
            set
            {
                if(value == RequirementType.SystemRequirement)
                {
                    type = "SystemRequirement";
                }
                else
                {
                    type = "SoftwareRequirement";
                }
                requirementType = value;
            }
        }

        public string RequirementState
        {
            get => requirementState;
            set => requirementState = value;
        }

        public string RequirementTitle
        {
            get => requirementTitle;
            set => requirementTitle = value;
        }

        public string RequirementDescription
        {
            get => requirementDescription;
            set => requirementDescription = value;
        }

        public string RequirementRevision
        {
            get => requirementRevision;
            set => requirementRevision = value;
        }
    }
}

