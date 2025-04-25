using RoboClerk.ContentCreators;

namespace RoboClerk
{
    public enum EliminationReason
    {
        FilteredOut,        // Filtered by the inclusion/exclusion filters
        LinkedItemMissing,  // Removed because its linked parent/related item was filtered
        IgnoredLinkTarget   // Removed because it was linked to an ignored item
    }

    public class EliminatedLinkedItem : LinkedItem
    {
        private string eliminationReason = string.Empty;
        private EliminationReason eliminationType;
        private LinkedItem originalItem;

        public EliminatedLinkedItem()
        {

        }

        public EliminatedLinkedItem(LinkedItem originalItem, string reason, EliminationReason eliminationType)
        {
            // Copy all properties from the original item
            this.id = originalItem.ItemID;
            this.title = originalItem.ItemTitle;
            this.type = originalItem.ItemType;
            this.category = originalItem.ItemCategory;
            this.revision = originalItem.ItemRevision;
            this.status = originalItem.ItemStatus;
            this.targetVersion = originalItem.ItemTargetVersion;
            this.lastUpdated = originalItem.ItemLastUpdated;

            if (originalItem.HasLink)
                this.link = originalItem.Link;

            // Copy linked items
            foreach (var linkedItem in originalItem.LinkedItems)
            {
                this.AddLinkedItem(linkedItem);
            }

            // Set elimination details
            this.eliminationReason = reason;
            this.eliminationType = eliminationType;
        }

        public string EliminationReason
        {
            get => eliminationReason;
            set => eliminationReason = value;
        }

        public EliminationReason EliminationType
        {
            get => eliminationType;
            set => eliminationType = value;
        }
    }
}
