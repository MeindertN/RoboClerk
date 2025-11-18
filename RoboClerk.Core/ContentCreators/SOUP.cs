using Microsoft.CodeAnalysis.Scripting;
using RoboClerk.Core.Configuration;
using RoboClerk.Core;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk.ContentCreators
{
    public class SOUP : MultiItemContentCreator
    {

        public SOUP(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration conf)
            : base(data, analysis, conf)
        {

        }

        private string GenerateSoupCheck(string soupName)
        {
            var extDeps = data.GetAllExternalDependencies();
            var soups = data.GetAllSOUP();

            var issues = new List<string>();
            bool issueDetected = false;
            
            // Collect all issues (format-agnostic logic)
            foreach (var extDep in extDeps)
            {
                bool soupNameMatch = false;
                bool soupVersionMatch = false;
                string soupVersion = string.Empty;

                foreach (var soup in soups)
                {
                    if (soup.SOUPName == extDep.Name)
                    {
                        soupNameMatch = true;
                        soupVersion = soup.SOUPVersion;
                        if (soup.SOUPVersion == extDep.Version)
                        {
                            soupVersionMatch = true;
                        }
                        break;
                    }
                }

                if (soupNameMatch)
                {
                    if (!soupVersionMatch)
                    {
                        issues.Add($"An external dependency ({extDep.Name}) has a matching {soupName} " +
                            $"item with a mismatched version (\"{soupVersion}\" instead of \"{extDep.Version}\").");
                        issueDetected = true;
                    }
                }
                else
                {
                    issues.Add($"An external dependency {extDep.Name} {extDep.Version} " +
                        $"does not seem to have a matching {soupName} item.");
                    issueDetected = true;
                }
            }
            
            foreach (var soup in soups)
            {
                if (soup.SOUPLinkedLib)
                {
                    bool depNameMatch = false;
                    foreach (var extDep in extDeps)
                    {
                        if (extDep.Name == soup.SOUPName)
                        {
                            depNameMatch = true;
                            break;
                        }
                    }
                    if (!depNameMatch)
                    {
                        issues.Add($"A {soupName} item (i.e. \"{soup.SOUPName} {soup.SOUPVersion}\") that is marked as being linked into " +
                            $"the software does not have a matching external dependency.");
                        issueDetected = true;
                    }
                }
            }
            
            if (!issueDetected)
            {
                issues.Add($"No {soupName} related issues detected!");
            }

            // Generate format-specific output
            if (configuration.OutputFormat.ToUpper() == "HTML" || configuration.OutputFormat.ToUpper() == "DOCX")
            {
                return GenerateHTMLSoupCheck(soupName, issues);
            }
            else
            {
                return GenerateASCIIDocSoupCheck(soupName, issues);
            }
        }

        private string GenerateASCIIDocSoupCheck(string soupName, List<string> issues)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Roboclerk detected the following potential {soupName} issues:");
            sb.AppendLine();
            foreach (var issue in issues)
            {
                sb.AppendLine($"* {issue}");
            }
            return sb.ToString();
        }

        private string GenerateHTMLSoupCheck(string soupName, List<string> issues)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<div>");
            sb.AppendLine($"    <h3>Roboclerk detected the following potential {soupName} issues:</h3>");
            sb.AppendLine("    <ul>");
            foreach (var issue in issues)
            {
                sb.AppendLine($"        <li>{issue}</li>");
            }
            sb.AppendLine("    </ul>");
            sb.AppendLine("</div>");
            return sb.ToString();
        }

        protected override string GenerateContent(IRoboClerkTag tag, List<LinkedItem> items, TraceEntity sourceTE, TraceEntity docTE)
        {
            var dataShare = CreateScriptingBridge(tag, sourceTE);
            var extension = (configuration.OutputFormat == "ASCIIDOC" ? "adoc" : "html");
            if (tag.HasParameter("BRIEF") && tag.GetParameterOrDefault("BRIEF").ToUpper() == "TRUE")
            {
                //this will print a brief list of all soups and versions that Roboclerk knows about
                dataShare.Items = items;
                var briefFileIdentifier = configuration.ProjectID + $"./ItemTemplates/{configuration.OutputFormat}/SOUP_brief.{extension}";
                ItemTemplateRenderer renderer;
                if (ItemTemplateRenderer.ExistsInCache(briefFileIdentifier))
                {
                    renderer = ItemTemplateRenderer.FromCachedTemplate(briefFileIdentifier);
                }
                else
                {
                    var file = data.GetTemplateFile($"./ItemTemplates/{configuration.OutputFormat}/SOUP_brief.{extension}");
                    renderer = ItemTemplateRenderer.FromString(file, briefFileIdentifier);
                }
                var result = renderer.RenderItemTemplate(dataShare);
                ProcessTraces(docTE, dataShare);
                return result;
            }
            else if (tag.HasParameter("CHECKSOUP") && tag.GetParameterOrDefault("CHECKSOUP").ToUpper() == "TRUE")
            {
                //this will retrieve a list of external dependencies (from a dependency manager like NuGet or Gradle)
                //and compare them with the known SOUP items. Any discrepancies are listed.
                return GenerateSoupCheck(sourceTE.Name);
            }
            else
            {
                var fileIdentifier = configuration.ProjectID + $"./ItemTemplates/{configuration.OutputFormat}/SOUP.{extension}";
                ItemTemplateRenderer renderer;
                if (ItemTemplateRenderer.ExistsInCache(fileIdentifier))
                {
                    renderer = ItemTemplateRenderer.FromCachedTemplate(fileIdentifier);
                }
                else
                {
                    var file = data.GetTemplateFile($"./ItemTemplates/{configuration.OutputFormat}/SOUP.{extension}");
                    renderer = ItemTemplateRenderer.FromString(file, fileIdentifier);
                }
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
                        logger.Error($"A compilation error occurred while compiling SOUP.adoc script: {e.Message}");
                        throw;
                    }
                }
                ProcessTraces(docTE, dataShare);
                return output.ToString();
            }
        }
    }
}
