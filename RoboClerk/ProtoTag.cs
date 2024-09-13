using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboClerk
{
    internal class ProtoTag
    {
        private int startIndex = -1;
        private int endIndex = -1;
        private bool containerTag = false;
        public ProtoTag(int startIndex, bool containerTag = false) 
        {
            this.startIndex = startIndex;
            this.containerTag = containerTag;
        }

        public bool hasEndIndex()
        {
            return endIndex != -1;
        }

        public int StartIndex { get { return startIndex; } }
        public int EndIndex { set { endIndex = value; } get { return endIndex; } }
        public bool ContainerTag { get { return containerTag; } }
    }
}
