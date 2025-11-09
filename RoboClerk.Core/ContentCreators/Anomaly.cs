using Microsoft.CodeAnalysis.Scripting;
using RoboClerk.Core.Configuration;
using RoboClerk.Core;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk.ContentCreators
{
    public class Anomaly : MultiItemContentCreator
    {
        public Anomaly(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration config)
            : base(data, analysis, config)
        {

        }

        protected override string GenerateContent(IRoboClerkTag tag, List<LinkedItem> items, TraceEntity te, TraceEntity docTE)
        {
            StringBuilder output = new StringBuilder();
            var dataShare = new ScriptingBridge(data, analysis, te, configuration);
            var extension = (configuration.OutputFormat == "ASCIIDOC" ? "adoc" : "html");
            var fileIdentifier = configuration.ProjectID + $"./ItemTemplates/{configuration.OutputFormat}/Anomaly.{extension}";
            
            // Check if compiled template already exists in cache
            ItemTemplateRenderer renderer;
            if (ItemTemplateRenderer.ExistsInCache(fileIdentifier))
            {
                renderer = ItemTemplateRenderer.FromCachedTemplate(fileIdentifier);
            }
            else
            {
                var file = data.GetTemplateFile($"./ItemTemplates/{configuration.OutputFormat}/Anomaly.{extension}");
                renderer = ItemTemplateRenderer.FromString(file, fileIdentifier);
            }
            
            bool anomalyRendered = false;
            foreach (var item in items)
            {
                if (tag.GetParameterOrDefault("IncludeClosed", "FALSE").ToUpper() == "TRUE" ||
                     ((AnomalyItem)item).AnomalyState.ToUpper() != "CLOSED")
                {
                    dataShare.Item = item;
                    try
                    {
                        anomalyRendered = true;
                        var result = renderer.RenderItemTemplate(dataShare);
                        output.Append(result);
                    }
                    catch (CompilationErrorException e)
                    {
                        logger.Error($"A compilation error occurred while compiling Anomaly.adoc script: {e.Message}");
                        throw;
                    }
                }
            }
            // if we do custom selection just for one item type, we need to handle the case when our selection 
            // process results in no selections.
            if (!anomalyRendered)
            {
                output.Append("No outstanding Anomaly found.");
            }
            ProcessTraces(docTE, dataShare);
            return output.ToString();
        }
    }
}
