using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk.ContentCreators
{
    public class SoftwareSystemTests : ContentCreatorBase
    {
        protected bool automated = true;

        public SoftwareSystemTests()
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
                if(ShouldBeIncluded(tag,test,properties) )
                {
                    testCaseFound = true;
                    output.AppendLine(test.ToText());
                    analysis.AddTrace(docTitle, new TraceLink(TraceEntityType.SoftwareSystemTest, analysis.GetTraceEntityForTitle(docTitle), test.TestCaseID));
                    foreach (var parent in test.Parents)
                    {
                        analysis.AddTrace(docTitle, new TraceLink(TraceEntityType.SoftwareRequirement, analysis.GetTraceEntityForTitle(docTitle), parent.Item1));
                    }
                }
            }
            if (!testCaseFound)
            {
                return $"Unable to find {(automated?"automated":"manual")} test case(s). Check if test cases are provided or if a valid test case identifier is specified.";
            }
            return output.ToString();
        }
    }
}
