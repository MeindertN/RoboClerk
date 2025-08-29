using Microsoft.CodeAnalysis.Scripting;
using RoboClerk.Configuration;
using RoboClerk.Core;
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

        private string CheckResults(List<LinkedItem> items, TraceEntity docTE)
        {
            var results = data.GetAllTestResults();
            var errorItems = new List<string>();
            bool errorsFound = false;
            
            // Collect all error messages (format-agnostic logic)
            foreach (var item in items)
            {
                bool found = false;
                foreach (var result in results)
                {
                    if (result.Type == TestResultType.UNIT && result.ID == item.ItemID)
                    {
                        found = true;
                        if (result.Status == TestResultStatus.FAIL)
                        {
                            errorItems.Add($"Unit test with ID \"{result.ID}\" has failed.");
                            errorsFound = true;
                        }
                        break;
                    }
                }
                if (!found)
                {
                    errorsFound = true;
                    errorItems.Add($"Result for unit test with ID \"{item.ItemID}\" not found in results.");
                }
            }
            
            foreach (var result in results)
            {
                if (result.Type != TestResultType.UNIT)
                    continue;
                bool found = false;
                foreach (var item in items)
                {
                    if (result.ID == item.ItemID)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    errorsFound = true;
                    errorItems.Add($"Result for unit test with ID \"{result.ID}\" found, but test plan does not contain such a unit test.");
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
                sb.AppendLine("RoboClerk detected problems with the unit testing:\n");
                foreach (var error in errorItems)
                {
                    sb.AppendLine($"* {error}");
                }
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine("All unit tests from the test plan were successfully executed and passed.");
            }
            
            return sb.ToString();
        }

        private string GenerateHTMLCheckResults(bool errorsFound, List<string> errorItems)
        {
            StringBuilder sb = new StringBuilder();
            
            if (errorsFound)
            {
                sb.AppendLine("<div>");
                sb.AppendLine("    <h3>RoboClerk detected problems with the unit testing:</h3>");
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
                sb.AppendLine("    <p>All unit tests from the test plan were successfully executed and passed.</p>");
                sb.AppendLine("</div>");
            }
            
            return sb.ToString();
        }

        protected override string GenerateContent(IRoboClerkTag tag, List<LinkedItem> items, TraceEntity sourceTE, TraceEntity docTE)
        {
            var dataShare = new ScriptingBridge(data, analysis, sourceTE, configuration);
            if (tag.HasParameter("CHECKRESULTS") && tag.GetParameterOrDefault("CHECKRESULTS").ToUpper() == "TRUE")
            {
                //this will go over all unit test results (if available) and prints a summary statement or a list of found issues.
                return CheckResults(items, docTE);
            }
            else if (tag.HasParameter("BRIEF") && tag.GetParameterOrDefault("BRIEF").ToUpper() == "TRUE")
            {
                //this will print a brief list of all soups and versions that Roboclerk knows about
                dataShare.Items = items;
                var file = data.GetTemplateFile($"./ItemTemplates/{configuration.OutputFormat}/UnitTest_brief.{(configuration.OutputFormat == "HTML" ? "html" : "adoc")}");
                var renderer = new ItemTemplateRenderer(file);
                var result = renderer.RenderItemTemplate(dataShare);
                ProcessTraces(docTE, dataShare);
                return result;
            }
            else
            {
                var file = data.GetTemplateFile($"./ItemTemplates/{configuration.OutputFormat}/UnitTest.{(configuration.OutputFormat == "HTML" ? "html" : "adoc")}");
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
