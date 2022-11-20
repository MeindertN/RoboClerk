using RoboClerk.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RoboClerk.ContentCreators
{
    public class UnitTest : ContentCreatorBase
    {
        public UnitTest(IDataSources data, ITraceabilityAnalysis analysis)
            : base(data, analysis)
        {

        }

        private string GenerateBriefADOC(List<UnitTestItem> unitTests, TraceEntity sourceType, RoboClerkTag tag, System.Reflection.PropertyInfo[] properties, ITraceabilityAnalysis analysis, TraceEntity docTrace)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("|====");
            sb.AppendLine($"| {sourceType.Name} ID | Purpose | Acceptance Criteria");
            sb.AppendLine();

            foreach (var item in unitTests)
            {
                if (ShouldBeIncluded<UnitTestItem>(tag, item, properties) && CheckUpdateDateTime(tag, item))
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
            if (item.ItemLastUpdated != DateTime.MinValue)
            {
                sb.AppendLine($"| *Last Updated* | {item.ItemLastUpdated.ToString("yyyy/MM/dd HH:mm:ss")}");
                sb.AppendLine();
            }
            sb.Append("| *Trace Link:* ");
            sb.AppendLine($"| {GetLinkedField(item, ItemLinkType.Related)}");
            sb.AppendLine();
            string tempPurpose = item.UnitTestPurpose == string.Empty ? "N/A" : item.UnitTestPurpose;
            sb.AppendLine($"| *Purpose* | {tempPurpose}");
            sb.AppendLine();
            string tempAcceptanceCriteria = item.UnitTestAcceptanceCriteria == string.Empty ? "N/A" : item.UnitTestAcceptanceCriteria;
            sb.AppendLine($"| *Acceptance Criteria* | {tempAcceptanceCriteria}");
            sb.AppendLine();

            sb.AppendLine("|====");
            return sb.ToString();
        }

        public override string GetContent(RoboClerkTag tag, DocumentConfig doc)
        {
            var unitTests = data.GetAllSoftwareUnitTests();
            StringBuilder output = new StringBuilder();
            bool unitTestFound = false;
            var sourceType = analysis.GetTraceEntityForID("SoftwareUnitTest");
            var properties = typeof(UnitTestItem).GetProperties();

            if (tag.HasParameter("BRIEF") && tag.GetParameterOrDefault("BRIEF").ToUpper() == "TRUE")
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
                    output.AppendLine(GenerateADOC(test, sourceType.Name));
                    analysis.AddTrace(analysis.GetTraceEntityForID("SoftwareUnitTest"), test.ItemID, analysis.GetTraceEntityForTitle(doc.DocumentTitle), test.ItemID);

                    var links = test.LinkedItems.Where(x => x.LinkType == ItemLinkType.Related);
                    if (links.Count() == 0)
                    {
                        //in case there are no parents, ensure that the broken trace is included, assume that unit tests link to software requirements
                        analysis.AddTrace(analysis.GetTraceEntityForID("SoftwareRequirement"), null, analysis.GetTraceEntityForTitle(doc.DocumentTitle), test.ItemID);
                    }
                    else foreach (var link in links)
                    {
                        Item item = data.GetItem(link.TargetID);
                        if (item != null)
                        {
                            var te = analysis.GetTraceEntityForID(item.ItemType);
                            analysis.AddTrace(te, link.TargetID, analysis.GetTraceEntityForTitle(doc.DocumentTitle), test.ItemID);
                        }
                        else
                        {
                            throw new Exception($"Unable to find linked item with ID \"{link.TargetID}\" for unit test \"{test.ItemID}\" in \"{doc.DocumentTitle}\".");
                        }
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
