using System.Collections.Generic;

namespace RoboClerk.Configuration
{
    public interface IConfiguration
    {
        List<string> DataSourcePlugins { get; }
        string PluginDir { get; }
        string PluginConfigDir { get; }
        string OutputDir { get; }
        string TemplateDir { get; }
        string ProjectRoot { get; }
        bool ClearOutputDir { get; }
        string LogLevel { get; }
        string OutputFormat { get; }
        string MediaDir { get; }
        bool AICheckTemplateContents { get; }
        string AIPlugin { get; }
        string FileProviderPlugin { get; }
        List<TraceEntity> TruthEntities { get; }
        List<TraceEntity> AICheckTraceEntities { get; }
        List<DocumentConfig> Documents { get; }
        List<TraceConfig> TraceConfig { get; }
        ConfigurationValues ConfigVals { get; }
        CheckpointConfig CheckpointConfig { get; }
        void AddProjectConfig(string projectConfig);
        string CommandLineOptionOrDefault(string name, string defaultValue);

    }
}
