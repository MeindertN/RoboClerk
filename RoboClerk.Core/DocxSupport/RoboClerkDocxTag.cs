using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using RoboClerk.Core.Configuration;

namespace RoboClerk.Core.DocxSupport
{
    /// <summary>
    /// Docx-based implementation of IRoboClerkTag using Word content controls
    /// </summary>
    public sealed class RoboClerkDocxTag : RoboClerkBaseTag
    {
        private readonly SdtElement contentControl;
        private readonly string contentControlId;
        private readonly IConfiguration? configuration;
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly string[] NewlineSplit = { "\r\n", "\r", "\n" };

        public RoboClerkDocxTag(SdtElement contentControl, IConfiguration? configuration = null)
        {
            this.contentControl = contentControl ?? throw new ArgumentNullException(nameof(contentControl));
            this.configuration = configuration;
            contentControlId = GetContentControlId();
            ParseContentControlProperties();
        }

        public override bool Inline => false; 
        public string ContentControlId => contentControlId;

        /// <summary>
        /// Gets the current content converted to raw OpenXML format.
        /// This performs the conversion on-demand to ensure the latest content is returned.
        /// </summary>
        public string GeneratedOpenXml
        {
            get
            {
                try
                {
                    // Create a temporary content element to perform the conversion
                    var tempContentElement = CreateTemporaryContentElement();
                    if (tempContentElement == null)
                        return string.Empty;

                    var fmt = CaptureOriginalFormatting(GetContentElement() ?? tempContentElement);

                    // Perform the same conversion logic as ConvertContentToOpenXml but on temp element
                    if (IsOpenXmlContent(contents))
                    {
                        ConvertEmbeddedOpenXmlToOpenXml(contents, tempContentElement);
                    }
                    else if (IsHtmlContent(contents))
                    {
                        ConvertHtmlToOpenXml(contents, tempContentElement);
                    }
                    else
                    {
                        ConvertTextToOpenXml(contents, tempContentElement, fmt);
                    }

                    // Extract the raw OpenXML from the temporary element
                    return ExtractRawOpenXml(tempContentElement);
                }
                catch (Exception ex)
                {
                    logger.Warn($"Failed to generate OpenXML for content control {contentControlId}: {ex.Message}");
                    return string.Empty;
                }
            }
        }

        public override string Contents 
        { 
            get => contents; 
            set => contents = value;
        }

        public override IEnumerable<IRoboClerkTag> ProcessNestedTags()
        {
            if (string.IsNullOrEmpty(Contents))
                return Enumerable.Empty<IRoboClerkTag>();

            // Parse nested text-based RoboClerk tags within the content control content
            var nestedRoboClerkTags = RoboClerkTextParser.ExtractRoboClerkTags(Contents);
            return nestedRoboClerkTags.Cast<IRoboClerkTag>();
        }

        /// <summary>
        /// Converts the current text content to OpenXML and updates the content control.
        /// This should be called just before saving the document.
        /// </summary>
        public void ConvertContentToOpenXml()
        {
            var contentElement = GetContentElement();
            if (contentElement is null) return;

            var fmt = CaptureOriginalFormatting(contentElement);
            contentElement.RemoveAllChildren();

            if (IsOpenXmlContent(contents)) { ConvertEmbeddedOpenXmlToOpenXml(contents, contentElement); return; }
            if (IsHtmlContent(contents)) { ConvertHtmlToOpenXml(contents, contentElement); return; }

            ConvertTextToOpenXml(contents, contentElement, fmt);
        }

        /// <summary>
        /// Creates a temporary content element for OpenXML generation without affecting the actual document
        /// </summary>
        private OpenXmlElement? CreateTemporaryContentElement()
        {
            var actualContentElement = GetContentElement();
            if (actualContentElement == null)
                return null;

            // Create a temporary element of the same type as the actual content element
            return actualContentElement switch
            {
                SdtContentBlock => new SdtContentBlock(),
                SdtContentRun => new SdtContentRun(),
                SdtContentCell => new SdtContentCell(),
                _ => new SdtContentBlock() // Default fallback
            };
        }

