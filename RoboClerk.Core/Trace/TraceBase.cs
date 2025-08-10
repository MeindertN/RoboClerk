namespace RoboClerk
{
    public class TraceBase
    {
        protected readonly TraceEntity source;
        protected readonly TraceEntity target;

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
