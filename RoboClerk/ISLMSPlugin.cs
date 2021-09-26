using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk
{
    public interface ISLMSPlugin : IPlugin
    {
        void RefreshItems();
        List<RequirementItem> GetProductRequirements();
        List<RequirementItem> GetSoftwareRequirements();
        List<Item> GetBugs();
        List<TestCaseItem> GetTestCases();
    }
}
