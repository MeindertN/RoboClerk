
using RoboClerk.ContentCreators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Tomlyn;
using Tomlyn.Model;

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
        private Dictionary<string, (Document, Commands)> documents = //key identifies the document
            new Dictionary<string, (Document, Commands)>(); //first string in value is the filename and second is file content
        private TraceabilityAnalysis traceAnalysis = null;
        private string outputDir = string.Empty;
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public RoboClerkCore(string configFile, string projectConfigFile)
        {
            logger.Debug($"Loading configuration files into RoboClerk: {configFile} and {projectConfigFile}");
            (configFile, projectConfigFile) = LoadConfigFiles(configFile, projectConfigFile);
            logger.Debug("Setting up data sources");
            dataSources = new DataSources(configFile, projectConfigFile);
            logger.Debug("initiating traceability analysis");
            traceAnalysis = new TraceabilityAnalysis(projectConfigFile);
            logger.Debug("Processing the configuration files");
            ProcessConfigs(configFile, projectConfigFile);
        }

        private (string, string) LoadConfigFiles(string configFile, string projectConfigFile)
        {
            string config;
            string projectConfig;
            try
            {
                config = File.ReadAllText(configFile);
            }
            catch (IOException e)
            {
                throw new Exception($"Unable to read config file: {configFile}");
            }
            try
            {
                projectConfig = File.ReadAllText(projectConfigFile);
            }
            catch (IOException e)
            {
                throw new Exception($"Unable to read project config file {projectConfigFile}");
            }
            return (config, projectConfig);
        }

        private void ProcessConfigs(string config, string projectConfig)
        {
            //read in the output dir
            var toml = Toml.Parse(config).ToModel();
            outputDir = (string)toml["OutputDirectory"];
            //read in the documents 
            toml = Toml.Parse(projectConfig).ToModel();
            foreach (var doctable in (TomlTable)toml["Document"])
            {
                TomlTable doc = (TomlTable)doctable.Value;
                if (!doc.ContainsKey("template"))
                {
                    throw new Exception($"Error reading template location out of project config file for document {doctable.Key}");
                }

                if ((string)doc["template"] != string.Empty)
                {
                    try
                    {
                        Document document = new Document((string)doc["title"]);
                        document.FromFile((string)doc["template"]);
                        Commands commands = null;
                        if (doc.ContainsKey("Command"))
                        {
                            commands = new Commands((TomlTableArray)doc["Command"], outputDir, Path.GetFileName((string)doc["template"]));
                        }
                        documents[(string)doc["title"]] = (document, commands);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Unable to read document {doctable.Key} from {(string)doc["template"]}. Check project config file and template file location.");
                    }
                }
            }
        }

        public void GenerateDocs()
        {
            logger.Info("Starting document generation.");
            var tempDic = new Dictionary<string, (string, string)>();
            foreach (var doc in documents)
            {
                logger.Info($"Generating document: {doc.Key}");
                //go over the tag list to determine what information should be collected from where
                foreach (var tag in doc.Value.Item1.RoboClerkTags)
                {
                    if (tag.Source == DataSource.Trace)
                    {
                        logger.Debug($"Trace tag found and added to traceability: {tag.GetParameterOrDefault("ID", "ERROR")}");
                        //grab trace tag and add to the trace analysis
                        IContentCreator contentCreator = new Trace();
                        tag.Contents = contentCreator.GetContent(tag, dataSources, traceAnalysis, doc.Value.Item1.Title);
                        continue;
                    }
                    if (tag.Source != DataSource.Unknown)
                    {
                        if (tag.Source == DataSource.Config)
                        {
                            logger.Debug($"Configuration file item requested: {tag.ContentCreatorID}");
                            tag.Contents = dataSources.GetConfigValue(tag.ContentCreatorID);
                        }
                        else if (tag.Source == DataSource.Comment)
                        {
                            tag.Contents = string.Empty;
                        }
                        else if (tag.Source == DataSource.Post)
                        {
                            IContentCreator cc = new PostLayout();
                            tag.Contents = cc.GetContent(tag,dataSources,traceAnalysis, doc.Value.Item1.Title);
                        }
                        else
                        {
                            logger.Debug($"Looking for content creator class: {tag.ContentCreatorID}");
                            var te = traceAnalysis.GetTraceEntityForAnyProperty(tag.ContentCreatorID);

                            IContentCreator contentCreator = GetContentObject(te == null ? tag.ContentCreatorID : te.ID);
                            if (contentCreator != null)
                            {
                                logger.Debug($"Content creator {tag.ContentCreatorID} found.");
                                tag.Contents = contentCreator.GetContent(tag, dataSources, traceAnalysis, doc.Value.Item1.Title);
                                continue;
                            }
                            logger.Warn($"Content creator {tag.ContentCreatorID} not found.");
                            tag.Contents = $"UNABLE TO CREATE CONTENT, ENSURE THAT THE CONTENT CREATOR CLASS ({tag.ContentCreatorID}) IS KNOWN TO ROBOCLERK.\n";
                        }
                    }
                }
                logger.Info($"Finished creating document {doc.Value.Item1}");
            }
            logger.Info("Finished creating documents.");
        }

        public void SaveDocumentsToDisk()
        {
            logger.Info($"Saving documents to directory: {outputDir}");
            foreach (var doc in documents)
            {
                logger.Debug($"Writing document to disk: {Path.GetFileName(doc.Value.Item1.TemplateFile)}");
                File.WriteAllText(Path.Combine(outputDir, Path.GetFileName(doc.Value.Item1.TemplateFile)), doc.Value.Item1.ToText());
                //run the commands
                logger.Info($"Running commands associated with {doc.Key}");
                doc.Value.Item2.RunCommands();
            }
        }

        private IContentCreator GetContentObject(string contentCreatorID)
        {
            Assembly thisAssembly = Assembly.GetAssembly(this.GetType());
            Type[] contentTypes = thisAssembly
                .GetTypes()
                .Where(t => typeof(IContentCreator).IsAssignableFrom(t) && t.IsClass)
                .ToArray();

            foreach (Type contentType in contentTypes)
            {
                if (contentType.Name == contentCreatorID)
                {
                    return Activator.CreateInstance(contentType) as IContentCreator;
                }
            }
            return null;
        }
    }
}
