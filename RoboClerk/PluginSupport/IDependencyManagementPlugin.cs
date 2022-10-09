using System.Collections.Generic;

namespace RoboClerk
{
    public interface IDependencyManagementPlugin : IPlugin
    {
        void RefreshItems();
        List<ExternalDependency> GetDependencies();
    }
}
