using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk
{
    public enum TraceLinkType
    {
        SoftwareRequirementTrace,
        ProductRequirementTrace,
        TestCaseTrace,
        Unknown
    };


    public class TraceLink
    {
        private string traceID;
        private TraceLinkType traceLinkType;
        public TraceLink(string id, TraceLinkType tlt)
        {
            traceID = id;
            traceLinkType = tlt;
        }

        public string TraceID
        {
            get => traceID;
        }

        public TraceLinkType TraceType
        {
            get => traceLinkType;
        }
    }
}
