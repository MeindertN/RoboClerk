using System.Collections.Generic;
using System.IO;

namespace RoboClerk
{
    public interface IDataSources
    {
        void RefreshDataSources();
        List<LinkedItem> GetItems(TraceEntity te);
        List<AnomalyItem> GetAllAnomalies();
        List<EliminatedAnomalyItem> GetAllEliminatedAnomalies();
        List<RequirementItem> GetAllSoftwareRequirements();
        List<EliminatedRequirementItem> GetAllEliminatedSoftwareRequirements();
        List<UnitTestItem> GetAllUnitTests();
        List<SoftwareSystemTestItem> GetAllSoftwareSystemTests();
        List<EliminatedSoftwareSystemTestItem> GetAllEliminatedSoftwareSystemTests();
        List<RequirementItem> GetAllSystemRequirements();
        List<EliminatedRequirementItem> GetAllEliminatedSystemRequirements();
        List<ExternalDependency> GetAllExternalDependencies();
        List<TestResult> GetAllTestResults();
        List<RequirementItem> GetAllDocumentationRequirements();
        List<EliminatedRequirementItem> GetAllEliminatedDocumentationRequirements();
        List<RiskItem> GetAllRisks();
        List<EliminatedRiskItem> GetAllEliminatedRisks();
        List<SOUPItem> GetAllSOUP();
        List<EliminatedSOUPItem> GetAllEliminatedSOUP();
        List<DocContentItem> GetAllDocContents();
        List<EliminatedDocContentItem> GetAllEliminatedDocContents();
        AnomalyItem GetAnomaly(string id);
        string GetConfigValue(string key);
        Item? GetItem(string id);
        RequirementItem GetSoftwareRequirement(string id);
        EliminatedRequirementItem GetEliminatedSoftwareRequirement(string id);
        SoftwareSystemTestItem GetSoftwareSystemTest(string id);
        UnitTestItem GetUnitTest(string id);
        RequirementItem GetSystemRequirement(string id);
        EliminatedRequirementItem GetEliminatedSystemRequirement(string id);
        RequirementItem GetDocumentationRequirement(string id);
        EliminatedRequirementItem GetEliminatedDocumentationRequirement(string id);
        DocContentItem GetDocContent(string id);
        EliminatedDocContentItem GetEliminatedDocContent(string id);
        RiskItem GetRisk(string id);
        EliminatedRiskItem GetEliminatedRisk(string id);
        SOUPItem GetSOUP(string id);
        EliminatedSOUPItem GetEliminatedSOUP(string id);
        string GetTemplateFile(string fileName);
        Stream GetFileStreamFromTemplateDir(string fileName);
        string ToJSON();
    }
}
