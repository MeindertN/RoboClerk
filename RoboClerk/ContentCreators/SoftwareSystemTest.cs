using Microsoft.CodeAnalysis.Scripting;
using RoboClerk.Configuration;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk.ContentCreators
{
    public class SoftwareSystemTest : MultiItemContentCreator
    {
        public SoftwareSystemTest(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration conf)
            : base(data, analysis, conf)
        {

        }

        private string CheckResults(List<LinkedItem> items, TraceEntity docTE)
        {
            StringBuilder errors = new StringBuilder();
            bool errorsFound = false;
            var results = data.GetAllTestResults();
            foreach (var item in items)
            {
                if (!((SoftwareSystemTestItem)item).TestCaseAutomated)
                {
                    continue;
                }
                bool found = false;
                foreach (var result in results)
                {
                    if (result.Type == TestResultType.SYSTEM && result.ID == item.ItemID)
                    {
                        found = true;
                        if (result.Status == TestResultStatus.FAIL)
                        {
                            errors.AppendLine($"* Test with ID \"{result.ID}\" has failed.");
                            errorsFound = true;
                        }
                        break;
                    }
                }
                if (!found)
                {
                    errorsFound = true;
                    errors.AppendLine($"* Result for test with ID \"{item.ItemID}\" not found in results.");
                }
            }
            foreach (var result in results)
            {
                if (result.Type != TestResultType.SYSTEM)
                    continue;
                bool found = false;
                foreach (var item in items)
                {
                    if (((SoftwareSystemTestItem)item).TestCaseAutomated && result.ID == item.ItemID)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    errorsFound = true;
                    errors.AppendLine($"* Result for test with ID \"{result.ID}\" found, but test plan does not contain such an automated test.");
                }
            }
            if (errorsFound)
            {
                errors.Insert(0, "RoboClerk detected problems with the automated testing:\n\n");
                errors.AppendLine();
                return errors.ToString();
            }
            else
            {
                return "All automated tests from the test plan were successfully executed and passed.";
            }
        }

        protected override string GenerateADocContent(RoboClerkTag tag, List<LinkedItem> items, TraceEntity sourceTE, TraceEntity docTE)
        {
            StringBuilder output = new StringBuilder();
            var dataShare = new ScriptingBridge(data, analysis, sourceTE);
            if (tag.HasParameter("CHECKRESULTS") && tag.GetParameterOrDefault("CHECKRESULTS").ToUpper() == "TRUE")
            {
                //this will go over all SYSTEM test results (if available) and prints a summary statement or a list of found issues.
                return CheckResults(items, docTE);
            }
            else
            {
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
            }
            return output.ToString();
        }
    }
}
