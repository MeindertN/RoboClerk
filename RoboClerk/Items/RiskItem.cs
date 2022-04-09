using System;

namespace RoboClerk
{
    public class RiskItem : LinkedItem
    {
        private string primaryHazard = string.Empty;
        private string failureMode = string.Empty;
        private string causeOfFailure = string.Empty;
        private int severityScore = int.MaxValue;
        private int occurenceScore = int.MaxValue;
        private int detectabilityScore = int.MaxValue;

        private string riskControlMeasure = string.Empty;
        private string riskControlMeasureType = string.Empty;
        private string riskControlImplementation = string.Empty;
        private int modifiedOccScore = int.MaxValue;
        private int modifiedDetScore = int.MaxValue;

        private string riskRevision = string.Empty;

        public RiskItem()
        {
            type = "Risk";
            id = Guid.NewGuid().ToString();
        }

        public override string ToText()
        {
            throw new NotImplementedException();
        }

        public string PrimaryHazard
        {
            get { return primaryHazard; }
            set { primaryHazard = value; }
        }

        public string FailureMode
        {
            get { return failureMode; }
            set { failureMode = value; }
        }

        public string CauseOfFailure
        {
            get { return causeOfFailure; }
            set { causeOfFailure = value; }
        }

        public int OccurenceScore
        {
            get { return occurenceScore; }
            set { occurenceScore = value; }
        }

        public int SeverityScore
        {
            get { return severityScore; }
            set { severityScore = value; }
        }

        public int DetectabilityScore
        {
            get { return detectabilityScore; }
            set { detectabilityScore = value; }
        }

        public string RiskControlMeasure
        {
            get { return riskControlMeasure; }
            set { riskControlMeasure = value; }
        }

        public string RiskControlMeasureType
        {
            get { return riskControlMeasureType; }
            set { riskControlMeasureType = value; }
        }

        public string RiskControlImplementation
        {
            get { return riskControlImplementation; }
            set { riskControlImplementation = value; }
        }

        public int ModifiedOccScore
        {
            get {  return modifiedOccScore; }
            set { modifiedOccScore = value; }
        }

        public int ModifiedDetScore
        {
            get { return modifiedDetScore; }
            set { modifiedDetScore = value; }
        }

        public string RiskRevision
        {
            get { return riskRevision; }
            set { riskRevision = value; }
        }
    }
}
