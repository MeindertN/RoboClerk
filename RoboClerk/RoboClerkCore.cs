
using RoboClerk.Configuration;
using RoboClerk.ContentCreators;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;

namespace RoboClerk
{
    public class RoboClerkCore : IRoboClerkCore
    {
        private readonly IDataSources dataSources = null;
        private readonly ITraceabilityAnalysis traceAnalysis = null;
        private readonly IConfiguration configuration = null;
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private List<Document> documents = new List<Document>();
        private IFileSystem fileSystem = null;

        public RoboClerkCore(IConfiguration config, IDataSources dataSources, ITraceabilityAnalysis traceAnalysis, IFileSystem fs)
        {
            configuration = config;
            this.dataSources = dataSources;
            this.traceAnalysis = traceAnalysis;
            fileSystem = fs;
        }

        public void GenerateDocs()
        {
            logger.Info("Starting document generation.");
            if (configuration.MediaDir != string.Empty)
            {
                if (fileSystem.Directory.Exists(configuration.MediaDir))
                {
                    CleanAndCopyMediaDirectory();
                }
                else
                {
                    logger.Warn($"Configured media directory \"{configuration.MediaDir}\" does not exist. Some of the output documents may have missing images.");
                }
            }
            var configDocuments = configuration.Documents;
            foreach (var doc in configDocuments)
            {
                if (doc.DocumentTemplate == string.Empty)
                    continue;  //skip documents without template
                logger.Info($"Reading document template: {doc.RoboClerkID}");
                Document document = new Document(doc.DocumentTitle, doc.DocumentTemplate);
                document.FromStream(fileSystem.FileStream.Create(fileSystem.Path.Join(configuration.TemplateDir, doc.DocumentTemplate), FileMode.Open));
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
                            IContentCreator contentCreator = new Trace(dataSources, traceAnalysis);
                            tag.Contents = contentCreator.GetContent(tag, doc);
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
                                tag.Contents = cc.GetContent(tag, doc);
                            }
                            else if (tag.Source == DataSource.Reference)
                            {
                                IContentCreator cc = new Reference(traceAnalysis);
                                tag.Contents = cc.GetContent(tag, doc);
                            }
                            else if (tag.Source == DataSource.Document)
                            {
                                IContentCreator cc = new ContentCreators.Document();
                                tag.Contents = cc.GetContent(tag, doc);
                            }
                            else
                            {
                                logger.Debug($"Looking for content creator class: {tag.ContentCreatorID}");
                                var te = traceAnalysis.GetTraceEntityForAnyProperty(tag.ContentCreatorID);

                                IContentCreator contentCreator = GetContentObject(te == default(TraceEntity) ? tag.ContentCreatorID : te.ID);
                                if (contentCreator != null)
                                {
                                    logger.Debug($"Content creator {tag.ContentCreatorID} found.");
                                    tag.Contents = contentCreator.GetContent(tag, doc);
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

        private void CleanAndCopyMediaDirectory()
        {
            logger.Info("Cleaning the media directory and copying the media files.");
            string toplineDir = fileSystem.Path.GetFileName(configuration.MediaDir);
            string targetDir = fileSystem.Path.Combine(configuration.OutputDir, toplineDir);
            if (fileSystem.Directory.Exists(targetDir))
            {
                fileSystem.Directory.Delete(targetDir, true);
            }
            fileSystem.Directory.CreateDirectory(targetDir);
            string[] files = fileSystem.Directory.GetFiles(configuration.MediaDir, "*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                if (!file.Contains(".gitignore"))
                {
                    var relPath = fileSystem.Path.GetRelativePath(configuration.MediaDir, file);
                    string destPath = fileSystem.Path.Combine(targetDir, relPath);
                    fileSystem.Directory.CreateDirectory(fileSystem.Path.GetDirectoryName(destPath));
                    fileSystem.File.Copy(file, destPath);
                }
            }
        }

        public void SaveDocumentsToDisk()
        {
            logger.Info($"Saving documents to directory: {configuration.OutputDir}");
            foreach (var doc in documents)
            {
                logger.Debug($"Writing document to disk: {fileSystem.Path.GetFileName(doc.TemplateFile)}");
                fileSystem.File.WriteAllText(fileSystem.Path.Combine(configuration.OutputDir, fileSystem.Path.GetFileName(doc.TemplateFile)), doc.ToText());
                //run the commands
                logger.Info($"Running commands associated with {doc.Title}");
                var configDoc = configuration.Documents.Find(x => x.DocumentTitle == doc.Title);
                if (configDoc != null && configDoc.Commands != null)
                {
                    configDoc.Commands.RunCommands();
                }
                else
                {
                    logger.Warn($"No commands found for {doc.Title}. Ensure this is intended.");
                }
                fileSystem.File.WriteAllText(fileSystem.Path.Combine(configuration.OutputDir, "DataSourceData.json"), dataSources.ToJSON());
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
                if (contentType.Name.ToUpper() == contentCreatorID.ToUpper())
                {
                    return Activator.CreateInstance(contentType, dataSources, traceAnalysis) as IContentCreator;
                }
            }
            return null;
        }
    }
}
