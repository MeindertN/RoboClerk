using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace RoboClerk.Core
{
    /// <summary>
    /// Docx-based implementation of IRoboClerkTag using Word content controls
    /// </summary>
    public class RoboClerkDocxTag : RoboClerkBaseTag
    {
        private readonly SdtElement contentControl;
        private readonly string contentControlId;

        public RoboClerkDocxTag(SdtElement contentControl)
        {
            this.contentControl = contentControl ?? throw new ArgumentNullException(nameof(contentControl));
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
                contents = contentElement.InnerText;
            }
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
                    // Plain text content
                    var paragraph = new Paragraph(new Run(new Text(newContent)));
                    contentElement.AppendChild(paragraph);
                }
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
                    var converter = new HtmlToOpenXml.HtmlConverter(mainPart);
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

                var paragraph = new Paragraph(new Run(new Text(htmlContent)));
                contentElement.AppendChild(paragraph);
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
