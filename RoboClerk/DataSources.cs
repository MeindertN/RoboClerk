﻿using RoboClerk.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RoboClerk
{
    public class DataSources : IDataSources
    {
        
        private List<ISLMSPlugin> slmsPlugins = new List<ISLMSPlugin>();
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
                    var plugin = pluginLoader.LoadPlugin<ISLMSPlugin>((string)val, dir);
                    if (plugin != null)
                    {
                        plugin.Initialize(configuration);
                        plugin.RefreshItems();
                        slmsPlugins.Add(plugin);
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
                return GetAllSystemLevelTests().Cast<LinkedItem>().ToList();   
            }
            else if(te.ID == "SoftwareUnitTest")
            {
                return GetAllUnitLevelTests().Cast<LinkedItem>().ToList();
            }
            else if(te.ID == "Risk")
            {
                return GetAllRisks().Cast<LinkedItem>().ToList();
            }
            else
            {
                throw new Exception($"No datasource available for unknown trace entity: {te.ID}");
            }
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


        public List<UnitTestItem> GetAllUnitLevelTests()
        {
            throw new NotImplementedException("Unit level tests not implemented yet"); ;
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

        public List<TestCaseItem> GetAllSystemLevelTests()
        {
            List<TestCaseItem> items = new List<TestCaseItem>();
            foreach (var plugin in slmsPlugins)
            {
                items.AddRange(plugin.GetSoftwareSystemTests());
            }
            return items;
        }

        public TestCaseItem GetSystemLevelTest(string id)
        {
            var items = GetAllSystemLevelTests();
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
            var tcase = GetAllSystemLevelTests();
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
