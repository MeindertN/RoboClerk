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
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private List<string> eliminatedItemIDs = new List<string>();

        public ItemLinkUpdater(IDataSources dataSources)
        {
            this.dataSources = dataSources ?? throw new ArgumentNullException(nameof(dataSources));
        }

        public IReadOnlyList<string> EliminatedItemIDs => eliminatedItemIDs.AsReadOnly();

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

            bool rescan = true;
            while (rescan)
            {
                rescan = false;
                // Collect all linked items from all plugins
                var allItems = GetAllLinkedItemsFromPlugins(plugins);
                var allEliminatedItems = GetAllEliminatedLinkedItemsFromPlugins(plugins);

                foreach (var plugin in plugins)
                {
                    logger.Debug($"ItemLinkUpdater: Processing plugin '{plugin.Name}' with {allItems.Count} items for link updates.");
                    var pluginItems = new List<LinkedItem>();
                    GetAllLinkedItemsFromPlugin(pluginItems, plugin);

                    // Process each item to ensure bidirectional links
                    foreach (var sourceItem in pluginItems)
                    {
                        if (UpdateLinksForItem(sourceItem, allItems, allEliminatedItems))
                        {
                            // If any links were removed, check if any links remain
                            if (!sourceItem.LinkedItems.Any())
                            {
                                logger.Info($"ItemLinkUpdater: Item with ID '{sourceItem.ItemID}' has no remaining links and is being eliminated.");
                                plugin.EliminateItem(sourceItem.ItemID, "All items this item linked to were eliminated.", EliminationReason.LinkedItemMissing);
                                rescan = true;
                            }
                        }
                    }
                }
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
                GetAllLinkedItemsFromPlugin(allItems, plugin);
            }

            return allItems;
        }

        private static void GetAllLinkedItemsFromPlugin(List<LinkedItem> allItems, IDataSourcePlugin plugin)
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
            allItems.AddRange(plugin.GetTestResults().Cast<LinkedItem>());
        }

        /// <summary>
        /// Collects all EliminatedLinkedItem instances from all plugins
        /// </summary>
        private List<EliminatedLinkedItem> GetAllEliminatedLinkedItemsFromPlugins(List<IDataSourcePlugin> plugins)
        {
            var allEliminatedItems = new List<EliminatedLinkedItem>();

            foreach (var plugin in plugins)
            {
                GetAllEliminatedLinkedItemsFromPlugin(allEliminatedItems, plugin);
            }

            return allEliminatedItems;
        }

        private static void GetAllEliminatedLinkedItemsFromPlugin(List<EliminatedLinkedItem> allEliminatedItems, IDataSourcePlugin plugin)
        {
            // Get all different types of eliminated items from each plugin
            allEliminatedItems.AddRange(plugin.GetEliminatedSystemRequirements().Cast<EliminatedLinkedItem>());
            allEliminatedItems.AddRange(plugin.GetEliminatedSoftwareRequirements().Cast<EliminatedLinkedItem>());
            allEliminatedItems.AddRange(plugin.GetEliminatedDocumentationRequirements().Cast<EliminatedLinkedItem>());
            allEliminatedItems.AddRange(plugin.GetEliminatedDocContents().Cast<EliminatedLinkedItem>());
            allEliminatedItems.AddRange(plugin.GetEliminatedSoftwareSystemTests().Cast<EliminatedLinkedItem>());
            allEliminatedItems.AddRange(plugin.GetEliminatedAnomalies().Cast<EliminatedLinkedItem>());
            allEliminatedItems.AddRange(plugin.GetEliminatedRisks().Cast<EliminatedLinkedItem>());
            allEliminatedItems.AddRange(plugin.GetEliminatedSOUP().Cast<EliminatedLinkedItem>());
        }

        /// <summary>
        /// Updates links for a specific item by ensuring all its outgoing links
        /// have corresponding incoming links on the target items
        /// </summary>
        private bool UpdateLinksForItem(LinkedItem sourceItem, List<LinkedItem> allItems, List<EliminatedLinkedItem> allEliminatedItems)
        {
            bool verifyRemainingLinks = false;
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
                    }
                }
                else
                {
                    // Check if the target item was eliminated, if not, throw exception
                    var eliminatedTarget = allEliminatedItems.FirstOrDefault(item => item.ItemID == outgoingLink.TargetID);
                    if (eliminatedTarget == null)
                    {
                        throw new Exception($"ItemLinkUpdater: Target item with ID '{outgoingLink.TargetID}' not found and was not eliminated. Link from item '{sourceItem.ItemID}' of type '{sourceItem.ItemType}' is invalid.");
                    }
                    logger.Warn($"ItemLinkUpdater: Target item with ID '{outgoingLink.TargetID}' not found for link from item '{sourceItem.ItemID}' of type '{sourceItem.ItemType}'. Removing this link.");
                    sourceItem.RemoveLinkedItem(outgoingLink);
                    verifyRemainingLinks = true;
                }
            }
            return verifyRemainingLinks;
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
                ItemLinkType.DOC => ItemLinkType.DocumentedBy,
                ItemLinkType.DocumentedBy => ItemLinkType.DOC,
                ItemLinkType.UnitTest => ItemLinkType.UnitTests,
                ItemLinkType.UnitTests => ItemLinkType.UnitTest,
                ItemLinkType.ResultOf => ItemLinkType.Result, // Test result is result of test -> Test has a result
                ItemLinkType.Result => ItemLinkType.ResultOf, // Test has a result -> Test result is result of test
                ItemLinkType.Related => ItemLinkType.Related, // Related is bidirectional
                ItemLinkType.Duplicate => ItemLinkType.Duplicate, // Duplicate is bidirectional
                ItemLinkType.None => ItemLinkType.None,
                _ => ItemLinkType.None
            };
        }
    }
}