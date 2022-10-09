using RoboClerk.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RoboClerk.ContentCreators
{
    internal class UnitTest : ContentCreatorBase
    {
        private string GenerateBriefADOC(List<UnitTestItem> unitTests, TraceEntity sourceType, RoboClerkTag tag, System.Reflection.PropertyInfo[] properties, ITraceabilityAnalysis analysis, TraceEntity docTrace)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("|====");
            sb.AppendLine($"| {sourceType.Name} ID | Purpose | Acceptance Criteria");
            sb.AppendLine();

            foreach (var item in unitTests)
            {
                if (ShouldBeIncluded<UnitTestItem>(tag, item, properties))
                {
                    sb.Append(item.HasLink ? $"| {item.Link}[{item.ItemID}]" : $"| {item.ItemID} ");
                    sb.AppendLine($"| {item.UnitTestPurpose} | {item.UnitTestAcceptanceCriteria}");
                    sb.AppendLine();
                    
                    analysis.AddTrace(sourceType, item.ItemID, docTrace, item.ItemID);
                }
            }
            sb.AppendLine("|====");
            return sb.ToString();
        }

        private string GenerateADOC(UnitTestItem item, string name)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("|====");
            string tempID = item.HasLink ? $"{item.Link}[{item.ItemID}]" : $"{item.ItemID} ";
            sb.AppendLine($"| *{name} ID* | {tempID}");
            sb.AppendLine();
            if (item.ItemRevision != string.Empty)
            {
                sb.AppendLine($"| *Revision* | {item.ItemRevision}");
                sb.AppendLine();
            }
            if(item.LinkedItems.Count() != 0)
            {
                sb.Append($"| *Trace Link* | ");
                foreach (var linkedItem in item.LinkedItems)
                {
                    sb.Append($"{linkedItem.TargetID}  ");
                }
                sb.AppendLine();
            }
            string tempPurpose = item.UnitTestPurpose == string.Empty ? "N/A" : item.UnitTestPurpose;
            sb.AppendLine($"| *Purpose* | {tempPurpose}");
            sb.AppendLine();
            string tempAcceptanceCriteria = item.UnitTestAcceptanceCriteria == string.Empty ? "N/A" : item.UnitTestAcceptanceCriteria;
            sb.AppendLine($"| *Acceptance Criteria* | {tempAcceptanceCriteria}");
            sb.AppendLine();

            sb.AppendLine("|====");
            return sb.ToString();
        }

        public override string GetContent(RoboClerkTag tag, IDataSources data, ITraceabilityAnalysis analysis, DocumentConfig doc)
        {
            var unitTests = data.GetAllSoftwareUnitTests();
            StringBuilder output = new StringBuilder();
            bool unitTestFound = false;
            var sourceType = analysis.GetTraceEntityForID("SoftwareUnitTest");
            var properties = typeof(UnitTestItem).GetProperties();

            if (tag.Parameters.ContainsKey("BRIEF") && tag.Parameters["BRIEF"].ToUpper() == "TRUE")
            {
                //this will print a brief list of all soups and versions that Roboclerk knows about
                if (unitTests.Count > 0)
                {
                    unitTestFound = true;
                    output.AppendLine(GenerateBriefADOC(unitTests, sourceType, tag, properties, analysis, analysis.GetTraceEntityForTitle(doc.DocumentTitle)));
                }
            }
            else foreach (var test in unitTests)
            {
                if (ShouldBeIncluded(tag, test, properties) && CheckUpdateDateTime(tag, test))
                {
                    unitTestFound = true;
                    try
                    {
                        output.AppendLine(GenerateADOC(test, sourceType.Name));
                    }
                    catch
                    {
                        logger.Error($"An error occurred while rendering unit test {test.ItemID} in {doc.DocumentTitle}.");
                        throw;
                    }
                    analysis.AddTrace(analysis.GetTraceEntityForID("SoftwareUnitTest"), test.ItemID, analysis.GetTraceEntityForTitle(doc.DocumentTitle), test.ItemID);

                    var links = test.LinkedItems.Where(x => x.LinkType == ItemLinkType.Related);
                    if (links.Count() == 0)
                    {
                        //in case there are no parents, ensure that the broken trace is included
                        analysis.AddTrace(analysis.GetTraceEntityForID("SoftwareRequirement"), null, analysis.GetTraceEntityForTitle(doc.DocumentTitle), test.ItemID);
                    }
                    else foreach (var link in links)
                    {
                        analysis.AddTrace(analysis.GetTraceEntityForID("SoftwareRequirement"), link.TargetID, analysis.GetTraceEntityForTitle(doc.DocumentTitle), test.ItemID);
                    }
                }
            }
            if (!unitTestFound)
            {
                return $"Unable to find specified unit test(s). Check if unit tests are provided or if a valid unit test identifier is specified.";
            }
            return output.ToString();
        }        
    }
}
