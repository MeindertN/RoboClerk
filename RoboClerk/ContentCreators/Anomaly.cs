using DocumentFormat.OpenXml.Bibliography;
using RoboClerk.Configuration;
using System.Text;

namespace RoboClerk.ContentCreators
{
    public class Anomaly : ContentCreatorBase
    {
        public Anomaly(IDataSources data, ITraceabilityAnalysis analysis)
            : base(data, analysis)
        {

        }

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
            sb.AppendLine(item.ItemTitle == string.Empty ? "| MISSING" : $"| {item.ItemTitle}");
            sb.AppendLine();

            sb.Append("| Severity: ");
            sb.AppendLine(item.AnomalySeverity == string.Empty ? "| N/A" : $"| {item.AnomalySeverity}");
            sb.AppendLine();

            sb.AppendLine("| Justification: ");
            sb.AppendLine(item.AnomalyJustification == string.Empty ? "| N/A" : $"a| {item.AnomalyJustification}");
            sb.AppendLine("|====");
            return sb.ToString();
        }

        public override string GetContent(RoboClerkTag tag, DocumentConfig doc)
        {
            bool foundAnomaly = false;
            var sourceTraceEntity = analysis.GetTraceEntityForID("Anomaly");
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
                        foundAnomaly = true;
                        output.AppendLine(GenerateADoc(anomaly, analysis.GetTraceEntityForID("Anomaly")));
                        analysis.AddTrace(sourceTraceEntity, anomaly.ItemID, 
                            analysis.GetTraceEntityForTitle(doc.DocumentTitle), anomaly.ItemID);
                    }
                    catch
                    {
                        logger.Error($"An error occurred while rendering anomaly {anomaly.ItemID} in {doc.DocumentTitle}.");
                        throw;
                    }

                }
            }
            if (!foundAnomaly)
            {
                return $"No outstanding {sourceTraceEntity.Name} found.";
            }
            return output.ToString();
        }
    }
}
