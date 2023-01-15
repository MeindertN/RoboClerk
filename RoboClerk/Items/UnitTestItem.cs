using System;


namespace RoboClerk
{
    public class UnitTestItem : LinkedItem
    {
        private string unitTestState = "";
        private string unitTestPurpose = "";
        private string unitTestAcceptanceCriteria = "";
        private string unitTestFileLocation = "";

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
    }
}
