using System;
using System.Collections.Generic;

namespace RoboClerk
{
    public class TestCaseItem : LinkedItem
    {
        private string testCaseState = string.Empty;
        private string testCaseDescription = string.Empty;
        private bool testCaseAutomated = false;
        private bool testCaseToUnitTest = false;
        private List<string[]> testCaseSteps = new List<string[]>();
        public TestCaseItem()
        {
            type = "TestCase";
            id = Guid.NewGuid().ToString();
        }

        public string TestCaseState
        {
            get => testCaseState;
            set => testCaseState = value;
        }

        public string TestCaseDescription
        {
            get => testCaseDescription;
            set => testCaseDescription = value;
        }

        public List<string[]> TestCaseSteps
        {
            get => testCaseSteps;
            set => testCaseSteps = value;
        }

        public bool TestCaseAutomated
        {
            get => testCaseAutomated;
            set => testCaseAutomated = value;
        }

        public bool TestCaseToUnitTest
        {
            get => testCaseToUnitTest;
            set => testCaseToUnitTest = value;
        }
    }
}

