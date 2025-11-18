using RoboClerk.Core.Configuration;
using RoboClerk.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace RoboClerk.ContentCreators
{
    public abstract class MultiItemContentCreator : ContentCreatorBase
    {
        public MultiItemContentCreator(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration config)
            : base(data, analysis, config)
        {
        }

        protected abstract string GenerateContent(IRoboClerkTag tag, List<LinkedItem> items, TraceEntity sourceTE, TraceEntity docTE);

        /// <summary>
        /// Creates a ScriptingBridge with the current tag set for parameter access.
        /// </summary>
        /// <param name="tag">The RoboClerk tag to associate with the bridge</param>
        /// <param name="sourceTE">The source trace entity</param>
        /// <returns>A configured ScriptingBridge instance</returns>
        protected ScriptingBridge CreateScriptingBridge(IRoboClerkTag tag, TraceEntity sourceTE)
        {
            var bridge = new ScriptingBridge(data, analysis, sourceTE, configuration);
            bridge.Tag = tag;
            return bridge;
        }

        /// <summary>
        /// Performs natural sorting on a string value, treating numeric parts as numbers
        /// </summary>
        /// <param name="value">The string value to create sort keys for</param>
        /// <returns>An enumerable of comparable objects for natural sorting</returns>
        private static IEnumerable<IComparable> GetNaturalSortKeys(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                yield break;
            }

            // Split the string into parts, keeping numeric sequences together
            var parts = Regex.Split(value, @"(\d+)");
            
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part))
                    continue;

                // Try to parse as integer for numeric parts
                if (int.TryParse(part, out int numericValue))
                {
                    yield return numericValue;
                }
                else
                {
                    // Non-numeric parts are compared as strings (case-insensitive)
                    yield return part.ToLowerInvariant();
                }
            }
        }

        /// <summary>
        /// Sorts items based on tag parameters SORTBY and SORTORDER with natural sorting for string values
        /// </summary>
        /// <param name="tag">The RoboClerk tag containing sorting parameters</param>
        /// <param name="items">The items to sort</param>
        /// <returns>Sorted list of items</returns>
        protected List<LinkedItem> SortItems(IRoboClerkTag tag, List<LinkedItem> items)
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

                // Perform sorting with natural sort for strings
                var sortedItems = ascending 
                    ? items.OrderBy(item => GetNaturalSortValue(item, sortProperty), new NaturalSortComparer()).ToList()
                    : items.OrderByDescending(item => GetNaturalSortValue(item, sortProperty), new NaturalSortComparer()).ToList();

                logger.Debug($"Sorted {items.Count} items by {sortProperty.Name} in {(ascending ? "ascending" : "descending")} order using natural sorting");
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
        private object GetNaturalSortValue(LinkedItem item, PropertyInfo property)
        {
            var value = property.GetValue(item);
            
            // Handle null values by returning empty string for consistent sorting
            if (value == null)
            {
                return string.Empty;
            }

            // For strings, return the string for natural sorting
            if (value is string stringValue)
            {
                return stringValue ?? string.Empty;
            }

            // For non-string types, convert to string for consistent natural sorting
            return value.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Comparer that implements natural sorting for strings containing numeric parts
        /// </summary>
        private class NaturalSortComparer : IComparer<object>
        {
            public int Compare(object x, object y)
            {
                if (x == null && y == null) return 0;
                if (x == null) return -1;
                if (y == null) return 1;

                string strX = x.ToString() ?? string.Empty;
                string strY = y.ToString() ?? string.Empty;

                var keysX = GetNaturalSortKeys(strX).ToArray();
                var keysY = GetNaturalSortKeys(strY).ToArray();

                int minLength = Math.Min(keysX.Length, keysY.Length);
                
                for (int i = 0; i < minLength; i++)
                {
                    int comparison = keysX[i].CompareTo(keysY[i]);
                    if (comparison != 0)
                    {
                        return comparison;
                    }
                }

                // If all compared parts are equal, the shorter one comes first
                return keysX.Length.CompareTo(keysY.Length);
            }
        }

        public override string GetContent(IRoboClerkTag tag, DocumentConfig doc)
        {
            var te = analysis.GetTraceEntityForAnyProperty(tag.ContentCreatorID);
            if (te == null)
            {
                throw new Exception($"Trace entity for content creator \"{tag.ContentCreatorID}\" is missing, this trace entity must be present for RoboClerk to function.");
            }
            bool foundContent = false;
            var items = data.GetItems(te);
            StringBuilder output = new StringBuilder();
            PropertyInfo[] properties = Array.Empty<PropertyInfo>();
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
                content = GenerateContent(tag, includedItems, te, analysis.GetTraceEntityForTitle(doc.DocumentTitle));
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
