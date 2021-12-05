using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk.ContentCreators
{
    public class AutomatedSystemLevelTests : SystemLevelTestBase
    {
        public AutomatedSystemLevelTests()
        {

        }

        override public string GetContent(RoboClerkTag tag, DataSources data, TraceabilityAnalysis analysis, string docTitle)
        {
            automated = true;
            return base.GetContent(tag, data, analysis, docTitle);
        }
    }
}
