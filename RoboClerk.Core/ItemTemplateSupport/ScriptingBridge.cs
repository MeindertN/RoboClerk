using RoboClerk.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RoboClerk
{
    public class ScriptingBridge<T> where T : Item
    {
        private IDataSources data = null;
        private ITraceabilityAnalysis analysis = null;
        private IConfiguration configuration = null;
        private List<string> traces = new List<string>();
        private List<T> items = new List<T>();
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public ScriptingBridge(IDataSources data, ITraceabilityAnalysis trace, TraceEntity sourceTraceEntity, IConfiguration config)
        {
            this.data = data;
            analysis = trace;
            configuration = config;
            SourceTraceEntity = sourceTraceEntity;
        }

        /// <summary>
        /// The item that needs to be rendered in the documentation. 
        /// </summary>
        public T Item { get; set; }

        /// <summary>
        /// The items that need to be rendered in the documentation (empty if there is only a single item). 
        /// </summary>
        public IEnumerable<T> Items
        {
            get { return items; }
            set { items = (List<T>)value; }
        }

        /// <summary>
        /// This function adds a trace link to the item who's ID is provided in the id parameter.
        /// </summary>
        /// <param name="id"></param>
        public void AddTrace(string id)
        {
            traces.Add(id);
        }

        /// <summary>
        /// Returns all the traces in the bridge.
        /// </summary>
        public IEnumerable<string> Traces
        {
            get { return traces; }
        }

        /// <summary>
        /// Returns the source trace entity, indicates what kind of trace item is being rendered.
        /// </summary>
        public TraceEntity SourceTraceEntity { get; }

        /// <summary>
        /// Provides access to the RoboClerk datasources object. This can be used to search in all 
        /// the information RoboClerk has.
        /// </summary>
        public IDataSources Sources { get { return data; } }

        /// <summary>
        /// Provides access to the traceability analysis object. The object stores all traceability
        /// information in RoboClerk.
        /// </summary>
        public ITraceabilityAnalysis TraceabilityAnalysis { get { return analysis; } }

        /// <summary>
        /// Returns all linked items attached to li with a link type of linkType
        /// </summary>
        /// <param name="li"></param>
        /// <param name="linkType"></param>
        /// <returns></returns>
        public IEnumerable<LinkedItem> GetLinkedItems(LinkedItem li, ItemLinkType linkType)
        {
            List<LinkedItem> results = new List<LinkedItem>();
            var linkedItems = li.LinkedItems.Where(x => x.LinkType == linkType);
            foreach (var item in linkedItems)
            {
                var linkedItem = (LinkedItem)data.GetItem(item.TargetID);
                if (linkedItem != null)
                {
                    results.Add(linkedItem);
                }
            }
            return results;
        }

        /// <summary>
        /// This function retrieves all items linked to li with a link of type linkType and returns an
        /// asciidoc string that has links to all these items. Usually used to provide trace information.
        /// You can use includeTitle to control if the title of the linked item is included as well.
        /// </summary>
        /// <param name="li"></param>
        /// <param name="linkType"></param>
        /// <param name="includeTitle"</param>
        /// <returns>AsciiDoc string with http link to item.</returns>
        /// <exception cref="System.Exception"></exception>
        public string GetLinkedField(LinkedItem li, ItemLinkType linkType, bool includeTitle = true)
        {
            StringBuilder field = new StringBuilder();
            var linkedItems = li.LinkedItems.Where(x => x.LinkType == linkType);
            if (linkedItems.Count() > 0)
            {
                foreach (var item in linkedItems)
                {
                    if (field.Length > 0)
                    {
                        field.Append(" / ");
                    }
                    var linkedItem = data.GetItem(item.TargetID);
                    if (linkedItem != null)
                    {
                        AddTrace(linkedItem.ItemID);
                        field.Append(linkedItem.HasLink ? GetItemLinkString(linkedItem) : linkedItem.ItemID);
                        if (includeTitle && linkedItem.ItemTitle != string.Empty)
                        {
                            field.Append($": \"{linkedItem.ItemTitle}\"");
                        }
                    }
                    else
                    {
                        logger.Error($"Item with ID {li.ItemID} has a trace link to item with ID {item.TargetID} but that item does not exist.");
                        throw new System.Exception("Item with invalid trace link encountered.");
                    }
                }
                return field.ToString();
            }
            return "N/A";
        }

        /// <summary>
        /// Returns a hyperlink string for the provided item, formatted according to the configured output format.
        /// </summary>
        /// <param name="item">The item for which to generate a hyperlink.</param>
        /// <returns>
        /// A formatted hyperlink string. If the output format is HTML, returns an &lt;a&gt; tag.
        /// If the format is ASCIIDOC, returns an AsciiDoc-style link. 
        /// If the item has no link, returns just the item ID.
        /// </returns>
        /// <exception cref="NotSupportedException">Thrown if the configured output format is not supported.</exception>
        public string GetItemLinkString(Item item)
        {
            string format = configuration.OutputFormat.ToUpper();
            string result = item.ItemID;
            if (item.HasLink)
            {
                if (format == "HTML")
                {
                    result = $"<a href=\"{item.Link}\">{item.ItemID}</a>";
                }
                else if (format == "ASCIIDOC")
                {
                    result = $"{item.Link}[{item.ItemID}]";
                }
                else
                {
                    logger.Warn($"Unknown output format \"{format}\" specified. Links may be missing from output.");
                }
            }
            return result;
        }

        /// <summary>
        /// Convenience function, checks if value is an empty string, if so, the
        /// defaultValue is returned.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public string GetValOrDef(string value, string defaultValue)
        {
            if (value == string.Empty)
            {
                return defaultValue;
            }
            return value;
        }

        /// <summary>
        /// Convenience function, calls ToString on input and returns resulting string.
        /// </summary>
        /// <param name="input"></param>
        /// <returns>string representation</returns>
        public string Insert(object input)
        {
            return input.ToString();
        }

        /// <summary>
        /// Convenience function, combines other convenience functions and ensures that
        /// ASCIDOC meant to be inside a table cell will render in an appropriate way by
        /// embedding tables and removing embedded headings because ASCIIDOC does not 
        /// support scoped headings and most likely the user does not want to have
        /// embedded headings numbered with the document headers.
        /// </summary>
        /// <param name="input">The AsciiDoc content that may contain headings and 
        /// or tables</param>
        /// <returns>Modified AsciiDoc that can be embedded in a table cell</returns>
        public string ProcessAsciidocForTableCell(string input)
        {
            string temp = ConvertHeadingsForASCIIDOCTableCell(input);
            return EmbedAsciidocTables(temp);
        }

        /// <summary>
        /// Convenience function, takes any asciidoc tables in the input and makes them
        /// embedded tables. 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public string EmbedAsciidocTables(string input)
        {
            // Split the input into lines (preserving newlines)
            var lines = input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var outputLines = new List<string>();
            bool inTable = false;

            foreach (var line in lines)
            {
                string trimmed = line.Trim();

                // Detect the table delimiter
                if (trimmed == "|===")
                {
                    inTable = !inTable;
                    // Replace outer table delimiter with nested table delimiter
                    outputLines.Add(line.Replace("|===", "!==="));
                }
                else if (inTable)
                {
                    // Preserve leading whitespace
                    int leadingSpaces = line.Length - line.TrimStart().Length;
                    string converted = line;

                    // If the cell begins with a pipe, replace it with an exclamation mark,
                    // but only if the pipe is not escaped.
                    if (line.TrimStart().StartsWith("|"))
                    {
                        converted = new string(' ', leadingSpaces) + "!" + line.TrimStart().Substring(1);
                    }

                    // Replace any unescaped cell separator pipe.
                    // The regex (?<!\\)\| matches any pipe that is not preceded by a backslash.
                    converted = Regex.Replace(converted, @"(?<!\\)\|", "!");
                    outputLines.Add(converted);
                }
                else
                {
                    // Outside a table block, leave the line unchanged.
                    outputLines.Add(line);
                }
            }

            // Rejoin all lines into a single string.
            return string.Join("\n", outputLines);
        }

        /// <summary>
        /// Converts AsciiDoc heading syntax to alternative markup suitable for embedding in table cells.
        /// This prevents heading content from being numbered along with main document headings.
        /// </summary>
        /// <param name="input">The AsciiDoc content that may contain headings</param>
        /// <returns>Modified AsciiDoc with headings converted to alternative markup</returns>
        public string ConvertHeadingsForASCIIDOCTableCell(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Process each line to detect and transform headings
            string[] lines = input.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                // Match heading patterns (e.g., "== Heading", "=== Subheading", etc.)
                var match = System.Text.RegularExpressions.Regex.Match(line, @"^(=+)\s+(.+)$");
                if (match.Success)
                {
                    int level = match.Groups[1].Length;
                    string headingText = match.Groups[2].Value.Trim();

                    // Convert heading based on its level
                    switch (level)
                    {
                        case 1: // Document title level
                        case 2: // Top section level - convert to bold
                            lines[i] = $"*{headingText}*";
                            break;
                        case 3: // Subsection level - convert to italic
                            lines[i] = $"_{headingText}_";
                            break;
                        case 4: // Subsubsection level - convert to italic with indentation
                            lines[i] = $"&#160;&#160; _{headingText}_";
                            break;
                        case 5: // Even deeper levels - use monospace with indentation
                        case 6:
                            lines[i] = $"&#160;&#160;&#160;&#160; `{headingText}`";
                            break;
                        default: // For extremely deep levels or unexpected cases
                            lines[i] = headingText;
                            break;
                    }

                    // Add a blank line after the heading for better readability
                    // only if there isn't already one and we're not at the end of the text
                    if (i < lines.Length - 1 && !string.IsNullOrWhiteSpace(lines[i + 1]))
                    {
                        lines[i] += "\n";
                    }
                }
            }

            return string.Join("\n", lines);
        }
    }

    

    public class ScriptingBridge : ScriptingBridge<LinkedItem>
    {
        public ScriptingBridge(IDataSources data, ITraceabilityAnalysis trace, TraceEntity sourceTraceEntity, IConfiguration config)
            : base(data, trace, sourceTraceEntity, config)
        {
        }
    }
}