        /// <summary>
        /// Extracts raw OpenXML from a content element
        /// </summary>
        private static string ExtractRawOpenXml(OpenXmlElement contentElement)
        {
            if (contentElement == null || !contentElement.HasChildren)
                return string.Empty;

            var xmlBuilder = new System.Text.StringBuilder();
            foreach (var child in contentElement.ChildElements)
            {
                xmlBuilder.AppendLine(child.OuterXml);
            }
            
            return xmlBuilder.ToString().Trim();
        }

        /// <summary>
        /// Captures the original formatting from the content control before modification
        /// </summary>
        private OriginalFormatting CaptureOriginalFormatting(OpenXmlElement root)
        {
            Paragraph? firstP = null; 
            Run? firstR = null;

            foreach (var e in root.Descendants())
            {
                if (firstP is null && e is Paragraph p && p.ParagraphProperties is not null)
                    firstP = p;
                if (firstR is null && e is Run r && r.RunProperties is not null)
                    firstR = r;
                if (firstP is not null && firstR is not null) break;
            }

            return new OriginalFormatting
            {
                ParagraphProperties = (ParagraphProperties?)firstP?.ParagraphProperties?.CloneNode(true),
                RunProperties = (RunProperties?)firstR?.RunProperties?.CloneNode(true)
            };
        }

        /// <summary>
        /// Helper record to store original formatting information
        /// </summary>
        private sealed record OriginalFormatting
        {
            public ParagraphProperties? ParagraphProperties { get; init; }
            public RunProperties? RunProperties { get; init; }
        }

        private string GetContentControlId()
        {
            var properties = contentControl.SdtProperties;
            var id = properties?.GetFirstChild<SdtId>();
            return id?.Val?.Value.ToString() ?? string.Empty;
        }

        private void ParseContentControlProperties()
        {
            var properties = contentControl.SdtProperties;
            var tag = properties?.GetFirstChild<Tag>();
            
            if (tag?.Val?.Value != null)
            {
                ParseTagContents(tag.Val.Value);
            }

            // Get initial content from the structured document tag content
            var contentElement = GetContentElement();
            if (contentElement != null)
            {
                contents = ExtractFormattedText(contentElement);
            }
        }

        /// <summary>
        /// Extracts text from OpenXml element while preserving basic formatting like line breaks
        /// </summary>
        /// <param name="element">The OpenXml element to extract text from</param>
        /// <returns>Formatted text with preserved line breaks</returns>
        private static string ExtractFormattedText(OpenXmlElement element)
        {
            var sb = new System.Text.StringBuilder();
            bool lastWasNewline = false;

            foreach (var d in element.Descendants())
            {
                switch (d)
                {
                    case Text t:
                        sb.Append(t.Text);
                        lastWasNewline = false;
                        break;
                    case Break or CarriageReturn:
                        sb.AppendLine();
                        lastWasNewline = true;
                        break;
                    case Paragraph when !lastWasNewline && sb.Length > 0:
                        sb.AppendLine();
                        lastWasNewline = true;
                        break;
                    case TabChar:
                        sb.Append('\t');
                        lastWasNewline = false;
                        break;
                }
            }
            return sb.ToString().TrimEnd('\r', '\n');
        }

        private void ParseTagContents(string tagContents) => ParseCompleteTag(tagContents);
        
        private OpenXmlElement? GetContentElement()
        {
            var blockContent = contentControl.GetFirstChild<SdtContentBlock>();
            if (blockContent != null) return blockContent;

            var runContent = contentControl.GetFirstChild<SdtContentRun>();
            if (runContent != null) return runContent;

            var cellContent = contentControl.GetFirstChild<SdtContentCell>();
            return cellContent;
        }

