using System.Text;

namespace RoboClerk.ContentCreators
{
    public class SoftwareSystemTest : ContentCreatorBase
    {
        protected bool automated = true;

        public SoftwareSystemTest()
        {

        }

        public override string GetContent(RoboClerkTag tag, DataSources data, TraceabilityAnalysis analysis, string docTitle)
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
                    output.AppendLine(test.ToText());
                    analysis.AddTrace(docTitle, new TraceLink(analysis.GetTraceEntityForID("SoftwareSystemTest"), test.TestCaseID, analysis.GetTraceEntityForTitle(docTitle), test.TestCaseID));
                    
                    foreach (var parent in test.Parents)
                    {
                        analysis.AddTrace(docTitle, new TraceLink(analysis.GetTraceEntityForID("SoftwareRequirement"), parent.Item1, analysis.GetTraceEntityForTitle(docTitle), test.TestCaseID));
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
