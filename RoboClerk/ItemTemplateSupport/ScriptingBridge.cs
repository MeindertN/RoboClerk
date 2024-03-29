﻿using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RoboClerk
{
    public class ScriptingBridge
    {
        private IDataSources data = null;
        private ITraceabilityAnalysis analysis = null;
        private List<string> traces = new List<string>();
        private List<LinkedItem> items = new List<LinkedItem>();
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public ScriptingBridge(IDataSources data, ITraceabilityAnalysis trace, TraceEntity sourceTraceEntity)
        {
            this.data = data;
            this.analysis = trace;
            SourceTraceEntity = sourceTraceEntity;
        }

        /// <summary>
        /// The item that needs to be rendered in the documentation. 
        /// </summary>
        public LinkedItem Item { get; set; }

        /// <summary>
        /// The items that need to be rendered in the documentation (empty if there is only a single item). 
        /// </summary>
        public IEnumerable<LinkedItem> Items
        {
            get { return items; }
            set { items = (List<LinkedItem>)value; }
        }

        /// <summary>
        /// This function adds a trace link to the item who's ID is provided in the id parameter.
        /// </summary>
        /// <param name="id"></param>
        public void AddTrace(string id)
        {
            traces.Add(id);
        }

        /// <summary>
        /// Returns all the traces in the bridge.
        /// </summary>
        public IEnumerable<string> Traces
        {
            get { return traces; }
        }

        /// <summary>
        /// Returns the source trace entity, indicates what kind of trace item is being rendered.
        /// </summary>
        public TraceEntity SourceTraceEntity { get; }

        /// <summary>
        /// Provides access to the RoboClerk datasources object. This can be used to search in all 
        /// the information RoboClerk has.
        /// </summary>
        public IDataSources Sources { get { return data; } }

        /// <summary>
        /// Provides access to the traceability analysis object. The object stores all traceability
        /// information in RoboClerk.
        /// </summary>
        public ITraceabilityAnalysis TraceabilityAnalysis { get { return analysis; } }

        /// <summary>
        /// Returns all linked items attached to li with a link type of linkType
        /// </summary>
        /// <param name="li"></param>
        /// <param name="linkType"></param>
        /// <returns></returns>
        public IEnumerable<LinkedItem> GetLinkedItems(LinkedItem li, ItemLinkType linkType)
        {
            List<LinkedItem> results = new List<LinkedItem>();
            var linkedItems = li.LinkedItems.Where(x => x.LinkType == linkType);
            foreach (var item in linkedItems)
            {
                var linkedItem = (LinkedItem)data.GetItem(item.TargetID);
                if (linkedItem != null)
                {
                    results.Add(linkedItem);
                }
            }
            return results;
        }

        /// <summary>
        /// This function retrieves all items linked to li with a link of type linkType and returns an
        /// asciidoc string that has links to all these items. Usually used to provide trace information.
        /// You can use includeTitle to control if the title of the linked item is included as well.
        /// </summary>
        /// <param name="li"></param>
        /// <param name="linkType"></param>
        /// <param name="includeTitle"</param>
        /// <returns>AsciiDoc string with http link to item.</returns>
        /// <exception cref="System.Exception"></exception>
        public string GetLinkedField(LinkedItem li, ItemLinkType linkType, bool includeTitle = true)
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
                        AddTrace(linkedItem.ItemID);
                        field.Append(linkedItem.HasLink ? $"{linkedItem.Link}[{linkedItem.ItemID}]" : linkedItem.ItemID);
                        if (includeTitle && linkedItem.ItemTitle != string.Empty)
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

        /// <summary>
        /// Returns an asciidoc hyperlink for the provided item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public string GetItemLinkString(Item item)
        {
            return item.HasLink ? $"{item.Link}[{item.ItemID}]" : $"{item.ItemID}";
        }

        /// <summary>
        /// Convenience function, checks if value is an empty string, if so, the
        /// defaultValue is returned.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public string GetValOrDef(string value, string defaultValue)
        {
            if (value == string.Empty)
            {
                return defaultValue;
            }
            return value;
        }

        /// <summary>
        /// Convenience function, calls ToString on input and returns resulting string.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public string Insert(object input)
        {
            return input.ToString();
        }
    }
}
