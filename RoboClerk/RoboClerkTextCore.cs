using RoboClerk.AISystem;
using RoboClerk.Core.Configuration;
using RoboClerk.ContentCreators;
using RoboClerk.Core;
using RoboClerk.Core.ASCIIDOCSupport;
using RoboClerk.Core.DocxSupport;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using IConfiguration = RoboClerk.Core.Configuration.IConfiguration;

namespace RoboClerk
{
    public class RoboClerkTextCore : IRoboClerkCore
    {
        private readonly IDataSources dataSources = null;
        private readonly ITraceabilityAnalysis traceAnalysis = null;
        private readonly IConfiguration configuration = null;
        private readonly IAISystemPlugin aiPlugin = null;
        private readonly IContentCreatorFactory contentCreatorFactory = null;
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private List<IDocument> documents = new List<IDocument>();
        private IFileSystem fileSystem = null;
        private IPluginLoader pluginLoader = null;

        public RoboClerkTextCore(IConfiguration config, IDataSources dataSources, ITraceabilityAnalysis traceAnalysis, 
            IFileSystem fs, IPluginLoader loader, IContentCreatorFactory contentCreatorFactory, IAISystemPlugin aiPlugin = null)
        {
            configuration = config;
            this.dataSources = dataSources;
            this.traceAnalysis = traceAnalysis;
            this.aiPlugin = aiPlugin;
            fileSystem = fs;
            pluginLoader = loader;
            this.contentCreatorFactory = contentCreatorFactory;
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
            if(aiPlugin != null) 
            {
                List<IDocument> docs = ProcessTemplates(aiPlugin.GetAIPromptTemplates());
                // AI prompts are typically text-based, so convert them
                List<TextDocument> textDocs = docs.OfType<TextDocument>().ToList();
                aiPlugin.SetPrompts(textDocs);
            }
            var configDocuments = configuration.Documents;
            documents.AddRange(ProcessTemplates(configDocuments));
            logger.Info("Finished creating documents.");
        }

        private List<IDocument> ProcessTemplates(IEnumerable<DocumentConfig> configDocuments)
        {
            List<IDocument> docs = new List<IDocument>();
            foreach (var doc in configDocuments)
            {
                if (doc.DocumentTemplate == string.Empty)
                    continue;  //skip documents without template

                logger.Info($"Reading document template: {doc.RoboClerkID}");
                
                IDocument document = CreateDocument(doc);
                document.FromStream(fileSystem.FileStream.New(fileSystem.Path.Join(configuration.TemplateDir, doc.DocumentTemplate), FileMode.Open));
                
                logger.Info($"Generating document: {doc.RoboClerkID}");
                ProcessDocumentTags(document, doc);
                
                docs.Add(document);
                logger.Info($"Finished creating document {doc.RoboClerkID}");
            }
            return docs;
        }

        private IDocument CreateDocument(DocumentConfig doc)
        {
            string extension = fileSystem.Path.GetExtension(doc.DocumentTemplate).ToLowerInvariant();
            
            return extension switch
            {
                ".docx" => new DocxDocument(doc.DocumentTitle, doc.DocumentTemplate, configuration),
                ".adoc" or ".asciidoc" or ".txt" or ".htm" or ".html" => new TextDocument(doc.DocumentTitle, doc.DocumentTemplate, fileSystem),
                _ => throw new NotSupportedException($"Document template format '{extension}' is not supported.")
            };
        }

        private void ProcessDocumentTags(IDocument document, DocumentConfig doc)
        {
            // Get all tags in the document
            var tags = document.RoboClerkTags.ToList(); // Create a snapshot
            bool anyTagProcessed = false;
            
            foreach (var tag in tags)
            {
                if (ProcessSingleTag(tag, doc))
                {
                    anyTagProcessed = true;
                    
                    // Process nested tags recursively - this handles all nested levels internally
                    ProcessNestedTagsRecursively(tag, document.DocumentType);
                }
            }
            
            // Refresh document content if any tags were processed
            if (anyTagProcessed)
            {
                RefreshDocumentContent(document);
            }
        }

        private bool ProcessSingleTag(IRoboClerkTag tag, DocumentConfig doc)
        {
            if (tag.Source == DataSource.Trace)
            {
                logger.Debug($"Trace tag found and added to traceability: {tag.GetParameterOrDefault("ID", "ERROR")}");
                IContentCreator contentCreator = contentCreatorFactory.CreateContentCreator(DataSource.Trace, null);
                string newContent = contentCreator.GetContent(tag, doc);
                if (tag.Contents != newContent)
                {
                    tag.Contents = newContent;
                    return true;
                }
                return false;
            }
            
            if (tag.Source != DataSource.Unknown)
            {
                try
                {
                    IContentCreator contentCreator = contentCreatorFactory.CreateContentCreator(tag.Source, tag.ContentCreatorID);
                    string newContent = contentCreator.GetContent(tag, doc);
                    if (tag.Contents != newContent)
                    {
                        tag.Contents = newContent;
                        return true;
                    }
                    return false;
                }
                catch (InvalidOperationException ex)
                {
                    logger.Warn($"Content creator for source '{tag.Source}' not found: {ex.Message}");
                    string errorContent = $"UNABLE TO CREATE CONTENT, ENSURE THAT THE CONTENT CREATOR CLASS '{tag.Source}:{tag.ContentCreatorID}' IS KNOWN TO ROBOCLERK.\n";
                    if (tag.Contents != errorContent)
                    {
                        tag.Contents = errorContent;
                        return true;
                    }
                    return false;
                }
            }
            return false;
        }

