using RoboClerk.Core.Configuration;
using RoboClerk.Core;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text;

namespace RoboClerk.ContentCreators
{
    public class TemplateSection : ContentCreatorBase
    {
        public TemplateSection(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration conf)
            : base(data, analysis, conf)
        {

        }

        public override string GetContent(IRoboClerkTag tag, DocumentConfig doc)
        {
            string filename = tag.GetParameterOrDefault("FILENAME", string.Empty);
            if (filename == string.Empty)
            {
                throw new TagInvalidException(tag.Contents, $"TemplateSection tag without valid fileName parameter found in {doc.DocumentTitle}");
            }

            // Check if trying to insert DOCX into non-DOCX output format
            if (Path.GetExtension(filename).ToLowerInvariant() == ".docx")
            {
                string outputFormat = configuration.OutputFormat?.ToUpperInvariant() ?? "TEXT";
                if (outputFormat != "DOCX")
                {
                    throw new TagInvalidException(tag.Contents, 
                        $"Cannot insert DOCX file '{filename}' into {outputFormat} output format. " +
                        $"DOCX template sections are only supported when output format is DOCX.");
                }
                
                // Extract OpenXML content for DOCX output
                return ExtractOpenXmlContent(filename);
            }

            try
            {
                return data.GetTemplateFile(filename);
            }
            catch
            {
                logger.Error($"Error occurred trying to load \"{filename}\" from the template directory. Ensure \"{filename}\" is in the input directory.");
                throw;
            }
        }

        private string ExtractOpenXmlContent(string filename)
        {
            try
            {
                using (var stream = data.GetFileStreamFromTemplateDir(filename))
                using (var document = WordprocessingDocument.Open(stream, false))
                {
                    if (document.MainDocumentPart?.Document?.Body == null)
                        return string.Empty;

                    var xmlBuilder = new StringBuilder();
                    
                    // Create a special marker to indicate this is OpenXML content
                    xmlBuilder.AppendLine("<!--OPENXML_CONTENT-->");
                    
                    // Extract all body elements as XML
                    foreach (var element in document.MainDocumentPart.Document.Body.Elements())
                    {
                        // Skip sections as they contain page-level formatting we don't want to embed
                        if (element is not SectionProperties)
                        {
                            xmlBuilder.AppendLine(element.OuterXml);
                        }
                    }
                    
                    return xmlBuilder.ToString();
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error extracting OpenXML content from DOCX file \"{filename}\": {ex.Message}");
                throw;
            }
        }
    }
}
