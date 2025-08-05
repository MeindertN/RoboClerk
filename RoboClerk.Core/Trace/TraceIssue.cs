namespace RoboClerk
{
    public enum TraceIssueType
    {
        Extra,
        Missing,
        Incorrect,
        PossiblyExtra,
        PossiblyMissing
    };

    public class TraceIssue : TraceLink
    {
        private TraceIssueType issueType;

        public TraceIssue(TraceEntity source, string sourceID, TraceEntity target, string targetID, TraceIssueType it)
            : base(source, sourceID, target, targetID)
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
            return source.GetHashCode() ^ target.GetHashCode() ^ SourceID.GetHashCode() ^ TargetID.GetHashCode();
        }


        public override bool Equals(object? obj)
        {
            var comp = obj as TraceIssue;
            return (comp.Source == source) &&
                (comp.Target == target) &&
                (comp.SourceID == SourceID) &&
                (comp.TargetID == TargetID);
        }
    }
}
