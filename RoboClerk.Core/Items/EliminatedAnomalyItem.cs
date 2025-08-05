namespace RoboClerk
{
    public class EliminatedAnomalyItem : EliminatedLinkedItem
    {
        public EliminatedAnomalyItem()
        {
            //for serialization
        }

        public EliminatedAnomalyItem(AnomalyItem originalItem, string reason, EliminationReason eliminationType)
            : base(originalItem, reason, eliminationType)
        {
            // For anomalies, we need to keep their specific properties
            AnomalyState = originalItem.AnomalyState;
            AnomalyAssignee = originalItem.AnomalyAssignee;
            AnomalySeverity = originalItem.AnomalySeverity;
            AnomalyJustification = originalItem.AnomalyJustification;
            AnomalyDetailedDescription = originalItem.AnomalyDetailedDescription;
        }

        public string AnomalyState { get; private set; } = string.Empty;
        public string AnomalyAssignee { get; private set; } = string.Empty;
        public string AnomalySeverity { get; private set; } = string.Empty;
        public string AnomalyJustification { get; private set; } = string.Empty;
        public string AnomalyDetailedDescription { get; private set; } = string.Empty;
    }
}
