using System.Collections.Generic;

namespace RoboClerk.Configuration
{
    public interface IConfiguration
    {
        List<string> DataSourcePlugins { get; }
        List<string> PluginDirs { get; }
        string PluginConfigDir { get; }
        string OutputDir { get; }
        string TemplateDir { get; }
        string ProjectRoot { get; }
        bool ClearOutputDir { get; }
        string LogLevel { get; }
        string MediaDir { get; }
        List<TraceEntity> TruthEntities { get; }
        List<DocumentConfig> Documents { get; }
        List<TraceConfig> TraceConfig { get; }
        ConfigurationValues ConfigVals { get; }
        CheckpointConfig CheckpointConfig { get; }
        string CommandLineOptionOrDefault(string name, string defaultValue);

    }
}