        private void ConvertTextToOpenXml(string text, OpenXmlElement el, OriginalFormatting fmt)
        {
            if (string.IsNullOrEmpty(text)) { AppendEmpty(el, fmt); return; }

            switch (el)
            {
                case SdtContentRun:                      ConvertTextToRunContent(text, el, fmt);   break;
                case SdtContentCell or SdtContentBlock:  ConvertTextToBlockContent(text, el, fmt); break;
                default:                                 ConvertTextToBlockContent(text, el, fmt); break;
            }
        }

        private void ConvertTextToBlockContent(string text, OpenXmlElement el, OriginalFormatting fmt)
        {
            if (string.IsNullOrEmpty(text)) { AppendEmpty(el, fmt); return; }

            var lines = text.Split(NewlineSplit, StringSplitOptions.None);
            foreach (var line in lines)
                el.AppendChild(MakeParagraph(fmt, line));
        }

        private void ConvertTextToRunContent(string text, OpenXmlElement el, OriginalFormatting fmt)
        {
            var lines = text.Split(NewlineSplit, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                var r = new Run { RunProperties = (RunProperties?)fmt.RunProperties?.CloneNode(true) };
                if (!string.IsNullOrEmpty(lines[i])) r.AppendChild(new Text(lines[i]));
                if (i < lines.Length - 1) r.AppendChild(new Break());
                el.AppendChild(r);
            }
        }

        private static Paragraph MakeParagraph(OriginalFormatting fmt, string text)
        {
            var p = new Paragraph { ParagraphProperties = (ParagraphProperties?)fmt.ParagraphProperties?.CloneNode(true) };
            var r = new Run { RunProperties = (RunProperties?)fmt.RunProperties?.CloneNode(true) };
            if (!string.IsNullOrEmpty(text)) r.AppendChild(new Text(text));
            p.AppendChild(r); 
            return p;
        }

        private static void AppendEmpty(OpenXmlElement contentElement, OriginalFormatting fmt)
        {
            switch (contentElement)
            {
                case SdtContentBlock or SdtContentCell:
                    var p = new Paragraph { ParagraphProperties = (ParagraphProperties?)fmt.ParagraphProperties?.CloneNode(true) };
                    p.AppendChild(new Run { RunProperties = (RunProperties?)fmt.RunProperties?.CloneNode(true) });
                    contentElement.AppendChild(p);
                    break;
                case SdtContentRun:
                    contentElement.AppendChild(new Run { RunProperties = (RunProperties?)fmt.RunProperties?.CloneNode(true) });
                    break;
            }
        }

        // Keep the original overload for backward compatibility with existing calls
        private void ConvertTextToOpenXml(string text, OpenXmlElement el)
            => ConvertTextToOpenXml(text, el, new OriginalFormatting());

        private static bool IsOpenXmlContent(string content) => content.Contains("<!--OPENXML_CONTENT-->");

        private static bool IsHtmlContent(string content) => content.Contains("<") && content.Contains(">") && !IsOpenXmlContent(content);

