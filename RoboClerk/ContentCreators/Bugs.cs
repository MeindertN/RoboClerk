using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk.ContentCreators
{
    class Bugs : IContentCreator
    {

        public Bugs()
        {

        }

        public string GetContent(RoboClerkTag tag, DataSources data, TraceabilityAnalysis analysis, string docTitle)
        {
            var bugs = data.GetAllBugs();
            StringBuilder output = new StringBuilder();
            foreach (var bug in bugs)
            {
                if(bug.BugState == "Closed")
                {
                    continue; //skip closed bugs as they are no longer outstanding
                }
                output.AppendLine(bug.ToMarkDown());
            }
            if (bugs.Count == 0)
            {
                return $"No outstanding bugs found.";
            }
            return output.ToString();
        }
    }
}
