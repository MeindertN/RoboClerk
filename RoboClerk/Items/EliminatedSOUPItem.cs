namespace RoboClerk
{
    public class EliminatedSOUPItem : EliminatedLinkedItem
    {
        public EliminatedSOUPItem(SOUPItem originalItem, string reason, EliminationReason eliminationType)
            : base(originalItem, reason, eliminationType)
        {
            // For SOUPs, we need to keep their specific properties
            SOUPName = originalItem.SOUPName;
            SOUPVersion = originalItem.SOUPVersion;
            SOUPLinkedLib = originalItem.SOUPLinkedLib;
            SOUPDetailedDescription = originalItem.SOUPDetailedDescription;
            SOUPAnomalyListDescription = originalItem.SOUPAnomalyListDescription;
            SOUPEnduserTraining = originalItem.SOUPEnduserTraining;
            SOUPLicense = originalItem.SOUPLicense;
            SOUPInstalledByUser = originalItem.SOUPInstalledByUser;
            SOUPInstalledByUserText = originalItem.SOUPInstalledByUserText;
            SOUPPerformanceCritical = originalItem.SOUPPerformanceCritical;
            SOUPPerformanceCriticalText = originalItem.SOUPPerformanceCriticalText;
            SOUPCybersecurityCritical = originalItem.SOUPCybersecurityCritical;
            SOUPCybersecurityCriticalText = originalItem.SOUPCybersecurityCriticalText;
            SOUPManufacturer = originalItem.SOUPManufacturer;
        }

        public string SOUPName { get; private set; }
        public string SOUPVersion { get; private set; }
        public bool SOUPLinkedLib { get; private set; }
        public string SOUPDetailedDescription { get; private set; }
        public string SOUPAnomalyListDescription { get; private set; }
        public string SOUPEnduserTraining { get; private set; }
        public string SOUPLicense { get; private set; }
        public bool SOUPInstalledByUser { get; private set; }
        public string SOUPInstalledByUserText { get; private set; }
        public bool SOUPPerformanceCritical { get; private set; }
        public string SOUPPerformanceCriticalText { get; private set; }
        public bool SOUPCybersecurityCritical { get; private set; }
        public string SOUPCybersecurityCriticalText { get; private set; }
        public string SOUPManufacturer { get; private set; }
    }
}
