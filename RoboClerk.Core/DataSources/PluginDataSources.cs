using Microsoft.Extensions.DependencyInjection;
using RoboClerk.Core.Configuration;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Security.Cryptography;

namespace RoboClerk
{
    public class PluginDataSources : DataSourcesBase, IDataSources
    {
        private List<IDataSourcePlugin> plugins = new List<IDataSourcePlugin>();

        private readonly IPluginLoader pluginLoader;

        public PluginDataSources(IConfiguration configuration, IPluginLoader pluginLoader, IFileProviderPlugin fileSystem)
            : base(configuration, fileSystem)
        {
            this.pluginLoader = pluginLoader;
            LoadPlugins();
        }

        private void LoadPlugins()
        {
            foreach (var val in configuration.DataSourcePlugins)
            {
                bool found = false;
                foreach (var dir in configuration.PluginDirs)
                {
                    var plugin = pluginLoader.LoadByName<IDataSourcePlugin>(
                        pluginDir: dir,
                        typeName: val,
                        configureGlobals: sc =>
                        {
                            sc.AddSingleton(fileSystem);
                            sc.AddSingleton(configuration);
                        });
                    if (plugin != null)
                    {
                        plugin.InitializePlugin(configuration);
                        if (plugin as IDataSourcePlugin != null)
                        {
                            found = true;
                            var temp = plugin as IDataSourcePlugin;
                            temp.RefreshItems();
                            plugins.Add(temp);
                            break;
                        }
                    }
                }
                if(!found)
                {
                    //if we don't find a specified datasource plugin, that is reason to quit.
                    //otherwise data could be missing from the project and the user would not know.
                    throw new Exception($"Unable to find plugin {val}, please ensure the name of the plugin and plugin directories in the RoboClerk config file are correct.");
                }
            }
        }

        public override void RefreshDataSources()
        {
            foreach (var plugin in plugins)
            {
                plugin.RefreshItems();
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

        public override List<EliminatedSOUPItem> GetAllEliminatedSOUP()
        {
            List<EliminatedSOUPItem> list = new List<EliminatedSOUPItem>();
            foreach (var plugin in plugins)
            {
                list.AddRange(plugin.GetEliminatedSOUP());
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

        public override List<EliminatedRiskItem> GetAllEliminatedRisks()
        {
            List<EliminatedRiskItem> list = new List<EliminatedRiskItem>();
            foreach (var plugin in plugins)
            {
                list.AddRange(plugin.GetEliminatedRisks());
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

        public override List<TestResult> GetAllTestResults()
        {
            var results = new List<TestResult>();
            foreach (var plugin in plugins)
            {
                results.AddRange(plugin.GetTestResults());
            }
            return results;
        }

        public override List<UnitTestItem> GetAllUnitTests()
        {
            var unitTests = new List<UnitTestItem>();
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

        public override List<EliminatedDocContentItem> GetAllEliminatedDocContents()
        {
            List<EliminatedDocContentItem> items = new List<EliminatedDocContentItem>();
            foreach (var plugin in plugins)
            {
                items.AddRange(plugin.GetEliminatedDocContents());
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

        public override List<EliminatedAnomalyItem> GetAllEliminatedAnomalies()
        {
            List<EliminatedAnomalyItem> items = new List<EliminatedAnomalyItem>();
            foreach (var plugin in plugins)
            {
                items.AddRange(plugin.GetEliminatedAnomalies());
            }
            return items;
        }

        public override List<EliminatedRequirementItem> GetAllEliminatedSystemRequirements()
        {
            List<EliminatedRequirementItem> items = new List<EliminatedRequirementItem>();
            foreach (var plugin in plugins)
            {
                items.AddRange(plugin.GetEliminatedSystemRequirements());
            }
            return items;
        }

        public override List<EliminatedRequirementItem> GetAllEliminatedSoftwareRequirements()
        {
            List<EliminatedRequirementItem> items = new List<EliminatedRequirementItem>();
            foreach (var plugin in plugins)
            {
                items.AddRange(plugin.GetEliminatedSoftwareRequirements());
            }
            return items;
        }

        public override List<EliminatedRequirementItem> GetAllEliminatedDocumentationRequirements()
        {
            List<EliminatedRequirementItem> items = new List<EliminatedRequirementItem>();
            foreach (var plugin in plugins)
            {
                items.AddRange(plugin.GetEliminatedDocumentationRequirements());
            }
            return items;
        }

        public override List<EliminatedSoftwareSystemTestItem> GetAllEliminatedSoftwareSystemTests()
        {
            List<EliminatedSoftwareSystemTestItem> items = new List<EliminatedSoftwareSystemTestItem>();
            foreach (var plugin in plugins)
            {
                items.AddRange(plugin.GetEliminatedSoftwareSystemTests());
            }
            return items;
        }
    }
}
