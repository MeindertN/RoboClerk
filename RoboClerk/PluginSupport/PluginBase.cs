﻿using DocumentFormat.OpenXml.Office2010.ExcelAc;
using RoboClerk.Configuration;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Reflection;
using Tomlyn;
using Tomlyn.Model;

namespace RoboClerk
{
    public abstract class PluginBase : IPlugin
    {
        protected string name = string.Empty;
        protected string description = string.Empty;
        protected static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        protected IFileSystem fileSystem = null;

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

        public PluginBase(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public string Name
        {
            get
            {
                return name;
            }
        }

        public string Description
        {
            get
            {
                return description;
            }
        }

        public abstract void Initialize(IConfiguration config);

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
                        string newValue = currentValue.Replace("|", "\\|");
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

        protected string GetStringForKey(TomlTable config, string keyName, bool required)
        {
            string result = string.Empty;
            if (config.ContainsKey(keyName))
            {
                return (string)config[keyName];
            }
            else
            {
                if (required)
                {
                    throw new Exception($"Required key \"{keyName}\" missing from configuration file for {name}. Cannot continue.");
                }
                else
                {
                    logger.Warn($"Key \\\"{keyName}\\\" missing from configuration file for {name}. Attempting to continue.");
                    return string.Empty;
                }
            }
        }

        protected TomlTable GetConfigurationTable(string pluginConfDir, string confFileName)
        {
            var assembly = Assembly.GetAssembly(this.GetType());
            var configFileLocation = $"{fileSystem.Path.GetDirectoryName(assembly?.Location)}/Configuration/{confFileName}";
            if (pluginConfDir != string.Empty)
            {
                configFileLocation = fileSystem.Path.Combine(pluginConfDir, confFileName);
            }
            return Toml.Parse(fileSystem.File.ReadAllText(configFileLocation)).ToModel();
        }
    }
}
