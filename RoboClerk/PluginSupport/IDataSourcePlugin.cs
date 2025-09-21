using System.Collections.Generic;

namespace RoboClerk
{
    public interface IDataSourcePlugin : IPlugin
    {
        void RefreshItems();
        IEnumerable<RequirementItem> GetSystemRequirements();
        IEnumerable<RequirementItem> GetSoftwareRequirements();
        IEnumerable<RequirementItem> GetDocumentationRequirements();
        IEnumerable<DocContentItem> GetDocContents();
        IEnumerable<AnomalyItem> GetAnomalies();
        IEnumerable<SoftwareSystemTestItem> GetSoftwareSystemTests();
        IEnumerable<RiskItem> GetRisks();
        IEnumerable<SOUPItem> GetSOUP();
        IEnumerable<UnitTestItem> GetUnitTests();
        IEnumerable<ExternalDependency> GetDependencies();
        IEnumerable<TestResult> GetTestResults();

        // Add methods for eliminated items
        IEnumerable<EliminatedRequirementItem> GetEliminatedSystemRequirements();
        IEnumerable<EliminatedRequirementItem> GetEliminatedSoftwareRequirements();
        IEnumerable<EliminatedRequirementItem> GetEliminatedDocumentationRequirements();
        IEnumerable<EliminatedSoftwareSystemTestItem > GetEliminatedSoftwareSystemTests();
        IEnumerable<EliminatedRiskItem> GetEliminatedRisks();
        IEnumerable<EliminatedSOUPItem> GetEliminatedSOUP();
        IEnumerable<EliminatedAnomalyItem > GetEliminatedAnomalies();
        IEnumerable<EliminatedDocContentItem> GetEliminatedDocContents();
    }
}
