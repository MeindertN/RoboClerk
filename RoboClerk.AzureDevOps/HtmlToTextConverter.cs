using System.Collections.Generic;
using System.Text;
using HtmlAgilityPack;

// credit where credit is due, this was shamelessly taken from
// the following stack overflow article: https://stackoverflow.com/a/30088920

namespace RoboClerk.AzureDevOps
{
    public class HtmlToTextConverter
    {
        public static string ToPlainText(string withHTML)
        {
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(withHTML);
            var builder = new System.Text.StringBuilder();
            var state = ToPlainTextState.StartLine;

            Plain(builder, ref state, new[] { htmlDoc.DocumentNode });
            return builder.ToString();
        }
        private static void Plain(StringBuilder builder, ref ToPlainTextState state, IEnumerable<HtmlAgilityPack.HtmlNode> nodes)
        {
            foreach (var node in nodes)
            {
                if (node is HtmlAgilityPack.HtmlTextNode)
                {
                    var text = (HtmlAgilityPack.HtmlTextNode)node;
                    Process(builder, ref state, HtmlAgilityPack.HtmlEntity.DeEntitize(text.Text).ToCharArray());
                }
                else
                {
                    var tag = node.Name.ToLower();

                    if (tag == "br")
                    {
                        builder.AppendLine();
                        state = ToPlainTextState.StartLine;
                    }
                    else if (NonVisibleTags.Contains(tag))
                    {
                    }
                    else if (InlineTags.Contains(tag))
                    {
                        Plain(builder, ref state, node.ChildNodes);
                    }
                    else
                    {
                        if (state != ToPlainTextState.StartLine)
                        {
                            builder.AppendLine();
                            state = ToPlainTextState.StartLine;
                        }
                        Plain(builder, ref state, node.ChildNodes);
                        if (state != ToPlainTextState.StartLine)
                        {
                            builder.AppendLine();
                            state = ToPlainTextState.StartLine;
                        }
                    }

                }

            }
        }

        private static void Process(System.Text.StringBuilder builder, ref ToPlainTextState state, params char[] chars)
        {
            foreach (var ch in chars)
            {
                if (char.IsWhiteSpace(ch))
                {
                    if (IsHardSpace(ch))
                    {
                        if (state == ToPlainTextState.WhiteSpace)
                            builder.Append(' ');
                        builder.Append(' ');
                        state = ToPlainTextState.NotWhiteSpace;
                    }
                    else
                    {
                        if (state == ToPlainTextState.NotWhiteSpace)
                            state = ToPlainTextState.WhiteSpace;
                    }
                }
                else
                {
                    if (state == ToPlainTextState.WhiteSpace)
                        builder.Append(' ');
                    builder.Append(ch);
                    state = ToPlainTextState.NotWhiteSpace;
                }
            }
        }
        private static bool IsHardSpace(char ch)
        {
            return ch == 0xA0 || ch == 0x2007 || ch == 0x202F;
        }

        private static readonly HashSet<string> InlineTags = new HashSet<string>
    {
        //from https://developer.mozilla.org/en-US/docs/Web/HTML/Inline_elemente
        "b", "big", "i", "small", "tt", "abbr", "acronym",
        "cite", "code", "dfn", "em", "kbd", "strong", "samp",
        "var", "a", "bdo", "br", "img", "map", "object", "q",
        "script", "span", "sub", "sup", "button", "input", "label",
        "select", "textarea"
    };

        private static readonly HashSet<string> NonVisibleTags = new HashSet<string>
    {
        "script", "style"
    };

        private enum ToPlainTextState
        {
            StartLine = 0,
            NotWhiteSpace,
            WhiteSpace,
        }
    }
}
