using RoboClerk.Configuration;
using RoboClerk.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboClerk.ContentCreators
{
    public class Eliminated : MultiItemContentCreator
    {
        public Eliminated(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration conf)
            : base(data, analysis, conf)
        {
        }

        protected override string GenerateContent(IRoboClerkTag tag, List<LinkedItem> items, TraceEntity sourceTE, TraceEntity docTE)
        {
            var dataShare = new ScriptingBridge<EliminatedLinkedItem>(data, analysis, sourceTE, configuration);

            // Get all eliminated items based on the type requested
            List<EliminatedLinkedItem> eliminatedItems = new List<EliminatedLinkedItem>();
            string reqType = tag.GetParameterOrDefault("TYPE", "ALL").ToUpper();

            switch (reqType)
            {
                case "SYSTEM":
                    eliminatedItems.AddRange(data.GetAllEliminatedSystemRequirements());
                    break;
                case "SOFTWARE":
                    eliminatedItems.AddRange(data.GetAllEliminatedSoftwareRequirements());
                    break;
                case "DOCUMENTATION":
                    eliminatedItems.AddRange(data.GetAllEliminatedDocumentationRequirements());
                    break;
                case "TESTCASE":
                    eliminatedItems.AddRange(data.GetAllEliminatedSoftwareSystemTests());
                    break;
                case "RISK":
                    eliminatedItems.AddRange(data.GetAllEliminatedRisks());
                    break;
                case "DOCCONTENT":
                    eliminatedItems.AddRange(data.GetAllEliminatedDocContents());
                    break;
                case "ANOMALY":
                    eliminatedItems.AddRange(data.GetAllEliminatedAnomalies());
                    break;
                case "SOUP":
                    eliminatedItems.AddRange(data.GetAllEliminatedSOUP());
                    break;
                case "ALL":
                default:
                    eliminatedItems.AddRange(data.GetAllEliminatedRisks());
                    eliminatedItems.AddRange(data.GetAllEliminatedSystemRequirements());
                    eliminatedItems.AddRange(data.GetAllEliminatedSoftwareRequirements());
                    eliminatedItems.AddRange(data.GetAllEliminatedDocumentationRequirements());
                    eliminatedItems.AddRange(data.GetAllEliminatedSoftwareSystemTests());
                    eliminatedItems.AddRange(data.GetAllEliminatedDocContents());
                    eliminatedItems.AddRange(data.GetAllEliminatedAnomalies());
                    eliminatedItems.AddRange(data.GetAllEliminatedSOUP());
                    break;
            }

            if (!eliminatedItems.Any())
                return "No eliminated items found.";

            dataShare.Items = eliminatedItems;
            var file = data.GetTemplateFile($"./ItemTemplates/{configuration.OutputFormat}/Eliminated.{(configuration.OutputFormat == "HTML" ? "html" : "adoc")}");
            var renderer = new ItemTemplateRenderer(file);
            var result = renderer.RenderItemTemplate(dataShare);
            return result;
        }
    }
}
