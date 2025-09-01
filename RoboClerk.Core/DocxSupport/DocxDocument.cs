using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using RoboClerk.Configuration;

namespace RoboClerk.Core.DocxSupport
{
    /// <summary>
    /// Docx document implementation that supports RoboClerk tags via content controls
    /// </summary>
    public class DocxDocument : IDocument
    {
        private string title = string.Empty;
        private string templateFile = string.Empty;
        private WordprocessingDocument? wordDocument;
        private List<RoboClerkDocxTag> docxTags = new List<RoboClerkDocxTag>();
        private readonly IConfiguration? configuration;

        public DocxDocument(string title, string templateFile, IConfiguration? configuration = null)
        {
            this.title = title;
            this.templateFile = templateFile;
            this.configuration = configuration;
        }

        public void FromStream(Stream stream)
        {
            // Create a copy of the stream in memory since we need to be able to write to it
            var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            memoryStream.Position = 0;

            wordDocument = WordprocessingDocument.Open(memoryStream, true);
            ParseContentControls();
        }

        public void SaveToFile(string filePath)
        {
            if (wordDocument == null)
                throw new InvalidOperationException("Document not loaded. Call FromStream first.");

            // Ensure the document is properly saved
            wordDocument.Save();

            // Clone the document to the specified file path
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite))
            {
                wordDocument.Clone(fileStream);
            }
        }

        public string ToText()
        {
            if (wordDocument?.MainDocumentPart?.Document?.Body == null)
                return string.Empty;

            return wordDocument.MainDocumentPart.Document.Body.InnerText;
        }

        public void FromString(string content)
        {
            // For docx documents, we can't directly update from string content
            // This method is mainly for text documents, but we'll provide a basic implementation
            if (wordDocument?.MainDocumentPart?.Document?.Body != null)
            {
                wordDocument.MainDocumentPart.Document.Body.RemoveAllChildren();
                var paragraph = new Paragraph(new Run(new Text(content)));
                wordDocument.MainDocumentPart.Document.Body.AppendChild(paragraph);
                
                // Re-parse content controls
                ParseContentControls();
            }
        }

        public DocumentType DocumentType => DocumentType.Docx;

        public IEnumerable<IRoboClerkTag> RoboClerkTags => docxTags.Cast<IRoboClerkTag>();

        public string Title
        {
            get => title;
            set => title = value;
        }

        public string TemplateFile => templateFile;

        private void ParseContentControls()
        {
            docxTags.Clear();

            if (wordDocument?.MainDocumentPart?.Document == null)
                return;

            var contentControls = wordDocument.MainDocumentPart.Document.Descendants<SdtElement>().ToList();
            
            foreach (var contentControl in contentControls)
            {
                try
                {
                    var docxTag = new RoboClerkDocxTag(contentControl, configuration);
                    
                    // Only add tags that have a valid source (not Unknown)
                    if (docxTag.Source != DataSource.Unknown)
                    {
                        docxTags.Add(docxTag);
                    }
                }
                catch (Exception ex)
                {
                    // Log warning for invalid content control but continue processing
                    var logger = NLog.LogManager.GetCurrentClassLogger();
                    logger.Warn($"Failed to parse content control as RoboClerk tag: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            wordDocument?.Dispose();
        }
    }
}
