using System.Text;

namespace RoboClerk.ContentCreators
{
    class Anomaly : IContentCreator
    {
        public string GetContent(RoboClerkTag tag, DataSources data, TraceabilityAnalysis analysis, string docTitle)
        {
            var bugs = data.GetAllAnomalies();
            StringBuilder output = new StringBuilder();
            foreach (var bug in bugs)
            {
                if (bug.AnomalyState == "Closed")
                {
                    continue; //skip closed bugs as they are no longer outstanding
                }
                output.AppendLine(bug.ToText());
            }
            if (bugs.Count == 0)
            {
                return $"No outstanding bugs found.";
            }
            return output.ToString();
        }
    }
}
