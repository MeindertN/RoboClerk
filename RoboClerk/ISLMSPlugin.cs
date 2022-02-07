using System.Collections.Generic;

namespace RoboClerk
{
    public interface ISLMSPlugin : IPlugin
    {
        void RefreshItems();
        List<RequirementItem> GetSystemRequirements();
        List<RequirementItem> GetSoftwareRequirements();
        List<AnomalyItem> GetBugs();
        List<TestCaseItem> GetSoftwareSystemTests();
    }
}
