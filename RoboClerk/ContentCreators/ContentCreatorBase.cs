using RoboClerk.Configuration;
using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace RoboClerk.ContentCreators
{
    public abstract class ContentCreatorBase : IContentCreator
    {
        protected static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        protected ITraceabilityAnalysis analysis = null;
        protected IDataSources data = null;

        public ContentCreatorBase(IDataSources data, ITraceabilityAnalysis analysis)
        {
            this.data = data;
            this.analysis = analysis;
        }

        public abstract string GetContent(RoboClerkTag tag, DocumentConfig doc);

        protected bool ShouldBeIncluded<T>(RoboClerkTag tag, T item, PropertyInfo[] properties)
        {
            foreach (var param in tag.Parameters)
            {
                foreach (var prop in properties)
                {
                    if (prop.Name.ToUpper() == param)
                    {
                        if (prop.GetValue(item).ToString().ToUpper() != tag.GetParameterOrDefault(param,string.Empty).ToUpper())
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        protected bool CheckUpdateDateTime(RoboClerkTag tag, Item item)
        {
            foreach (var param in tag.Parameters)
            {
                if (param.ToUpper() == "OLDERTHAN" && DateTime.Compare(item.ItemLastUpdated,Convert.ToDateTime(tag.GetParameterOrDefault(param))) >= 0)
                {
                    return false;
                }
                if (param.ToUpper() == "NEWERTHAN" && DateTime.Compare(item.ItemLastUpdated, Convert.ToDateTime(tag.GetParameterOrDefault(param))) <= 0)
                {
                    return false;
                }
            }
            return true;
        }

        protected void ProcessTraces(TraceEntity docTE, ScriptingBridge dataShare)
        {
            foreach (var trace in dataShare.Traces)
            {
                var item = data.GetItem(trace);
                if (item != null)
                {
                    TraceEntity sourceTE = analysis.GetTraceEntityForID(item.ItemType);
                    analysis.AddTrace(sourceTE, trace, docTE, trace);
                }
                else
                {
                    logger.Warn($"Cannot find item with ID \"{trace}\" as referenced in {docTE.Name}. Possible trace issue.");
                    analysis.AddTrace(null, trace, docTE, trace);
                }
            }
        }

    }
}
