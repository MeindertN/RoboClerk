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
        public abstract string GetContent(RoboClerkTag tag, IDataSources data, ITraceabilityAnalysis analysis, DocumentConfig doc);

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

        protected string GetLinkedField(LinkedItem sourceItem, IDataSources data, ItemLinkType linkType)
        {
            StringBuilder field = new StringBuilder();
            var linkedItems = sourceItem.LinkedItems.Where(x => x.LinkType == linkType);
            if (linkedItems.Count() > 0)
            {
                foreach (var item in linkedItems)
                {
                    if (field.Length > 0)
                    {
                        field.Append(" / ");
                    }
                    var linkedItem = data.GetItem(item.TargetID);
                    if (linkedItem != null)
                    {
                        field.Append(linkedItem.HasLink ? $"{linkedItem.Link}[{linkedItem.ItemID}]" : linkedItem.ItemID);
                        if (linkedItem.ItemTitle != string.Empty)
                        {
                            field.Append($": \"{linkedItem.ItemTitle}\"");
                        }
                    }
                    else
                    {
                        field.Append(item.TargetID);
                    }
                }
                return field.ToString();
            }
            return "N/A";
        }

    }
}
