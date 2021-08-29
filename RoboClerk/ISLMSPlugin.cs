using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk
{
    public interface ISLMSPlugin
    {
        string Name { get; }
        string Description { get; }
        void Initialize(string organizationName, string projectName, string accessToken);
        void RefreshItems();
        IEnumerable<Item> GetProductRequirements();
        List<TraceItem> GetSoftwareRequirements();
        List<Item> GetBugs();
        List<TraceItem> GetTestCases();
    }
}
