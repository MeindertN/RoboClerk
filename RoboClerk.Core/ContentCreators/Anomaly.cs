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

        /// <summary>
        /// Static metadata for the Anomaly content creator
        /// </summary>
        public static ContentCreatorMetadata StaticMetadata { get; } = CreateAnomalyMetadata();

        private static ContentCreatorMetadata CreateAnomalyMetadata()
        {
            var parameters = new List<ContentCreatorParameter>();
            parameters.AddRange(GetCommonMultiItemParametersStatic(typeof(AnomalyItem)));
            
            // Add anomaly-specific parameters
            parameters.Add(new ContentCreatorParameter("IncludeClosed", 
                "Set to 'true' to include closed/resolved anomalies in the output", 
                ParameterValueType.Boolean, required: false, defaultValue: "false")
            {
                AllowedValues = new List<string> { "true", "false" },
                ExampleValue = "true"
            });
            parameters.Add(new ContentCreatorParameter("AnomalyState", 
                "Filter anomalies by state (e.g., 'Open', 'Closed', 'In Progress')", 
                ParameterValueType.String, required: false)
            {
                ExampleValue = "Open"
            });
            parameters.Add(new ContentCreatorParameter("AnomalyAssignee", 
                "Filter anomalies by assignee", 
                ParameterValueType.String, required: false)
            {
                ExampleValue = "John.Doe"
            });
            parameters.Add(new ContentCreatorParameter("AnomalySeverity", 
                "Filter anomalies by severity level", 
                ParameterValueType.String, required: false)
            {
                ExampleValue = "Critical"
            });
            
            var metadata = new ContentCreatorMetadata(
                "SLMS",
                "Anomaly (Bug/Issue)",
                "Manages and displays anomalies (bugs, defects, issues)")
            {
                Category = "Testing",
                Tags = new List<ContentCreatorTag>
                {
                    new ContentCreatorTag("Anomaly", "Displays detailed anomaly/bug information", "Anomaly")
                    {
                        Category = "Anomaly Management",
                        Description = "Displays anomalies with all details including state, severity, assignee, justification, and detailed description. " +
                            "By default, only open/active anomalies are shown unless IncludeClosed is set to true.",
                        Parameters = parameters,
                        ExampleUsage = "@@SLMS:Anomaly()@@"
                    }
                }
            };
            return metadata;
        }

        /// <summary>
        /// Override GetMetadata to prevent base class from adding common parameters twice.
        /// </summary>
        public override ContentCreatorMetadata GetMetadata()
        {
            return GetContentCreatorMetadata();
        }

        protected override ContentCreatorMetadata GetContentCreatorMetadata() => StaticMetadata;

        protected override string GenerateContent(IRoboClerkTag tag, List<LinkedItem> items, TraceEntity te, TraceEntity docTE)
        {
            StringBuilder output = new StringBuilder();
            var dataShare = CreateScriptingBridge(tag, te);
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
            var itemsToRender = new List<LinkedItem>();
            foreach (var item in items)
            {
                if (tag.GetParameterOrDefault("IncludeClosed", "FALSE").ToUpper() == "TRUE" ||
                     ((AnomalyItem)item).AnomalyState.ToUpper() != "CLOSED")
                {
                    itemsToRender.Add(item);
                }
            }

            int count = itemsToRender.Count;
            int index = 0;
            foreach (var item in itemsToRender)
            {
                dataShare.Item = item;
                dataShare.Index = index;
                dataShare.Count = count;
                dataShare.IsFirst = (index == 0);
                dataShare.IsLast = (index == count - 1);
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
                index++;
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
