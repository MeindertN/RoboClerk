using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk.ContentCreators
{
    public class ManualSystemLevelTests : SystemLevelTestBase
    {
        public ManualSystemLevelTests()
        {

        }

        override public string GetContent(RoboClerkTag tag, DataSources data, TraceabilityAnalysis analysis, string docTitle)
        {
            automated = false;
            return base.GetContent(tag, data, analysis, docTitle);
        }
    }
}
