using RoboClerk.Configuration;
using System.Collections.Generic;
using System.IO.Abstractions;

namespace RoboClerk
{
    public class PluginDataSources : DataSourcesBase, IDataSources
    {
        private List<IPlugin> plugins = new List<IPlugin>();

        private readonly IPluginLoader pluginLoader = null;

        public PluginDataSources(IConfiguration configuration, IPluginLoader pluginLoader, IFileSystem fileSystem)
            : base(configuration, fileSystem)
        {
            this.pluginLoader = pluginLoader;

            LoadPlugins();
        }

        private void LoadPlugins()
        {
            foreach (var val in configuration.DataSourcePlugins)
            {
                foreach (var dir in configuration.PluginDirs)
                {
                    var plugin = pluginLoader.LoadPlugin<IPlugin>(val, dir, fileSystem);
                    if (plugin != null)
                    {
                        plugin.Initialize(configuration);
                        if (plugin as IPlugin != null)
                        {
                            var temp = plugin as IPlugin;
                            temp.RefreshItems();
                            plugins.Add(temp);
                            continue;
                        }
                    }
                }
            }
        }

        public override List<SOUPItem> GetAllSOUP()
        {
            List<SOUPItem> list = new List<SOUPItem>();
            foreach (var plugin in plugins)
            {
                list.AddRange(plugin.GetSOUP());
            }
            return list;
        }

        public override List<RiskItem> GetAllRisks()
        {
            List<RiskItem> list = new List<RiskItem>();
            foreach (var plugin in plugins)
            {
                list.AddRange(plugin.GetRisks());
            }
            return list;
        }

        public override List<ExternalDependency> GetAllExternalDependencies()
        {
            var dependencies = new List<ExternalDependency>();
            foreach (var plugin in plugins)
            {
                dependencies.AddRange(plugin.GetDependencies());
            }
            return dependencies;
        }

        public override List<UnitTestItem> GetAllUnitTests()
        {
            var unitTests = new List<UnitTestItem>();
            foreach (var plugin in plugins)
            {
                unitTests.AddRange(plugin.GetUnitTests());
            }
            foreach (var plugin in plugins)
            {
                unitTests.AddRange(plugin.GetUnitTests());
            }
            return unitTests;
        }

        public override List<RequirementItem> GetAllSoftwareRequirements()
        {
            List<RequirementItem> items = new List<RequirementItem>();
            foreach (var plugin in plugins)
            {
                items.AddRange(plugin.GetSoftwareRequirements());
            }
            return items;
        }

        public override List<RequirementItem> GetAllSystemRequirements()
        {
            List<RequirementItem> items = new List<RequirementItem>();
            foreach (var plugin in plugins)
            {
                items.AddRange(plugin.GetSystemRequirements());
            }
            return items;
        }

        public override List<RequirementItem> GetAllDocumentationRequirements()
        {
            List<RequirementItem> items = new List<RequirementItem>();
            foreach (var plugin in plugins)
            {
                items.AddRange(plugin.GetDocumentationRequirements());
            }
            return items;
        }

        public override List<DocContentItem> GetAllDocContents()
        {
            List<DocContentItem> items = new List<DocContentItem>();
            foreach (var plugin in plugins)
            {
                items.AddRange(plugin.GetDocContents());
            }
            return items;
        }

        public override List<SoftwareSystemTestItem> GetAllSoftwareSystemTests()
        {
            List<SoftwareSystemTestItem> items = new List<SoftwareSystemTestItem>();
            foreach (var plugin in plugins)
            {
                items.AddRange(plugin.GetSoftwareSystemTests());
            }
            return items;
        }

        public override List<AnomalyItem> GetAllAnomalies()
        {
            List<AnomalyItem> items = new List<AnomalyItem>();
            foreach (var plugin in plugins)
            {
                items.AddRange(plugin.GetAnomalies());
            }
            return items;
        }
    }
}
