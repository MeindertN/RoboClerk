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
                contents = value;
                UpdateContentControl(value);
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

        private void UpdateContentControl(string newContent)
        {
            var contentElement = GetContentElement();
            if (contentElement != null)
            {
                contentElement.RemoveAllChildren();

                if (IsHtmlContent(newContent))
                {
                    ConvertHtmlToOpenXml(newContent, contentElement);
                }
                else
                {
                    // Plain text content - preserve line breaks when updating
                    ConvertTextToOpenXml(newContent, contentElement);
                }
            }
        }

        /// <summary>
        /// Converts plain text to OpenXml elements while preserving line breaks
        /// </summary>
        /// <param name="text">The text to convert</param>
        /// <param name="contentElement">The content element to append to</param>
        private void ConvertTextToOpenXml(string text, OpenXmlElement contentElement)
        {
            if (string.IsNullOrEmpty(text))
            {
                // For empty content, we still need to create a proper structure
                if (contentElement is SdtContentBlock)
                {
                    var emptyParagraph = new Paragraph(new Run(new Text("")));
                    contentElement.AppendChild(emptyParagraph);
                }
                else if (contentElement is SdtContentRun)
                {
                    var emptyRun = new Run(new Text(""));
                    contentElement.AppendChild(emptyRun);
                }
                return;
            }

            // Handle different content control types appropriately
            if (contentElement is SdtContentBlock)
            {
                ConvertTextToBlockContent(text, contentElement);
            }
            else if (contentElement is SdtContentRun)
            {
                ConvertTextToRunContent(text, contentElement);
            }
            else if (contentElement is SdtContentCell)
            {
                ConvertTextToBlockContent(text, contentElement); // Cells can contain paragraphs
            }
            else
            {
                // Fallback: try to determine what type of content to create
                ConvertTextToBlockContent(text, contentElement);
            }
        }

        private void ConvertTextToBlockContent(string text, OpenXmlElement contentElement)
        {
            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            
            if (lines.Length == 1 && !text.Contains('\n'))
            {
                // Single line - create one paragraph
                var paragraph = new Paragraph();
                var run = new Run(new Text(text));
                paragraph.AppendChild(run);
                contentElement.AppendChild(paragraph);
            }
            else
            {
                // Multiple lines - create paragraphs for each line
                for (int i = 0; i < lines.Length; i++)
                {
                    var paragraph = new Paragraph();
                    var run = new Run();
                    
                    if (!string.IsNullOrEmpty(lines[i]))
                    {
                        run.AppendChild(new Text(lines[i]));
                    }
                    
                    paragraph.AppendChild(run);
                    contentElement.AppendChild(paragraph);
                }
            }
        }

        private void ConvertTextToRunContent(string text, OpenXmlElement contentElement)
        {
            // For run content, we need to handle line breaks differently
            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            
            for (int i = 0; i < lines.Length; i++)
            {
                var run = new Run();
                
                if (!string.IsNullOrEmpty(lines[i]))
                {
                    run.AppendChild(new Text(lines[i]));
                }
                
                // Add line break for all but the last line
                if (i < lines.Length - 1)
                {
                    run.AppendChild(new Break());
                }
                
                contentElement.AppendChild(run);
            }
        }

        private bool IsHtmlContent(string content)
        {
            return content.Contains("<") && content.Contains(">");
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
