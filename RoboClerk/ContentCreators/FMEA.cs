using Microsoft.CodeAnalysis.Scripting;
using System.Collections.Generic;

namespace RoboClerk.ContentCreators
{
    public class FMEA : MultiItemContentCreator
    {
        public FMEA(IDataSources data, ITraceabilityAnalysis analysis) 
            :base(data, analysis)
        {
        }

        protected override string GenerateADocContent(RoboClerkTag tag, List<LinkedItem> items, TraceEntity sourceTE, TraceEntity docTE)
        {
            var dataShare = new ScriptingBridge(data, analysis, sourceTE);
            var file = data.GetTemplateFile(@"./ItemTemplates/Risk.adoc");
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
