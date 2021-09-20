using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk
{
    public static class MarkdownTableUtils
    {
        internal static string GenerateTableSeparator(int[] columnwidths)
        {
            StringBuilder output = new StringBuilder("+");
            foreach (var width in columnwidths)
            {
                output.Append('-', width);
                output.Append('+', 1);
            }
            return output.ToString();
        }

        internal static string GenerateLeftMostTableCell(int cellWidth, string content)
        {
            if(content.Length > cellWidth)
            {
                throw new ArgumentException($"Content \"{content}\" is too large for cell of size {cellWidth}.");
            }
            
            StringBuilder sb = new StringBuilder("|");
            sb.Append(' ', 1);
            sb.Append(content);
            sb.Append(' ', cellWidth - 1 - content.Length);
            return sb.ToString();
        }

        internal static string GenerateRightMostTableCell(int[] cellWidth, string content)
        {
            //determine how many lines we will need to store this content
            StringBuilder sb = new StringBuilder();
            string[] lines = content.Split('\n');
            bool first = true;
            foreach (var line in lines)
            {
                if (!first)
                {
                    sb.Append(GenerateLeftMostTableCell(cellWidth[0], ""));
                }
                sb.Append("| ");
                sb.AppendLine(line);
                first = false;
            }
            return sb.ToString();
        }

        internal static string GenerateTestCaseStepsHeader(int[] testStepColumnWidth)
        {
            if(testStepColumnWidth.Length < 4)
            {
                throw new ArgumentException("Not enough columnwidths available to create test case steps header.");
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(GenerateLeftMostTableCell(testStepColumnWidth[0], "Nr."));
            sb.Append(GenerateLeftMostTableCell(testStepColumnWidth[1], "Action"));
            sb.Append(GenerateLeftMostTableCell(testStepColumnWidth[2], "Expected Result"));
            sb.Append(GenerateLeftMostTableCell(testStepColumnWidth[3], "Pass"));
            sb.AppendLine("|");
            sb.Append('-', testStepColumnWidth[0]);
            sb.Append("|");
            sb.Append('-', testStepColumnWidth[1]);
            sb.Append("|");
            sb.Append('-', testStepColumnWidth[2]);
            sb.Append("|");
            sb.Append('-', testStepColumnWidth[3]);
            sb.AppendLine("|");

            return sb.ToString();
        }

        internal static string GenerateTestCaseStepLine(int[] testStepColumnWidth, string[] step, int stepNr)
        {
            if(step.Length < 2)
            {
                throw new ArgumentException("Not enough information to build step line.");
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(GenerateLeftMostTableCell(testStepColumnWidth[0], stepNr.ToString()));
            sb.Append(GenerateLeftMostTableCell(testStepColumnWidth[1], step[0].Replace("\n", "").Replace("\r", "")));
            sb.Append(GenerateLeftMostTableCell(testStepColumnWidth[2], step[1].Replace("\n", "").Replace("\r", "")));
            sb.Append(GenerateLeftMostTableCell(testStepColumnWidth[3], " "));
            sb.AppendLine("|");
            return sb.ToString();
        }


    }
}
