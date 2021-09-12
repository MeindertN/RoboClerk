using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk
{
    public static class MarkdownTableUtils
    {
        public static string generateTableSeparator(int[] columnwidths)
        {
            StringBuilder output = new StringBuilder("+");
            foreach (var width in columnwidths)
            {
                output.Append('-', width);
                output.Append('+', 1);
            }
            return output.ToString();
        }

        public static string generateLeftMostTableCell(int cellWidth, string content)
        {
            StringBuilder sb = new StringBuilder("|");
            sb.Append(' ', 1);
            sb.Append(content);
            sb.Append(' ', cellWidth - 1 - content.Length);
            return sb.ToString();
        }

        public static string generateRightMostTableCell(int[] cellWidth, string content)
        {
            //determine how many lines we will need to store this content
            StringBuilder sb = new StringBuilder();
            string[] lines = content.Split('\n');
            bool first = true;
            foreach (var line in lines)
            {
                if (!first)
                {
                    sb.Append(generateLeftMostTableCell(cellWidth[0], ""));
                }
                sb.Append("| ");
                sb.AppendLine(line);
                first = false;
            }
            return sb.ToString();
        }
    }
}
