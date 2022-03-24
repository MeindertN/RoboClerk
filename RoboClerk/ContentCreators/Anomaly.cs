using RoboClerk.Configuration;
using System.Text;

namespace RoboClerk.ContentCreators
{
    class Anomaly : ContentCreatorBase
    {
        public override string GetContent(RoboClerkTag tag, IDataSources data, ITraceabilityAnalysis analysis, DocumentConfig doc)
        {
            var bugs = data.GetAllAnomalies();
            StringBuilder output = new StringBuilder();
            foreach (var bug in bugs)
            {
                if (bug.AnomalyState == "Closed")
                {
                    continue; //skip closed bugs as they are no longer outstanding
                }
                try
                {
                    output.AppendLine(bug.ToText());
                }
                catch
                {
                    logger.Error($"An error occurred while rendering anomaly {bug.ItemID} in {doc.DocumentTitle}.");
                    throw;
                }
            }
            if (bugs.Count == 0)
            {
                return $"No outstanding bugs found.";
            }
            return output.ToString();
        }
    }
}
