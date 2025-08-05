namespace RoboClerk
{
    public class EliminatedDocContentItem : EliminatedLinkedItem
    {
        public EliminatedDocContentItem() 
        {
            //for serialization
        }

        public EliminatedDocContentItem(DocContentItem originalItem, string reason, EliminationReason eliminationType)
            : base(originalItem, reason, eliminationType)
        {
            // For DocContent, we need to keep their specific properties
            DocContent = originalItem.DocContent;
        }

        public string DocContent { get; private set; } = string.Empty;
    }
}
