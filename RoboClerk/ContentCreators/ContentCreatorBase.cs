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

        protected string GetParentField(LinkedItem item, IDataSources data)
        {
            StringBuilder parentField = new StringBuilder();
            var parents = item.LinkedItems.Where(x => x.LinkType == ItemLinkType.Parent);
            if (parents.Count() > 0)
            {
                foreach (var parent in parents)
                {
                    if (parentField.Length > 0)
                    {
                        parentField.Append(" / ");
                    }
                    var parentItem = data.GetItem(parent.TargetID) as RequirementItem;
                    if (parentItem != null)
                    {
                        parentField.Append(parentItem.HasLink ? $"{parentItem.Link}[{parentItem.ItemID}]" : parentItem.ItemID);
                        if (parentItem.ItemTitle != string.Empty)
                        {
                            parentField.Append($": \"{parentItem.ItemTitle}\"");
                        }
                    }
                    else
                    {
                        parentField.Append(parent.TargetID);
                    }
                }
                return parentField.ToString();
            }
            return "N/A";
        }

    }
}
