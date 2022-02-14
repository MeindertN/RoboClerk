using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboClerk.Configuration
{
    public interface IConfiguration
    {
        List<string> DataSourcePlugins { get; }
        internal List<string> PluginDirs { get; }
        internal string OutputDir { get; }
        internal string LogLevel { get; }
        internal List<TraceEntity> TruthEntities { get; }
        internal List<DocumentConfig> Documents { get; }
        internal List<TraceConfig> TraceConfig { get; }
        internal ConfigurationValues ConfigVals { get; }
    }
}
