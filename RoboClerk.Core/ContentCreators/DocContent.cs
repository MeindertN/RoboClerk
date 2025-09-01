using Microsoft.CodeAnalysis.Scripting;
using RoboClerk.Core;
using System.Collections.Generic;
using System.Text;
using IConfiguration = RoboClerk.Configuration.IConfiguration;

namespace RoboClerk.ContentCreators
{
    internal class DocContent : MultiItemContentCreator
    {

        public DocContent(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration conf)
            : base(data, analysis, conf)
        {

        }

        protected override string GenerateContent(IRoboClerkTag tag, List<LinkedItem> items, TraceEntity sourceTE, TraceEntity docTE)
        {
            StringBuilder output = new StringBuilder();
            var dataShare = new ScriptingBridge(data, analysis, sourceTE, configuration);
            var extension = (configuration.OutputFormat == "ASCIIDOC" ? "adoc" : "html");
            var file = data.GetTemplateFile($"./ItemTemplates/{configuration.OutputFormat}/DocContent.{extension}");
            var renderer = new ItemTemplateRenderer(file);
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
