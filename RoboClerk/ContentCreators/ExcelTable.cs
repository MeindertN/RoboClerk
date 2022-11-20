using ClosedXML.Excel;
using RoboClerk.Configuration;
using System.Text;

namespace RoboClerk.ContentCreators
{
    public class ExcelTable : ContentCreatorBase
    {
        public ExcelTable(IDataSources data, ITraceabilityAnalysis analysis)
            :base(data,analysis)
        {

        }

        public override string GetContent(RoboClerkTag tag, DocumentConfig doc)
        {
            string excelFilename = tag.GetParameterOrDefault("FILENAME", string.Empty);
            string excelWorkSheetName = tag.GetParameterOrDefault("WORKSHEET", "Sheet1");
            string excelRange = tag.GetParameterOrDefault("RANGE", string.Empty);
            XLWorkbook wb = null;
            IXLWorksheet ws = null;
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

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("|===");
            foreach( var row in ws.Range(excelRange).Rows() )
            {
                foreach( var cell in row.Cells() )
                {
                    sb.Append("| ");
                    if (cell.HasHyperlink)
                    {
                        
                        sb.Append($"{cell.GetHyperlink().ExternalAddress}[{cell.GetRichText().Text}] ");
                    }
                    else
                    {
                        string text = cell.GetRichText().Text;
                        if (text != string.Empty)
                        {
                            if (cell.Style.Font.Bold)
                            {
                                sb.Append('*');
                            }
                            if (cell.Style.Font.Italic)
                            {
                                sb.Append('_');
                            }
                            sb.Append(text);
                            if (cell.Style.Font.Italic)
                            {
                                sb.Append('_');
                            }
                            if (cell.Style.Font.Bold)
                            {
                                sb.Append('*');
                            }
                        }
                        sb.Append(' ');
                    }
                }
                sb.AppendLine();
                sb.AppendLine();
            }
            sb.AppendLine("|===");
            return sb.ToString();
        }
    }
}
