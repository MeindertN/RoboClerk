using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using RoboClerk.Core.Configuration;
using IConfiguration = RoboClerk.Core.Configuration.IConfiguration;

namespace RoboClerk.Server.Services
{
    /// <summary>
    /// Utility class for converting content to OpenXML format for Word add-in consumption
    /// Leverages the same conversion logic as RoboClerkDocxTag but without requiring a content control
    /// </summary>
    public class OpenXmlConverter
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Converts content to OpenXML format based on content type detection
        /// </summary>
        public static string ConvertToOpenXml(string content, IConfiguration? configuration = null)
        {
            if (string.IsNullOrEmpty(content))
                return string.Empty;

            try
            {
                if (IsHtmlContent(content))
                {
                    return ConvertHtmlToOpenXml(content, configuration);
                }
                else
                {
                    return ConvertPlainTextToOpenXml(content);
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "Failed to convert content to OpenXML, returning as plain text");
                return ConvertPlainTextToOpenXml(content);
            }
        }

        /// <summary>
        /// Determines if content contains HTML
        /// </summary>
        private static bool IsHtmlContent(string content)
        {
            return content.Contains("<") && content.Contains(">") && 
                   !content.Contains("<!--OPENXML_CONTENT-->");
        }

        /// <summary>
        /// Converts HTML content to OpenXML using HtmlToOpenXml library
        /// Uses the same approach as RoboClerkDocxTag but creates the result as a string
        /// </summary>
        private static string ConvertHtmlToOpenXml(string htmlContent, IConfiguration? configuration)
        {
            try
            {
                // For now, use basic conversion to avoid enum issues
                // TODO: Implement full HtmlToOpenXml conversion when enum is resolved
                return ConvertHtmlToOpenXmlBasic(htmlContent);
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "HtmlToOpenXml conversion failed, using basic conversion");
                return ConvertHtmlToOpenXmlBasic(htmlContent);
            }
        }

        /// <summary>
        /// Basic HTML to OpenXML conversion as fallback
        /// </summary>
        private static string ConvertHtmlToOpenXmlBasic(string htmlContent)
        {
            var result = new System.Text.StringBuilder();
            
            // Basic HTML conversion
            var content = htmlContent
                .Replace("<p>", "")
                .Replace("</p>", "\n")
                .Replace("<br>", "\n")
                .Replace("<br/>", "\n")
                .Replace("<strong>", "**")
                .Replace("</strong>", "**")
                .Replace("<em>", "_")
                .Replace("</em>", "_")
                .Replace("<b>", "**")
                .Replace("</b>", "**")
                .Replace("<i>", "_")
                .Replace("</i>", "_");
            
            // Split by lines and create OpenXML paragraphs
            var lines = content.Split('\n', StringSplitOptions.None);
            foreach (var line in lines)
            {
                var escapedLine = EscapeXml(line);
                result.Append($"<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t>{escapedLine}</w:t></w:r></w:p>");
            }
            
            return result.ToString();
        }

        /// <summary>
        /// Converts plain text to OpenXML format
        /// </summary>
        private static string ConvertPlainTextToOpenXml(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            var lines = plainText.Split('\n', StringSplitOptions.None);
            var result = new System.Text.StringBuilder();
            
            foreach (var line in lines)
            {
                var escapedLine = EscapeXml(line);
                result.Append($"<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t>{escapedLine}</w:t></w:r></w:p>");
            }
            
            return result.ToString();
        }

        /// <summary>
        /// Escapes XML characters
        /// </summary>
        private static string EscapeXml(string text)
        {
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }
    }
}