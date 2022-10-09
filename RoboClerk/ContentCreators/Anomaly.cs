using RoboClerk.Configuration;
using System.Text;

namespace RoboClerk.ContentCreators
{
    class Anomaly : ContentCreatorBase
    {

        private string GenerateADoc(AnomalyItem item, TraceEntity tet)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("|====");
            sb.Append($"| {(tet==null?"Anomaly":tet.Name)} ID: ");
            sb.AppendLine(item.HasLink ? $"| {item.Link}[{item.ItemID}]" : $"| {item.ItemID}");
            sb.AppendLine();

            sb.Append("| Revision: ");
            sb.AppendLine(item.ItemRevision == string.Empty ? "| N/A" : $"| {item.ItemRevision}");
            sb.AppendLine();
            
            sb.Append("| State: ");
            sb.AppendLine(item.AnomalyState == string.Empty ? "| N/A" : $"| {item.AnomalyState}");
            sb.AppendLine();
            
            sb.Append("| Assigned To: ");
            sb.AppendLine(item.AnomalyAssignee == string.Empty ? "| NOT ASSIGNED" : $"| {item.AnomalyAssignee}");
            sb.AppendLine();

            sb.Append("| Title: ");
            sb.AppendLine(item.AnomalyTitle == string.Empty ? "| MISSING" : $"| {item.AnomalyTitle}");
            sb.AppendLine();

            sb.Append("| Severity: ");
            sb.AppendLine(item.AnomalySeverity == string.Empty ? "| N/A" : $"| {item.AnomalySeverity}");
            sb.AppendLine();

            sb.AppendLine("| Justification: ");
            sb.AppendLine(item.AnomalyJustification == string.Empty ? "| N/A" : $"a| {item.AnomalyJustification}");
            sb.AppendLine("|====");
            return sb.ToString();
        }

        public override string GetContent(RoboClerkTag tag, IDataSources data, ITraceabilityAnalysis analysis, DocumentConfig doc)
        {
            var anomalies = data.GetAllAnomalies();
            StringBuilder output = new StringBuilder();
            var properties = typeof(AnomalyItem).GetProperties();
            foreach (var anomaly in anomalies)
            {
                if (ShouldBeIncluded(tag, anomaly, properties) && CheckUpdateDateTime(tag,anomaly))
                {
                    if (anomaly.AnomalyState == "Closed")
                    {
                        continue; //skip closed bugs as they are no longer outstanding
                    }
                    try
                    {
                        output.AppendLine(GenerateADoc(anomaly, analysis.GetTraceEntityForID("Anomaly")));
                    }
                    catch
                    {
                        logger.Error($"An error occurred while rendering anomaly {anomaly.ItemID} in {doc.DocumentTitle}.");
                        throw;
                    }
                }
            }
            if (anomalies.Count == 0)
            {
                return $"No outstanding bugs found.";
            }
            return output.ToString();
        }
    }
}
