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

        protected override string GenerateContent(IRoboClerkTag tag, List<LinkedItem> items, TraceEntity sourceTE, TraceEntity docTE)
        {
            var dataShare = new ScriptingBridge(data, analysis, sourceTE, configuration);
            dataShare.Items = items;
            var extension = (configuration.OutputFormat == "ASCIIDOC" ? "adoc" : "html");
            var file = data.GetTemplateFile($"./ItemTemplates/{configuration.OutputFormat}/Risk.{extension}");
            var renderer = new ItemTemplateRenderer(file);
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
