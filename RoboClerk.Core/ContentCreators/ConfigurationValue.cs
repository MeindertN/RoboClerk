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

        /// <summary>
        /// Static metadata for the ConfigurationValue content creator
        /// </summary>
        public static ContentCreatorMetadata StaticMetadata { get; } = new ContentCreatorMetadata("Config", "Configuration Value", 
            "Retrieves configuration values from the RoboClerk configuration")
        {
            Category = "Configuration",
            Tags = new List<ContentCreatorTag>
            {
                new ContentCreatorTag("[ConfigKey]", "Retrieves the value of a configuration key")
                {
                    Category = "Configuration Access",
                    Description = "Replace [ConfigKey] with the actual configuration key name. " +
                        "Returns the value of the specified configuration key from the RoboClerk configuration file.",
                    ExampleUsage = "@@Config:ProjectName@@"
                }
            }
        };

        public override ContentCreatorMetadata GetMetadata() => StaticMetadata;

        public override string GetContent(IRoboClerkTag tag, DocumentConfig doc)
        {
            return data.GetConfigValue(tag.ContentCreatorID);
        }
    }
}
