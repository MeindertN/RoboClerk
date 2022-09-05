using System.Collections.Generic;
using Tomlyn.Model;

namespace RoboClerk.Configuration
{
    public class TraceConfigElement
    {
        private List<string> forwardFilters = new List<string>();
        private List<string> backwardFilters = new List<string>();
        private string forwardLinkType = string.Empty;
        private string backwardLinkType = string.Empty;

        public void AddForwardFilterString(string filter)
        {
            forwardFilters.Add(filter);
        }

        public void AddBackwardFilterString(string filter)
        {
            backwardFilters.Add(filter);
        }

        public string ForwardLinkType
        {
            get { return forwardLinkType; }
            set { forwardLinkType = value; }
        }

        public string BackwardLinkType
        {
            set { backwardLinkType = value; }
            get { return backwardLinkType; }
        }

        public List<string> ForwardFilters
        {
            get { return forwardFilters; }
        }

        public List<string> BackwardFilters
        {
            get { return backwardFilters; }
        }
    }

    public class TraceConfig
    {
        private string id = string.Empty;
        private RoboClerkOrderedDictionary<string,TraceConfigElement> traces = new RoboClerkOrderedDictionary<string, TraceConfigElement>();

        public TraceConfig(string ID)
        {
            id = ID;
        }

        public void AddTraces(TomlTable toml)
        {
            foreach (var doc in toml)
            {
                var traceTarget = (TomlTable)(doc.Value);
                if (!traces.ContainsKey(doc.Key))
                {
                    traces[doc.Key] = new TraceConfigElement();
                }
                foreach (var element in (TomlArray)traceTarget["forward"])
                {
                    traces[doc.Key].AddForwardFilterString((string)element);
                }
                foreach (var element in (TomlArray)traceTarget["backward"])
                {
                    traces[doc.Key].AddBackwardFilterString((string)element);
                }
                traces[doc.Key].ForwardLinkType = (string)traceTarget["forwardLink"];
                traces[doc.Key].BackwardLinkType = (string)traceTarget["backwardLink"];
            }
        }
        public string ID => id;
        public RoboClerkOrderedDictionary<string,TraceConfigElement> Traces => traces;
    }
}
