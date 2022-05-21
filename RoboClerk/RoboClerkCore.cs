
using RoboClerk.Configuration;
using RoboClerk.ContentCreators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace RoboClerk
{
    public enum DocumentFormat
    {
        Markdown,
        HTML
    };

    internal class RoboClerkCore : IRoboClerkCore
    {
        private readonly IDataSources dataSources = null;
        private readonly ITraceabilityAnalysis traceAnalysis = null;
        private readonly IConfiguration configuration = null;
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private List<Document> documents = new List<Document>();

        public RoboClerkCore(IConfiguration config, IDataSources dataSources, ITraceabilityAnalysis traceAnalysis)
        {
            configuration = config;
            this.dataSources = dataSources;
            this.traceAnalysis = traceAnalysis;           
        }

        public void GenerateDocs()
        {
            logger.Info("Starting document generation.");
            if(configuration.ClearOutputDir)
            {
                CleanOutputDirectory();
            }
            if(configuration.MediaDir != string.Empty)
            {
                if(Directory.Exists(configuration.MediaDir))
                {
                    CleanAndCopyMediaDirectory();
                }
                else
                {
                    logger.Warn($"Configured media directory \"{configuration.MediaDir}\" does not exist. Some of the output documents will have missing images.");
                }
            }
            var configDocuments = configuration.Documents;
            foreach (var doc in configDocuments)
            {
                if (doc.DocumentTemplate == string.Empty)
                    continue;  //skip documents without template
                logger.Info($"Reading document template: {doc.RoboClerkID}");
                Document document = new Document(doc.DocumentTitle);
                document.FromFile(Path.Join(configuration.TemplateDir, doc.DocumentTemplate));
                logger.Info($"Generating document: {doc.RoboClerkID}");
                int nrOfLevels = 0;
                //go over the tag list to determine what information should be collected from where
                do
                {
                    nrOfLevels++;
                    foreach (var tag in document.RoboClerkTags)
                    {
                        if (tag.Source == DataSource.Trace)
                        {
                            logger.Debug($"Trace tag found and added to traceability: {tag.GetParameterOrDefault("ID", "ERROR")}");
                            //grab trace tag and add to the trace analysis
                            IContentCreator contentCreator = new Trace();
                            tag.Contents = contentCreator.GetContent(tag, dataSources, traceAnalysis, doc);
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
                                tag.Contents = cc.GetContent(tag, dataSources, traceAnalysis, doc);
                            }
                            else if (tag.Source == DataSource.Reference)
                            {
                                IContentCreator cc = new Reference();
                                tag.Contents = cc.GetContent(tag, dataSources, traceAnalysis, doc);
                            }
                            else if (tag.Source == DataSource.Document)
                            {
                                IContentCreator cc = new ContentCreators.Document();
                                tag.Contents = cc.GetContent(tag, dataSources, traceAnalysis, doc);
                            }
                            else
                            {
                                logger.Debug($"Looking for content creator class: {tag.ContentCreatorID}");
                                var te = traceAnalysis.GetTraceEntityForAnyProperty(tag.ContentCreatorID);

                                IContentCreator contentCreator = GetContentObject(te == default(TraceEntity) ? tag.ContentCreatorID : te.ID);
                                if (contentCreator != null)
                                {
                                    logger.Debug($"Content creator {tag.ContentCreatorID} found.");
                                    tag.Contents = contentCreator.GetContent(tag, dataSources, traceAnalysis, doc);
                                    continue;
                                }
                                logger.Warn($"Content creator {tag.ContentCreatorID} not found.");
                                tag.Contents = $"UNABLE TO CREATE CONTENT, ENSURE THAT THE CONTENT CREATOR CLASS ({tag.ContentCreatorID}) IS KNOWN TO ROBOCLERK.\n";
                            }
                        }
                    }
                    string documentContent = document.ToText();
                    document.FromString(documentContent);
                }
                while (document.RoboClerkTags.Count() > 0 && nrOfLevels < 5);
                documents.Add(document);
                logger.Info($"Finished creating document {doc.RoboClerkID}");
            }
            logger.Info("Finished creating documents.");
        }

        private void CleanOutputDirectory()
        {
            logger.Info("Cleaning output directory.");
            string[] files = Directory.GetFiles(configuration.OutputDir);
            foreach (string file in files)
            {
                if (!file.Contains("RoboClerkLog.txt") &&
                    !file.Contains(".gitignore"))
                {
                    File.Delete(file);
                }
            }
        }

        private void CleanAndCopyMediaDirectory()
        {
            logger.Info("Cleaning the media directory and copying the media files.");
            string toplineDir = Path.GetFileName(configuration.MediaDir);
            string targetDir = Path.Combine(configuration.OutputDir, toplineDir);
            if(Directory.Exists(targetDir))
            {
                Directory.Delete(targetDir, true);
            }
            Directory.CreateDirectory(targetDir);
            string[] files = Directory.GetFiles(configuration.MediaDir);
            foreach (string file in files)
            {
                if (!file.Contains(".gitignore"))
                {
                    File.Copy(file, Path.Combine(targetDir,Path.GetFileName(file)));
                }
            }
        }

        public void SaveDocumentsToDisk()
        {
            logger.Info($"Saving documents to directory: {configuration.OutputDir}");
            foreach (var doc in documents)
            {
                logger.Debug($"Writing document to disk: {Path.GetFileName(doc.TemplateFile)}");
                File.WriteAllText(Path.Combine(configuration.OutputDir, Path.GetFileName(doc.TemplateFile)), doc.ToText());
                //run the commands
                logger.Info($"Running commands associated with {doc.Title}");
                var configDoc = configuration.Documents.Find(x => x.DocumentTitle == doc.Title);
                configDoc.Commands.RunCommands();
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
