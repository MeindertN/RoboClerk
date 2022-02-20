using System.Collections.Generic;
using Tomlyn.Model;

namespace RoboClerk.Configuration
{
    public class TraceConfig
    {
        private string id = string.Empty;
        private RoboClerkOrderedDictionary<string,List<string>> traces = new RoboClerkOrderedDictionary<string, List<string>>();

        public TraceConfig(string ID)
        {
            id = ID;
        }

        public void AddTraces(TomlTable toml)
        {
            foreach (var doc in toml)
            {
                foreach (var obj in (TomlArray)doc.Value)
                {
                    if(!traces.ContainsKey(doc.Key))
                    {
                        traces[doc.Key] = new List<string>();
                    }
                    traces[doc.Key].Add((string)obj);
                }
            }
        }
        public string ID => id;
        public RoboClerkOrderedDictionary<string,List<string>> Traces => traces;
    }
}
