using Microsoft.CodeAnalysis.Scripting;
using RoboClerk.Core.Configuration;
using RoboClerk.Core;
using System.Collections.Generic;

namespace RoboClerk.ContentCreators
{
    public class Risk : MultiItemContentCreator
    {
        public Risk(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration conf)
            : base(data, analysis, conf)
        {
        }

        /// <summary>
        /// Static metadata for the Risk content creator
        /// </summary>
        public static ContentCreatorMetadata StaticMetadata { get; } = CreateRiskMetadata();

        private static ContentCreatorMetadata CreateRiskMetadata()
        {
            var parameters = new List<ContentCreatorParameter>();
            parameters.AddRange(GetCommonMultiItemParametersStatic(typeof(RiskItem)));
            
            // Add Risk specific parameters
            parameters.Add(new ContentCreatorParameter("RiskPrimaryHazard", "Filter by primary hazard", ParameterValueType.String, required: false));
            parameters.Add(new ContentCreatorParameter("RiskFailureMode", "Filter by failure mode", ParameterValueType.String, required: false));
            parameters.Add(new ContentCreatorParameter("RiskCauseOfFailure", "Filter by cause of failure", ParameterValueType.String, required: false));
            parameters.Add(new ContentCreatorParameter("RiskMethodOfDetection", "Filter by method of detection", ParameterValueType.String, required: false));
            parameters.Add(new ContentCreatorParameter("RiskOccurenceScore", "Filter by occurrence score", ParameterValueType.Integer, required: false));
            parameters.Add(new ContentCreatorParameter("RiskSeverityScore", "Filter by severity score", ParameterValueType.Integer, required: false));
            parameters.Add(new ContentCreatorParameter("RiskDetectabilityScore", "Filter by detectability score", ParameterValueType.Integer, required: false));
            parameters.Add(new ContentCreatorParameter("RiskControlMeasure", "Filter by control measure", ParameterValueType.String, required: false));
            parameters.Add(new ContentCreatorParameter("RiskControlMeasureType", "Filter by control measure type", ParameterValueType.String, required: false));
            parameters.Add(new ContentCreatorParameter("RiskControlImplementation", "Filter by control implementation", ParameterValueType.String, required: false));
            parameters.Add(new ContentCreatorParameter("RiskModifiedOccScore", "Filter by modified occurrence score", ParameterValueType.Integer, required: false));
            parameters.Add(new ContentCreatorParameter("RiskModifiedDetScore", "Filter by modified detectability score", ParameterValueType.Integer, required: false));

            var metadata = new ContentCreatorMetadata(
                "SLMS",
                "Risk",
                "Manages and displays risk items including risk assessments and control measures")
            {
                Category = "Requirements & Traceability",
                Tags = new List<ContentCreatorTag>
                {
                    new ContentCreatorTag("Risk", "Displays detailed risk information including severity, control measures, and mitigation", "Risk")
                    {
                        Category = "Risk Management",
                        Description = "Displays risk items with all details including risk description, severity assessment, probability, impact, " +
                            "control measures, mitigation strategies, and residual risk.",
                        Parameters = parameters,
                        ExampleUsage = "@@SLMS:Risk()@@"
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

        protected override string GenerateContent(IRoboClerkTag tag, List<LinkedItem> items, TraceEntity sourceTE, TraceEntity docTE)
        {
            var dataShare = CreateScriptingBridge(tag, sourceTE);
            dataShare.Items = items;
            var extension = (configuration.OutputFormat == "ASCIIDOC" ? "adoc" : "html");
            var fileIdentifier = configuration.ProjectID + $"./ItemTemplates/{configuration.OutputFormat}/Risk.{extension}";
            
            // Check if compiled template already exists in cache
            ItemTemplateRenderer renderer;
            if (ItemTemplateRenderer.ExistsInCache(fileIdentifier))
            {
                renderer = ItemTemplateRenderer.FromCachedTemplate(fileIdentifier);
            }
            else
            {
                var file = data.GetTemplateFile($"./ItemTemplates/{configuration.OutputFormat}/Risk.{extension}");
                renderer = ItemTemplateRenderer.FromString(file, fileIdentifier);
            }
            
            try
            {
                var result = renderer.RenderItemTemplate(dataShare);
                ProcessTraces(docTE, dataShare);
                return result;
            }
            catch (CompilationErrorException e)
            {
                logger.Error($"A compilation error occurred while compiling Risk.adoc script: {e.Message}");
                throw;
            }
        }
    }
}
