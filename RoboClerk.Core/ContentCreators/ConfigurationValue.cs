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
        /// Gets metadata for the ConfigurationValue content creator, optionally using configuration to populate tags
        /// </summary>
        public static ContentCreatorMetadata GetMetadata(IConfiguration? config = null)
        {
            var metadata = new ContentCreatorMetadata("Config", "Configuration Value", 
                "Retrieves configuration values from the RoboClerk configuration")
            {
                Category = "Configuration",
                Tags = new List<ContentCreatorTag>()
            };

            if (config != null && config.ConfigVals != null)
            {
                foreach (var key in config.ConfigVals.Keys)
                {
                    metadata.Tags.Add(new ContentCreatorTag(key, $"Retrieves the value of configuration key '{key}'", "ConfigurationValue")
                    {
                        Category = "Configuration Access",
                        Description = $"Returns the value of the '{key}' configuration key from the RoboClerk configuration file.",
                        Parameters = new List<ContentCreatorParameter>()
                        {
                            new ContentCreatorParameter("key",
                                "Set to the key of the config value to retrieve.",
                                ParameterValueType.String, required: true)
                            {
                                AllowedValues = new List<string> { key },
                                ExampleValue = key
                            }
                        },
                        ExampleUsage = $"@@Config:ConfigurationValue(key={key})@@"
                    });
                }
            }

            return metadata;
        }

        /// <summary>
        /// Static metadata for the ConfigurationValue content creator
        /// </summary>
        public static ContentCreatorMetadata StaticMetadata { get; } = GetMetadata();

        public override ContentCreatorMetadata GetMetadata() => GetMetadata(configuration);

        public override string GetContent(IRoboClerkTag tag, DocumentConfig doc)
        {
            if (!tag.HasParameter("key"))
            {
                //for backwards compatibility, if no key is specified, use the ContentCreatorID as the key
                return data.GetConfigValue(tag.ContentCreatorID);
            }
            return data.GetConfigValue(tag.GetParameterOrDefault("key"));
        }
    }
}
