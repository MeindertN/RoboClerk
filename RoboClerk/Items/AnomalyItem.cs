using System;

namespace RoboClerk
{
    public class AnomalyItem : LinkedItem
    {
        private string anomalyState = string.Empty;
        private string anomalyAssignee = string.Empty;
        private string anomalySeverity = string.Empty;
        private string anomalyJustification = string.Empty;
        private string anomalyDetailedDescription = string.Empty;

        public AnomalyItem()
        {
            type = "Anomaly";
            id = Guid.NewGuid().ToString();
        }

        public string AnomalyState
        {
            get => anomalyState;
            set => anomalyState = value;
        }

        public string AnomalyAssignee
        {
            get => anomalyAssignee;
            set => anomalyAssignee = value;
        }

        public string AnomalySeverity
        {
            get => anomalySeverity;
            set => anomalySeverity = value;
        }

        public string AnomalyJustification
        {
            get => anomalyJustification;
            set => anomalyJustification = value;
        }

        public string AnomalyDetailedDescription
        {
            get => anomalyDetailedDescription;
            set => anomalyDetailedDescription = value;
        }
    }
}
