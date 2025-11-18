using System;
using System.Collections.Generic;

namespace RoboClerk
{
    public class SoftwareSystemTestItem : LinkedItem
    {
        private string testCaseState = string.Empty;
        private string testCaseDescription = string.Empty;
        private bool testCaseAutomated = false;
        private bool testCaseToUnitTest = false;
        private List<TestStep> testCaseSteps = new List<TestStep>();

        public SoftwareSystemTestItem()
        {
            type = "SoftwareSystemTest";
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

        public IEnumerable<TestStep> TestCaseSteps
        {
            get => testCaseSteps;
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

        public void AddTestCaseStep(TestStep step)
        {
            testCaseSteps.Add(step);
        }

        public void ClearTestCaseSteps()
        {
            testCaseSteps.Clear();
        }

        public void KickToUnitTest(string id)
        {
            if (id != null && id.Length > 0)
            {
                testCaseToUnitTest = true;
                linkedItems.Add(new ItemLink(id, ItemLinkType.UnitTest));
            }
        }

        static public List<TestStep> GetTestSteps(string testDescription)
        {
            string[] lines = testDescription.Split('\n');
            List<TestStep> output = new List<TestStep>();
            bool thenFound = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue; //skip empty lines
                }
                if (!line.ToUpper().Contains("THEN:") && !thenFound)
                {
                    output.Add(new TestStep((output.Count + 1).ToString(), line, string.Empty));
                }
                else
                {
                    if (!thenFound)
                    {
                        thenFound = true;
                        output[output.Count - 1].ExpectedResult = line;
                    }
                    else if (line.ToUpper().Contains("WHEN:"))
                    {
                        output.Add(new TestStep((output.Count + 1).ToString(), line, string.Empty));
                        thenFound = false;
                    }
                    else if (!line.ToUpper().Contains("AND:"))
                    {
                        output[output.Count - 1].ExpectedResult = output[output.Count - 1].ExpectedResult + '\n' + line;
                    }
                    else
                    {
                        output.Add(new TestStep((output.Count + 1).ToString(), string.Empty, line));
                    }
                }
            }
            return output;
        }
    }
}

