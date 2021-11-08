using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk.ContentCreators
{
    public class AutomatedSystemLevelTests : IContentCreator
    {
        public AutomatedSystemLevelTests()
        {

        }

        public string GetContent(DataSources data, TraceabilityAnalysis analysis, string docTitle)
        {
            var systemTests = data.GetAllSystemLevelTests();
            //select only the tests that are marked as automated
            StringBuilder output = new StringBuilder();
            foreach (var test in systemTests)
            {
                if (test.TestCaseAutomated)
                {
                    output.AppendLine(test.ToMarkDown());
                    analysis.AddTrace(docTitle, new TraceLink(test.TestCaseID, TraceLinkType.TestCaseTrace));
                    foreach (var parent in test.Parents)
                    {
                        analysis.AddTrace(docTitle, new TraceLink(parent.Item1, TraceLinkType.SoftwareRequirementTrace));
                    }
                }
            }
            return output.ToString();
        }
    }
}
