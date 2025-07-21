using System;

namespace RoboClerk
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

    public class TestResult
    {
        private string id;
        private string name;
        private TestResultType type;
        private TestResultStatus status;
        private string message;
        private DateTime executionTime;

        public TestResult(string id, TestResultType type, TestResultStatus status, string name = default, 
            string message = default, DateTime executionTime = default)
        {
            this.id = id;
            this.name = name;
            this.type = type;
            this.status = status;
            this.message = message;
            this.executionTime = executionTime;
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public string ID
        {
            get { return id; }
            set { id = value; }
        }

        public TestResultType Type
        {
            get { return type; }
            set { type = value; }
        }

        public TestResultStatus Status
        { 
            get { return status; } 
            set { status = value; } 
        }

        public string Message
        {
            get { return message; } 
            set { message = value; } 
        }

        public DateTime ExecutionTime
        {
            get { return executionTime; } 
            set { executionTime = value; } 
        }
    }
}
