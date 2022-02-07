using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk
{
    public static class MarkdownTableUtils
    {
        internal static string GenerateGridTableSeparator(int[] columnwidths)
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
            if (content.Length > cellWidth)
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
            List<string> finalLines = new List<string>();
            foreach (var line in lines)
            {
                if (line.Length <= cellWidth[1])
                {
                    finalLines.Add(line);
                }
                else
                {
                    string[] words = line.Split(' ');
                    StringBuilder builder = new StringBuilder();
                    foreach (var word in words)
                    {
                        if (builder.Length + word.Length + 1 > cellWidth[1])
                        {
                            finalLines.Add(builder.ToString());
                            builder.Clear();
                        }
                        builder.Append($" {word}");
                    }
                    if (builder.Length > 0)
                    {
                        finalLines.Add(builder.ToString());
                    }
                }
            }
            bool first = true;
            foreach (var line in finalLines)
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

        internal static string GenerateTestCaseStepsHeader(int[] testStepColumnWidth, bool automated)
        {
            if ((automated && testStepColumnWidth.Length < 4) || (!automated && testStepColumnWidth.Length < 5))
            {
                throw new ArgumentException("Not enough columnwidths available to create test case steps header.");
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(GenerateLeftMostTableCell(testStepColumnWidth[0], "**Step**"));
            sb.Append(GenerateLeftMostTableCell(testStepColumnWidth[1], "**Action**"));
            sb.Append(GenerateLeftMostTableCell(testStepColumnWidth[2], "**Expected Result**"));
            if (!automated)
            {
                sb.Append(GenerateLeftMostTableCell(testStepColumnWidth[3], "**Actual Result**"));
                sb.Append(GenerateLeftMostTableCell(testStepColumnWidth[4], "**Test Status**"));
            }
            else
            {
                sb.Append(GenerateLeftMostTableCell(testStepColumnWidth[3], "**Test Status**"));
            }
            sb.AppendLine("|");
            return sb.ToString();
        }

        internal static string GenerateTestCaseStepLine(int[] testStepColumnWidth, string[] step, int stepNr, bool automated)
        {
            if (step.Length < 2)
            {
                throw new ArgumentException("Not enough information to build step line.");
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(GenerateLeftMostTableCell(testStepColumnWidth[0], stepNr.ToString()));
            sb.Append(GenerateLeftMostTableCell(testStepColumnWidth[1], step[0].Replace("\n", "").Replace("\r", "")));
            sb.Append(GenerateLeftMostTableCell(testStepColumnWidth[2], step[1].Replace("\n", "").Replace("\r", "")));
            if (!automated)
            {
                sb.Append(GenerateLeftMostTableCell(testStepColumnWidth[3], "  "));
                sb.Append(GenerateLeftMostTableCell(testStepColumnWidth[4], "  "));
            }
            else
            {
                sb.Append(GenerateLeftMostTableCell(testStepColumnWidth[3], "  "));
            }
            sb.AppendLine("|");
            return sb.ToString();
        }

        internal static string GenerateTraceMatrixHeader(List<string> headers)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string title in headers)
            {
                sb.Append(GenerateLeftMostTableCell(title.Length + 2, title));
            }
            sb.AppendLine("|");
            sb.Append("| ");
            foreach (string title in headers)
            {
                sb.Append('-', title.Length);
                sb.Append(" | ");
            }
            sb.AppendLine("");

            return sb.ToString();
        }

        internal static string GenerateTraceMatrixLine(List<string> items)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in items)
            {
                sb.Append(GenerateLeftMostTableCell(item.Length + 2, item));
            }
            sb.AppendLine(" |");
            return sb.ToString();
        }

    }
}
