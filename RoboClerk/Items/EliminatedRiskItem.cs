namespace RoboClerk
{
    public class EliminatedRiskItem : EliminatedLinkedItem
    {

        public EliminatedRiskItem() 
        {
            //for serialization
        }

        public EliminatedRiskItem(RiskItem originalItem, string reason, EliminationReason eliminationType)
            : base(originalItem, reason, eliminationType)
        {
            // For risks, we need to keep their specific properties
            PrimaryHazard = originalItem.RiskPrimaryHazard;
            FailureMode = originalItem.RiskFailureMode;
            CauseOfFailure = originalItem.RiskCauseOfFailure;
            MethodOfDetection = originalItem.RiskMethodOfDetection;

            SeverityScore = originalItem.RiskSeverityScore;
            OccurenceScore = originalItem.RiskOccurenceScore;
            DetectabilityScore = originalItem.RiskModifiedDetScore;

            RiskControlMeasure = originalItem.RiskControlMeasure;
            RiskControlMeasureType = originalItem.RiskControlMeasureType;
            RiskControlImplementation = originalItem.RiskControlImplementation;
            ModifiedOccScore = originalItem.RiskModifiedOccScore;
            ModifiedDetScore = originalItem.RiskModifiedDetScore;
        }

        public string PrimaryHazard { get; private set; }
        public string FailureMode { get; private set; }
        public string CauseOfFailure { get; private set; }
        public string MethodOfDetection { get; private set; }

        public int SeverityScore { get; private set; }
        public int OccurenceScore { get; private set; }
        public int DetectabilityScore { get; private set; }

        public string RiskControlMeasure { get; private set; }
        public string RiskControlMeasureType { get; private set; }
        public string RiskControlImplementation { get; private set; }
        public int ModifiedOccScore { get; private set; }
        public int ModifiedDetScore { get; private set; }
    }
}
