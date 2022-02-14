using System.Reflection;

namespace RoboClerk.ContentCreators
{
    public abstract class ContentCreatorBase : IContentCreator
    {
        public abstract string GetContent(RoboClerkTag tag, IDataSources data, ITraceabilityAnalysis analysis, string docTitle);

        protected bool ShouldBeIncluded<T>(RoboClerkTag tag, T item, PropertyInfo[] properties)
        {
            foreach (var param in tag.Parameters)
            {
                foreach (var prop in properties)
                {
                    if (prop.Name.ToUpper() == param.Key)
                    {
                        if (prop.GetValue(item).ToString() != param.Value)
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
