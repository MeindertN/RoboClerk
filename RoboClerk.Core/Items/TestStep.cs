


namespace RoboClerk
{
    public class TestStep
    {
        public TestStep(string step, string action, string expectedResult)
        {
            Step = step;
            Action = action;
            ExpectedResult = expectedResult;
        }

        public string Step { get; set; }

        public string Action { get; set; }

        public string ExpectedResult { get; set; }
    }
}
