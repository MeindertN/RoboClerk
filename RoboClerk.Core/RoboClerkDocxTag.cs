using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using RoboClerk.Configuration;

namespace RoboClerk.Core
{
    /// <summary>
    /// Docx-based implementation of IRoboClerkTag using Word content controls
    /// </summary>
    public class RoboClerkDocxTag : RoboClerkBaseTag
    {
        private readonly SdtElement contentControl;
        private readonly string contentControlId;
        private readonly IConfiguration? configuration;
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public RoboClerkDocxTag(SdtElement contentControl, IConfiguration? configuration = null)
        {
            this.contentControl = contentControl ?? throw new ArgumentNullException(nameof(contentControl));
            this.configuration = configuration;
            this.contentControlId = GetContentControlId();
            ParseContentControlProperties();
        }

        public override bool Inline => false; 
        public string ContentControlId => contentControlId;

        public override string Contents 
        { 
            get => contents; 
            set 
            {
                // Just store the content as text - don't update OpenXML immediately
                contents = value;
            }
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
            if (contentElement != null)
            {
                // Capture original formatting before removing children
                var originalFormatting = CaptureOriginalFormatting(contentElement);
                
                contentElement.RemoveAllChildren();

                if (IsOpenXmlContent(contents))
                {
                    ConvertEmbeddedOpenXmlToOpenXml(contents, contentElement);
                }
                else if (IsHtmlContent(contents))
                {
                    ConvertHtmlToOpenXml(contents, contentElement);
                }
                else
                {
                    // Plain text content - preserve line breaks and original formatting when updating
                    ConvertTextToOpenXml(contents, contentElement, originalFormatting);
                }
            }
        }

        /// <summary>
        /// Captures the original formatting from the content control before modification
        /// </summary>
        private OriginalFormatting CaptureOriginalFormatting(OpenXmlElement contentElement)
        {
            var formatting = new OriginalFormatting();
            
            // Capture paragraph properties
            var firstParagraph = contentElement.Descendants<Paragraph>().FirstOrDefault();
            if (firstParagraph?.ParagraphProperties != null)
            {
                formatting.ParagraphProperties = (ParagraphProperties)firstParagraph.ParagraphProperties.CloneNode(true);
            }
            
            // Capture run properties
            var firstRun = contentElement.Descendants<Run>().FirstOrDefault();
            if (firstRun?.RunProperties != null)
            {
                formatting.RunProperties = (RunProperties)firstRun.RunProperties.CloneNode(true);
            }
            
            return formatting;
        }

        /// <summary>
        /// Helper class to store original formatting information
        /// </summary>
        private class OriginalFormatting
        {
            public ParagraphProperties? ParagraphProperties { get; set; }
            public RunProperties? RunProperties { get; set; }
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
        private string ExtractFormattedText(OpenXmlElement element)
        {
            var textBuilder = new System.Text.StringBuilder();
            
            foreach (var descendant in element.Descendants())
            {
                switch (descendant)
                {
                    case Text text:
                        textBuilder.Append(text.Text);
                        break;
                    case Break br:
                        textBuilder.AppendLine();
                        break;
                    case CarriageReturn cr:
                        textBuilder.AppendLine();
                        break;
                    case Paragraph p when textBuilder.Length > 0 && !textBuilder.ToString().EndsWith("\n"):
                        // Add line break after paragraph if not already there
                        textBuilder.AppendLine();
                        break;
                    case TabChar:
                        textBuilder.Append('\t');
                        break;
                }
            }
            
            // Clean up trailing newlines but preserve internal formatting
            return textBuilder.ToString().TrimEnd('\r', '\n');
        }

        private void ParseTagContents(string tagContents)
        {
            try
            {
                ParseCompleteTag(tagContents);
            }
            catch (TagInvalidException e)
            {
                // For DOCX tags, we don't have document position info, but we can still provide context
                throw;
            }
        }
        
        private OpenXmlElement? GetContentElement()
        {
            // Try to find different types of content elements in priority order
            var blockContent = contentControl.Descendants<SdtContentBlock>().FirstOrDefault();
            if (blockContent != null) return blockContent;

            var runContent = contentControl.Descendants<SdtContentRun>().FirstOrDefault();
            if (runContent != null) return runContent;

            var cellContent = contentControl.Descendants<SdtContentCell>().FirstOrDefault();
            if (cellContent != null) return cellContent;

            return null;
        }

