using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Tomlyn;
using Tomlyn.Model;

namespace RoboClerk
{
    public class DataSources
    {
        private ConfigurationValues configVals = null;
        private List<ISLMSPlugin> slmsPlugins = new List<ISLMSPlugin>();

        public DataSources(string configFile, string projectConfigFile)
        {
            configVals = new ConfigurationValues();
            configVals.FromToml(projectConfigFile);
            Configure(configFile);
        }

        private void Configure(string config)
        {
            var toml = Toml.Parse(config).ToModel();

            foreach (var val in (TomlArray)toml["DataSourcePlugin"])
            {
                foreach (var dir in (TomlArray)toml["RelativePluginDirs"])
                {
                    string path = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/{dir}";
                    path = Path.GetFullPath(path);
                    var plugin = PluginLoader.LoadPlugin<ISLMSPlugin>((string)val,path);
                    if (plugin != null)
                    {
                        plugin.Initialize();
                        plugin.RefreshItems();
                        slmsPlugins.Add(plugin);
                    }
                }
            }
        }

        public List<RequirementItem> GetAllSoftwareRequirements()
        {
            List<RequirementItem> items = new List<RequirementItem>();
            foreach(var plugin in slmsPlugins)
            {
                items.AddRange(plugin.GetSoftwareRequirements());
            }
            return items;
        }

        public RequirementItem GetSoftwareRequirement(string id)
        {
            var reqs = GetAllSoftwareRequirements();
            return reqs.Find(f => (f.RequirementID == id));
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
            return reqs.Find(f => (f.RequirementID == id));
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
            return items.Find(f => (f.TestCaseID == id));
        }

        public List<BugItem> GetAllBugs()
        {
            List<BugItem> items = new List<BugItem>();
            foreach(var plugin in slmsPlugins)
            {
                items.AddRange(plugin.GetBugs());
            }
            return items;
        }

        public BugItem GetBug(string id)
        {
            var items = GetAllBugs();
            return items.Find(f => (f.BugID == id));
        }

        public Item GetItem(string id)
        {
            var sreq = GetAllSoftwareRequirements();
            int idx = -1;
            if( (idx = sreq.FindIndex(o => o.RequirementID == id)) >= 0)
            {
                return sreq[idx];
            }
            sreq = GetAllSystemRequirements();
            if ((idx = sreq.FindIndex(o => o.RequirementID == id)) >= 0)
            {
                return sreq[idx];
            }
            var tcase = GetAllSystemLevelTests();
            if ((idx = tcase.FindIndex(o => o.TestCaseID == id)) >= 0)
            {
                return tcase[idx];
            }
            var bugs = GetAllBugs();
            if ((idx = bugs.FindIndex(o => o.BugID == id)) >= 0)
            {
                return bugs[idx];
            }
            return null;
        }

        public string GetConfigValue(string key)
        {
            return configVals.GetValue(key);
        }
    }
}
