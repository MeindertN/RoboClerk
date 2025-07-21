using System;


namespace RoboClerk
{
    public class UnitTestItem : LinkedItem
    {
        private string unitTestState = "";
        private string unitTestPurpose = "";
        private string unitTestAcceptanceCriteria = "";
        private string unitTestFileLocation = "";
        private string unitTestFileName = "";
        private string unitTestFunctionName = "";

        public UnitTestItem()
        {
            type = "UnitTest";
            id = Guid.NewGuid().ToString();
        }

        public string UnitTestState
        {
            get { return unitTestState; }
            set { unitTestState = value; }
        }

        public string UnitTestPurpose
        {
            get { return unitTestPurpose; }
            set { unitTestPurpose = value; }
        }

        public string UnitTestAcceptanceCriteria
        {
            get { return unitTestAcceptanceCriteria; }
            set { unitTestAcceptanceCriteria = value; }
        }

        public string UnitTestFileLocation
        {
            get { return unitTestFileLocation; }
            set { unitTestFileLocation = value; }
        }

        public string UnitTestFileName
        { 
            get { return unitTestFileName; } 
            set { unitTestFileName = value; } 
        }

        public string UnitTestFunctionName
        { 
            get { return unitTestFunctionName; } 
            set { unitTestFunctionName = value; } 
        }
    }
}
