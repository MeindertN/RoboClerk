using RoboClerk.Configuration;
using System;
using System.Collections.Generic;
using Tomlyn.Model;

namespace RoboClerk
{
    public abstract class SLMSPluginBase : ISLMSPlugin
    {
        protected string name = string.Empty;
        protected string description = string.Empty;
        protected static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        protected List<RequirementItem> systemRequirements = new List<RequirementItem>();
        protected List<RequirementItem> softwareRequirements = new List<RequirementItem>();
        protected List<RequirementItem> documentationRequirements = new List<RequirementItem>();
        protected List<TestCaseItem> testCases = new List<TestCaseItem>();
        protected List<AnomalyItem> bugs = new List<AnomalyItem>();
        protected List<RiskItem> risks = new List<RiskItem>();
        protected List<SOUPItem> soup = new List<SOUPItem>();
        protected List<DocContentItem> docContents = new List<DocContentItem>(); 

        public string Name
        {
            get => name;
        }

        public string Description
        {
            get => description;
        }


        public List<AnomalyItem> GetAnomalies()
        {
            return bugs;
        }

        public List<RequirementItem> GetSystemRequirements()
        {
            return systemRequirements;
        }

        public List<RequirementItem> GetSoftwareRequirements()
        {
            return softwareRequirements;
        }

        public List<RequirementItem> GetDocumentationRequirements()
        {
            return documentationRequirements;
        }

        public List<DocContentItem> GetDocContents()
        {
            return docContents;
        }

        public List<TestCaseItem> GetSoftwareSystemTests()
        {
            return testCases;
        }

        public List<RiskItem> GetRisks()
        {
            return risks;
        }

        public List<SOUPItem> GetSOUP()
        {
            return soup;
        }

        public List<UnitTestItem> GetUnitTests()
        {
            return new List<UnitTestItem>();
        }

        protected string GetStringForKey(TomlTable config, string keyName, bool required)
        {
            string result = string.Empty;
            if (config.ContainsKey(keyName))
            {
                return (string)config[keyName];
            }
            else
            {
                if (required)
                {
                    throw new Exception($"Required key \"{keyName}\" missing from configuration file for {name}. Cannot continue.");
                }
                else
                {
                    logger.Warn($"Key \\\"{keyName}\\\" missing from configuration file for {name}. Attempting to continue.");
                    return string.Empty;
                }
            }
        }

        public abstract void RefreshItems();
        public abstract void Initialize(IConfiguration config);
    }
}
