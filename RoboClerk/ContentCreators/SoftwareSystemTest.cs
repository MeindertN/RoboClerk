using Microsoft.CodeAnalysis.Scripting;
using RoboClerk.Configuration;
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
            var results = data.GetAllTestResults();
            var errorItems = new List<string>();
            bool errorsFound = false;
            
            // Collect all error messages (format-agnostic logic)
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
                    if ((result.Type == TestResultType.SYSTEM && result.ID == item.ItemID) ||
                         (item.TestCaseToUnitTest && result.Type == TestResultType.UNIT &&
                          item.LinkedItems.Any(o => o.LinkType == ItemLinkType.UnitTest && o.TargetID == result.ID)))
                    {
                        found = true;
                        if (result.Status == TestResultStatus.FAIL)
                        {
                            errorItems.Add($"Test with ID \"{result.ID}\" has failed.");
                            errorsFound = true;
                        }
                        break;
                    }
                }
                if (!found)
                {
                    errorsFound = true;
                    string additional = item.TestCaseToUnitTest ? "Unit test r" : "R";
                    errorItems.Add($"{additional}esult for test with ID \"{item.ItemID}\" not found in results.");
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
                    errorItems.Add($"Result for test with ID \"{result.ID}\" found, but test plan does not contain such an automated test.");
                }
            }

            // Generate format-specific output
            if (configuration.OutputFormat.ToUpper() == "HTML")
            {
                return GenerateHTMLCheckResults(errorsFound, errorItems);
            }
            else
            {
                return GenerateASCIIDocCheckResults(errorsFound, errorItems);
            }
        }

        private string GenerateASCIIDocCheckResults(bool errorsFound, List<string> errorItems)
        {
            StringBuilder sb = new StringBuilder();
            
            if (errorsFound)
            {
                sb.AppendLine("RoboClerk detected problems with the automated testing:\n");
                foreach (var error in errorItems)
                {
                    sb.AppendLine($"* {error}");
                }
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine("All automated tests from the test plan were successfully executed and passed.");
            }
            
            return sb.ToString();
        }

        private string GenerateHTMLCheckResults(bool errorsFound, List<string> errorItems)
        {
            StringBuilder sb = new StringBuilder();
            
            if (errorsFound)
            {
                sb.AppendLine("<div>");
                sb.AppendLine("    <h3>RoboClerk detected problems with the automated testing:</h3>");
                sb.AppendLine("    <ul>");
                foreach (var error in errorItems)
                {
                    sb.AppendLine($"        <li>{error}</li>");
                }
                sb.AppendLine("    </ul>");
                sb.AppendLine("</div>");
            }
            else
            {
                sb.AppendLine("<div>");
                sb.AppendLine("    <p>All automated tests from the test plan were successfully executed and passed.</p>");
                sb.AppendLine("</div>");
            }
            
            return sb.ToString();
        }

        protected override string GenerateContent(RoboClerkTag tag, List<LinkedItem> items, TraceEntity sourceTE, TraceEntity docTE)
        {
            StringBuilder output = new StringBuilder();
            var dataShare = new ScriptingBridge(data, analysis, sourceTE, configuration);
            if (tag.HasParameter("CHECKRESULTS") && tag.GetParameterOrDefault("CHECKRESULTS").ToUpper() == "TRUE")
            {
                //this will go over all SYSTEM test results (if available) and prints a summary statement or a list of found issues.
                return CheckResults(items, docTE);
            }
            else
            {
                var file = data.GetTemplateFile($"./ItemTemplates/{configuration.OutputFormat}/SoftwareSystemTest_automated.{(configuration.OutputFormat == "HTML" ? "html" : "adoc")}");
                var rendererAutomated = new ItemTemplateRenderer(file);
                file = data.GetTemplateFile($"./ItemTemplates/{configuration.OutputFormat}/SoftwareSystemTest_manual.{(configuration.OutputFormat == "HTML" ? "html" : "adoc")}");
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
                        if (tc.TestCaseToUnitTest)
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
