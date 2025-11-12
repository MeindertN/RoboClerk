using RoboClerk.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.Json;

namespace RoboClerk
{
    public abstract class DataSourcesBase : IDataSources
    {
        protected readonly IConfiguration configuration = null;
        protected readonly IFileSystem fileSystem = null;
        protected static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public DataSourcesBase(IConfiguration configuration, IFileSystem fileSystem)
        {
            this.configuration = configuration;
            this.fileSystem = fileSystem;
        }

        public abstract List<SOUPItem> GetAllSOUP();

        public abstract List<EliminatedSOUPItem> GetAllEliminatedSOUP();

        public abstract List<RiskItem> GetAllRisks();

        public abstract List<EliminatedRiskItem> GetAllEliminatedRisks();

        public abstract List<ExternalDependency> GetAllExternalDependencies();

        public abstract List<TestResult> GetAllTestResults();

        public abstract List<EliminatedTestResult> GetAllEliminatedTestResults();

        public abstract List<UnitTestItem> GetAllUnitTests();

        public abstract List<EliminatedUnitTestItem> GetAllEliminatedUnitTests();

        public abstract List<RequirementItem> GetAllSoftwareRequirements();

        public abstract List<EliminatedRequirementItem> GetAllEliminatedSoftwareRequirements();

        public abstract List<RequirementItem> GetAllSystemRequirements();

        public abstract List<EliminatedRequirementItem> GetAllEliminatedSystemRequirements();

        public abstract List<RequirementItem> GetAllDocumentationRequirements();

        public abstract List<EliminatedRequirementItem> GetAllEliminatedDocumentationRequirements();

        public abstract List<DocContentItem> GetAllDocContents();

        public abstract List<EliminatedDocContentItem> GetAllEliminatedDocContents();

        public abstract List<SoftwareSystemTestItem> GetAllSoftwareSystemTests();

        public abstract List<EliminatedSoftwareSystemTestItem> GetAllEliminatedSoftwareSystemTests();

        public abstract List<AnomalyItem> GetAllAnomalies();

        public abstract List<EliminatedAnomalyItem> GetAllEliminatedAnomalies();

        public List<LinkedItem> GetItems(TraceEntity te)
        {
            if (te.ID == "SystemRequirement")
            {
                return GetAllSystemRequirements().Cast<LinkedItem>().ToList();
            }
            else if (te.ID == "SoftwareRequirement")
            {
                return GetAllSoftwareRequirements().Cast<LinkedItem>().ToList();
            }
            else if (te.ID == "SoftwareSystemTest")
            {
                return GetAllSoftwareSystemTests().Cast<LinkedItem>().ToList();
            }
            else if (te.ID == "UnitTest")
            {
                return GetAllUnitTests().Cast<LinkedItem>().ToList();
            }
            else if (te.ID == "TestResult")
            {
                return GetAllTestResults().Cast<LinkedItem>().ToList();
            }
            else if (te.ID == "Risk")
            {
                return GetAllRisks().Cast<LinkedItem>().ToList();
            }
            else if (te.ID == "SOUP")
            {
                return GetAllSOUP().Cast<LinkedItem>().ToList();
            }
            else if (te.ID == "Anomaly")
            {
                return GetAllAnomalies().Cast<LinkedItem>().ToList();
            }
            else if (te.ID == "DocumentationRequirement")
            {
                return GetAllDocumentationRequirements().Cast<LinkedItem>().ToList();
            }
            else if (te.ID == "DocContent")
            {
                return GetAllDocContents().Cast<LinkedItem>().ToList();
            }
            else if (te.ID == "Eliminated")
            {
                return  GetAllEliminatedRisks().Cast<LinkedItem>()
                        .Concat(GetAllEliminatedSystemRequirements())
                        .Concat(GetAllEliminatedSoftwareRequirements())
                        .Concat(GetAllEliminatedDocumentationRequirements())
                        .Concat(GetAllEliminatedSoftwareSystemTests())
                        .Concat(GetAllEliminatedAnomalies())
                        .Concat(GetAllEliminatedDocContents())
                        .Concat(GetAllEliminatedSOUP())
                        .Concat(GetAllEliminatedUnitTests())
                        .Concat(GetAllEliminatedTestResults())
                        .ToList();
            }
            else
            {
                throw new Exception($"No datasource available for unknown trace entity: {te.ID}");
            }
        }

        public SOUPItem GetSOUP(string id)
        {
            List<SOUPItem> list = GetAllSOUP();
            return list.Find(f => (f.ItemID == id));
        }

        public EliminatedSOUPItem GetEliminatedSOUP(string id)
        {
            List<EliminatedSOUPItem> list = GetAllEliminatedSOUP();
            return list.Find(f => (f.ItemID == id));
        }

        public RiskItem GetRisk(string id)
        {
            List<RiskItem> list = GetAllRisks();
            return list.Find(f => (f.ItemID == id));
        }

        public EliminatedRiskItem GetEliminatedRisk(string id)
        {
            List<EliminatedRiskItem> list = GetAllEliminatedRisks();
            return list.Find(f => (f.ItemID == id));
        }

        public UnitTestItem GetUnitTest(string id)
        {
            var items = GetAllUnitTests();
            return items.Find(f => (f.ItemID == id));
        }

        public EliminatedUnitTestItem GetEliminatedUnitTest(string id)
        {
            var items = GetAllEliminatedUnitTests();
            return items.Find(f => (f.ItemID == id));
        }

        public RequirementItem GetSoftwareRequirement(string id)
        {
            var reqs = GetAllSoftwareRequirements();
            return reqs.Find(f => (f.ItemID == id));
        }

