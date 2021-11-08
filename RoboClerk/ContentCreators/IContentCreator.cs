using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk.ContentCreators
{   
    // content creators use datasources to create document content. They are also responsible for
    // ensuring that the trace is kept up to date with any trace items they add to a document.
    public interface IContentCreator
    {
        public string GetContent(DataSources data, TraceabilityAnalysis analysis, string docTitle);
    }
}
