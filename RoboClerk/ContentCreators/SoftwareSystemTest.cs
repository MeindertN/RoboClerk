using RoboClerk.Configuration;
using System.Linq;
using System.Text;

namespace RoboClerk.ContentCreators
{
    public class SoftwareSystemTest : ContentCreatorBase
    {
        protected bool automated = true;

        public SoftwareSystemTest()
        {

        }

        public override string GetContent(RoboClerkTag tag, IDataSources data, ITraceabilityAnalysis analysis, DocumentConfig doc)
        {
            var systemTests = data.GetAllSystemLevelTests();
            StringBuilder output = new StringBuilder();
            bool testCaseFound = false;

            var properties = typeof(TestCaseItem).GetProperties();
            foreach (var test in systemTests)
            {
                if (ShouldBeIncluded(tag, test, properties))
                {
                    testCaseFound = true;
                    try
                    {
                        output.AppendLine(test.ToText());
                    }
                    catch
                    {
                        logger.Error($"An error occurred while rendering software system test {test.ItemID} in {doc.DocumentTitle}.");
                        throw;
                    }
                    analysis.AddTrace(analysis.GetTraceEntityForID("SoftwareSystemTest"), test.ItemID, analysis.GetTraceEntityForTitle(doc.DocumentTitle), test.ItemID);

                    var parents = test.LinkedItems.Where(x => x.LinkType == ItemLinkType.Parent);
                    if (parents.Count() == 0)
                    {
                        //in case there are no parents, ensure that the broken trace is included
                        analysis.AddTrace(analysis.GetTraceEntityForID("SoftwareRequirement"), null, analysis.GetTraceEntityForTitle(doc.DocumentTitle), test.ItemID);
                    }
                    else foreach (var parent in parents)
                    {
                        analysis.AddTrace(analysis.GetTraceEntityForID("SoftwareRequirement"), parent.TargetID, analysis.GetTraceEntityForTitle(doc.DocumentTitle), test.ItemID);
                    }
                }
            }
            if (!testCaseFound)
            {
                return $"Unable to find {(automated ? "automated" : "manual")} test case(s). Check if test cases are provided or if a valid test case identifier is specified.";
            }
            return output.ToString();
        }
    }
}
