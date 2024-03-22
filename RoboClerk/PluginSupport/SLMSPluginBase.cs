using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using Tomlyn.Model;
using IConfiguration = RoboClerk.Configuration.IConfiguration;

namespace RoboClerk
{
    public abstract class SLMSPluginBase : DataSourcePluginBase
    {
        protected string prsName = string.Empty;
        protected string srsName = string.Empty;
        protected string docName = string.Empty;
        protected string cntName = string.Empty;
        protected string tcName = string.Empty;
        protected string bugName = string.Empty;
        protected string riskName = string.Empty;
        protected string soupName = string.Empty;
        protected TomlArray ignoreList = new TomlArray();

        public SLMSPluginBase(IFileSystem fileSystem)
            : base(fileSystem)
        {

        }

        public override void Initialize(IConfiguration configuration)
        {
            try
            {
                var config = GetConfigurationTable(configuration.PluginConfigDir, $"{name}.toml");
                prsName = configuration.CommandLineOptionOrDefault("SystemRequirement", GetStringForKey(config, "SystemRequirement", false));
                srsName = configuration.CommandLineOptionOrDefault("SoftwareRequirement", GetStringForKey(config, "SoftwareRequirement", false));
                docName = configuration.CommandLineOptionOrDefault("DocumentationRequirement", GetStringForKey(config, "DocumentationRequirement", false));
                cntName = configuration.CommandLineOptionOrDefault("DocContent", GetStringForKey(config, "DocContent", false));
                tcName = configuration.CommandLineOptionOrDefault("SoftwareSystemTest", GetStringForKey(config, "SoftwareSystemTest", false));
                bugName = configuration.CommandLineOptionOrDefault("Anomaly", GetStringForKey(config, "Anomaly", false));
                riskName = configuration.CommandLineOptionOrDefault("Risk", GetStringForKey(config, "Risk", false));
                soupName = configuration.CommandLineOptionOrDefault("SOUP", GetStringForKey(config, "SOUP", false));
                if (config.ContainsKey("Ignore"))
                {
                    ignoreList = (TomlArray)config["Ignore"];
                }
                else
                {
                    logger.Warn($"Key \"Ignore\" missing from configuration file for {name}. Attempting to continue.");
                }
            }
            catch (Exception e)
            {
                logger.Error($"Error reading configuration file for {name}.");
                logger.Error(e);
                throw new Exception($"The {name} could not read its configuration. Aborting...");
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
                    if (!retrievedIDs.Contains(itemLink.TargetID))
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