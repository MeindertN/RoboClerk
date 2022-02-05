
using System.Collections.Generic;
using System.IO;
using Tomlyn;
using Tomlyn.Model;
using RoboClerk.ContentCreators;
using System.Reflection;
using System;
using System.Linq;

namespace RoboClerk
{
    public enum DocumentFormat
    {
        Markdown,
        HTML
    };

    public class RoboClerkCore
    {
        private DataSources dataSources = null;
        private Dictionary<string, (string, string)> documents = //key identifies the document
            new Dictionary<string, (string, string)>(); //first string in value is the filename and second is file content
        private TraceabilityAnalysis traceAnalysis = null;
        private string outputDir = string.Empty;
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public RoboClerkCore(string configFile, string projectConfigFile)
        {
            logger.Debug($"Loading configuration files into RoboClerk: {configFile} and {projectConfigFile}");
            (configFile,projectConfigFile) = LoadConfigFiles(configFile, projectConfigFile);
            logger.Debug("Setting up data sources");
            dataSources = new DataSources(configFile, projectConfigFile);
            logger.Debug("initiating traceability analysis");
            traceAnalysis = new TraceabilityAnalysis(projectConfigFile);
            logger.Debug("Processing the configuration files");
            ProcessConfigs(configFile,projectConfigFile);
        }

        private (string, string) LoadConfigFiles(string configFile, string projectConfigFile)
        {
            string config;
            string projectConfig;
            try
            {
                config = File.ReadAllText(configFile);
            }
            catch(IOException e)
            {
                throw new Exception($"Unable to read config file: {configFile}");
            }
            try
            {
                projectConfig = File.ReadAllText(projectConfigFile);
            }
            catch(IOException e)
            {
                throw new Exception($"Unable to read project config file {projectConfigFile}");
            }
            return (config, projectConfig);
        }

        private void ProcessConfigs(string config, string projectConfig)
        {
            var toml = Toml.Parse(projectConfig).ToModel();
            foreach (var docloc in (TomlTable)toml["DocumentLocations"])
            {
                TomlArray arr = (TomlArray)docloc.Value;
                if ((string)arr[2] != string.Empty)
                {
                    try
                    {
                        documents[(string)arr[0]] = ((string)arr[2], File.ReadAllText((string)arr[2]));
                    }
                    catch(Exception e)
                    {
                        logger.Error(e);
                        throw new Exception($"Unable to read {(string)arr[0]} from {(string)arr[2]} ");
                    }
                }
            }
            toml = Toml.Parse(config).ToModel();
            outputDir = (string)toml["OutputDirectory"];
        }

        public void GenerateDocs()
        {
            logger.Info("Starting document generation.");
            var tempDic = new Dictionary<string, (string, string)>();
            foreach (var doc in documents)
            {

                //load the document from disk into a document structure
                logger.Info($"Generating document: {doc.Key}");
                Document document = new Document(doc.Key);
                document.FromText(doc.Value.Item2);
                //go over the tag list to determine what information should be collected from where
                foreach(var tag in document.RoboClerkTags)
                {
                    if(tag.Source == DataSource.Trace)
                    {
                        logger.Debug($"Trace tag found and added to traceability: {tag.GetParameterOrDefault("ID","ERROR")}");
                        //grab trace tag and add to the trace analysis
                        IContentCreator contentCreator = new Trace();
                        tag.Contents = contentCreator.GetContent(tag, dataSources, traceAnalysis, document.Title);
                        continue;
                    }
                    if (tag.Source != DataSource.Info && tag.Source != DataSource.Unknown)
                    {
                        if (tag.Source == DataSource.Config)
                        {
                            logger.Debug($"Configuration file item requested: {tag.ContentCreatorID}");
                            tag.Contents = dataSources.GetConfigValue(tag.ContentCreatorID);
                        }
                        else
                        {
                            logger.Debug($"Looking for content creator class: {tag.ContentCreatorID}");
                            IContentCreator contentCreator = GetContentObject(tag);
                            if (contentCreator != null)
                            {
                                logger.Debug($"Content creator {tag.ContentCreatorID} found.");
                                tag.Contents = contentCreator.GetContent(tag,dataSources,traceAnalysis,document.Title);
                            }
                            else
                            {
                                logger.Warn($"Content creator {tag.ContentCreatorID} not found.");
                                tag.Contents = "UNABLE TO CREATE CONTENT, ENSURE THAT THE CONTENT CREATOR CLASS IS KNOWN TO ROBOCLERK.\n";
                            }
                        }
                    }
                }
                logger.Info($"Finished creating document {doc.Value.Item1}");
                tempDic[doc.Key] = (doc.Value.Item1,document.ToText());
            }
            logger.Info("Finished creating documents.");
            documents = tempDic;
        }

        public void SaveDocumentsToDisk()
        {
            logger.Info($"Saving documents to directory: {outputDir}");
            foreach (var doc in documents)
            {
                logger.Debug($"Writing document to disk: {Path.GetFileName(doc.Value.Item1)}");
                File.WriteAllText(Path.Combine(outputDir,Path.GetFileName(doc.Value.Item1)), doc.Value.Item2);
            }
        }

        private IContentCreator GetContentObject(RoboClerkTag tag)
        {
            Assembly thisAssembly = Assembly.GetAssembly(this.GetType());
            Type[] contentTypes = thisAssembly
                .GetTypes()
                .Where(t => typeof(IContentCreator).IsAssignableFrom(t) && t.IsClass)
                .ToArray();

            foreach (Type contentType in contentTypes)
            {
                if (contentType.Name == tag.ContentCreatorID)
                {
                    return Activator.CreateInstance(contentType) as IContentCreator;
                }
            }
            return null;
        }
    }
}
