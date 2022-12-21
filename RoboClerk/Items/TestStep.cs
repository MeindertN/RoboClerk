


namespace RoboClerk
{
    public class TestStep
    {
        private string step = string.Empty;
        private string action = string.Empty;
        private string expectedResult = string.Empty;
        
        public TestStep(string step, string action, string expectedResult) 
        {
            this.step = step;
            this.action = action;
            this.expectedResult = expectedResult;
        }

        public string Step { get; set; }

        public string Action { get; set; }
        
        public string ExpectedResult { get; set; }
    }
}
