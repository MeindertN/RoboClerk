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

    public class TraceIssue : TraceLink
    {
        private TraceIssueType issueType;

        public TraceIssue(TraceEntity source, TraceEntity target, string id, TraceIssueType it)
            : base(source, target, id)
        {
            issueType = it;
            base.valid = false;
        }

        public TraceIssueType IssueType
        {
            get => issueType;
        }

        public override int GetHashCode()
        {
            return source.GetHashCode() ^ target.GetHashCode() ^ TraceID.GetHashCode();
        }


        public override bool Equals(object obj)
        {
            var comp = obj as TraceIssue;
            return (comp.Source == source) &&
                (comp.Target == target) &&
                (comp.TraceID == TraceID);
        }
    }
}
