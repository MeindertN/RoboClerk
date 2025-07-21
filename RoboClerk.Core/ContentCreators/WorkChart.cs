using RoboClerk.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RoboClerk.ContentCreators
{
    internal class WorkChart : IContentCreator
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private IDataSources data = null;
        private ITraceabilityAnalysis analysis = null;

        public WorkChart(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration configuration)
        {
            this.data = data;
            this.analysis = analysis;
        }

        public WorkChart(IDataSources data, ITraceabilityAnalysis analysis)
        {
            this.data = data;
            this.analysis = analysis;
        }

        public string GetContent(RoboClerkTag tag, DocumentConfig doc)
        {
            TraceEntity systemTruthSource = analysis.GetTraceEntityForID("SystemRequirement");
            var traceMatrixSystemLevel = analysis.PerformAnalysis(data, systemTruthSource);
            TraceEntity softwareTruthSource = analysis.GetTraceEntityForID("SoftwareRequirement");
            var traceMatrixSoftwareLevel = analysis.PerformAnalysis(data, softwareTruthSource);

            StringBuilder workChart = new StringBuilder();
            workChart.AppendLine("|====");
            workChart.Append($"| {systemTruthSource.Abbreviation} ID# ");
            workChart.Append($"| {softwareTruthSource.Abbreviation} ID# ");
            workChart.Append($"| {systemTruthSource.Name} Description ");
            workChart.Append("| Assigned to ");
            workChart.AppendLine("| Status ");

            //first sort the system requirements based on the number of specs associated with them
            List<(int, int)> sortedIndices = new List<(int, int)>();
            for (int index = 0; index < traceMatrixSystemLevel[systemTruthSource].Count; ++index)
            {
                sortedIndices.Add((index, traceMatrixSystemLevel[softwareTruthSource][index].Count(x => x != null)));
            }
            sortedIndices.Sort((a, b) => { return b.Item2.CompareTo(a.Item2); });

            foreach (var index in sortedIndices)
            {
                var systemLevelItem = traceMatrixSystemLevel[systemTruthSource][index.Item1][0];
                workChart.Append((systemLevelItem.HasLink ? $"| {systemLevelItem.Link}[{systemLevelItem.ItemID}]" : $"| {systemLevelItem.ItemID}"));
                var softwareLevelItems = traceMatrixSystemLevel[softwareTruthSource][index.Item1];
                if (softwareLevelItems.Count == 0 || softwareLevelItems[0] == null)
                {
                    workChart.Append("| ");
                }
                else
                {
                    StringBuilder sdss = new StringBuilder();
                    workChart.Append("| ");
                    bool added = false;
                    foreach (var sli in softwareLevelItems)
                    {
                        workChart.Append((sli.HasLink ? $"{sli.Link}[{sli.ItemID}]" : $"{sli.ItemID}"));
                        workChart.Append(", ");
                        added = true;
                    }
                    if (added)
                    {
                        workChart.Remove(workChart.Length - 2, 2);
                    }
                }
                RequirementItem srs = systemLevelItem as RequirementItem;
                workChart.Append($"| {srs.ItemTitle}");
                workChart.Append($"| {srs.RequirementAssignee}");
                workChart.AppendLine($"| {srs.RequirementState}");
            }
            workChart.AppendLine("|====");
            return workChart.ToString();
        }
    }
}
