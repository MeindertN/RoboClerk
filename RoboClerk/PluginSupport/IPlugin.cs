using RoboClerk.Configuration;
using System.Collections.Generic;

namespace RoboClerk
{
    public interface IPlugin
    {
        string Name { get; }
        string Description { get; }
        void Initialize(IConfiguration config);
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
    }
}