        private void ConvertTextToOpenXml(string text, OpenXmlElement contentElement, OriginalFormatting originalFormatting)
        {
            if (string.IsNullOrEmpty(text))
            {
                // For empty content, we still need to create a proper structure while preserving original formatting
                if (contentElement is SdtContentBlock)
                {
                    var emptyParagraph = CreateParagraphWithFormatting(originalFormatting);
                    emptyParagraph.AppendChild(CreateRunWithFormatting(originalFormatting, ""));
                    contentElement.AppendChild(emptyParagraph);
                }
                else if (contentElement is SdtContentRun)
                {
                    var emptyRun = CreateRunWithFormatting(originalFormatting, "");
                    contentElement.AppendChild(emptyRun);
                }
                return;
            }

            // Handle different content control types appropriately
            if (contentElement is SdtContentBlock)
            {
                ConvertTextToBlockContent(text, contentElement, originalFormatting);
            }
            else if (contentElement is SdtContentRun)
            {
                ConvertTextToRunContent(text, contentElement, originalFormatting);
            }
            else if (contentElement is SdtContentCell)
            {
                ConvertTextToBlockContent(text, contentElement, originalFormatting); // Cells can contain paragraphs
            }
            else
            {
                // Fallback: try to determine what type of content to create
                ConvertTextToBlockContent(text, contentElement, originalFormatting);
            }
        }

        private void ConvertTextToBlockContent(string text, OpenXmlElement contentElement, OriginalFormatting originalFormatting)
        {
            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            
            if (lines.Length == 1 && !text.Contains('\n'))
            {
                // Single line - create one paragraph with preserved formatting
                var paragraph = CreateParagraphWithFormatting(originalFormatting);
                var run = CreateRunWithFormatting(originalFormatting, text);
                paragraph.AppendChild(run);
                contentElement.AppendChild(paragraph);
            }
            else
            {
                // Multiple lines - create paragraphs for each line with preserved formatting
                for (int i = 0; i < lines.Length; i++)
                {
                    var paragraph = CreateParagraphWithFormatting(originalFormatting);
                    var run = CreateRunWithFormatting(originalFormatting, lines[i]);
                    paragraph.AppendChild(run);
                    contentElement.AppendChild(paragraph);
                }
            }
        }

        private void ConvertTextToRunContent(string text, OpenXmlElement contentElement, OriginalFormatting originalFormatting)
        {
            // For run content, we need to handle line breaks differently while preserving formatting
            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            
            for (int i = 0; i < lines.Length; i++)
            {
                var run = CreateRunWithFormatting(originalFormatting, lines[i]);
                
                // Add line break for all but the last line
                if (i < lines.Length - 1)
                {
                    run.AppendChild(new Break());
                }
                
                contentElement.AppendChild(run);
            }
        }

        private Paragraph CreateParagraphWithFormatting(OriginalFormatting originalFormatting)
        {
            var paragraph = new Paragraph();
            
            if (originalFormatting.ParagraphProperties != null)
            {
                // Clone the paragraph properties to preserve formatting
                paragraph.ParagraphProperties = (ParagraphProperties)originalFormatting.ParagraphProperties.CloneNode(true);
            }
            
            return paragraph;
        }

        private Run CreateRunWithFormatting(OriginalFormatting originalFormatting, string text)
        {
            var run = new Run();
            
            if (originalFormatting.RunProperties != null)
            {
                // Clone the run properties to preserve formatting
                run.RunProperties = (RunProperties)originalFormatting.RunProperties.CloneNode(true);
            }
            
            if (!string.IsNullOrEmpty(text))
            {
                run.AppendChild(new Text(text));
            }
            
            return run;
        }

        // Keep the original overload for backward compatibility with existing calls
        private void ConvertTextToOpenXml(string text, OpenXmlElement contentElement)
        {
            var emptyFormatting = new OriginalFormatting();
            ConvertTextToOpenXml(text, contentElement, emptyFormatting);
        }

        private bool IsOpenXmlContent(string content)
        {
            return content.Contains("<!--OPENXML_CONTENT-->");
        }

        private bool IsHtmlContent(string content)
        {
            return content.Contains("<") && content.Contains(">") && !IsOpenXmlContent(content);
        }

        private void ConvertEmbeddedOpenXmlToOpenXml(string openXmlContent, OpenXmlElement contentElement)
        {
            try
            {
                // Remove the marker and clean content
                var cleanedContent = openXmlContent.Replace("<!--OPENXML_CONTENT-->", "").Trim();
                
                if (string.IsNullOrEmpty(cleanedContent))
                {
                    // Empty content - create empty paragraph
                    if (contentElement is SdtContentBlock)
                    {
                        contentElement.AppendChild(new Paragraph(new Run(new Text(""))));
                    }
                    else if (contentElement is SdtContentRun)
                    {
                        contentElement.AppendChild(new Run(new Text("")));
                    }
                    return;
                }

                // Parse the XML content directly using OpenXML
                var lines = cleanedContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                
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
                        if (contentElement is SdtContentBlock)
                        {
                            var paragraph = new Paragraph(new Run(new Text(line)));
                            contentElement.AppendChild(paragraph);
                        }
                        else if (contentElement is SdtContentRun)
                        {
                            var run = new Run(new Text(line));
                            contentElement.AppendChild(run);
                        }
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

        private OpenXmlElement? ParseOpenXmlElement(string xmlString)
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
                    return (OpenXmlElement)firstElement.CloneNode(true);
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
                var logger = NLog.LogManager.GetCurrentClassLogger();
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
