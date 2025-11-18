namespace RoboClerk
{
    public class EliminatedUnitTestItem : EliminatedLinkedItem
    {
        public EliminatedUnitTestItem()
        {
        }

        public EliminatedUnitTestItem(UnitTestItem originalItem, string reason, EliminationReason eliminationType)
            : base(originalItem, reason, eliminationType)
        {
            // Copy UnitTestItem-specific properties
            UnitTestState = originalItem.UnitTestState;
            UnitTestPurpose = originalItem.UnitTestPurpose;
            UnitTestAcceptanceCriteria = originalItem.UnitTestAcceptanceCriteria;
            UnitTestFileLocation = originalItem.UnitTestFileLocation;
            UnitTestFileName = originalItem.UnitTestFileName;
            UnitTestFunctionName = originalItem.UnitTestFunctionName;
        }

        public string UnitTestState { get; set; }
        public string UnitTestPurpose { get; set; }
        public string UnitTestAcceptanceCriteria { get; set; }
        public string UnitTestFileLocation { get; set; }
        public string UnitTestFileName { get; set; }
        public string UnitTestFunctionName { get; set; }
    }
}