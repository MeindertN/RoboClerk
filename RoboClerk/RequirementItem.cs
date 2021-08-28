using System;
using System.Text;
using RoboClerk;

namespace RoboClerk
{
    public class RequirementItem : TraceItem
    {
        private string requirementType;
        private string requirementCategory;
        private string requirementState;
        private string requirementID;
        private Uri requirementLink;
        private string requirementParentID;
        private Uri requirementParentLink;
        private string requirementTitle;
        private string requirementDescription;
        public RequirementItem()
        {
            type = "RequirementItem";
            id = Guid.NewGuid().ToString();
        }
        
        private string generateTableSeparator(int[] columnwidths)
        {
            StringBuilder output = new StringBuilder("+");
            foreach(var width in columnwidths)
            {
                output.Append('-',width);
                output.Append('+',1);
            }
            return output.ToString();
        }

        private string generateLeftMostTableCell(int cellWidth, string content)
        {
            StringBuilder sb = new StringBuilder("|");
            sb.Append(' ',1);
            sb.Append(content);
            sb.Append(' ',cellWidth-1-content.Length);
            return sb.ToString();
        }

        private string generateRightMostTableCell(int[] cellWidth, string content)
        {
            //determine how many lines we will need to store this content
            StringBuilder sb = new StringBuilder();
            string[] lines = content.Split('\n');
            bool first = true;
            foreach(var line in lines)
            {
                if(!first)
                {
                    sb.Append(generateLeftMostTableCell(cellWidth[0],""));
                }
                sb.Append("| ");
                sb.AppendLine(line);
                first = false;
            }
            return sb.ToString();
        }

        public override string ToMarkDown()
        {
            StringBuilder sb = new StringBuilder();
            int[] columnWidths = new int[2] { 22, 80 };
            string separator = generateTableSeparator(columnWidths);
            sb.AppendLine(separator);
            sb.Append(generateLeftMostTableCell(22,"Requirement ID:"));
            sb.Append(generateRightMostTableCell(columnWidths,requirementID));
            sb.AppendLine(separator);
            sb.Append(generateLeftMostTableCell(22,"Requirement Category:"));
            sb.Append(generateRightMostTableCell(columnWidths,requirementCategory));
            sb.AppendLine(separator);
            sb.Append(generateLeftMostTableCell(22,"Parent ID:"));
            sb.Append(generateRightMostTableCell(columnWidths,requirementParentID));
            sb.AppendLine(separator);
            sb.Append(generateLeftMostTableCell(22,"Title:"));
            sb.Append(generateRightMostTableCell(columnWidths,requirementTitle));
            sb.AppendLine(separator);
            sb.Append(generateLeftMostTableCell(22,"Description:"));
            sb.Append(generateRightMostTableCell(columnWidths,requirementDescription));
            return sb.ToString();
        }

        public string RequirementType
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
            set => requirementID = value;
        }

        public Uri RequirementLink 
        {
            get => requirementLink;
            set => requirementLink = value;
        }

        public string RequirementParentID 
        {
            get => requirementParentID;
            set => requirementParentID = value;
        }

        public Uri RequirementParentLink 
        {
            get => requirementParentLink;
            set => requirementParentLink = value;
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
    }
}