        private void ProcessNestedTagsRecursively(IRoboClerkTag tag, DocumentType docType)
        {
            if (string.IsNullOrEmpty(tag.Contents))
                return;
            
            // Process nested tags recursively up to 5 levels to prevent infinite loops
            int nestedLevel = 0;
            string currentContent = tag.Contents;
            
            do
            {
                nestedLevel++;
                var nestedTags = new List<IRoboClerkTag>();
                bool contentChanged = false;
                
                if (docType == DocumentType.Text)
                {
                    // For text documents, extract text-based nested tags
                    var textBasedTags = RoboClerkTextParser.ExtractRoboClerkTags(currentContent);
                    nestedTags.AddRange(textBasedTags.Cast<IRoboClerkTag>());
                }
                else if (docType == DocumentType.Docx && tag is RoboClerkDocxTag docxTag)
                {
                    // For DOCX documents, handle nested tags within content controls
                    nestedTags.AddRange(ProcessDocxNestedContent(docxTag));
                }
                
                // Process each nested tag found at this level using the existing ProcessSingleTag logic
                foreach (var nestedTag in nestedTags)
                {
                    // Create a temporary DocumentConfig for nested processing
                    // This ensures nested tags can be processed independently
                    var tempDoc = new DocumentConfig("temp", "temp", "temp", "temp", "temp");
                    
                    if (ProcessSingleTag(nestedTag, tempDoc))
                    {
                        contentChanged = true;
                    }
                }
                
                // If content changed, update the parent tag's content and prepare for next iteration
                if (contentChanged)
                {
                    currentContent = ReconstructTextContentWithProcessedTags(currentContent, nestedTags.Cast<RoboClerkTextTag>().ToList());
                    tag.Contents = currentContent;
                }
                
                // Exit if no changes or max nested levels reached
                if (!contentChanged || nestedLevel >= 5)
                    break;
                    
            } while (true);
        }

        private string ReconstructTextContentWithProcessedTags(string originalContent, List<RoboClerkTextTag> processedTags)
        {
            if (processedTags.Count == 0)
                return originalContent;
                
            // Use the existing RoboClerkTextParser.ReInsertRoboClerkTags method to rebuild content
            return RoboClerkTextParser.ReInsertRoboClerkTags(originalContent, processedTags);
        }

        private IEnumerable<IRoboClerkTag> ProcessDocxNestedContent(RoboClerkDocxTag docxTag)
        {
            var nestedTags = new List<IRoboClerkTag>();
            
            // Strategy 1: Check for text-based nested RoboClerk tags
            var textBasedNested = RoboClerkTextParser.ExtractRoboClerkTags(docxTag.Contents);
            nestedTags.AddRange(textBasedNested.Cast<IRoboClerkTag>());
            
            // Strategy 2: Check for embedded content controls (if content contains Word XML)
            // Note: This would require more complex implementation to parse embedded Word documents
            // For now, we focus on text-based nested tags within content controls
            
            return nestedTags;
        }

        private void RefreshDocumentContent(IDocument document)
        {
            if (document.DocumentType == DocumentType.Text)
            {
                // Refresh text document by reparsing
                string documentContent = document.ToText();
                document.FromString(documentContent);
            }
        }

        public void SaveDocumentsToDisk()
        {
            logger.Info($"Saving documents to directory: {configuration.OutputDir}");
            
            // Ensure the output directory exists
            if (!fileSystem.Directory.Exists(configuration.OutputDir))
            {
                fileSystem.Directory.CreateDirectory(configuration.OutputDir);
            }
            
            foreach (var doc in documents)
            {
                logger.Debug($"Writing document to disk: {fileSystem.Path.GetFileName(doc.TemplateFile)}");
                
                string outputPath = fileSystem.Path.Combine(configuration.OutputDir, fileSystem.Path.GetFileName(doc.TemplateFile));
                doc.SaveToFile(outputPath);
                
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
            }
            
            fileSystem.File.WriteAllText(fileSystem.Path.Combine(configuration.OutputDir, "DataSourceData.json"), dataSources.ToJSON());
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
    }
}