        private void ConvertEmbeddedOpenXmlToOpenXml(string openXmlContent, OpenXmlElement contentElement)
        {
            try
            {
                // Remove the marker and clean content
                var cleanedContent = openXmlContent.Replace("<!--OPENXML_CONTENT-->", "").Trim();
                
                if (string.IsNullOrEmpty(cleanedContent))
                {
                    // Empty content - create empty paragraph
                    AppendEmpty(contentElement, new OriginalFormatting());
                    return;
                }

                // Parse the XML content directly using OpenXML
                var lines = cleanedContent.Split(NewlineSplit, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;
                        
                    try
                    {
                        // Parse each OpenXML element and add it to the content element
                        var parsedElement = ParseOpenXmlElement(line.Trim());
                        if (parsedElement != null)
                        {
                            contentElement.AppendChild(parsedElement);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Warn($"Failed to parse OpenXML line, treating as text: {ex.Message}");
                        // Fallback: treat as text in a paragraph
                        AppendEmpty(contentElement, new OriginalFormatting());
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to convert embedded OpenXML content: {ex.Message}");
                // Fallback to plain text
                ConvertTextToOpenXml(openXmlContent, contentElement);
            }
        }

        private static OpenXmlElement? ParseOpenXmlElement(string xmlString)
        {
            try
            {
                // Create a temporary document to help parse the XML
                var tempDoc = new Document();
                tempDoc.InnerXml = $"<w:body xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">{xmlString}</w:body>";
                
                // Get the first child element from the temporary body
                var firstElement = tempDoc.Body?.FirstChild;
                if (firstElement != null)
                {
                    // Clone the element to avoid ownership issues
                    return firstElement.CloneNode(true);
                }
                
                return null;
            }
            catch (Exception ex)
            {
                logger.Debug($"Failed to parse OpenXML element: {ex.Message}");
                
                // Alternative approach: try to create specific element types based on XML content
                try
                {
                    if (xmlString.Contains("<w:p ") || xmlString.StartsWith("<w:p>"))
                    {
                        var paragraph = new Paragraph();
                        paragraph.InnerXml = xmlString;
                        return paragraph;
                    }
                    else if (xmlString.Contains("<w:tbl ") || xmlString.StartsWith("<w:tbl>"))
                    {
                        var table = new Table();
                        table.InnerXml = xmlString;
                        return table;
                    }
                    else if (xmlString.Contains("<w:r ") || xmlString.StartsWith("<w:r>"))
                    {
                        var run = new Run();
                        run.InnerXml = xmlString;
                        return run;
                    }
                    else
                    {
                        // Default to paragraph
                        var paragraph = new Paragraph();
                        paragraph.InnerXml = xmlString;
                        return paragraph;
                    }
                }
                catch
                {
                    return null;
                }
            }
        }

        private void ConvertHtmlToOpenXml(string htmlContent, OpenXmlElement contentElement)
        {
            try
            {
                // Get the main document part from the content control
                var mainPart = GetMainDocumentPart();
                if (mainPart != null)
                {
                    HtmlToOpenXml.HtmlConverter converter;
                    
                    // Configure BaseImageUrl if configuration is available
                    if (configuration?.OutputDir != null)
                    {
                        // Convert the output directory to a proper file URI
                        var outputDirUri = new Uri(configuration.OutputDir.Replace('\\', '/'), UriKind.RelativeOrAbsolute);
                        if (!outputDirUri.IsAbsoluteUri)
                        {
                            // If it's a relative path, make it absolute
                            outputDirUri = new Uri(Path.GetFullPath(configuration.OutputDir));
                        }
                        
                        var webRequest = new HtmlToOpenXml.IO.DefaultWebRequest()
                        {
                            BaseImageUrl = outputDirUri
                        };
                        converter = new HtmlToOpenXml.HtmlConverter(mainPart, webRequest);
                    }
                    else
                    {
                        converter = new HtmlToOpenXml.HtmlConverter(mainPart);
                    }
                    
                    var paragraphs = converter.Parse(htmlContent);

                    foreach (var paragraph in paragraphs)
                    {
                        contentElement.AppendChild(paragraph);
                    }
                }
            }
            catch (Exception ex)
            {
                // Fallback to plain text if HTML conversion fails
                logger.Warn($"HTML conversion failed, using plain text: {ex.Message}");
                ConvertTextToOpenXml(htmlContent, contentElement);
            }
        }

        private MainDocumentPart? GetMainDocumentPart()
        {
            // Navigate up to find the WordprocessingDocument
            var current = contentControl.Parent;
            while (current != null)
            {
                if (current is Document document)
                {
                    return document.MainDocumentPart;
                }
                current = current.Parent;
            }
            return null;
        }
    }
}
