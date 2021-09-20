using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk
{
    public abstract class TraceItem : Item
    {
        protected List<string> parents = new List<string>();
        protected List<string> children = new List<string>();

        public IEnumerable<string> Parents
        {
            get => parents;
        }

        public IEnumerable<string> Children
        {
            get => children;
        }

        public void AddChild(string child)
        {
            children.Add(child);
        }

        public void AddParent(string parent)
        {
            parents.Add(parent);
        }
    }
}
