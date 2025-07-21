using RoboClerk.Configuration;
using System.Collections.Generic;
using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace RoboClerk.AISystem
{
    public abstract class AISystemPluginBase : FileAccessPluginBase, IAISystemPlugin
    {
        private Dictionary<string,string> promptTemplateFiles = new Dictionary<string, string>();

        protected AISystemPluginBase(IFileProviderPlugin fileSystem) : base(fileSystem)
        {
        }

        public IEnumerable<DocumentConfig> GetAIPromptTemplates()
        {
            List<DocumentConfig> templates = new List<DocumentConfig>();
            foreach(var template in promptTemplateFiles)
            {
                DocumentConfig config = new DocumentConfig($"{template.Key}_AIPrompt", $"{template.Key}_AIPrompt",
                    $"{template.Key}_AIPrompt", $"{template.Key}_AIPrompt", template.Value);
                templates.Add(config);
            }
            return templates;
        }

        public override void InitializePlugin(IConfiguration configuration)
        {
            var config = GetConfigurationTable(configuration.PluginConfigDir, $"{name}.toml");
            promptTemplateFiles["SystemRequirement"]=configuration.CommandLineOptionOrDefault("SystemRequirement", GetObjectForKey<string>(config, "SystemRequirement", true));
            promptTemplateFiles["SoftwareRequirement"]=configuration.CommandLineOptionOrDefault("SoftwareRequirement", GetObjectForKey<string>(config, "SoftwareRequirement", true));
            promptTemplateFiles["DocumentationRequirement"]=configuration.CommandLineOptionOrDefault("DocumentationRequirement", GetObjectForKey<string>(config, "DocumentationRequirement", true));
        }

        public abstract string GetFeedback(TraceEntity et, Item item);

        public abstract void SetPrompts(List<Document> pts);
    }
}
