﻿using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk.ContentCreators
{
    public class ManualSystemLevelTests : IContentCreator
    {
        public ManualSystemLevelTests()
        {

        }

        public string GetContent(RoboClerkTag tag, DataSources data, TraceabilityAnalysis analysis, string docTitle)
        {
            var systemTests = data.GetAllSystemLevelTests();
            //select only the tests that are marked as manual
            StringBuilder output = new StringBuilder();
            bool testCaseFound = false;
            foreach (var test in systemTests)
            {
                if (!test.TestCaseAutomated)
                {
                    if (tag.TraceReference != string.Empty && tag.TraceReference != test.TestCaseID)
                    {
                        continue; //if a particular testcase was indicated, we ignore those that do not match
                    }
                    testCaseFound = true;
                    output.AppendLine(test.ToMarkDown());
                    analysis.AddTrace(docTitle, new TraceLink(test.TestCaseID, TraceLinkType.TestCaseTrace));
                    foreach (var parent in test.Parents)
                    {
                        analysis.AddTrace(docTitle, new TraceLink(parent.Item1, TraceLinkType.SoftwareRequirementTrace));
                    }
                }
            }
            if(!testCaseFound)
            {
                return "Unable to find test case(s). Check if test cases are provided or if a valid test case identifier is specified.";
            }
            return output.ToString();
        }
    }
}
