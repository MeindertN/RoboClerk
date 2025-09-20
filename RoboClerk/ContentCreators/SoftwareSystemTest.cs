using Microsoft.CodeAnalysis.Scripting;
using RoboClerk.Configuration;
using RoboClerk.Items;
using System.Collections.Generic;
using System.Linq;
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
            foreach (var i in items)
            {
                SoftwareSystemTestItem item = (SoftwareSystemTestItem)i;
                if (!item.TestCaseAutomated)
                {
                    continue;
                }
                bool found = false;
                foreach (var result in results)
                {
                    if ( (result.ResultType == TestResultType.SYSTEM && result.TestID == item.ItemID) || 
                         (item.TestCaseToUnitTest && result.ResultType == TestResultType.UNIT && 
                          item.LinkedItems.Any(o => o.LinkType == ItemLinkType.UnitTest && o.TargetID == result.TestID)) )
                    {
                        found = true;
                        if (result.ResultStatus == TestResultStatus.FAIL)
                        {
                            errors.AppendLine($"* Test with ID \"{result.TestID}\" has failed.");
                            errorsFound = true;
                        }
                        break;
                    }
                }
                if (!found)
                {
                    errorsFound = true;
                    string additional = item.TestCaseToUnitTest ? "Unit test r" : "R";
                    errors.AppendLine($"* {additional}esult for test with ID \"{item.ItemID}\" not found in results.");
                }
            }
            foreach (var result in results)
            {
                if (result.ResultType != TestResultType.SYSTEM)
                    continue;
                bool found = false;
                foreach (var item in items)
                {
                    if (((SoftwareSystemTestItem)item).TestCaseAutomated && result.TestID == item.ItemID)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    errorsFound = true;
                    errors.AppendLine($"* Result for test with ID \"{result.TestID}\" found, but test plan does not contain such an automated test.");
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
                        if( tc.TestCaseToUnitTest )
                        {
                            //trying to kick a manual test to the unit test plan is not correct. 
                            logger.Error($"Trying to test a manual {sourceTE.Name} ({tc.ItemID}) with a unit test is not valid.");
                            throw new System.InvalidOperationException($"Cannot kick manual test case {tc.ItemID} to unit test. Change test type to automated.");
                        }
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
