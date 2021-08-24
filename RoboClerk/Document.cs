using System;
using System.Collections.Generic;
using RoboClerk;

namespace RoboClerk
{
    abstract class Document
    {
        protected string title = "";

        protected List<RequestTag> requestTags = new List<RequestTag>();
                
        public void FromMarkDown(string markdown)
        {
            


        }

        public abstract string ToMarkDown();

        public IEnumerable<RequestTag> RequestTags
        {
            get => requestTags; 
        }
    }
}