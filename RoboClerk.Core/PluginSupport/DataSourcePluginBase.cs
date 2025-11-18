using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace RoboClerk
{
    public abstract class DataSourcePluginBase : FileAccessPluginBase, IDataSourcePlugin
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
        protected List<TestResult> testResults = new List<TestResult>();
        //we also keep track of any items that have been eliminated
        protected List<EliminatedRequirementItem> eliminatedSystemRequirements = new List<EliminatedRequirementItem>();
        protected List<EliminatedRequirementItem> eliminatedSoftwareRequirements = new List<EliminatedRequirementItem>();
        protected List<EliminatedRequirementItem> eliminatedDocumentationRequirements = new List<EliminatedRequirementItem>();
        protected List<EliminatedSoftwareSystemTestItem> eliminatedSoftwareSystemTests = new List<EliminatedSoftwareSystemTestItem>();
        protected List<EliminatedRiskItem> eliminatedRisks = new List<EliminatedRiskItem>();
        protected List<EliminatedDocContentItem> eliminatedDocContents = new List<EliminatedDocContentItem>();
        protected List<EliminatedSOUPItem> eliminatedSOUP = new List<EliminatedSOUPItem>();
        protected List<EliminatedAnomalyItem> eliminatedAnomalies = new List<EliminatedAnomalyItem>();
        protected List<EliminatedTestResult> eliminatedTestResults = new List<EliminatedTestResult>();
        protected List<EliminatedUnitTestItem> eliminatedUnitTests = new List<EliminatedUnitTestItem>();

        public DataSourcePluginBase(IFileProviderPlugin fileSystem)
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

        public IEnumerable<TestResult> GetTestResults()
        {
            return testResults;
        }

        public IEnumerable<EliminatedRequirementItem> GetEliminatedSystemRequirements()
        {
            return eliminatedSystemRequirements;
        }

        public IEnumerable<EliminatedRequirementItem> GetEliminatedSoftwareRequirements()
        {
            return eliminatedSoftwareRequirements;
        }

        public IEnumerable<EliminatedRequirementItem> GetEliminatedDocumentationRequirements()
        {
            return eliminatedDocumentationRequirements;
        }

        public IEnumerable<EliminatedSoftwareSystemTestItem> GetEliminatedSoftwareSystemTests()
        {
            return eliminatedSoftwareSystemTests;
        }

        public IEnumerable<EliminatedRiskItem> GetEliminatedRisks()
        {
            return eliminatedRisks;
        }

        public IEnumerable<EliminatedDocContentItem> GetEliminatedDocContents()
        {
            return eliminatedDocContents;
        }

        public IEnumerable<EliminatedAnomalyItem> GetEliminatedAnomalies()
        {
            return eliminatedAnomalies;
        }

        public IEnumerable<EliminatedSOUPItem> GetEliminatedSOUP()
        {
            return eliminatedSOUP;
        }

        public IEnumerable<EliminatedTestResult> GetEliminatedTestResults()
        {
            return eliminatedTestResults;
        }

        public IEnumerable<EliminatedUnitTestItem> GetEliminatedUnitTests()
        {
            return eliminatedUnitTests;
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
            testResults.Clear();
            eliminatedSystemRequirements.Clear();
            eliminatedSoftwareRequirements.Clear();
            eliminatedDocumentationRequirements.Clear();
            eliminatedSoftwareSystemTests.Clear();
            eliminatedSOUP.Clear();
            eliminatedRisks.Clear();
            eliminatedDocContents.Clear();
            eliminatedAnomalies.Clear();
            eliminatedTestResults.Clear();
            eliminatedUnitTests.Clear();
        }

        public void EliminateItem(string itemID, string reason, EliminationReason eliminationReason)
        {
            // Check system requirements
            var systemReq = systemRequirements.FirstOrDefault(item => item.ItemID == itemID);
            if (systemReq != null)
            {
                systemRequirements.Remove(systemReq);
                eliminatedSystemRequirements.Add(new EliminatedRequirementItem(systemReq, reason, eliminationReason));
                return;
            }

            // Check software requirements
            var softwareReq = softwareRequirements.FirstOrDefault(item => item.ItemID == itemID);
            if (softwareReq != null)
            {
                softwareRequirements.Remove(softwareReq);
                eliminatedSoftwareRequirements.Add(new EliminatedRequirementItem(softwareReq, reason, eliminationReason));
                return;
            }

            // Check documentation requirements
            var docReq = documentationRequirements.FirstOrDefault(item => item.ItemID == itemID);
            if (docReq != null)
            {
                documentationRequirements.Remove(docReq);
                eliminatedDocumentationRequirements.Add(new EliminatedRequirementItem(docReq, reason, eliminationReason));
                return;
            }

            // Check software system tests
            var testCase = testCases.FirstOrDefault(item => item.ItemID == itemID);
            if (testCase != null)
            {
                testCases.Remove(testCase);
                eliminatedSoftwareSystemTests.Add(new EliminatedSoftwareSystemTestItem(testCase, reason, eliminationReason));
                return;
            }

            // Check unit tests
            var unitTest = unitTests.FirstOrDefault(item => item.ItemID == itemID);
            if (unitTest != null)
            {
                unitTests.Remove(unitTest);
                eliminatedUnitTests.Add(new EliminatedUnitTestItem(unitTest, reason, eliminationReason));
                return;
            }

            // Check risks
            var risk = risks.FirstOrDefault(item => item.ItemID == itemID);
            if (risk != null)
            {
                risks.Remove(risk);
                eliminatedRisks.Add(new EliminatedRiskItem(risk, reason, eliminationReason));
                return;
            }

            // Check anomalies
            var anomaly = anomalies.FirstOrDefault(item => item.ItemID == itemID);
            if (anomaly != null)
            {
                anomalies.Remove(anomaly);
                eliminatedAnomalies.Add(new EliminatedAnomalyItem(anomaly, reason, eliminationReason));
                return;
            }

            // Check SOUP items
            var soupItem = soup.FirstOrDefault(item => item.ItemID == itemID);
            if (soupItem != null)
            {
                soup.Remove(soupItem);
                eliminatedSOUP.Add(new EliminatedSOUPItem(soupItem, reason, eliminationReason));
                return;
            }

            // Check doc content items
            var docContent = docContents.FirstOrDefault(item => item.ItemID == itemID);
            if (docContent != null)
            {
                docContents.Remove(docContent);
                eliminatedDocContents.Add(new EliminatedDocContentItem(docContent, reason, eliminationReason));
                return;
            }

            // Check test results
            var testResult = testResults.FirstOrDefault(item => item.ItemID == itemID);
            if (testResult != null)
            {
                testResults.Remove(testResult);
                eliminatedTestResults.Add(new EliminatedTestResult(testResult, reason, eliminationReason));
                return;
            }

            // If we reach here, the item was not found
            throw new ArgumentException($"Item with ID '{itemID}' not found in any item collection for plugin '{Name}'.");
        }

        protected string EscapeNonTablePipes(string text)
        {
            string tableBlockPattern = @"(?ms)(^\|===\s*$.*?^\|===\s*$)";

            // Use Regex.Split with a capturing group so that the table blocks are kept in the result.
            string[] segments = Regex.Split(text, tableBlockPattern);
            var sb = new StringBuilder();

            foreach (string segment in segments)
            {
                // If the segment itself matches our table block pattern, leave it unmodified.
                if (Regex.IsMatch(segment, @"(?ms)^\|===\s*$.*?^\|===\s*$"))
                {
                    sb.Append(segment);
                }
                else
                {
                    // Otherwise, escape any unescaped "|" in this segment.
                    string processed = Regex.Replace(segment, @"(?<!\\)\|", @"\|");
                    sb.Append(processed);
                }
            }
            return sb.ToString();
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
                        string? currentValue = property.GetValue(obj) as string;
                        if (currentValue != null)
                        {
                            // Escape pipes in non-table parts of the content.
                            string newValue = EscapeNonTablePipes(currentValue);
                            property.SetValue(obj, newValue);
                        }
                    }
                }
            }
        }

        // with the exception of docContent items, all other items are visualized
        // in tables and they need to escape the | character
        protected void ScrubItemContents()
        {
            ScrubItemsFields(systemRequirements);
            ScrubItemsFields(softwareRequirements);
            ScrubItemsFields(documentationRequirements);
            ScrubItemsFields(testCases);
            foreach (var testCase in testCases)
            {
                ScrubItemsFields(testCase.TestCaseSteps);
            }
            ScrubItemsFields(anomalies);
            ScrubItemsFields(risks);
            ScrubItemsFields(soup);
            ScrubItemsFields(unitTests);
            ScrubItemsFields(dependencies);
            ScrubItemsFields(testResults);
        }
    }
}
