using System.Collections.Generic;
using System.IO;

namespace RoboClerk
{
    public interface IDataSources
    {
        List<LinkedItem> GetItems(TraceEntity te);
        List<AnomalyItem> GetAllAnomalies();
        List<RequirementItem> GetAllSoftwareRequirements();
        List<UnitTestItem> GetAllSoftwareUnitTests();
        List<TestCaseItem> GetAllSoftwareSystemTests();
        List<RequirementItem> GetAllSystemRequirements();
        List<ExternalDependency> GetAllExternalDependencies();
        List<RequirementItem> GetAllDocumentationRequirements();
        List<RiskItem> GetAllRisks();
        List<SOUPItem> GetAllSOUP();
        List<DocContentItem> GetAllDocContents();
        AnomalyItem GetAnomaly(string id);
        string GetConfigValue(string key);
        Item GetItem(string id);
        RequirementItem GetSoftwareRequirement(string id);
        TestCaseItem GetSoftwareSystemTest(string id);
        UnitTestItem GetSoftwareUnitTest(string id);
        RequirementItem GetSystemRequirement(string id);
        RequirementItem GetDocumentationRequirement(string id);
        DocContentItem GetDocContent(string id);
        RiskItem GetRisk(string id);
        SOUPItem GetSOUP(string id);
        string GetTemplateFile(string fileName);
        Stream GetFileStreamFromTemplateDir(string fileName);
        string ToJSON();
    }
}
