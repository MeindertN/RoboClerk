using System;
using System.Collections.Generic;
using System.Linq;

namespace RoboClerk
{
    /// <summary>
    /// Simple helper class used by RoboClerkTextParser to track tag positions during parsing
    /// </summary>
    internal class TextTag
    {
        private int startIndex = -1;
        private int endIndex = -1;
        private bool containerTag = false;
        
        public TextTag(int startIndex, bool containerTag = false) 
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