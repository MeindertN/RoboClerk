using Microsoft.CodeAnalysis.Scripting;
using RoboClerk.Configuration;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk.ContentCreators
{
    public class UnitTest : MultiItemContentCreator
    {
        public UnitTest(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration conf)
            : base(data, analysis, conf)
        {

        }

        protected override string GenerateADocContent(RoboClerkTag tag, List<LinkedItem> items, TraceEntity sourceTE, TraceEntity docTE)
        {
            var dataShare = new ScriptingBridge(data, analysis, sourceTE);
            if (tag.HasParameter("BRIEF") && tag.GetParameterOrDefault("BRIEF").ToUpper() == "TRUE")
            {
                //this will print a brief list of all soups and versions that Roboclerk knows about
                dataShare.Items = items;
                var file = data.GetTemplateFile(@"./ItemTemplates/UnitTest_brief.adoc");
                var renderer = new ItemTemplateRenderer(file);
                var result = renderer.RenderItemTemplate(dataShare);
                ProcessTraces(docTE, dataShare);
                return result;
            }
            else
            {
                var file = data.GetTemplateFile(@"./ItemTemplates/UnitTest.adoc");
                var renderer = new ItemTemplateRenderer(file);
                StringBuilder output = new StringBuilder();
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
                        logger.Error($"A compilation error occurred while compiling UnitTest.adoc script: {e.Message}");
                        throw;
                    }
                }
                ProcessTraces(docTE, dataShare);
                return output.ToString();
            }
        }
    }
}
