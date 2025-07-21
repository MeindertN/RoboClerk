using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboClerk.Configuration
{
    public class TruthItemConfig
    {
        public string Name { get; }
        public bool Filtered { get; }

        public TruthItemConfig()
        {
            Name = string.Empty;
            Filtered = false;
        }

        public TruthItemConfig(string name, bool filter)
        {
            Name = name;
            Filtered = filter;
        }
    }
}
