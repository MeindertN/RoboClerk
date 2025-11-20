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

        protected override ContentCreatorMetadata GetContentCreatorMetadata()
        {
            var metadata = new ContentCreatorMetadata("SLMS", "Risk", 
                "Manages and displays risk items including risk assessments and control measures");
            
            metadata.Category = "Requirements & Traceability";

            var riskTag = new ContentCreatorTag("Risk", "Displays detailed risk information including severity, control measures, and mitigation");
            riskTag.Category = "Risk Management";
            // Common parameters will be automatically added
            riskTag.ExampleUsage = "@@SLMS:Risk()@@";
            metadata.Tags.Add(riskTag);

            return metadata;
        }

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
