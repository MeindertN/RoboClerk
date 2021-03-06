namespace RoboClerk
{
    public class TraceLink : TraceBase
    {
        protected string sourceID = string.Empty;
        protected string targetID = string.Empty;
        protected bool valid = true;

        public TraceLink(TraceEntity source, string sourceID, TraceEntity target, string targetID)
            : base(source, target)
        {
            this.sourceID = sourceID;
            this.targetID = targetID;
        }

        public string SourceID
        {
            get
            {
                return sourceID;
            }
        }

        public string TargetID
        {
            get
            {
                return targetID;
            }
        }

        public bool Valid
        {
            get => valid;
        }
    }
}
