using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboClerk
{
    public interface IDataSources
    {
        List<LinkedItem> GetItems(TraceEntity te);
        List<AnomalyItem> GetAllAnomalies();
        List<RequirementItem> GetAllSoftwareRequirements();
        List<TestCaseItem> GetAllSystemLevelTests();
        List<RequirementItem> GetAllSystemRequirements();
        List<RiskItem> GetAllRisks();
        AnomalyItem GetAnomaly(string id);
        string GetConfigValue(string key);
        Item GetItem(string id);
        RequirementItem GetSoftwareRequirement(string id);
        TestCaseItem GetSystemLevelTest(string id);
        RequirementItem GetSystemRequirement(string id);
        RiskItem GetRisk(string id);
        string GetTemplateFile(string fileName);
    }
}
