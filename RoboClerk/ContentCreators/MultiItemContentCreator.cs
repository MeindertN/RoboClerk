using RoboClerk.Configuration;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace RoboClerk.ContentCreators
{
    public abstract class MultiItemContentCreator : ContentCreatorBase
    {
        public MultiItemContentCreator(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration config)
            : base(data, analysis, config)
        {
        }

        protected abstract string GenerateADocContent(RoboClerkTag tag, List<LinkedItem> items, TraceEntity sourceTE, TraceEntity docTE);

        public override string GetContent(RoboClerkTag tag, DocumentConfig doc)
        {
            var te = analysis.GetTraceEntityForAnyProperty(tag.ContentCreatorID);
            if (te == null)
            {
                throw new Exception($"Trace entity for content creator \"{tag.ContentCreatorID}\" is missing, this trace entity must be present for RoboClerk to function.");
            }
            bool foundContent = false;
            var items = data.GetItems(te);
            StringBuilder output = new StringBuilder();
            PropertyInfo[] properties = null;
            if (items.Count > 0)
            {
                properties = items[0].GetType().GetProperties();
            }
            List<LinkedItem> includedItems = new List<LinkedItem>();
            int index = 0;
            foreach (var item in items)
            {
                if (ShouldBeIncluded(tag, item, properties) && CheckUpdateDateTime(tag, item))
                {
                    foundContent = true;
                    includedItems.Add(item);
                }
                index++;
            }
            string content = string.Empty;
            try
            {
                content = GenerateADocContent(tag, includedItems, te, analysis.GetTraceEntityForTitle(doc.DocumentTitle));
            }
            catch
            {
                logger.Error($"An error occurred while rendering {te.Name} in {doc.DocumentTitle}.");
                throw;
            }
            if (!foundContent)
            {
                content = $"Unable to find specified {te.Name}(s). Check if {te.Name}s are provided or if a valid {te.Name} identifier is specified.";
            }
            return content;
        }
    }
}