        public EliminatedRequirementItem GetEliminatedSoftwareRequirement(string id)
        {
            var reqs = GetAllEliminatedSoftwareRequirements();
            return reqs.Find(f => (f.ItemID == id));
        }

        public RequirementItem GetSystemRequirement(string id)
        {
            var reqs = GetAllSystemRequirements();
            return reqs.Find(f => (f.ItemID == id));
        }

        public EliminatedRequirementItem GetEliminatedSystemRequirement(string id)
        {
            var reqs = GetAllEliminatedSystemRequirements();
            return reqs.Find(f => (f.ItemID == id));
        }

        public RequirementItem GetDocumentationRequirement(string id)
        {
            var reqs = GetAllDocumentationRequirements();
            return reqs.Find(f => (f.ItemID == id));
        }

        public EliminatedRequirementItem GetEliminatedDocumentationRequirement(string id)
        {
            var reqs = GetAllEliminatedDocumentationRequirements();
            return reqs.Find(f => (f.ItemID == id));
        }

        public DocContentItem GetDocContent(string id)
        {
            var items = GetAllDocContents();
            return items.Find(f => (f.ItemID == id));
        }

        public EliminatedDocContentItem GetEliminatedDocContent(string id)
        {
            var items = GetAllEliminatedDocContents();
            return items.Find(f => (f.ItemID == id));
        }

        public SoftwareSystemTestItem GetSoftwareSystemTest(string id)
        {
            var items = GetAllSoftwareSystemTests();
            return items.Find(f => (f.ItemID == id));
        }

        public EliminatedSoftwareSystemTestItem GetEliminatedSoftwareSystemTestItem(string id)
        {
            var items = GetAllEliminatedSoftwareSystemTests();
            return items.Find(f => (f.ItemID == id));
        }

        public AnomalyItem GetAnomaly(string id)
        {
            var items = GetAllAnomalies();
            return items.Find(f => (f.ItemID == id));
        }

        public EliminatedAnomalyItem GetEliminatedAnomaly(string id)
        {
            var items = GetAllEliminatedAnomalies();
            return items.Find(f => (f.ItemID == id));
        }

        public Item GetItem(string id) //this will not return eliminated items
        {
            var sreq = GetAllSoftwareRequirements();
            int idx = -1;
            if ((idx = sreq.FindIndex(o => o.ItemID == id)) >= 0)
            {
                return sreq[idx];
            }
            sreq = GetAllSystemRequirements();
            if ((idx = sreq.FindIndex(o => o.ItemID == id)) >= 0)
            {
                return sreq[idx];
            }
            sreq = GetAllDocumentationRequirements();
            if ((idx = sreq.FindIndex(o => o.ItemID == id)) >= 0)
            {
                return sreq[idx];
            }
            var dcont = GetAllDocContents();
            if ((idx = dcont.FindIndex(o => o.ItemID == id)) >= 0)
            {
                return dcont[idx];
            }
            var tcase = GetAllSoftwareSystemTests();
            if ((idx = tcase.FindIndex(o => o.ItemID == id)) >= 0)
            {
                return tcase[idx];
            }
            var utest = GetAllUnitTests();
            if ((idx = utest.FindIndex(o => o.ItemID == id)) >= 0)
            {
                return utest[idx];
            }
            var testResults = GetAllTestResults();
            if ((idx = testResults.FindIndex(o => o.ItemID == id)) >= 0)
            {
                return testResults[idx];
            }
            var anomalies = GetAllAnomalies();
            if ((idx = anomalies.FindIndex(o => o.ItemID == id)) >= 0)
            {
                return anomalies[idx];
            }
            var SOUPs = GetAllSOUP();
            if ((idx = SOUPs.FindIndex(o => o.ItemID == id)) >= 0)
            {
                return SOUPs[idx];
            }
            var Risks = GetAllRisks();
            if ((idx = Risks.FindIndex(o => o.ItemID == id)) >= 0)
            {
                return Risks[idx];
            }
            return null;
        }

        public string GetConfigValue(string key)
        {
            return configuration.ConfigVals.GetValue(key);
        }

        public string GetTemplateFile(string fileName)
        {
            return fileSystem.File.ReadAllText(fileSystem.Path.Join(configuration.TemplateDir, fileName));
        }

        public Stream GetFileStreamFromTemplateDir(string fileName)
        {
            var stream = fileSystem.FileStream.New(fileSystem.Path.Join(configuration.TemplateDir, fileName), FileMode.Open);
            return stream;
        }

        public string ToJSON()
        {
            CheckpointDataStorage storage = new CheckpointDataStorage();
            storage.SystemRequirements = GetAllSystemRequirements();
            storage.SoftwareRequirements = GetAllSoftwareRequirements();
            storage.DocumentationRequirements = GetAllDocumentationRequirements();
            storage.DocContents = GetAllDocContents();
            storage.Risks = GetAllRisks().ToList();
            storage.UnitTests = GetAllUnitTests();
            storage.SoftwareSystemTests = GetAllSoftwareSystemTests();
            storage.SOUPs = GetAllSOUP();
            storage.Anomalies = GetAllAnomalies();
            storage.EliminatedSystemRequirements = GetAllEliminatedSystemRequirements();
            storage.EliminatedSoftwareRequirements = GetAllEliminatedSoftwareRequirements();
            storage.EliminatedDocumentationRequirements = GetAllEliminatedDocumentationRequirements();
            storage.EliminatedSoftwareSystemTests = GetAllEliminatedSoftwareSystemTests();
            storage.EliminatedRisks = GetAllEliminatedRisks();

            var options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Serialize(storage, options);
        }
    }
}
