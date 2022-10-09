using System.Collections.Generic;

namespace RoboClerk
{
    public interface ISourceCodeAnalysisPlugin : IPlugin
    {
        List<UnitTestItem> GetUnitTests();
        void RefreshItems();
    }
}
