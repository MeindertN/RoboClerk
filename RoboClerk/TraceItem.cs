using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk
{
    public abstract class TraceItem : Item
    {
        protected List<(string,Uri)> parents = new List<(string, Uri)>();
        protected List<(string,Uri)> children = new List<(string, Uri)>();

        public IEnumerable<(string,Uri)> Parents
        {
            get => parents;
        }

        public IEnumerable<(string,Uri)> Children
        {
            get => children;
        }

        public void AddChild(string child, Uri link)
        {
            children.Add((child,link));
        }

        public void AddParent(string parent, Uri link)
        {
            parents.Add((parent,link));
        }
    }
}
