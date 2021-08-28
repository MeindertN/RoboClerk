using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk.AzureDevOps
{
    class AzureDevOpsSLMSPlugin : ISLMSPlugin
    {
        private string name;
        private string description;
        private string organizationName;
        private string projectName;
        private string accessToken;

        public AzureDevOpsSLMSPlugin()
        {
            name = "AzureDevOpsSLMSPlugin";
            description = "A plugin that interfaces with azure devops to retrieve information needed by RoboClerk to create documentation.";
        }

        public string Name => name;

        public string Description => description;

        public List<Item> GetBugs()
        {
            throw new NotImplementedException();
        }

        public List<TraceItem> GetProductRequirements()
        {
            throw new NotImplementedException();
        }

        public List<TraceItem> GetSoftwareRequirements()
        {
            throw new NotImplementedException();
        }

        public List<Item> GetTestPlans()
        {
            throw new NotImplementedException();
        }

        public void Initialize(string orgName, string proName, string access)
        {
            organizationName = orgName;
            projectName = proName;
            accessToken = access;


        }
    }
}
