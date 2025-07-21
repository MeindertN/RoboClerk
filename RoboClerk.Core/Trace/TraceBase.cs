namespace RoboClerk
{
    public class TraceBase
    {
        protected TraceEntity source = null;
        protected TraceEntity target = null;

        public TraceBase(TraceEntity source, TraceEntity target)
        {
            this.source = source;
            this.target = target;
        }

        public TraceEntity Source
        {
            get => source;
        }

        public TraceEntity Target
        {
            get => target;
        }
    }
}
