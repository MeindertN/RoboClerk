using System.Collections.Generic;
using System.Linq;

namespace RoboClerk
{
    public class EliminatedSoftwareSystemTestItem : EliminatedLinkedItem
    {
        public EliminatedSoftwareSystemTestItem(SoftwareSystemTestItem originalItem, string reason, EliminationReason eliminationType)
            : base(originalItem, reason, eliminationType)
        {
            // For software system test items, we need to keep their specific properties

            TestCaseState = originalItem.TestCaseState;
            TestCaseDescription = originalItem.TestCaseDescription;
            TestCaseAutomated = originalItem.TestCaseAutomated;
            TestCaseToUnitTest = originalItem.TestCaseToUnitTest;
            TestCaseSteps = originalItem.TestCaseSteps.ToList();
        }

        public string TestCaseState { get; private set; }
        public string TestCaseDescription { get; private set; }
        public bool TestCaseAutomated { get; private set; }
        public bool TestCaseToUnitTest { get; private set; }
        public List<TestStep> TestCaseSteps { get; private set; }
    }
}