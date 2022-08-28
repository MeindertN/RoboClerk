using RoboClerk.Configuration;
using System.Collections.Generic;
using System.IO.Abstractions;

namespace RoboClerk
{
    public class PluginDataSources : DataSourcesBase,IDataSources
    {
        private List<ISLMSPlugin> slmsPlugins = new List<ISLMSPlugin>();
        private List<IDependencyManagementPlugin> dependencyManagementPlugins = new List<IDependencyManagementPlugin>();
        private List<ISourceCodeAnalysisPlugin> sourceCodeAnalysisPlugins = new List<ISourceCodeAnalysisPlugin>();

        private readonly IConfiguration configuration = null;
        private readonly IPluginLoader pluginLoader = null;

        public PluginDataSources(IConfiguration configuration, IPluginLoader pluginLoader, IFileSystem fileSystem) 
            : base(configuration,fileSystem)
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

        public override List<SOUPItem> GetAllSOUP()
        {
            List<SOUPItem> list = new List<SOUPItem>();
            foreach (var plugin in slmsPlugins)
            {
                list.AddRange(plugin.GetSOUP());
            }
            return list;
        }

        public override List<RiskItem> GetAllRisks()
        {
            List<RiskItem> list = new List<RiskItem>();
            foreach (var plugin in slmsPlugins)
            {
                list.AddRange(plugin.GetRisks());
            }
            return list;
        }

        public override List<ExternalDependency> GetAllExternalDependencies()
        {
            var dependencies = new List<ExternalDependency>();
            foreach (var plugin in dependencyManagementPlugins)
            {
                dependencies.AddRange(plugin.GetDependencies());
            }
            return dependencies;
        }

        public override List<UnitTestItem> GetAllSoftwareUnitTests()
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

        public override List<RequirementItem> GetAllSoftwareRequirements()
        {
            List<RequirementItem> items = new List<RequirementItem>();
            foreach (var plugin in slmsPlugins)
            {
                items.AddRange(plugin.GetSoftwareRequirements());
            }
            return items;
        }

        public override List<RequirementItem> GetAllSystemRequirements()
        {
            List<RequirementItem> items = new List<RequirementItem>();
            foreach (var plugin in slmsPlugins)
            {
                items.AddRange(plugin.GetSystemRequirements());
            }
            return items;
        }

        public override List<TestCaseItem> GetAllSoftwareSystemTests()
        {
            List<TestCaseItem> items = new List<TestCaseItem>();
            foreach (var plugin in slmsPlugins)
            {
                items.AddRange(plugin.GetSoftwareSystemTests());
            }
            return items;
        }

        public override List<AnomalyItem> GetAllAnomalies()
        {
            List<AnomalyItem> items = new List<AnomalyItem>();
            foreach (var plugin in slmsPlugins)
            {
                items.AddRange(plugin.GetAnomalies());
            }
            return items;
        }
    }
}
