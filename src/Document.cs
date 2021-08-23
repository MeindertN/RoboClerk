using System;
using System.Collections.Generic;
using RoboClerk;
using Markdig.Syntax;

namespace RoboClerk
{
    abstract class Document
    {
        private string title = "";
        
        private List<Section> sections = new List<Section>();

        public abstract void FromMarkDown(MarkdownDocument markdown);

        public IEnumerable<Section> Sections
        {
            get => sections; 
        }
    }
}