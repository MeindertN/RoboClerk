using System.Collections.Generic;

namespace RoboClerk
{
    public abstract class SLMSPluginBase : PluginBase, ISLMSPlugin
    {
        protected List<RequirementItem> systemRequirements = new List<RequirementItem>();
        protected List<RequirementItem> softwareRequirements = new List<RequirementItem>();
        protected List<RequirementItem> documentationRequirements = new List<RequirementItem>();
        protected List<TestCaseItem> testCases = new List<TestCaseItem>();
        protected List<AnomalyItem> bugs = new List<AnomalyItem>();
        protected List<RiskItem> risks = new List<RiskItem>();
        protected List<SOUPItem> soup = new List<SOUPItem>();
        protected List<DocContentItem> docContents = new List<DocContentItem>(); 

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

        public abstract void RefreshItems();
    }
}
