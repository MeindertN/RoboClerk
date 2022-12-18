using DocumentFormat.OpenXml.Vml.Office;
using Microsoft.Extensions.Logging.Configuration;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace RoboClerk
{
    public class ScriptingBridge
    {
        private IDataSources data = null;
        private ITraceabilityAnalysis trace = null;
        private List<string> traces = new List<string>();
        private List<LinkedItem> items = new List<LinkedItem>();
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public ScriptingBridge(IDataSources data, ITraceabilityAnalysis trace, TraceEntity sourceTraceEntity)
        {
            this.data = data;
            this.trace = trace;
            SourceTraceEntity = sourceTraceEntity;
        }

        public LinkedItem Item { get; set; }

        public IEnumerable<LinkedItem> Items 
        { 
            get { return items; } 
            set { items = (List<LinkedItem>)value; } 
        }

        public void AddTrace(string id)
        {
            traces.Add(id);
        }

        public IEnumerable<string> Traces
        {
            get { return traces; }
        }

        public TraceEntity SourceTraceEntity { get; }

        public IDataSources Sources { get { return data; } }

        public ITraceabilityAnalysis TraceabilityAnalysis { get { return trace; } }

        public IEnumerable<LinkedItem> GetLinkedItems(LinkedItem li, ItemLinkType linkType)
        {
            List<LinkedItem> results = new List<LinkedItem> ();
            var linkedItems = li.LinkedItems.Where(x => x.LinkType == linkType);
            foreach(var item in linkedItems)
            {
                var linkedItem = (LinkedItem)data.GetItem(item.TargetID);
                if(linkedItem != null)
                {
                    results.Add(linkedItem);
                }
            }
            return results;
        }

        public string GetLinkedField(LinkedItem li, ItemLinkType linkType)
        {
            StringBuilder field = new StringBuilder();
            var linkedItems = li.LinkedItems.Where(x => x.LinkType == linkType);
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
                        logger.Error($"Item with ID {li.ItemID} has a trace link to item with ID {item.TargetID} but that item does not exist.");
                        throw new System.Exception("Item with invalid trace link encountered.");
                    }
                }
                return field.ToString();
            }
            return "N/A";
        }

        public string GetItemLinkString(Item item)
        {
            return item.HasLink ? $"{item.Link}[{item.ItemID}]" : $"{item.ItemID}";
        }

        public string GetValOrDef(string value, string defaultValue)
        {
            if(value == string.Empty)
            {
                return defaultValue;
            }
            return value;
        }

        public string Insert(object input)
        {
            return input.ToString();
        }
    }
}
