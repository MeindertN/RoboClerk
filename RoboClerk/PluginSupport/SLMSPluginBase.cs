using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using Tomlyn.Model;
using RoboClerk.Configuration;
using IConfiguration = RoboClerk.Configuration.IConfiguration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace RoboClerk
{
    public abstract class SLMSPluginBase : DataSourcePluginBase
    {
        private Dictionary<string, TruthItemConfig> truthItemConfig = new Dictionary<string, TruthItemConfig>();
        protected TruthItemConfig PrsConfig => truthItemConfig["SystemRequirement"];
        protected TruthItemConfig SrsConfig => truthItemConfig["SoftwareRequirement"];
        protected TruthItemConfig DocConfig => truthItemConfig["DocumentationRequirement"];
        protected TruthItemConfig CntConfig => truthItemConfig["DocContent"];
        protected TruthItemConfig TcConfig => truthItemConfig["SoftwareSystemTest"];
        protected TruthItemConfig BugConfig => truthItemConfig["Anomaly"];
        protected TruthItemConfig RiskConfig => truthItemConfig["Risk"];
        protected TruthItemConfig SoupConfig => truthItemConfig["SOUP"];

        protected TomlArray ignoreList = new TomlArray();

        private Dictionary<string,HashSet<string>> inclusionFilters = new Dictionary<string,HashSet<string>>();
        private Dictionary<string,HashSet<string>> exclusionFilters = new Dictionary<string,HashSet<string>>();


        public SLMSPluginBase(IFileSystem fileSystem)
            : base(fileSystem)
        {

        }

        public override void InitializePlugin(IConfiguration configuration)
        {
            try
            {
                var config = GetConfigurationTable(configuration.PluginConfigDir, $"{name}.toml");
                AddTruthItemConfig("SystemRequirement", config);
                AddTruthItemConfig("SoftwareRequirement", config);
                AddTruthItemConfig("DocumentationRequirement", config);
                AddTruthItemConfig("DocContent", config);
                AddTruthItemConfig("SoftwareSystemTest", config);
                AddTruthItemConfig("Anomaly",config);
                AddTruthItemConfig("Risk", config);
                AddTruthItemConfig("SOUP", config);

                if (config.ContainsKey("Ignore"))
                {
                    ignoreList = (TomlArray)config["Ignore"];
                }
                else
                {
                    logger.Warn($"Key \"Ignore\" missing from configuration file for {name}. Attempting to continue.");
                }

                if (config.ContainsKey("ExcludedItemFilter"))
                {
                    TomlTable excludedFields = (TomlTable)config["ExcludedItemFilter"];
                    foreach (var field in excludedFields)
                    {
                        exclusionFilters[field.Key] = GetFilterValues((TomlArray)field.Value, "ExcludedItemFilter");
                    }
                }

                if (config.ContainsKey("IncludedItemFilter"))
                {
                    TomlTable includedFields = (TomlTable)config["IncludedItemFilter"];
                    foreach (var field in includedFields)
                    {
                        inclusionFilters[field.Key] = GetFilterValues((TomlArray)field.Value, "IncludedItemFilter");
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error($"Error reading configuration file for {name}.");
                logger.Error(e);
                throw new Exception($"The {name} could not read its configuration. Aborting...");
            }
        }

        private HashSet<string> GetFilterValues(TomlArray values, string id)
        {
            HashSet<string> vs = new HashSet<string>();
            foreach (var value in values)
            {
                if (value is string str)
                {
                    vs.Add(str);
                }
                else
                {
                    logger.Error($"One or more values in the {id} list in the {name} configuration file is not a string. Cannot parse.");
                    throw new Exception($"{id} list value not a string.");
                }
            }
            return vs;
        }

        private void AddTruthItemConfig(string itemName, TomlTable config)
        {
            if (config.ContainsKey(itemName))
            {
                try
                {
                    TomlTable item = (TomlTable)config[itemName];
                    string nm = GetObjectForKey<string>(item, "name", true);
                    bool flt = GetObjectForKey<bool>(item, "filter", true);
                    truthItemConfig[itemName] = new TruthItemConfig(nm, flt);
                }
                catch 
                {
                    logger.Error($"{name} configuration file has an entry for {itemName} but it is not valid.");
                    throw;
                }
            }
            else
            {
                throw new ArgumentException($"{name} configuration file does not contain valid truth item configuration for \"{itemName}\".");
            }
        }

        protected bool ExcludeItem(string fieldName, HashSet<string> values)
        {
            if(exclusionFilters.Count > 0) 
            {
                if (exclusionFilters.ContainsKey(fieldName))
                {
                    return exclusionFilters[fieldName].Overlaps(values);
                }
                else
                { 
                    return false; 
                }
            }
            else
            {
                return false;
            }
        }

        protected bool IncludeItem(string fieldName, HashSet<string> values)
        {
            if (inclusionFilters.Count > 0)
            {
                if (inclusionFilters.ContainsKey(fieldName))
                {
                    return inclusionFilters[fieldName].Overlaps(values);
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
        }

        protected void TrimLinkedItems<T>(List<T> items, List<string> retrievedIDs)
        {
            foreach (var item in items)
            {
                LinkedItem linkedItem = item as LinkedItem;
                List<ItemLink> linkedItemsToRemove = new List<ItemLink>();
                foreach (var itemLink in linkedItem.LinkedItems)
                {
                    // unit tests are a special case, we cannot use this function to remove those links because
                    // the unit tests do not come from the SLMS. Otherwise this function would remove all software
                    // system tests that are "kicked" to the unit level test plan.
                    if (!retrievedIDs.Contains(itemLink.TargetID) && itemLink.LinkType != ItemLinkType.UnitTest)
                    {
                        logger.Warn($"Removing a {itemLink.LinkType} link from item \"{linkedItem.ItemID}\" to item with ID \"{itemLink.TargetID}\" because that item has a status that causes it to be ignored.");
                        linkedItemsToRemove.Add(itemLink);
                    }
                }
                foreach (var itemLink in linkedItemsToRemove)
                {
                    linkedItem.RemoveLinkedItem(itemLink); //remove the link to an ignored item
                }
            }
        }
    }
}