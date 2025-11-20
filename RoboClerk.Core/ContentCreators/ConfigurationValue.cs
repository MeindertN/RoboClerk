using RoboClerk.Core.Configuration;
using RoboClerk.Core;

namespace RoboClerk.ContentCreators
{
    internal class ConfigurationValue : ContentCreatorBase
    {
        public ConfigurationValue(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration config) 
            : base(data, analysis, config)
        {

        }

        public override ContentCreatorMetadata GetMetadata()
        {
            var metadata = new ContentCreatorMetadata("Config", "Configuration Value", 
                "Retrieves configuration values from the RoboClerk configuration");
            
            metadata.Category = "Configuration";

            // Since Config content creator is dynamic based on configuration values,
            // we provide a general tag description
            var configTag = new ContentCreatorTag("[ConfigKey]", "Retrieves the value of a configuration key");
            configTag.Category = "Configuration Access";
            configTag.Description = "Replace [ConfigKey] with the actual configuration key name. " +
                "Returns the value of the specified configuration key from the RoboClerk configuration file.";
            configTag.ExampleUsage = "@@Config:ProjectName@@";
            metadata.Tags.Add(configTag);

            return metadata;
        }

        public override string GetContent(IRoboClerkTag tag, DocumentConfig doc)
        {
            return data.GetConfigValue(tag.ContentCreatorID);
        }
    }
}
