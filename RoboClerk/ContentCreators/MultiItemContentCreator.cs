using RoboClerk.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// Sorts items based on tag parameters SORTBY and SORTORDER
        /// </summary>
        /// <param name="tag">The RoboClerk tag containing sorting parameters</param>
        /// <param name="items">The items to sort</param>
        /// <returns>Sorted list of items</returns>
        protected List<LinkedItem> SortItems(RoboClerkTag tag, List<LinkedItem> items)
        {
            if (!tag.HasParameter("SORTBY"))
            {
                return items; // No sorting requested, return original list
            }

            string sortBy = tag.GetParameterOrDefault("SORTBY", "").Trim();
            string sortOrder = tag.GetParameterOrDefault("SORTORDER", "ASC").ToUpper().Trim();
            
            if (string.IsNullOrEmpty(sortBy))
            {
                logger.Warn("SORTBY parameter is empty, no sorting will be applied");
                return items;
            }

            bool ascending = sortOrder != "DESC";
            
            try
            {
                // Get the property to sort by
                if (items.Count == 0)
                {
                    return items;
                }

                var itemType = items.First().GetType();
                
                // Try to find exact property match (case-insensitive)
                var sortProperty = itemType.GetProperties()
                    .FirstOrDefault(p => p.Name.Equals(sortBy, StringComparison.OrdinalIgnoreCase));

                if (sortProperty == null)
                {
                    logger.Warn($"Property '{sortBy}' not found on type '{itemType.Name}', no sorting will be applied");
                    return items;
                }

                // Perform sorting
                var sortedItems = ascending 
                    ? items.OrderBy(item => GetSortValue(item, sortProperty)).ToList()
                    : items.OrderByDescending(item => GetSortValue(item, sortProperty)).ToList();

                logger.Debug($"Sorted {items.Count} items by {sortProperty.Name} in {(ascending ? "ascending" : "descending")} order");
                return sortedItems;
            }
            catch (Exception ex)
            {
                logger.Error($"Error sorting items by '{sortBy}': {ex.Message}");
                return items; // Return original list if sorting fails
            }
        }

        /// <summary>
        /// Gets the value to sort by, handling null values and different property types
        /// </summary>
        private object GetSortValue(LinkedItem item, PropertyInfo property)
        {
            var value = property.GetValue(item);
            
            // Handle null values by returning empty string for consistent sorting
            if (value == null)
            {
                return string.Empty;
            }

            // For strings, ensure case-insensitive sorting
            if (value is string stringValue)
            {
                return stringValue ?? string.Empty;
            }

            return value;
        }

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

            // Apply sorting if requested
            includedItems = SortItems(tag, includedItems);

            doc.AddEntityCount(te,(uint)includedItems.Count); //keep track of how many entities we're adding to the document
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
