using System;

namespace RoboClerk.Items
{
    public enum TestResultType
    {
        UNIT,
        SYSTEM
    }

    public enum TestResultStatus
    {
        PASS,
        FAIL
    }

    public class TestResult : LinkedItem
    {
        private string name;
        private string testID;
        private TestResultType resultType;
        private TestResultStatus resultStatus;
        private string message;
        private DateTime executionTime;

        public TestResult(string testID, TestResultType type, TestResultStatus status, string name = default, 
            string message = default, DateTime executionTime = default)
        {
            this.testID = testID;
            this.name = name;
            this.resultType = type;
            this.resultStatus = status;
            this.message = message;
            this.executionTime = executionTime;
            this.type = "TestResult";
            
            // Set the ItemID to a unique identifier for this test result
            this.id = Guid.NewGuid().ToString();
            
            // Create appropriate links based on the test result type
            CreateTestLinks();
        }

        private void CreateTestLinks()
        {
            if (!string.IsNullOrEmpty(testID))
            {
                // Create a link back to the test that this result belongs to
                // Use ResultOf link type to indicate this result is the result of the specified test
                AddLinkedItem(new ItemLink(testID, ItemLinkType.ResultOf));
            }
        }

        public string TestID
        {
            get { return testID; }         
        }   

        public string Name
        {
            get { return name; }
        }

        public TestResultType ResultType
        {
            get { return resultType; }
        }

        public TestResultStatus ResultStatus
        { 
            get { return resultStatus; } 
        }

        public string Message
        {
            get { return message; } 
        }

        public DateTime ExecutionTime
        {
            get { return executionTime; } 
        }
    }
}
