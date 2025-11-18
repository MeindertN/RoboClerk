using System;

namespace RoboClerk
{
    public class EliminatedTestResult : EliminatedLinkedItem
    {
        public EliminatedTestResult()
        {
        }

        public EliminatedTestResult(TestResult originalItem, string reason, EliminationReason eliminationType)
            : base(originalItem, reason, eliminationType)
        {
            // Copy TestResult-specific properties
            TestID = originalItem.TestID;
            Name = originalItem.Name;
            ResultType = originalItem.ResultType;
            ResultStatus = originalItem.ResultStatus;
            Message = originalItem.Message;
            ExecutionTime = originalItem.ExecutionTime;
        }

        public string TestID { get; set; }
        public string Name { get; set; }
        public TestType ResultType { get; set; }
        public TestResultStatus ResultStatus { get; set; }
        public string Message { get; set; }
        public DateTime ExecutionTime { get; set; }
    }
}