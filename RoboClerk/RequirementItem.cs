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
        private string requirementCategory = "";
        private string requirementState = "";
        private string requirementID = "";
        private Uri requirementLink;
        private string requirementTitle = "";
        private string requirementDescription = "";
        private string requirementRevision = "";
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
            List<string> finalLines = new List<string>();
            foreach(var line in lines)
            {
                if (line.Length <= cellWidth[1])
                {
                    finalLines.Add(line);
                }
                else
                {
                    string[] words = line.Split(' ');
                    StringBuilder builder = new StringBuilder();
                    foreach(var word in words)
                    {
                        if(builder.Length + word.Length + 1 > cellWidth[1])
                        {
                            finalLines.Add(builder.ToString());
                            builder.Clear();
                        }
                        builder.Append($" {word}");
                    }
                    if(builder.Length > 0)
                    {
                        finalLines.Add(builder.ToString());
                    }
                }
            }
            bool first = true;
            foreach(var line in finalLines)
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
            int[] columnWidths = new int[2] { 44, 160 };
            string separator = generateTableSeparator(columnWidths);
            sb.AppendLine(separator);
            sb.Append(generateLeftMostTableCell(columnWidths[0],"Requirement ID:"));
            sb.Append(generateRightMostTableCell(columnWidths,requirementID));
            sb.AppendLine(separator);
            sb.Append(generateLeftMostTableCell(columnWidths[0], "Requirement Revision:"));
            sb.Append(generateRightMostTableCell(columnWidths, requirementRevision));
            sb.AppendLine(separator);
            sb.Append(generateLeftMostTableCell(columnWidths[0], "Requirement Category:"));
            sb.Append(generateRightMostTableCell(columnWidths,requirementCategory));
            sb.AppendLine(separator);
            sb.Append(generateLeftMostTableCell(columnWidths[0], "Parent ID:"));
            sb.Append(generateRightMostTableCell(columnWidths,parents.Count == 0? "": parents[0].Item1));
            sb.AppendLine(separator);
            sb.Append(generateLeftMostTableCell(columnWidths[0], "Title:"));
            sb.Append(generateRightMostTableCell(columnWidths,requirementTitle));
            sb.AppendLine(separator);
            sb.Append(generateLeftMostTableCell(columnWidths[0], "Description:"));
            sb.Append(generateRightMostTableCell(columnWidths,requirementDescription));
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
            set => requirementID = value;
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

