using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk
{
    public enum TraceIssueType
    {
        Extra,
        Missing,
        PossiblyExtra,
        PossiblyMissing
    }; 

    public class TraceIssue
    {
        private TraceEntityType source;
        private TraceEntityType target;
        private string traceID;
        private TraceIssueType issueType;

        public TraceIssue(TraceEntityType src, TraceEntityType tgt, string id, TraceIssueType it)
        {
            source = src;
            target = tgt;
            traceID = id;
            issueType = it;
        }

        public TraceEntityType Source
        {
            get => source;
        }

        public TraceEntityType Target
        {
            get => target;
        }

        public string TraceID
        {
            get => traceID;
        }

        public TraceIssueType IssueType
        {
            get => issueType;
        }
    }
}
