using System;
using System.Collections.Generic;
using System.Linq;

namespace RoboClerk
{
    /// <summary>
    /// Utility class responsible for updating bidirectional item links to ensure
    /// complementary relationships are maintained across all items.
    /// </summary>
    public class ItemLinkUpdater
    {
        private readonly IDataSources dataSources;

        public ItemLinkUpdater(IDataSources dataSources)
        {
            this.dataSources = dataSources ?? throw new ArgumentNullException(nameof(dataSources));
        }

        /// <summary>
        /// Updates all item links across all items in all plugins to ensure bidirectional
        /// relationships are maintained. If item A links to item B with link type X,
        /// and item B doesn't have a complementary link back to item A, it will be created.
        /// </summary>
        /// <param name="plugins">List of plugins containing items to update</param>
        public void UpdateAllItemLinks(List<IDataSourcePlugin> plugins)
        {
            if (plugins == null)
                throw new ArgumentNullException(nameof(plugins));

            // Collect all linked items from all plugins
            var allItems = GetAllLinkedItemsFromPlugins(plugins);

            // Process each item to ensure bidirectional links
            foreach (var sourceItem in allItems)
            {
                UpdateLinksForItem(sourceItem, allItems);
            }
        }

        /// <summary>
        /// Collects all LinkedItem instances from all plugins
        /// </summary>
        private List<LinkedItem> GetAllLinkedItemsFromPlugins(List<IDataSourcePlugin> plugins)
        {
            var allItems = new List<LinkedItem>();

            foreach (var plugin in plugins)
            {
                // Get all different types of items from each plugin
                allItems.AddRange(plugin.GetSystemRequirements().Cast<LinkedItem>());
                allItems.AddRange(plugin.GetSoftwareRequirements().Cast<LinkedItem>());
                allItems.AddRange(plugin.GetDocumentationRequirements().Cast<LinkedItem>());
                allItems.AddRange(plugin.GetDocContents().Cast<LinkedItem>());
                allItems.AddRange(plugin.GetSoftwareSystemTests().Cast<LinkedItem>());
                allItems.AddRange(plugin.GetAnomalies().Cast<LinkedItem>());
                allItems.AddRange(plugin.GetRisks().Cast<LinkedItem>());
                allItems.AddRange(plugin.GetSOUP().Cast<LinkedItem>());
                allItems.AddRange(plugin.GetUnitTests().Cast<LinkedItem>());
            }

            return allItems;
        }

        /// <summary>
        /// Updates links for a specific item by ensuring all its outgoing links
        /// have corresponding incoming links on the target items
        /// </summary>
        private void UpdateLinksForItem(LinkedItem sourceItem, List<LinkedItem> allItems)
        {
            foreach (var outgoingLink in sourceItem.LinkedItems.ToList()) // ToList to avoid collection modification issues
            {
                var targetItem = allItems.FirstOrDefault(item => item.ItemID == outgoingLink.TargetID);
                if (targetItem != null)
                {
                    var complementaryLinkType = GetComplementaryLinkType(outgoingLink.LinkType);
                    if (complementaryLinkType != ItemLinkType.None)
                    {
                        // Check if the target item already has a link back to the source item
                        var existingBackLink = targetItem.LinkedItems
                            .FirstOrDefault(link => link.TargetID == sourceItem.ItemID);

                        if (existingBackLink == null)
                        {
                            // Create the complementary link
                            var backLink = new ItemLink(sourceItem.ItemID, complementaryLinkType);
                            targetItem.AddLinkedItem(backLink);
                        }
                        else if (existingBackLink.LinkType != complementaryLinkType)
                        {
                            // Update the existing link type if it's different from what we expect
                            existingBackLink.LinkType = complementaryLinkType;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the complementary link type for a given link type.
        /// For example, Parent -> Child, TestedBy -> Tests, etc.
        /// </summary>
        private ItemLinkType GetComplementaryLinkType(ItemLinkType linkType)
        {
            return linkType switch
            {
                ItemLinkType.Parent => ItemLinkType.Child,
                ItemLinkType.Child => ItemLinkType.Parent,
                ItemLinkType.TestedBy => ItemLinkType.Tests,
                ItemLinkType.Tests => ItemLinkType.TestedBy,
                ItemLinkType.Predecessor => ItemLinkType.Successor,
                ItemLinkType.Successor => ItemLinkType.Predecessor,
                ItemLinkType.Affects => ItemLinkType.AffectedBy,
                ItemLinkType.AffectedBy => ItemLinkType.Affects,
                ItemLinkType.RiskControl => ItemLinkType.Risk,
                ItemLinkType.Risk => ItemLinkType.RiskControl,
                ItemLinkType.UnitTest => ItemLinkType.UnitTests,
                ItemLinkType.UnitTests => ItemLinkType.UnitTest,
                ItemLinkType.Related => ItemLinkType.Related, // Related is bidirectional
                ItemLinkType.Duplicate => ItemLinkType.Duplicate, // Duplicate is bidirectional
                ItemLinkType.DOC => ItemLinkType.None, // DOC links are unidirectional
                ItemLinkType.None => ItemLinkType.None,
                _ => ItemLinkType.None
            };
        }
    }
}