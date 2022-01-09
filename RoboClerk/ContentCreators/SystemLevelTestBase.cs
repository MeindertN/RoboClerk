using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk.ContentCreators
{
    public abstract class SystemLevelTestBase : IContentCreator
    {
        protected bool automated = true;

        public SystemLevelTestBase()
        {

        }

        public virtual string GetContent(RoboClerkTag tag, DataSources data, TraceabilityAnalysis analysis, string docTitle)
        {
            var systemTests = data.GetAllSystemLevelTests();
            StringBuilder output = new StringBuilder();
            bool testCaseFound = false;
            foreach (var test in systemTests)
            {
                if (test.TestCaseAutomated == automated)
                {
                    if (tag.TraceReference != string.Empty && tag.TraceReference != test.TestCaseID)
                    {
                        continue; //if a particular testcase was indicated, we ignore those that do not match
                    }
                    testCaseFound = true;
                    output.AppendLine(test.ToMarkDown());
                    analysis.AddTrace(docTitle, new TraceLink(TraceEntityType.TestCase, analysis.GetTraceEntityForTitle(docTitle), test.TestCaseID));
                    foreach (var parent in test.Parents)
                    {
                        analysis.AddTrace(docTitle, new TraceLink(TraceEntityType.SoftwareRequirement,analysis.GetTraceEntityForTitle(docTitle), parent.Item1));
                    }
                }
            }
            if (!testCaseFound)
            {
                return "Unable to find test case(s). Check if test cases are provided or if a valid test case identifier is specified.";
            }
            return output.ToString();
        }
    }
}
