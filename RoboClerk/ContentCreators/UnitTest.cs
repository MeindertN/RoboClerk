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

        private string CheckResults(List<LinkedItem> items, TraceEntity docTE)
        {
            StringBuilder errors = new StringBuilder();
            bool errorsFound = false;
            var results = data.GetAllTestResults();
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
                            errors.AppendLine($"* Unit test with ID \"{result.ID}\" has failed.");
                            errorsFound = true;
                        }
                        break;
                    }
                }
                if (!found)
                {
                    errorsFound = true;
                    errors.AppendLine($"* Result for unit test with ID \"{item.ItemID}\" not found in results.");
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
                    errors.AppendLine($"* Result for unit test with ID \"{result.ID}\" found, but test plan does not contain such a unit test.");
                }
            }
            if (errorsFound)
            {
                errors.Insert(0, "RoboClerk detected problems with the unit testing:\n\n");
                errors.AppendLine();
                return errors.ToString();
            }
            else
            {
                return "All unit tests from the test plan were successfully executed and passed.";
            }
        }

        protected override string GenerateADocContent(RoboClerkTag tag, List<LinkedItem> items, TraceEntity sourceTE, TraceEntity docTE)
        {
            var dataShare = new ScriptingBridge(data, analysis, sourceTE);
            if (tag.HasParameter("CHECKRESULTS") && tag.GetParameterOrDefault("CHECKRESULTS").ToUpper() == "TRUE")
            {
                //this will go over all unit test results (if available) and prints a summary statement or a list of found issues.
                return CheckResults(items, docTE);
            }
            else if (tag.HasParameter("BRIEF") && tag.GetParameterOrDefault("BRIEF").ToUpper() == "TRUE")
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
