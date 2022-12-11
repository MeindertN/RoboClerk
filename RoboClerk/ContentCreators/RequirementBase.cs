using Microsoft.CodeAnalysis.Scripting;
using RoboClerk.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RoboClerk.ContentCreators
{
    abstract public class RequirementBase : MultiItemContentCreator
    {
        protected List<RequirementItem> requirements = null;
        protected string requirementName = string.Empty;
        protected TraceEntity sourceType = null;

        public RequirementBase(IDataSources data, ITraceabilityAnalysis analysis)
            : base(data, analysis)
        {

        }

        protected override string GenerateADocContent(RoboClerkTag tag, List<LinkedItem> items, TraceEntity te, TraceEntity docTE)
        {
            StringBuilder output = new StringBuilder();
            var dataShare = new ScriptingBridge(data, analysis, te);
            var file = data.GetTemplateFile(@"./ItemTemplates/Requirement.adoc");
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
                    logger.Error($"A compilation error occurred while compiling Requirement.adoc script: {e.Message}");
                    throw;
                }
            }
            ProcessTraces(docTE, dataShare);
            return output.ToString();
        }
    }
}
