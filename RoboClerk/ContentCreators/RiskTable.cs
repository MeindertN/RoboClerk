using RoboClerk.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboClerk.ContentCreators
{
    //Risk table content creator basically creates a pipe delimited table that can be used for further processing
    public class RiskTable : ContentCreatorBase
    {
        public override string GetContent(RoboClerkTag tag, IDataSources data, ITraceabilityAnalysis analysis, DocumentConfig doc)
        {
            var risks = data.GetAllRisks();
            StringBuilder sb = new StringBuilder();
            foreach (var risk in risks)
            {
                sb.Append('|');
                sb.Append(risk.ItemCategory);
                sb.Append('|');
                sb.Append(risk.ItemID);
                sb.Append('|');
                sb.Append(risk.FailureMode);
                sb.Append('|');
                sb.Append(risk.CauseOfFailure);
                sb.Append('|');
                sb.Append(risk.PrimaryHazard);
                sb.Append('|');
                sb.Append(risk.SeverityScore.ToString());
                sb.Append('|');
                sb.Append(risk.OccurenceScore.ToString());
                sb.Append('|');
                sb.Append(risk.RiskControlMeasureType);
                sb.Append('|');
                sb.Append(risk.RiskControlMeasure);
                sb.Append('|');
                sb.Append(risk.RiskControlImplementation);
                sb.Append('|');
                switch(risk.RiskControlMeasureType)
                {
                    case "SOF": sb.Append("Implemented in Phase 1 during software development"); break;
                    case "LAB": sb.Append("Implemented through a change in the labeling"); break;
                    default: sb.Append("TBD"); break;
                }
                sb.Append('|');
                var linkedItems = risk.LinkedItems.Where(x => x.LinkType == ItemLinkType.Related);
                if (risk.RiskControlMeasureType == "SOF" && linkedItems.Any()) //we only trace to related items for software risk mitigators
                {
                    var linkedItem = linkedItems.First();
                    var item = data.GetItem(linkedItem.TargetID);
                    var tet = analysis.GetTraceEntityForID(item.ItemType);
                    analysis.AddTrace(tet, linkedItem.TargetID, analysis.GetTraceEntityForTitle(doc.DocumentTitle), linkedItem.TargetID);
                    sb.Append($"See {tet.Name}: {linkedItem.TargetID}");
                }
                sb.Append('|');
                sb.Append("Incomplete");
                sb.Append('|');
                sb.Append(risk.ModifiedOccScore == int.MaxValue?"":risk.ModifiedOccScore.ToString());

                analysis.AddTrace(analysis.GetTraceEntityForID("Risk"), risk.ItemID, analysis.GetTraceEntityForTitle(doc.DocumentTitle), risk.ItemID);
            }
            return sb.ToString();
        }
    }
}
