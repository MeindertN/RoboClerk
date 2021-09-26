using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk.ContentCreators
{
    public class ManualSystemLevelTests : IContentCreator
    {
        public ManualSystemLevelTests()
        {

        }

        public string GetContent(DataSources data)
        {
            var systemTests = data.GetAllSystemLevelTests();
            //select only the tests that are marked as manual
            StringBuilder output = new StringBuilder();
            foreach (var test in systemTests)
            {
                if (!test.TestCaseAutomated)
                {
                    output.AppendLine(test.ToMarkDown());
                }
            }
            return output.ToString();
        }
    }
}
