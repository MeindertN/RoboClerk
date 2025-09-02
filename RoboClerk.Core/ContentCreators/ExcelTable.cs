using ClosedXML.Excel;
using RoboClerk.Core.Configuration;
using RoboClerk.Core;
using System.Text;

namespace RoboClerk.ContentCreators
{
    public class ExcelTable : ContentCreatorBase
    {
        public ExcelTable(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration conf)
            : base(data, analysis, conf)
        {

        }

        public override string GetContent(IRoboClerkTag tag, DocumentConfig doc)
        {
            string excelFilename = tag.GetParameterOrDefault("FILENAME", string.Empty);
            string excelWorkSheetName = tag.GetParameterOrDefault("WORKSHEET", "Sheet1");
            string excelRange = tag.GetParameterOrDefault("RANGE", string.Empty);
            XLWorkbook wb;
            IXLWorksheet ws;
            try
            {
                wb = new XLWorkbook(data.GetFileStreamFromTemplateDir(excelFilename));
                ws = wb.Worksheet(excelWorkSheetName);
            }
            catch
            {
                logger.Error($"An error occurred while trying to load worksheet \"{excelWorkSheetName}\" from excelfile \"{excelFilename}\". RoboClerk tag is in document \"{doc.DocumentTitle}\". Tag contents: \"{tag.Contents}\"");
                throw;
            }

            if (configuration.OutputFormat.ToUpper() == "ASCIIDOC")
            {
                return GenerateASCIIDocTable(ws, excelRange);
            }
            else
            {
                return GenerateHTMLTable(ws, excelRange);
            }
        }

        private string GenerateASCIIDocTable(IXLWorksheet ws, string excelRange)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("|===");
            foreach (var row in ws.Range(excelRange).Rows())
            {
                foreach (var cell in row.Cells())
                {
                    sb.Append("| ");
                    sb.Append(FormatCellContentForASCIIDoc(cell));
                    sb.Append(' ');
                }
                sb.AppendLine();
                sb.AppendLine();
            }
            sb.AppendLine("|===");
            return sb.ToString();
        }

        private string GenerateHTMLTable(IXLWorksheet ws, string excelRange)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<div>");
            sb.AppendLine("    <table border=\"1\" cellspacing=\"0\" cellpadding=\"4\">");
            foreach (var row in ws.Range(excelRange).Rows())
            {
                sb.AppendLine("        <tr>");
                foreach (var cell in row.Cells())
                {
                    sb.Append("            <td>");
                    sb.Append(FormatCellContentForHTML(cell));
                    sb.Append("</td>");
                }
                sb.AppendLine();
                sb.AppendLine("        </tr>");
            }
            sb.AppendLine("    </table>");
            sb.AppendLine("</div>");
            return sb.ToString();
        }

        private string FormatCellContentForASCIIDoc(IXLCell cell)
        {
            if (cell.HasHyperlink)
            {
                return $"{cell.GetHyperlink().ExternalAddress}[{cell.GetRichText().Text}]";
            }
            else
            {
                string text = cell.GetRichText().Text;
                if (text != string.Empty)
                {
                    if (cell.Style.Font.Bold)
                    {
                        text = $"*{text}*";
                    }
                    if (cell.Style.Font.Italic)
                    {
                        text = $"_{text}_";
                    }
                }
                return text;
            }
        }

        private string FormatCellContentForHTML(IXLCell cell)
        {
            if (cell.HasHyperlink)
            {
                return $"<a href=\"{cell.GetHyperlink().ExternalAddress}\">{cell.GetRichText().Text}</a>";
            }
            else
            {
                string text = cell.GetRichText().Text;
                if (text != string.Empty)
                {
                    if (cell.Style.Font.Bold)
                    {
                        text = $"<strong>{text}</strong>";
                    }
                    if (cell.Style.Font.Italic)
                    {
                        text = $"<em>{text}</em>";
                    }
                }
                return text;
            }
        }
    }
}
