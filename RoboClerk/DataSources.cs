using RoboClerk.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RoboClerk
{
    public class DataSources : IDataSources
    {
        
        private List<ISLMSPlugin> slmsPlugins = new List<ISLMSPlugin>();
        private List<IDependencyManagementPlugin> dependencyManagementPlugins = new List<IDependencyManagementPlugin>();
        private List<ISourceCodeAnalysisPlugin> sourceCodeAnalysisPlugins = new List<ISourceCodeAnalysisPlugin>();

        private readonly IConfiguration configuration = null;
        private readonly IPluginLoader pluginLoader = null;

        public DataSources(IConfiguration configuration, IPluginLoader pluginLoader)
        {
            this.pluginLoader = pluginLoader;  
            this.configuration = configuration;

            LoadPlugins();
        }

        private void LoadPlugins()
        {
            foreach (var val in configuration.DataSourcePlugins)
            {
                foreach (var dir in configuration.PluginDirs)
                {
                    var plugin = pluginLoader.LoadPlugin<IPlugin>((string)val, dir);
                    if (plugin != null)
                    {
                        plugin.Initialize(configuration);
                        if (plugin as ISLMSPlugin != null)
                        {
                            var temp = plugin as ISLMSPlugin;
                            temp.RefreshItems();
                            slmsPlugins.Add(temp);
                            continue;
                        }
                        if (plugin as IDependencyManagementPlugin != null)
                        {
                            var temp = plugin as IDependencyManagementPlugin;
                            temp.RefreshItems();
                            dependencyManagementPlugins.Add(temp);
                            continue;
                        }
                        if (plugin as ISourceCodeAnalysisPlugin != null)
                        {
                            var temp = plugin as ISourceCodeAnalysisPlugin;
                            temp.RefreshItems();
                            sourceCodeAnalysisPlugins.Add(temp);
                            continue;
                        }
                    }
                }
            }
        }

        public List<LinkedItem> GetItems(TraceEntity te)
        {
            if(te.ID == "SystemRequirement")
            {
                return GetAllSystemRequirements().Cast<LinkedItem>().ToList();
            }
            else if(te.ID == "SoftwareRequirement")
            {
                return GetAllSoftwareRequirements().Cast<LinkedItem>().ToList(); 
            }
            else if(te.ID == "SoftwareSystemTest")
            {
                return GetAllSoftwareUnitTests().Cast<LinkedItem>().ToList();   
            }
            else if(te.ID == "SoftwareUnitTest")
            {
                return GetAllSoftwareUnitTests().Cast<LinkedItem>().ToList();
            }
            else if(te.ID == "Risk")
            {
                return GetAllRisks().Cast<LinkedItem>().ToList();
            }
            else if(te.ID == "SOUP")
            {
                return GetAllSOUP().Cast<LinkedItem>().ToList();
            }
            else
            {
                throw new Exception($"No datasource available for unknown trace entity: {te.ID}");
            }
        }

        public List<SOUPItem> GetAllSOUP()
        {
            List<SOUPItem> list = new List<SOUPItem>();
            foreach (var plugin in slmsPlugins)
            {
                list.AddRange(plugin.GetSOUP());
            }
            return list;
        }

        public SOUPItem GetSOUP(string id)
        {
            List<SOUPItem> list = GetAllSOUP();
            return list.Find(f => (f.ItemID == id));
        }

        public List<RiskItem> GetAllRisks()
        {
            List<RiskItem> list = new List<RiskItem>();
            foreach (var plugin in slmsPlugins)
            {
                list.AddRange(plugin.GetRisks());
            }
            return list;
        }

        public RiskItem GetRisk(string id)
        {
            List<RiskItem> list = GetAllRisks();
            return list.Find(f => (f.ItemID == id));
        }

        public List<ExternalDependency> GetAllExternalDependencies()
        {
            var dependencies = new List<ExternalDependency>();
            foreach (var plugin in dependencyManagementPlugins)
            {
                dependencies.AddRange(plugin.GetDependencies());
            }
            return dependencies;
        }

        public List<UnitTestItem> GetAllSoftwareUnitTests()
        {
            var unitTests = new List<UnitTestItem>();
            foreach(var plugin in slmsPlugins)
            {
                unitTests.AddRange(plugin.GetUnitTests());
            }
            foreach(var plugin in sourceCodeAnalysisPlugins)
            {
                unitTests.AddRange(plugin.GetUnitTests());
            }
            return unitTests;
        }

        public UnitTestItem GetSoftwareUnitTest(string id)
        {
            var items = GetAllSoftwareUnitTests();
            return items.Find(f => (f.ItemID == id));
        }

        public List<RequirementItem> GetAllSoftwareRequirements()
        {
            List<RequirementItem> items = new List<RequirementItem>();
            foreach (var plugin in slmsPlugins)
            {
                items.AddRange(plugin.GetSoftwareRequirements());
            }
            return items;
        }

        public RequirementItem GetSoftwareRequirement(string id)
        {
            var reqs = GetAllSoftwareRequirements();
            return reqs.Find(f => (f.ItemID == id));
        }

        public List<RequirementItem> GetAllSystemRequirements()
        {
            List<RequirementItem> items = new List<RequirementItem>();
            foreach (var plugin in slmsPlugins)
            {
                items.AddRange(plugin.GetSystemRequirements());
            }
            return items;
        }

        public RequirementItem GetSystemRequirement(string id)
        {
            var reqs = GetAllSystemRequirements();
            return reqs.Find(f => (f.ItemID == id));
        }

        public List<TestCaseItem> GetAllSoftwareSystemTests()
        {
            List<TestCaseItem> items = new List<TestCaseItem>();
            foreach (var plugin in slmsPlugins)
            {
                items.AddRange(plugin.GetSoftwareSystemTests());
            }
            return items;
        }

        public TestCaseItem GetSoftwareSystemTest(string id)
        {
            var items = GetAllSoftwareSystemTests();
            return items.Find(f => (f.ItemID == id));
        }

        public List<AnomalyItem> GetAllAnomalies()
        {
            List<AnomalyItem> items = new List<AnomalyItem>();
            foreach (var plugin in slmsPlugins)
            {
                items.AddRange(plugin.GetAnomalies());
            }
            return items;
        }

        public AnomalyItem GetAnomaly(string id)
        {
            var items = GetAllAnomalies();
            return items.Find(f => (f.ItemID == id));
        }

        public Item GetItem(string id)
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
            var tcase = GetAllSoftwareUnitTests();
            if ((idx = tcase.FindIndex(o => o.ItemID == id)) >= 0)
            {
                return tcase[idx];
            }
            var anomalies = GetAllAnomalies();
            if ((idx = anomalies.FindIndex(o => o.ItemID == id)) >= 0)
            {
                return anomalies[idx];
            }
            return null;
        }

        public string GetConfigValue(string key)
        {
            return configuration.ConfigVals.GetValue(key);
        }

        public string GetTemplateFile(string fileName)
        {
            return File.ReadAllText(Path.Join(configuration.TemplateDir,fileName));
        }

        public Stream GetFileStreamFromTemplateDir(string fileName)
        {
            var stream = new FileStream(Path.Join(configuration.TemplateDir, fileName),FileMode.Open);
            return stream;
        }
    }
}
