using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tomlyn.Model;

namespace RoboClerk.Configuration
{
    internal class TraceConfig
    {
        private string id = string.Empty;
        private RoboClerkOrderedDictionary<string,List<string>> traces = new RoboClerkOrderedDictionary<string, List<string>>();

        internal TraceConfig(string ID)
        {
            id = ID;
        }

        internal void AddTraces(TomlTable toml)
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
        internal string ID => id;
        internal RoboClerkOrderedDictionary<string,List<string>> Traces => traces;
    }
}
