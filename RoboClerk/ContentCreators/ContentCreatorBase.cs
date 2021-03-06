using RoboClerk.Configuration;
using System.Reflection;

namespace RoboClerk.ContentCreators
{
    public abstract class ContentCreatorBase : IContentCreator
    {
        protected static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public abstract string GetContent(RoboClerkTag tag, IDataSources data, ITraceabilityAnalysis analysis, DocumentConfig doc);

        protected bool ShouldBeIncluded<T>(RoboClerkTag tag, T item, PropertyInfo[] properties)
        {
            foreach (var param in tag.Parameters)
            {
                foreach (var prop in properties)
                {
                    if (prop.Name.ToUpper() == param.Key)
                    {
                        if (prop.GetValue(item).ToString().ToUpper() != param.Value.ToUpper())
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }
    }
}
