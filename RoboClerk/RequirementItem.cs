using System;
using System.Collections.Generic;
using System.Text;
using RoboClerk;

namespace RoboClerk
{
    public enum RequirementType
    {
        ProductRequirement,
        SoftwareRequirement
    };
    public class RequirementItem : TraceItem
    {
        private RequirementType requirementType;
        private string requirementCategory = string.Empty;
        private string requirementState = string.Empty;
        private string requirementID = string.Empty;
        private Uri requirementLink;
        private string requirementTitle = string.Empty;
        private string requirementDescription = string.Empty;
        private string requirementRevision = string.Empty;
        public RequirementItem()
        {
            type = "RequirementItem";
            id = Guid.NewGuid().ToString();
        }

        public override string ToMarkDown()
        {
            StringBuilder sb = new StringBuilder();
            int[] columnWidths = new int[2] { 44, 160 };
            string separator = MarkdownTableUtils.GenerateTableSeparator(columnWidths);
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0],"Requirement ID:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths,requirementID));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "Requirement Revision:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths, requirementRevision));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "Requirement Category:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths,requirementCategory));
            sb.AppendLine(separator);
            sb.Append(MarkdownTableUtils.GenerateLeftMostTableCell(columnWidths[0], "Parent ID:"));
            sb.Append(MarkdownTableUtils.GenerateRightMostTableCell(columnWidths,parents.Count == 0? "N/A": parents[0].Item1));
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

        public Uri RequirementLink 
        {
            get => requirementLink;
            set => requirementLink = value;
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

