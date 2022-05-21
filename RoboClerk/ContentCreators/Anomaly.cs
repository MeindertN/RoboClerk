using RoboClerk.Configuration;
using System.Text;

namespace RoboClerk.ContentCreators
{
    class Anomaly : ContentCreatorBase
    {

        private string GenerateMarkdown(AnomalyItem item, TraceEntity tet)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("|====");
            sb.Append($"| {(tet==null?"Anomaly":tet.Name)} ID: ");
            sb.AppendLine(item.HasLink ? $"| {item.ItemID}[{item.Link}]" : $"| {item.ItemID}");
            sb.AppendLine();

            sb.Append("| Revision: ");
            sb.AppendLine(item.AnomalyRevision == string.Empty ? "| N/A" : $"| {item.AnomalyRevision}");
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

            sb.Append("| Priority: ");
            sb.AppendLine(item.AnomalyPriority == string.Empty ? "| N/A" : $"| {item.AnomalyPriority}");
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
            foreach (var anomaly in anomalies)
            {
                if (anomaly.AnomalyState == "Closed")
                {
                    continue; //skip closed bugs as they are no longer outstanding
                }
                try
                {
                    output.AppendLine(GenerateMarkdown(anomaly,analysis.GetTraceEntityForID("Anomaly")));
                }
                catch
                {
                    logger.Error($"An error occurred while rendering anomaly {anomaly.ItemID} in {doc.DocumentTitle}.");
                    throw;
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
