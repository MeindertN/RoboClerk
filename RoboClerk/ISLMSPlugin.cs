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
        List<TraceItem> GetProductRequirements();
        List<TraceItem> GetSoftwareRequirements();
        List<Item> GetBugs();
        List<Item> GetTestPlans();
    }
}
