using System;
using System.Collections.Generic;
using System.Text;
using RoboClerk;

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
        private string requirementCategory = string.Empty;
        private string requirementState = string.Empty;
        private string requirementID = string.Empty;
        private string requirementTitle = string.Empty;
        private string requirementDescription = string.Empty;
        private string requirementRevision = string.Empty;
        public RequirementItem()
        {
            type = "RequirementItem";
            id = Guid.NewGuid().ToString();
        }

        public override string ToText()
        {
            StringBuilder sb = new StringBuilder();
            int[] columnWidths = new int[2] { 44, 160 };
            string separator = MarkdownTableUtils.GenerateGridTableSeparator(columnWidths);
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0],"Requirement ID:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, HasLink ? $"[{requirementID}]({link})" : requirementID));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "Requirement Revision:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, requirementRevision));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "Requirement Category:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths,requirementCategory));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "Parent ID:"));
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
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths,parentField));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "Title:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths,requirementTitle));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "Description:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths,requirementDescription));
            return sb.ToString();
        }

        public RequirementType TypeOfRequirement
        {
            get => requirementType;
            set => requirementType = value;
        }

        public string RequirementState 
        {
            get => requirementState;
            set => requirementState = value;
        }

        public string RequirementID 
        {
            get => requirementID;
            set
            {
                ItemID = value; //we set the itemID to the same value as the requirement ID
                requirementID = value;
            }
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

        public string RequirementCategory
        {
            get => requirementCategory;
            set => requirementCategory = value;
        }
    }
}

