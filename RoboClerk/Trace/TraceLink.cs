using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk
{
    public class TraceLink : TraceBase
    {
        protected string id = string.Empty;
        protected bool valid = true;

        public TraceLink(TraceEntity source, TraceEntity target, string id) 
            : base(source, target)
        {
            this.id = id;
        }

        public string TraceID
        {
            get => id;
        }

        public bool Valid
        {
            get => valid;
        }
    }
}
