using Microsoft.CodeAnalysis.Scripting;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk.ContentCreators
{
    internal class DocContent : MultiItemContentCreator
    {

        public DocContent(IDataSources data, ITraceabilityAnalysis analysis)
            : base(data, analysis)
        {

        }

        protected override string GenerateADocContent(RoboClerkTag tag, List<LinkedItem> items, TraceEntity sourceTE, TraceEntity docTE)
        {
            StringBuilder output = new StringBuilder();
            var dataShare = new ScriptingBridge(data, analysis, sourceTE);
            var file = data.GetTemplateFile(@"./ItemTemplates/DocContent.adoc");
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
