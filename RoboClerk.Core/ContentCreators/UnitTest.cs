using Microsoft.CodeAnalysis.Scripting;
using RoboClerk.Core.Configuration;
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

        /// <summary>
        /// Static metadata for the UnitTest content creator
        /// </summary>
        public static ContentCreatorMetadata StaticMetadata { get; } = CreateUnitTestMetadata();

        private static ContentCreatorMetadata CreateUnitTestMetadata()
        {
            var commonParams = GetCommonMultiItemParametersStatic(typeof(UnitTestItem));
            
            // Add UnitTest specific parameters
            commonParams.Add(new ContentCreatorParameter("UnitTestState", "Filter by unit test state", ParameterValueType.String, required: false));
            commonParams.Add(new ContentCreatorParameter("UnitTestFileLocation", "Filter by file location", ParameterValueType.String, required: false));
            commonParams.Add(new ContentCreatorParameter("UnitTestFileName", "Filter by file name", ParameterValueType.String, required: false));
            commonParams.Add(new ContentCreatorParameter("UnitTestFunctionName", "Filter by function name", ParameterValueType.String, required: false));

            var metadata = new ContentCreatorMetadata(
                "SLMS",
                "Unit Test",
                "Manages and displays unit test information including test results")
            {
                Category = "Testing",
                Tags = new List<ContentCreatorTag>
                {
                    new ContentCreatorTag("UnitTest Detail", "Displays detailed unit test information where each unit test is shown individually", "UnitTest")
                    {
                        Category = "Unit Test Management",
                        Description = "Displays unit tests with all details including purpose, acceptance criteria, file name, function name, and traceability.",
                        Parameters = new List<ContentCreatorParameter>(commonParams),
                        ExampleUsage = "@@SLMS:UnitTest()@@"
                    },
                    new ContentCreatorTag("UnitTest Summary", "Displays a list of all unit tests in a single table, optionally with results", "UnitTest")
                    {
                        Category = "Unit Test Management",
                        Description = "Displays a compact table view of all unit tests with summary information including file name, function name, and linked requirements.",
                        Parameters = new List<ContentCreatorParameter>(commonParams)
                        {
                            new ContentCreatorParameter("checkResults",
                                "Set to 'true' to validate unit test results against test plan and show results. Test results must be loaded into RoboClerk.",
                                ParameterValueType.Boolean, required: false)
                            {
                                AllowedValues = new List<string> { "true", "false" },
                                ExampleValue = "true"
                            },
                            new ContentCreatorParameter("showResults",
                                "Set to 'true' to show any matched results loaded into RoboClerk in the summary table. This requires the results to be loaded into RoboClerk.",
                                ParameterValueType.Boolean, required: true)
                            {
                                AllowedValues = new List<string> { "true", "false" },
                                ExampleValue = "false",
                                DefaultValue = "false"
                            },
                            new ContentCreatorParameter("brief",
                                "Always should be true for this tag.",
                                ParameterValueType.Boolean, required: true)
                            {
                                AllowedValues = new List<string> { "true" },
                                ExampleValue = "true",
                                DefaultValue = "true"
                            }
                        },
                        ExampleUsage = "@@SLMS:UnitTest(brief=true,checkResults=true)@@"
                    }
                }
            };
            return metadata;
        }

        /// <summary>
        /// Override GetMetadata to prevent base class from adding common parameters twice.
        /// </summary>
        public override ContentCreatorMetadata GetMetadata()
        {
            return GetContentCreatorMetadata();
        }

        protected override ContentCreatorMetadata GetContentCreatorMetadata() => StaticMetadata;

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
                    if (result.ResultType == TestType.UNIT && result.TestID == item.ItemID)
                    {
                        found = true;
                        if (result.ResultStatus == TestResultStatus.FAIL)
                        {
                            errorItems.Add($"Unit test with ID \"{result.TestID}\" has failed.");
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
                if (result.ResultType != TestType.UNIT)
                    continue;
                bool found = false;
                foreach (var item in items)
                {
                    if (result.TestID == item.ItemID)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    errorsFound = true;
                    errorItems.Add($"Result for unit test with ID \"{result.TestID}\" found, but test plan does not contain such a unit test.");
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
            var dataShare = CreateScriptingBridge(tag, sourceTE);
            var extension = (configuration.OutputFormat == "ASCIIDOC" ? "adoc" : "html");
            if (tag.HasParameter("CHECKRESULTS") && tag.GetParameterOrDefault("CHECKRESULTS").ToUpper() == "TRUE")
            {
                //this will go over all unit test results (if available) and prints a summary statement or a list of found issues.
                return CheckResults(items, docTE);
            }
            else if (tag.HasParameter("BRIEF") && tag.GetParameterOrDefault("BRIEF").ToUpper() == "TRUE")
            {
                //this will print a brief list of all soups and versions that Roboclerk knows about
                dataShare.Items = items;
                var briefFileIdentifier = configuration.ProjectID + $"./ItemTemplates/{configuration.OutputFormat}/UnitTest_brief.{extension}";
                ItemTemplateRenderer renderer;
                if (ItemTemplateRenderer.ExistsInCache(briefFileIdentifier))
                {
                    renderer = ItemTemplateRenderer.FromCachedTemplate(briefFileIdentifier);
                }
                else
                {
                    var file = data.GetTemplateFile($"./ItemTemplates/{configuration.OutputFormat}/UnitTest_brief.{extension}");
                    renderer = ItemTemplateRenderer.FromString(file, briefFileIdentifier);
                }
                var result = renderer.RenderItemTemplate(dataShare);
                ProcessTraces(docTE, dataShare);
                return result;
            }
            else
            {
                var fileIdentifier = configuration.ProjectID + $"./ItemTemplates/{configuration.OutputFormat}/UnitTest.{extension}";
                ItemTemplateRenderer renderer;
                if (ItemTemplateRenderer.ExistsInCache(fileIdentifier))
                {
                    renderer = ItemTemplateRenderer.FromCachedTemplate(fileIdentifier);
                }
                else
                {
                    var file = data.GetTemplateFile($"./ItemTemplates/{configuration.OutputFormat}/UnitTest.{extension}");
                    renderer = ItemTemplateRenderer.FromString(file, fileIdentifier);
                }
                StringBuilder output = new StringBuilder();
                int count = items.Count;
                int index = 0;
                foreach (var item in items)
                {
                    dataShare.Item = item;
                    dataShare.Index = index;
                    dataShare.Count = count;
                    dataShare.IsFirst = (index == 0);
                    dataShare.IsLast = (index == count - 1);
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
                    index++;
                }
                ProcessTraces(docTE, dataShare);
                return output.ToString();
            }
        }
    }
}
