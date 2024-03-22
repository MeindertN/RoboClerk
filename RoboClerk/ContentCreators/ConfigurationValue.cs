using RoboClerk.Configuration;

namespace RoboClerk.ContentCreators
{
    internal class ConfigurationValue : ContentCreatorBase
    {
        public ConfigurationValue(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration config) 
            : base(data, analysis, config)
        {

        }

        public override string GetContent(RoboClerkTag tag, DocumentConfig doc)
        {
            return data.GetConfigValue(tag.ContentCreatorID);
        }
    }
}
