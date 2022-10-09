using System;
using System.Text;

namespace RoboClerk
{
    public class AnomalyItem : LinkedItem
    {
        private string anomalyState = string.Empty;
        private string anomalyTitle = string.Empty;
        private string anomalyAssignee = string.Empty;
        private string anomalySeverity = string.Empty;
        private string anomalyJustification = string.Empty;

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

        public string AnomalyTitle
        {
            get => anomalyTitle;
            set => anomalyTitle = value;
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
    }
}
