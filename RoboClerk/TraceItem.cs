using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk
{
    public abstract class TraceItem : Item
    {
        protected List<TraceItem> parents;
        protected List<TraceItem> children;

        public IEnumerable<TraceItem> Parents
        {
            get => parents;
        }

        public IEnumerable<TraceItem> Children
        {
            get => children;
        }
    }
}
