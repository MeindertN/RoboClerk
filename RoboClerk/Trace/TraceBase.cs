using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk
{
    public class TraceBase
    {
        protected TraceEntityType source = TraceEntityType.Unknown;
        protected TraceEntityType target = TraceEntityType.Unknown;

        public TraceBase(TraceEntityType source, TraceEntityType target)
        {
            this.source = source;
            this.target = target;
        }

        public TraceEntityType Source
        {
            get => source;
        }

        public TraceEntityType Target
        {
            get => target;
        }
    }
}
