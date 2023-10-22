using Microsoft.CodeAnalysis.Scripting;
using RoboClerk.Configuration;
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

            StringBuilder output = new StringBuilder();
            output.AppendLine($"Roboclerk detected the following potential {soupName} issues:");
            output.AppendLine();
            bool issueDetected = false;
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
                        output.AppendLine($"* An external dependency ({extDep.Name}) has a matching {soupName} " +
                            $"item with a mismatched version (\"{soupVersion}\" instead of \"{extDep.Version}\").");
                        issueDetected = true;
                    }
                }
                else
                {
                    output.AppendLine($"* An external dependency {extDep.Name} {extDep.Version} " +
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
                        output.AppendLine($"* A {soupName} item (i.e. \"{soup.SOUPName} {soup.SOUPVersion}\") that is marked as being linked into " +
                            $"the software does not have a matching external dependency.");
                        issueDetected = true;
                    }
                }
            }
            if (!issueDetected)
            {
                output.AppendLine($"* No {soupName} related issues detected!");
            }
            return output.ToString();
        }

        protected override string GenerateADocContent(RoboClerkTag tag, List<LinkedItem> items, TraceEntity sourceTE, TraceEntity docTE)
        {
            var dataShare = new ScriptingBridge(data, analysis, sourceTE);
            if (tag.HasParameter("BRIEF") && tag.GetParameterOrDefault("BRIEF").ToUpper() == "TRUE")
            {
                //this will print a brief list of all soups and versions that Roboclerk knows about
                dataShare.Items = items;
                var file = data.GetTemplateFile(@"./ItemTemplates/SOUP_brief.adoc");
                var renderer = new ItemTemplateRenderer(file);
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
                var file = data.GetTemplateFile(@"./ItemTemplates/SOUP.adoc");
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
