using System;

namespace RoboClerk
{
    public class RiskItem : LinkedItem
    {
        private string primaryHazard = string.Empty;
        private string failureMode = string.Empty;
        private string causeOfFailure = string.Empty;
        private string methodOfDetection = string.Empty;

        private int severityScore = int.MaxValue;
        private int occurenceScore = int.MaxValue;
        private int detectabilityScore = int.MaxValue;

        private string riskControlMeasure = string.Empty;
        private string riskControlMeasureType = string.Empty;
        private string riskControlImplementation = string.Empty;
        private int modifiedOccScore = int.MaxValue;
        private int modifiedDetScore = int.MaxValue;

        public RiskItem()
        {
            type = "Risk";
            id = Guid.NewGuid().ToString();
        }

        public string RiskPrimaryHazard
        {
            get { return primaryHazard; }
            set { primaryHazard = value; }
        }

        public string RiskFailureMode
        {
            get { return failureMode; }
            set { failureMode = value; }
        }

        public string RiskCauseOfFailure
        {
            get { return causeOfFailure; }
            set { causeOfFailure = value; }
        }

        public string RiskMethodOfDetection
        {
            get { return methodOfDetection; }
            set { methodOfDetection = value; }
        }

        public int RiskOccurenceScore
        {
            get { return occurenceScore; }
            set { occurenceScore = value; }
        }

        public int RiskSeverityScore
        {
            get { return severityScore; }
            set { severityScore = value; }
        }

        public int RiskDetectabilityScore
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

        public int RiskModifiedOccScore
        {
            get { return modifiedOccScore; }
            set { modifiedOccScore = value; }
        }

        public int RiskModifiedDetScore
        {
            get { return modifiedDetScore; }
            set { modifiedDetScore = value; }
        }
    }
}
