using Microsoft.CodeAnalysis.Scripting;
using RoboClerk.Core.Configuration;
using RoboClerk.Core;
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

        protected override ContentCreatorMetadata GetContentCreatorMetadata()
        {
            var metadata = new ContentCreatorMetadata("SLMS", "Software System Test", 
                "Manages and displays software system test cases, both automated and manual");
            
            metadata.Category = "Testing";

            // Main test case tag
            var testTag = new ContentCreatorTag("SoftwareSystemTest", "Displays detailed software system test case information");
            testTag.Category = "Test Management";
            // Common parameters will be automatically added
            testTag.ExampleUsage = "@@SLMS:SoftwareSystemTest()@@";
            metadata.Tags.Add(testTag);

            // Brief test list tag
            var testBriefTag = new ContentCreatorTag("SoftwareSystemTest", "Displays a brief list of all software system test cases");
            testBriefTag.Category = "Test Management";
            testBriefTag.Parameters.Add(new ContentCreatorParameter("brief", 
                "Set to 'true' to display brief test case list", 
                ParameterValueType.Boolean, required: false)
            {
                AllowedValues = new List<string> { "true", "false" },
                ExampleValue = "true"
            });
            testBriefTag.ExampleUsage = "@@SLMS:SoftwareSystemTest(brief=true)@@";
            metadata.Tags.Add(testBriefTag);

            // Check results tag
            var checkResultsTag = new ContentCreatorTag("SoftwareSystemTest", "Validates automated test results");
            checkResultsTag.Category = "Test Validation";
            checkResultsTag.Parameters.Add(new ContentCreatorParameter("checkResults", 
                "Set to 'true' to validate automated test results against test plan. This requires the results to be loaded into RoboClerk.", 
                ParameterValueType.Boolean, required: false)
            {
                AllowedValues = new List<string> { "true", "false" },
                ExampleValue = "true"
            });
            checkResultsTag.ExampleUsage = "@@SLMS:SoftwareSystemTest(checkResults=true)@@";
            metadata.Tags.Add(checkResultsTag);

            return metadata;
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
                    if ( (result.ResultType == TestType.SYSTEM && result.TestID == item.ItemID) || 
                         (item.TestCaseToUnitTest && result.ResultType == TestType.UNIT && 
                          item.LinkedItems.Any(o => o.LinkType == ItemLinkType.UnitTest && o.TargetID == result.TestID)) )
                    {
                        found = true;
                        if (result.ResultStatus == TestResultStatus.FAIL)
                        {
                            errorItems.Add($"Test with ID \"{result.TestID}\" has failed.");
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
                if (result.ResultType != TestType.SYSTEM)
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
                    errorItems.Add($"Result for test with ID \"{result.TestID}\" found, but test plan does not contain such an automated test.");
                }
            }

            // Generate format-specific output
            if (configuration.OutputFormat.ToUpper() == "HTML" || configuration.OutputFormat.ToUpper() == "DOCX")
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

        protected override string GenerateContent(IRoboClerkTag tag, List<LinkedItem> items, TraceEntity sourceTE, TraceEntity docTE)
        {
            StringBuilder output = new StringBuilder();
            var dataShare = CreateScriptingBridge(tag, sourceTE);
            if (tag.HasParameter("CHECKRESULTS") && tag.GetParameterOrDefault("CHECKRESULTS").ToUpper() == "TRUE")
            {
                //this will go over all SYSTEM test results (if available) and prints a summary statement or a list of found issues.
                return CheckResults(items, docTE);
            }
            else
            {
                var extension = (configuration.OutputFormat == "ASCIIDOC" ? "adoc" : "html");
                if (tag.HasParameter("BRIEF") && tag.GetParameterOrDefault("BRIEF").ToUpper() == "TRUE")
                {
                    var briefFileIdentifier = configuration.ProjectID + $"./ItemTemplates/{configuration.OutputFormat}/SoftwareSystemTest_brief.{extension}";
                    //this will print a brief list of all software system tests that Roboclerk knows about
                    dataShare.Items = items;
                    ItemTemplateRenderer briefRenderer;
                    if (ItemTemplateRenderer.ExistsInCache(briefFileIdentifier))
                    {
                        briefRenderer = ItemTemplateRenderer.FromCachedTemplate(briefFileIdentifier);
                    }
                    else
                    {
                        var briefFile = data.GetTemplateFile($"./ItemTemplates/{configuration.OutputFormat}/SoftwareSystemTest_brief.{extension}");
                        briefRenderer = ItemTemplateRenderer.FromString(briefFile, briefFileIdentifier);
                    }
                     
                    var result = briefRenderer.RenderItemTemplate(dataShare);
                    ProcessTraces(docTE, dataShare);
                    return result;
                }               
                // Setup automated test template with caching
                var automatedFileIdentifier = configuration.ProjectID + $"./ItemTemplates/{configuration.OutputFormat}/SoftwareSystemTest_automated.{extension}";
                ItemTemplateRenderer rendererAutomated;
                if (ItemTemplateRenderer.ExistsInCache(automatedFileIdentifier))
                {
                    rendererAutomated = ItemTemplateRenderer.FromCachedTemplate(automatedFileIdentifier);
                }
                else
                {
                    var automatedFile = data.GetTemplateFile($"./ItemTemplates/{configuration.OutputFormat}/SoftwareSystemTest_automated.{extension}");
                    rendererAutomated = ItemTemplateRenderer.FromString(automatedFile, automatedFileIdentifier);
                }
                
                // Setup manual test template with caching
                var manualFileIdentifier = configuration.ProjectID + $"./ItemTemplates/{configuration.OutputFormat}/SoftwareSystemTest_manual.{extension}";
                ItemTemplateRenderer rendererManual;
                if (ItemTemplateRenderer.ExistsInCache(manualFileIdentifier))
                {
                    rendererManual = ItemTemplateRenderer.FromCachedTemplate(manualFileIdentifier);
                }
                else
                {
                    var manualFile = data.GetTemplateFile($"./ItemTemplates/{configuration.OutputFormat}/SoftwareSystemTest_manual.{extension}");
                    rendererManual = ItemTemplateRenderer.FromString(manualFile, manualFileIdentifier);
                }
                
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
