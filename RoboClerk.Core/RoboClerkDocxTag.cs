using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;

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
            // Parse the tag contents similar to RoboClerkTag
            // Format: "Source,ContentCreatorID,param1=value1,param2=value2"
            var parts = tagContents.Split(',').Select(p => p.Trim()).ToArray();
            if (parts.Length > 0)
            {
                source = GetSource(parts[0]);
                if (parts.Length > 1)
                    contentCreatorID = parts[1];
                
                // Parse parameters
                for (int i = 2; i < parts.Length; i++)
                {
                    var paramParts = parts[i].Split('=');
                    if (paramParts.Length == 2)
                    {
                        parameters[paramParts[0].Trim().ToUpper()] = paramParts[1].Trim();
                    }
                }
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
            // Find the content element and update it
            var contentElement = GetContentElement();
            if (contentElement != null)
            {
                contentElement.RemoveAllChildren();
                var paragraph = new Paragraph(new Run(new Text(newContent)));
                contentElement.AppendChild(paragraph);
            }
        }
    }
}
