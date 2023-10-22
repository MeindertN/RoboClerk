using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace RoboClerk
{
    public abstract class DataSourcePluginBase : PluginBase, IDataSourcePlugin
    {
        protected List<RequirementItem> systemRequirements = new List<RequirementItem>();
        protected List<RequirementItem> softwareRequirements = new List<RequirementItem>();
        protected List<RequirementItem> documentationRequirements = new List<RequirementItem>();
        protected List<SoftwareSystemTestItem> testCases = new List<SoftwareSystemTestItem>();
        protected List<AnomalyItem> anomalies = new List<AnomalyItem>();
        protected List<RiskItem> risks = new List<RiskItem>();
        protected List<SOUPItem> soup = new List<SOUPItem>();
        protected List<DocContentItem> docContents = new List<DocContentItem>();
        protected List<UnitTestItem> unitTests = new List<UnitTestItem>();
        protected List<ExternalDependency> dependencies = new List<ExternalDependency>();

        public DataSourcePluginBase(IFileSystem fileSystem)
            :base(fileSystem)
        {

        }

        public abstract void RefreshItems();

        public IEnumerable<AnomalyItem> GetAnomalies()
        {
            return anomalies;
        }

        public IEnumerable<RequirementItem> GetSystemRequirements()
        {
            return systemRequirements;
        }

        public IEnumerable<RequirementItem> GetSoftwareRequirements()
        {
            return softwareRequirements;
        }

        public IEnumerable<RequirementItem> GetDocumentationRequirements()
        {
            return documentationRequirements;
        }

        public IEnumerable<DocContentItem> GetDocContents()
        {
            return docContents;
        }

        public IEnumerable<SoftwareSystemTestItem> GetSoftwareSystemTests()
        {
            return testCases;
        }

        public IEnumerable<RiskItem> GetRisks()
        {
            return risks;
        }

        public IEnumerable<SOUPItem> GetSOUP()
        {
            return soup;
        }

        public IEnumerable<UnitTestItem> GetUnitTests()
        {
            return unitTests;
        }

        public IEnumerable<ExternalDependency> GetDependencies()
        {
            return dependencies;
        }

        protected void ClearAllItems()
        {
            systemRequirements.Clear();
            softwareRequirements.Clear();
            documentationRequirements.Clear();
            testCases.Clear();
            anomalies.Clear();
            risks.Clear();
            soup.Clear();
            docContents.Clear();
            unitTests.Clear();
            dependencies.Clear();
        }

        private void ScrubItemsFields<T>(IEnumerable<T> items)
        {
            foreach (var obj in items)
            {
                Type type = obj.GetType();
                PropertyInfo[] properties = type.GetProperties();

                foreach (PropertyInfo property in properties)
                {
                    if (property.PropertyType == typeof(string) && property.CanWrite)
                    {
                        // asciidoc uses | to seperate fields in a table, if the fields
                        // themselves contain a | character it needs to be escaped.
                        string currentValue = (string)property.GetValue(obj);
                        string newValue = Regex.Replace(currentValue, "(?<!\\\\)\\|", "\\|");
                        property.SetValue(obj, newValue);
                    }
                }
            }
        }

        // with the exception of docContent items, all other items are visualized
        // in tables and they need to escape the | character
        protected void ScrubItemContents()
        {
            ScrubItemsFields<RequirementItem>(systemRequirements);
            ScrubItemsFields<RequirementItem>(softwareRequirements);
            ScrubItemsFields<RequirementItem>(documentationRequirements);
            ScrubItemsFields<SoftwareSystemTestItem>(testCases);
            foreach (var testCase in testCases)
            {
                ScrubItemsFields<TestStep>(testCase.TestCaseSteps);
            }
            ScrubItemsFields<AnomalyItem>(anomalies);
            ScrubItemsFields<RiskItem>(risks);
            ScrubItemsFields<SOUPItem>(soup);
            ScrubItemsFields<UnitTestItem>(unitTests);
            ScrubItemsFields<ExternalDependency>(dependencies);
        }
    }
}
