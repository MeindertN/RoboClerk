using Microsoft.CodeAnalysis.Scripting;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk.ContentCreators
{
    public class SoftwareSystemTest : MultiItemContentCreator
    {
        public SoftwareSystemTest(IDataSources data, ITraceabilityAnalysis analysis)
            : base(data, analysis)
        {

        }

        protected override string GenerateADocContent(RoboClerkTag tag, List<LinkedItem> items, TraceEntity sourceTE, TraceEntity docTE)
        {
            StringBuilder output = new StringBuilder();
            var dataShare = new ScriptingBridge(data, analysis, sourceTE);
            var file = data.GetTemplateFile(@"./ItemTemplates/SoftwareSystemTest_automated.adoc");
            var rendererAutomated = new ItemTemplateRenderer(file);
            file = data.GetTemplateFile(@"./ItemTemplates/SoftwareSystemTest_manual.adoc");
            var rendererManual = new ItemTemplateRenderer(file);
            foreach (var item in items)
            {
                dataShare.Item = item;
                SoftwareSystemTestItem tc = (SoftwareSystemTestItem)item;
                var result = string.Empty;
                if (tc.TestCaseAutomated)
                {
                    try
                    {
                        result = rendererAutomated.RenderItemTemplate(dataShare);
                    }
                    catch (CompilationErrorException e)
                    {
                        logger.Error($"A compilation error occurred while compiling SoftwareSystemTest_automated.adoc script: {e.Message}");
                        throw;
                    }
                }
                else
                {
                    try
                    {
                        result = rendererManual.RenderItemTemplate(dataShare);
                    }
                    catch (CompilationErrorException e)
                    {
                        logger.Error($"A compilation error occurred while compiling SoftwareSystemTest_manual.adoc script: {e.Message}");
                        throw;
                    }
                }
                output.Append(result);
            }
            ProcessTraces(docTE, dataShare);
            return output.ToString();
        }
    }
}
