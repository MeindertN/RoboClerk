using RoboClerk.Configuration;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk.ContentCreators
{
    internal class TraceabilityMatrix : IContentCreator
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private IDataSources data = null;
        private ITraceabilityAnalysis analysis = null;

        public TraceabilityMatrix(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration configuration)
        {
            this.data = data;
            this.analysis = analysis;
        }

        public TraceabilityMatrix(IDataSources data, ITraceabilityAnalysis analysis)
        {
            this.data = data;
            this.analysis = analysis;
        }

        public string GetContent(RoboClerkTag tag, DocumentConfig doc)
        {
            bool includeDescription = false;
            bool includeStatus = true;
            if (tag.HasParameter("includeDescription"))
                includeDescription = tag.GetParameterOrDefault("includeDescription").ToUpper()=="TRUE";
            if (tag.HasParameter("includeStatus"))
                includeStatus = tag.GetParameterOrDefault("includeStatus").ToUpper() == "TRUE";
            
            TraceEntity systemTruthSource = analysis.GetTraceEntityForID("SystemRequirement");
            var traceMatrixSystemLevel = analysis.PerformAnalysis(data, systemTruthSource);
            TraceEntity softwareTruthSource = analysis.GetTraceEntityForID("SoftwareRequirement");
            var traceMatrixSoftwareLevel = analysis.PerformAnalysis(data, softwareTruthSource);
            TraceEntity riskSource = analysis.GetTraceEntityForID("Risk");

            StringBuilder matrix = new StringBuilder();
            matrix.AppendLine("|====");
            matrix.Append("| SRS ID# ");
            matrix.Append("| SRS Element");
            if (includeDescription)
            {
                matrix.Append("| SRS Description ");
            }
            matrix.Append("| SDS ID# ");
            matrix.Append("| SDS Element");
            if (includeDescription)
            {
                matrix.Append("| SDS Description ");
            }
            matrix.Append("| Validation ID# ");
            if (includeStatus)
            {
                matrix.AppendLine("| Status");
            }
            else
            {
                matrix.AppendLine();
            }

            for (int index = 0; index < traceMatrixSystemLevel[systemTruthSource].Count; ++index)
            {
                //go over all system requirements
                List<List<string>> lines = new List<List<string>>();
                List<string> activeLine = new List<string>();

                var systemLevelItem = traceMatrixSystemLevel[systemTruthSource][index][0];
                logger.Debug($"Working on system level item {systemLevelItem.ItemID}");
                activeLine.Add($"| {(systemLevelItem.HasLink ? $"{systemLevelItem.Link}[{systemLevelItem.ItemID}]" : systemLevelItem.ItemID)} ");
                activeLine.Add($"| {(systemLevelItem as RequirementItem).ItemTitle} ");
                if (includeDescription)
                    activeLine.Add($"a| {(systemLevelItem as RequirementItem).RequirementDescription} ");
                var softwareLevelItems = traceMatrixSystemLevel[softwareTruthSource][index];
                if (softwareLevelItems.Count == 0 || softwareLevelItems[0] == null)
                {
                    logger.Debug("No software level requirements found");
                    activeLine.Add("| MISSING ");
                    activeLine.Add("| MISSING ");
                    if (includeDescription)
                        activeLine.Add("| MISSING ");
                    activeLine.Add("| N/A ");
                    if (includeStatus)
                        activeLine.Add("| N/A ");
                    lines.Add(activeLine);
                }
                else
                {
                    foreach (var sli in softwareLevelItems)
                    {
                        logger.Debug($"Processing software level item {sli.ItemID} of type {sli.ItemType}");
                        RequirementItem sliReq = (sli as RequirementItem) ?? new RequirementItem(RequirementType.SoftwareRequirement);
                        logger.Debug($"Requirement title is {sliReq.ItemTitle}");
                        List<string> tempLine = new List<string>(activeLine);
                        tempLine.Add((sliReq.HasLink ? $"| {sliReq.Link}[{sliReq.ItemID}] " : $"| {sliReq.ItemID} "));
                        tempLine.Add($"| {sliReq.ItemTitle} ");
                        if (includeDescription)
                            tempLine.Add($"a| {sliReq.RequirementDescription} ");
                        for (int i = 0; i < traceMatrixSoftwareLevel[softwareTruthSource].Count; i++)
                        {
                            if (traceMatrixSoftwareLevel[softwareTruthSource][i][0].ItemID == sliReq.ItemID)
                            {
                                var tet = analysis.GetTraceEntityForID("SoftwareSystemTest");
                                if (tet == null || !traceMatrixSoftwareLevel.ContainsKey(tet))
                                {
                                    logger.Warn("Unable to find source of verification and validation data.");
                                    continue;
                                }
                                var systemTests = traceMatrixSoftwareLevel[tet][i];
                                if (systemTests.Count == 0)
                                {
                                    tempLine.Add("| N/A ");
                                    if (includeStatus)
                                        tempLine.Add("| N/A ");
                                }
                                else if (systemTests[0] == null)
                                {
                                    tempLine.Add("| MISSING ");
                                    if (includeStatus)
                                        tempLine.Add("| N/A ");    
                                }
                                else
                                {
                                    StringBuilder sb = new StringBuilder();
                                    sb.Append("| ");
                                    foreach (var test in systemTests)
                                    {
                                        sb.Append(test.HasLink ? $"{test.Link}[{test.ItemID}]" : test.ItemID);
                                        sb.Append(", ");
                                    }
                                    sb.Remove(sb.Length - 2, 2); //remove extra comma and space
                                    sb.Append(" ");
                                    tempLine.Add(sb.ToString());
                                    if (includeStatus)
                                        tempLine.Add("| Pass ");
                                    //figure out if there is a linked risk to the system level requirement
                                    /*List<Item> rarItems = new List<Item>();
                                    if (traceMatrixSystemLevel.ContainsKey(riskSource)) //ensure trace to risk is there
                                    {
                                        rarItems = traceMatrixSystemLevel[riskSource][index];
                                    }
                                    if (rarItems.Count == 0)
                                    {
                                        if (systemLevelItem.ItemCategory == "Risk Control Measure")
                                        {
                                            tempLine.Add("MISSING");
                                        }
                                        else
                                        {
                                            tempLine.Add("N/A");
                                        }
                                    }
                                    else
                                    {
                                        sb.Clear();
                                        foreach (var rarItem in rarItems)
                                        {
                                            if (rarItem == null)
                                            {
                                                sb.Append("MISSING");
                                            }
                                            else
                                            {
                                                sb.Append(rarItem.HasLink ? $"{rarItem.Link}[{rarItem.ItemID}]" : rarItem.ItemID);
                                            }
                                            sb.Append(", ");
                                        }
                                        sb.Remove(sb.Length - 2, 2); //remove extra comma and space
                                        tempLine.Add(sb.ToString());
                                    }*/
                                }
                                lines.Add(tempLine);
                                break;
                            }
                        }
                    }
                }
                foreach (var line in lines)
                {
                    foreach (var item in line)
                    {
                        matrix.Append(item);
                    }
                    matrix.AppendLine();
                }
            }
            matrix.AppendLine("|====");
            return matrix.ToString();
        }
    }
}
