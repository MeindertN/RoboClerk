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

        public override string GetContent(IRoboClerkTag tag, DocumentConfig doc)
        {
            return data.GetConfigValue(tag.ContentCreatorID);
        }
    }
}
