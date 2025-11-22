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

        /// <summary>
        /// Static metadata for the DocContent content creator
        /// </summary>
        public static ContentCreatorMetadata StaticMetadata { get; } = new ContentCreatorMetadata(
            "SLMS",
            "Documentation Content",
            "Manages and displays documentation content items")
        {
            Category = "Requirements & Traceability",
            Tags = new List<ContentCreatorTag>
            {
                new ContentCreatorTag("DocContent", "Displays documentation content items")
                {
                    Category = "Documentation Management",
                    Description = "Displays documentation content items with all details including content description, revision history, and traceability. " +
                        "Documentation content items are typically used for sections, procedures, or reference material that needs to be tracked and traced. " +
                        "Common filtering parameters (ItemID, ItemCategory, ItemStatus, ItemTitle, ItemProject, OlderThan, NewerThan, SortBy, SortOrder) are automatically available.",
                    ExampleUsage = "@@SLMS:DocContent()@@"
                }
            }
        };

        protected override ContentCreatorMetadata GetContentCreatorMetadata() => StaticMetadata;

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
