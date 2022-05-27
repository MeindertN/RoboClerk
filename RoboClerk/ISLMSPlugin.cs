using System.Collections.Generic;

namespace RoboClerk
{
    public interface ISLMSPlugin : IPlugin
    {
        void RefreshItems();
        List<RequirementItem> GetSystemRequirements();
        List<RequirementItem> GetSoftwareRequirements();
        List<AnomalyItem> GetAnomalies();
        List<TestCaseItem> GetSoftwareSystemTests();
        List<RiskItem> GetRisks();
        List<SOUPItem> GetSOUP();
    }
}
