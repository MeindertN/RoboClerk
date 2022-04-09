using System;


namespace RoboClerk
{
    public class UnitTestItem : LinkedItem
    {
        private string unitTestState = "";
        private string unitTestPurpose = "";
        private string unitTestAcceptanceCriteria = "";
        private string unitTestRevision = "";
        
        public UnitTestItem()
        {
            type = "UnitTest";
            id = Guid.NewGuid().ToString();
        }

        public override string ToText()
        {
            throw new NotImplementedException();
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

        public string UnitTestRevision
        {
            get { return unitTestRevision; }
            set { unitTestRevision = value; }
        }
    }
}
