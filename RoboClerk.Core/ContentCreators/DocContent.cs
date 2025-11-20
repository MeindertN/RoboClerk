using Microsoft.CodeAnalysis.Scripting;
using RoboClerk.Core;
using System.Collections.Generic;
using System.Text;
using IConfiguration = RoboClerk.Core.Configuration.IConfiguration;

namespace RoboClerk.ContentCreators
{
    internal class DocContent : MultiItemContentCreator
    {

        public DocContent(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration conf)
            : base(data, analysis, conf)
        {

        }

        protected override ContentCreatorMetadata GetContentCreatorMetadata()
        {
            var metadata = new ContentCreatorMetadata("SLMS", "Documentation Content", 
                "Manages and displays documentation content items");
            
            metadata.Category = "Requirements & Traceability";

            var docContentTag = new ContentCreatorTag("DocContent", "Displays documentation content items");
            docContentTag.Category = "Documentation Management";
            // Common parameters will be automatically added
            docContentTag.ExampleUsage = "@@SLMS:DocContent()@@";
            metadata.Tags.Add(docContentTag);

            return metadata;
        }

        protected override string GenerateContent(IRoboClerkTag tag, List<LinkedItem> items, TraceEntity sourceTE, TraceEntity docTE)
        {
            StringBuilder output = new StringBuilder();
            var dataShare = CreateScriptingBridge(tag, sourceTE);
            var extension = (configuration.OutputFormat == "ASCIIDOC" ? "adoc" : "html");
            var fileIdentifier = configuration.ProjectID + $"./ItemTemplates/{configuration.OutputFormat}/DocContent.{extension}";
            
            // Check if compiled template already exists in cache
            ItemTemplateRenderer renderer;
            if (ItemTemplateRenderer.ExistsInCache(fileIdentifier))
            {
                renderer = ItemTemplateRenderer.FromCachedTemplate(fileIdentifier);
            }
            else
            {
                var file = data.GetTemplateFile($"./ItemTemplates/{configuration.OutputFormat}/DocContent.{extension}");
                renderer = ItemTemplateRenderer.FromString(file, fileIdentifier);
            }
            
            foreach (var item in items)
            {
                dataShare.Item = item;
                try
                {
                    var result = renderer.RenderItemTemplate(dataShare);
                    output.Append(result);
                }
                catch (CompilationErrorException e)
                {
                    logger.Error($"A compilation error occurred while compiling DocContent.adoc script: {e.Message}");
                    throw;
                }
            }
            ProcessTraces(docTE, dataShare);
            return output.ToString();
        }
    }
}
