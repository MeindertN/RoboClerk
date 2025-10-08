using System.Collections.Generic;

namespace RoboClerk.Core.Configuration
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
        string OutputFormat { get; }
        string MediaDir { get; }
        bool AICheckTemplateContents { get; }
        string AIPlugin { get; }
        List<TraceEntity> TruthEntities { get; }
        List<TraceEntity> AICheckTraceEntities { get; }
        List<DocumentConfig> Documents { get; }
        List<TraceConfig> TraceConfig { get; }
        ConfigurationValues ConfigVals { get; }
        CheckpointConfig CheckpointConfig { get; }
        string GetCommandLineOption(string name);
        bool HasCommandLineOption(string name);
        public void AddOrUpdateCommandLineOption(string name, string value);
        string CommandLineOptionOrDefault(string name, string defaultValue);
    }
}
