using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RoboClerk.Redmine
{
    public class TextileToAsciiDocConverter
    {
        /// <summary>
        /// Converts a Textile string (as used by Redmine) into an AsciiDoc string.
        /// </summary>
        /// <param name="textile">The Textile formatted string.</param>
        /// <returns>The converted AsciiDoc string.</returns>
        public string ConvertTextile2AsciiDoc(string textile)
        {
            if (textile == null)
                throw new ArgumentNullException(nameof(textile));

            // Store pre tag contents with unique placeholders to protect them from conversion
            Dictionary<string, string> preTagContents = new Dictionary<string, string>();
            int preTagIndex = 0;

            // Extract and replace pre tags with placeholders
            textile = Regex.Replace(textile, @"<pre>(.*?)</pre>", m =>
            {
                string content = m.Groups[1].Value;
                string placeholder = $"PRETAGPLACEHOLDER{preTagIndex}";
                preTagContents[placeholder] = content;
                preTagIndex++;
                return placeholder;
            }, RegexOptions.Singleline);

            // --- Convert Headings ---
            // Example: "h1. Heading" => "== Heading"
            textile = Regex.Replace(textile, @"^h(\d)\.\s+(.*)$", m =>
            {
                int level = int.Parse(m.Groups[1].Value);
                // In AsciiDoc, a level-1 section is typically "==", level-2 "===", etc.
                string prefix = new string('=', level + 1);
                return $"{prefix} {m.Groups[2].Value}";
            }, RegexOptions.Multiline);

            // --- Convert Pre tags ---
            // Convert <pre> tags to AsciiDoc literal blocks
            textile = Regex.Replace(textile, @"<pre>(.*?)</pre>", m =>
            {
                // Extract content between <pre> tags
                string content = m.Groups[1].Value;

                // Return as AsciiDoc literal block
                return "\n....\n" + content + "\n....\n";
            }, RegexOptions.Singleline);

            // --- Convert Links ---
            // Textile: "link text":http://example.com
            // AsciiDoc: link:http://example.com[link text]
            textile = Regex.Replace(textile, @"""([^""]+)""\s*:\s*(\S+)", "link:$2[$1]");

            // --- Convert Images ---
            // Textile: !http://example.com/image.png!
            // AsciiDoc: image::http://example.com/image.png[]
            textile = Regex.Replace(textile, @"!(\S+)!",
                m => $"image::{m.Groups[1].Value}[]");

            // --- Convert Unordered Lists ---
            // Instead of using spaces for nesting, output multiple '*' characters.
            // Example: Textile "* Item" or "** Nested item" become "* Item" and "** Item"
            textile = Regex.Replace(textile, @"^(?<stars>\*+)\s+", m =>
            {
                int level = m.Groups["stars"].Value.Length;
                return new string('*', level) + " ";
            }, RegexOptions.Multiline);

            // --- Convert Ordered Lists ---
            // Instead of using spaces for nesting, output multiple '.' characters.
            // Example: Textile "# Item" or "## Nested item" become ". Item" and ".. Item"
            textile = Regex.Replace(textile, @"^(?<hashes>#+)\s+", m =>
            {
                int level = m.Groups["hashes"].Value.Length;
                return new string('.', level) + " ";
            }, RegexOptions.Multiline);

            // --- Convert Blockquotes (bq.) ---
            // Textile blockquotes starting with "bq. " become AsciiDoc blockquotes.
            textile = Regex.Replace(textile, @"^bq\.\s+(.*)$", m =>
                "____\n" + m.Groups[1].Value + "\n____", RegexOptions.Multiline);

            // --- Convert Blockquotes (lines beginning with '>') ---
            // Lines starting with ">" are also treated as quotes.
            textile = Regex.Replace(textile, @"^>\s*(.*)$", m =>
                "____\n" + m.Groups[1].Value + "\n____", RegexOptions.Multiline);

            // --- Convert Inline Code ---
            // Textile inline code marked with @ characters is converted to AsciiDoc inline code.
            // Example: @print("hello")@ becomes `print("hello")`
            textile = Regex.Replace(textile, @"@([^@]+)@", m =>
                "`" + m.Groups[1].Value + "`");

            // --- Convert Strikethrough ---
            // Textile uses hyphen-delimited text for strikethrough, e.g.: -deleted text-
            // We convert it to AsciiDoc’s inline strike format: [strike]#text#
            textile = Regex.Replace(textile, @"(^|\s)-(.+?)-(?=$|\s)", m =>
                m.Groups[1].Value + "[strike]#" + m.Groups[2].Value + "#");

            // --- Process Tables ---
            // Convert contiguous table lines into an AsciiDoc table block.
            textile = ProcessTables(textile);

            // --- Ensure List Blocks Are Preceded by a Blank Line ---
            textile = EnsureListBlocksHaveLeadingBlankLine(textile);

            // Restore pre tag contents with AsciiDoc literal blocks
            foreach (var placeholder in preTagContents.Keys)
            {
                textile = textile.Replace(placeholder, $"\n....\n{preTagContents[placeholder]}\n....\n");
            }

            return textile;
        }

        /// <summary>
        /// Processes contiguous table lines in the input and wraps them in an AsciiDoc table block.
        /// </summary>
        /// <param name="input">The text to process.</param>
        /// <returns>The text with any detected tables converted.</returns>
        private string ProcessTables(string input)
        {
            var lines = input.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var output = new StringBuilder();
            var tableBlock = new List<string>();

            foreach (var line in lines)
            {
                // A simple pattern: a table row starts and ends with a pipe.
                if (Regex.IsMatch(line, @"^\|.*\|$"))
                {
                    tableBlock.Add(line);
                }
                else
                {
                    if (tableBlock.Count > 0)
                    {
                        output.Append(ProcessTableBlock(tableBlock));
                        tableBlock.Clear();
                    }
                    output.AppendLine(line);
                }
            }
            if (tableBlock.Count > 0)
            {
                output.Append(ProcessTableBlock(tableBlock));
            }
            return output.ToString();
        }

        /// <summary>
        /// Converts a block of Textile table rows into an AsciiDoc table.
        /// </summary>
        /// <param name="tableLines">A list of strings, each representing a table row in Textile.</param>
        /// <returns>A string containing the AsciiDoc table block.</returns>
        private string ProcessTableBlock(List<string> tableLines)
        {
            var sb = new StringBuilder();
            int numColumns = 0;
            if (tableLines.Count > 0)
            {
                // Determine the number of columns from the first row.
                var firstLine = tableLines[0].Trim();
                // Splitting by '|' leaves empty strings at the beginning and end.
                var cells = Regex.Split(firstLine, @"\|")
                                 .Where(x => !string.IsNullOrEmpty(x))
                                 .ToArray();
                numColumns = cells.Length;
                sb.AppendLine($"[cols=\"{numColumns}*\"]");
            }
            sb.AppendLine("|===");
            foreach (var line in tableLines)
            {
                string trimmedLine = line.Trim();
                // Remove the starting and ending pipe (if present)
                if (trimmedLine.StartsWith("|"))
                    trimmedLine = trimmedLine.Substring(1);
                if (trimmedLine.EndsWith("|"))
                    trimmedLine = trimmedLine.Substring(0, trimmedLine.Length - 1);
                // Split the row into cells.
                var cells = trimmedLine.Split(new char[] { '|' }, StringSplitOptions.None);
                // Process each cell: if the cell starts with "_.", treat it as a header cell.
                for (int i = 0; i < cells.Length; i++)
                {
                    string cell = cells[i].Trim();
                    if (cell.StartsWith("_."))
                    {
                        // Remove the header marker and prefix with '^' for AsciiDoc header cell.
                        cell = cell.Substring(2).Trim();
                        cells[i] = $"^{cell}";
                    }
                    else
                    {
                        cells[i] = cell;
                    }
                }
                // In AsciiDoc, each row starts with a pipe, and cells are separated by " |"
                sb.Append("|" + string.Join(" |", cells) + "\n");
            }
            sb.AppendLine("|===");
            return sb.ToString();
        }

        /// <summary>
        /// Ensures that list blocks (ordered or unordered) are preceded by a blank line.
        /// This helps the AsciiDoc processor to correctly recognize them as lists.
        /// </summary>
        /// <param name="text">The converted text.</param>
        /// <returns>The text with a blank line inserted before list blocks where needed.</returns>
        private string EnsureListBlocksHaveLeadingBlankLine(string text)
        {
            // Split by newline so we can process line by line.
            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var result = new List<string>();

            // We'll insert a blank line before the first line of any contiguous list block,
            // provided it isn't already preceded by a blank line.
            for (int i = 0; i < lines.Length; i++)
            {
                // Detect if the current line is a list item.
                bool isListItem = Regex.IsMatch(lines[i], @"^(?:\*+|\.+)\s+");
                if (isListItem)
                {
                    // If this is the first line of the file, or the previous line is not blank,
                    // and we are at the start of a list block, then insert a blank line.
                    if (i > 0 && !string.IsNullOrWhiteSpace(lines[i - 1]))
                    {
                        // Also, avoid inserting duplicate blank lines.
                        if (result.Count > 0 && !string.IsNullOrWhiteSpace(result.Last()))
                        {
                            result.Add(string.Empty);
                        }
                    }
                }
                result.Add(lines[i]);
            }
            return string.Join("\n", result);
        }
    }
}
