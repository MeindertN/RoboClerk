using AngleSharp.Io;
using DocumentFormat.OpenXml;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoboClerk.ContentCreators;
using RoboClerk.Core;
using RoboClerk.Core.ASCIIDOCSupport;
using RoboClerk.Core.Configuration;
using RoboClerk.Core.DocxSupport;
using RoboClerk.Server.Models;
using RoboClerk.SharePointFileProvider;
using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using IConfiguration = RoboClerk.Core.Configuration.IConfiguration;

namespace RoboClerk.Server.Services
{
    public class ProjectManager : IProjectManager
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IFileSystem fileSystem;
        private readonly IDataSourcesFactory dataSourcesFactory;
        private readonly ConcurrentDictionary<string, ProjectContext> loadedProjects = new();
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public ProjectManager(IServiceProvider serviceProvider, IFileSystem fileSystem, IDataSourcesFactory dataSourcesFactory)
        {
            this.serviceProvider = serviceProvider;
            this.fileSystem = fileSystem;
            this.dataSourcesFactory = dataSourcesFactory;
        }

        public async Task<ProjectLoadResult> LoadProjectAsync(LoadProjectRequest request)
        {
            try
            {
                // Generate deterministic project ID from driveId and config path
                var projectConfigPath = fileSystem.Path.Combine(request.ProjectPath, "RoboClerkConfig", "projectConfig.toml");
                var projectId = request.ProjectIdentifier ?? GenerateProjectIdentifier(request.SPDriveId, projectConfigPath);

                // Check if this project is already loaded
                if (loadedProjects.ContainsKey(projectId))
                {
                    logger.Info($"Project already loaded, returning existing instance: {projectId}");
                    var existingProject = loadedProjects[projectId];
                    var existingConfig = existingProject.ProjectServiceProvider.GetRequiredService<IConfiguration>();
                    return new ProjectLoadResult
                    {
                        Success = true,
                        ProjectId = projectId,
                        ProjectName = existingProject.ProjectName,
                        LastUpdated = existingProject.LastUpdated,
                        Documents = existingConfig.Documents
                            .Where(d => d.DocumentTemplate.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                            .Select(d => new DocumentInfo(d.RoboClerkID, d.DocumentTitle, d.DocumentTemplate))
                            .ToList()
                    };
                }

                // Validate SharePoint path
                if (!IsSharePointPath(request.ProjectPath, request))
                {
                    logger.Warn($"Non-SharePoint path provided: {request.ProjectPath}");
                    return new ProjectLoadResult { Success = false, Error = "Only SharePoint project paths are supported for Word add-in use" };
                }

                var sharePointFileProvider = await CreateSharePointFileProviderAsync(request);
                if (sharePointFileProvider == null)
                {
                    return new ProjectLoadResult { Success = false, Error = "Failed to initialize SharePoint file provider" };
                }

                // Load project configuration from SharePoint to determine ProjectRoot and other settings
                logger.Info($"Loading project configuration from SharePoint: {projectConfigPath}");

                var baseConfiguration = serviceProvider.GetRequiredService<IConfiguration>();

                // Check if base configuration is the concrete type we need
                if (baseConfiguration is not RoboClerk.Configuration.Configuration concreteConfig)
                {
                    return new ProjectLoadResult { Success = false, Error = "Configuration service is not of expected type" };
                }

                // Create configuration with project config loaded
                RoboClerk.Configuration.Configuration configuration;
                try
                {
                    // Use the new Configuration builder pattern to clone existing and add project config
                    configuration = RoboClerk.Configuration.ConfigurationBuilder
                        .FromExisting(concreteConfig.Clone())
                        .WithProjectConfig(sharePointFileProvider, projectConfigPath)
                        .Build();
                    configuration.AddOrUpdateCommandLineOption("SPDriveId", request.SPDriveId);
                    configuration.AddOrUpdateCommandLineOption("SPSiteUrl", request.SPSiteUrl);
                    configuration.ProjectID = projectId;  //necesary to uniquely identify the project the config belongs to
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to load project configuration from SharePoint");
                    return new ProjectLoadResult { Success = false, Error = $"Failed to load project configuration: {ex.Message}" };
                }
                logger.Info($"Loading SharePoint project from: {request.ProjectPath}");

                // Filter for DOCX documents only
                var docxDocuments = configuration.Documents
                    .Where(d => d.DocumentTemplate.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (!docxDocuments.Any())
                {
                    return new ProjectLoadResult { Success = false, Error = "No DOCX documents found in SharePoint project" };
                }

                // Create project-specific service provider with all dependencies
                var projectServiceProvider = CreateProjectServiceProvider(
                    sharePointFileProvider,
                    configuration);

                var projectContext = new ProjectContext
                {
                    ProjectId = projectId,
                    ProjectPath = request.ProjectPath,
                    ProjectName = configuration.ProjectName,
                    ProjectServiceProvider = projectServiceProvider,
                    LoadedDocuments = new ConcurrentDictionary<string, IDocument>()
                };

                loadedProjects[projectId] = projectContext;

                logger.Info($"Successfully loaded SharePoint project: {projectId} with {docxDocuments.Count} DOCX documents");
                
                foreach (var docxDocument in docxDocuments)
                {
                    projectContext.LoadedDocuments[docxDocument.RoboClerkID] = ProcessTemplate(projectServiceProvider,docxDocument);
                }

                return new ProjectLoadResult
                {
                    Success = true,
                    ProjectId = projectId,
                    ProjectName = configuration.ProjectName,
                    Documents = docxDocuments.Select(d => new DocumentInfo(
                        d.RoboClerkID,
                        d.DocumentTitle,
                        d.DocumentTemplate
                    )).ToList()
                };
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to load SharePoint project: {request.ProjectPath}");
                return new ProjectLoadResult { Success = false, Error = $"Failed to load SharePoint project: {ex.Message}" };
            }
        }

        private IDocument ProcessTemplate(IServiceProvider projectServiceProvider, Core.Configuration.DocumentConfig docxDocument)
        {
            var configuration = projectServiceProvider.GetRequiredService<IConfiguration>();
            var fileSystem = projectServiceProvider.GetRequiredService<IFileProviderPlugin>();
            IDocument document= new DocxDocument(docxDocument.DocumentTitle, docxDocument.DocumentTemplate, configuration);
            logger.Info($"Reading document template: {docxDocument.RoboClerkID}");
            byte[] bytes = fileSystem.ReadAllBytes(fileSystem.Combine(configuration.TemplateDir, docxDocument.DocumentTemplate));
            document.FromStream(new MemoryStream(bytes));

            logger.Info($"Generating document: {docxDocument.RoboClerkID}");
            var contentCreatorFactory = projectServiceProvider.GetRequiredService<IContentCreatorFactory>();
            ProcessDocumentTags(document, docxDocument, contentCreatorFactory);

            logger.Info($"Finished creating document {docxDocument.RoboClerkID}");
            return document;
        }

        private void ProcessDocumentTags(IDocument document, DocumentConfig doc, IContentCreatorFactory factory)
        {
            // Get all tags in the document
            var tags = document.RoboClerkTags.ToList(); // Create a snapshot
            bool anyTagProcessed = false;

            foreach (var tag in tags)
            {
                if (ProcessSingleTag(tag, doc, factory))
                {
                    anyTagProcessed = true;

                    // Process nested tags recursively - this handles all nested levels internally
                    ProcessNestedTagsRecursively(tag, factory);
                }
            }
        }

        private bool ProcessSingleTag(IRoboClerkTag tag, DocumentConfig doc, IContentCreatorFactory factory)
        {
            if (tag.Source == DataSource.Trace)
            {
                logger.Debug($"Trace tag found and added to traceability: {tag.GetParameterOrDefault("ID", "ERROR")}");
                IContentCreator contentCreator = factory.CreateContentCreator(DataSource.Trace, null);
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
                    IContentCreator contentCreator = factory.CreateContentCreator(tag.Source, tag.ContentCreatorID);
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

        private void ProcessNestedTagsRecursively(IRoboClerkTag tag, IContentCreatorFactory factory)
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

                // we currently only support text-based nested tags
                var textBasedNested = RoboClerkTextParser.ExtractRoboClerkTags(currentContent);
                nestedTags.AddRange(textBasedNested.Cast<IRoboClerkTag>());

                // Process each nested tag found at this level using the existing ProcessSingleTag logic
                foreach (var nestedTag in nestedTags)
                {
                    // Create a temporary DocumentConfig for nested processing
                    // This ensures nested tags can be processed independently
                    var tempDoc = new DocumentConfig("temp", "temp", "temp", "temp", "temp");

                    if (ProcessSingleTag(nestedTag, tempDoc, factory))
                    {
                        contentChanged = true;
                    }
                }

                // If content changed, update the parent tag's content and prepare for next iteration
                if (contentChanged)
                {
                    tag.Contents = RoboClerkTextParser.ReInsertRoboClerkTags(currentContent, nestedTags.Cast<RoboClerkTextTag>().ToList()); 
                }

                // Exit if no changes or max nested levels reached
                if (!contentChanged || nestedLevel >= 5)
                    break;

            } while (true);
        }

        public async Task<List<ConfigurationValue>> GetProjectConfigurationAsync(string projectId)
        {
            if (!loadedProjects.TryGetValue(projectId, out var project))
                throw new ArgumentException("SharePoint project not loaded");

            var configuration = project.ProjectServiceProvider.GetRequiredService<IConfiguration>();
            var configValues = new List<ConfigurationValue>
            {
                new("ProjectType", "SharePoint"),
                new("OutputDirectory", configuration.OutputDir),
                new("TemplateDirectory", configuration.TemplateDir),
                new("ProjectRoot", configuration.ProjectRoot),
                new("MediaDirectory", configuration.MediaDir),
                new("LogLevel", configuration.LogLevel),
                new("OutputFormat", configuration.OutputFormat),
                new("PluginConfigDir", configuration.PluginConfigDir)
            };

            // Add data source plugins
            for (int i = 0; i < configuration.DataSourcePlugins.Count; i++)
            {
                configValues.Add(new($"DataSourcePlugin[{i}]", configuration.DataSourcePlugins[i]));
            }

            // Add plugin directories
            for (int i = 0; i < configuration.PluginDirs.Count; i++)
            {
                configValues.Add(new($"PluginDir[{i}]", configuration.PluginDirs[i]));
            }

            return configValues;
        }

        /// <summary>
        /// Gets tag content using RoboClerkDocxTag for OpenXML conversion.
        /// This method expects the Word add-in to provide content control information.
        /// Uses virtual content controls to avoid modifying the original document.
        /// </summary>
        public async Task<TagContentResult> GetTagContentWithContentControlAsync(string projectId, RoboClerkContentControlTagRequest tagRequest)
        {
            if (!loadedProjects.TryGetValue(projectId, out var project))
                throw new ArgumentException("SharePoint project not loaded");

            try
            {
                var contentCreatorFactory = project.ProjectServiceProvider.GetRequiredService<IContentCreatorFactory>();
                var configuration = project.ProjectServiceProvider.GetRequiredService<IConfiguration>();
                
                // Find the appropriate document config
                var docxDocuments = configuration.Documents
                    .Where(d => d.DocumentTemplate.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                var docConfig = docxDocuments.FirstOrDefault(d => d.RoboClerkID == tagRequest.DocumentId);
                if (docConfig == null)
                    return new TagContentResult { Success = false, Error = "Document not found in SharePoint project" };

                // Load the document to get access to the actual content control structure
                if (!project.LoadedDocuments.TryGetValue(tagRequest.DocumentId, out var document) ||
                    document is not DocxDocument docxDocument)
                {
                    // Document not loaded, try to load it
                    try
                    {
                        var processedDocument = ProcessTemplate(project.ProjectServiceProvider, docConfig);
                        project.LoadedDocuments[tagRequest.DocumentId] = processedDocument;
                        docxDocument = (DocxDocument)processedDocument;
                    }
                    catch (Exception ex)
                    {
                        return new TagContentResult { Success = false, Error = $"Failed to load document: {ex.Message}" };
                    }
                }
                else
                {
                    docxDocument = (DocxDocument)document;
                }

                // First try to find an existing RoboClerkDocxTag in the document
                var docxTag = docxDocument.RoboClerkTags
                    .OfType<RoboClerkDocxTag>()
                    .FirstOrDefault(t => t.ContentControlId == tagRequest.ContentControlId);

                // If not found in document, use virtual content control manager
                if (docxTag == null)
                {
                    logger.Info($"Content control {tagRequest.ContentControlId} not found in document, creating virtual tag");
                    
                    // Create or get virtual content control - this doesn't modify the original document
                    var virtualTag = project.ContentControlManager.GetOrCreateContentControl(
                        tagRequest.DocumentId,
                        tagRequest.ContentControlId,
                        tagRequest.RoboClerkTag,
                        configuration
                    );
                    
                    docxTag = virtualTag;
                }
                else
                {
                    logger.Info($"Content control {tagRequest.ContentControlId} found in document, using existing tag");
                }

                // Get content using the content creator
                var contentCreator = contentCreatorFactory.CreateContentCreator(docxTag.Source, docxTag.ContentCreatorID);
                var content = contentCreator.GetContent(docxTag, docConfig);

                // Update the tag content - the GeneratedOpenXml property will handle conversion on-demand
                docxTag.Contents = content;
                
                // Get the raw OpenXML content from the RoboClerkDocxTag
                var openXmlContent = docxTag.GeneratedOpenXml;

                if (string.IsNullOrEmpty(openXmlContent))
                {
                    logger.Warn($"No OpenXML content generated for content control {tagRequest.ContentControlId}");
                    return new TagContentResult { Success = false, Error = "No content generated" };
                }

                logger.Info($"Generated {openXmlContent.Length} characters of OpenXML for content control {tagRequest.ContentControlId}");
                return new TagContentResult
                {
                    Success = true,
                    Content = openXmlContent
                };
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to get tag content for content control: {tagRequest.ContentControlId}");
                return new TagContentResult { Success = false, Error = $"Failed to generate content: {ex.Message}" };
            }
        }

        /// <summary>
        /// Validates that a project is properly configured for Word add-in usage with SharePoint
        /// </summary>
        public bool ValidateProjectForWordAddInAsync(string projectId,LoadProjectRequest request)
        {
            if (!loadedProjects.TryGetValue(projectId, out var project))
                return false;

            try
            {
                // Verify SharePoint path
                if (!IsSharePointPath(project.ProjectPath,request))
                {
                    logger.Warn($"Project path is not a SharePoint URL: {project.ProjectPath}");
                    return false;
                }

                // Verify basic project structure
                if (project.ProjectServiceProvider == null)
                {
                    logger.Warn($"Project service provider missing for project: {projectId}");
                    return false;
                }

                // Check if SharePoint file provider is available and working
                var fileProvider = project.ProjectServiceProvider.GetRequiredService<IFileProviderPlugin>();
                if (!fileProvider.GetType().Name.Contains("SharePoint"))
                {
                    logger.Warn($"SharePoint file provider not available for project: {projectId}");
                    return false;
                }                

                logger.Info($"SharePoint project validated successfully: {projectId}");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to validate SharePoint project for Word add-in: {projectId}");
                return false;
            }
        }

        // if project refresh is full, also refresh data sources, otherwise just re-analyze documents
        public async Task<RefreshResult> RefreshProjectAsync(string projectId, bool full)
        {
            if (!loadedProjects.TryGetValue(projectId, out var project))
                throw new ArgumentException("SharePoint project not loaded");

            try
            {
                IDataSources? newDataSources = null;
                
                if (full)
                {
                    logger.Info($"Full refresh: Refreshing data sources for SharePoint project: {projectId}");
                    
                    // Refresh data sources by recreating them
                    var config = project.ProjectServiceProvider.GetRequiredService<IConfiguration>();
                    newDataSources = dataSourcesFactory.CreateDataSources(config);
                    
                    logger.Info($"Successfully refreshed SharePoint project data sources: {projectId}");
                }
                else
                {
                    logger.Info($"Partial refresh: Skipping data source refresh for SharePoint project: {projectId}");
                }
                
                // Clear virtual tags for the project
                var clearedVirtualTags = project.ContentControlManager.ClearAllVirtualTags();
                logger.Info($"Cleared {clearedVirtualTags} virtual tags for project {projectId}");
                
                // Always rescan and process the project directory (regardless of full flag)
                logger.Info($"Rescanning and processing project directory for SharePoint project: {projectId}");
                
                // Get the current configuration
                var currentConfig = project.ProjectServiceProvider.GetRequiredService<IConfiguration>();
                
                // Get DOCX documents from configuration
                var docxDocuments = currentConfig.Documents
                    .Where(d => d.DocumentTemplate.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                // Clear existing loaded documents if doing a full refresh
                if (full)
                {
                    // Dispose of existing documents before clearing
                    foreach (var document in project.LoadedDocuments.Values.OfType<IDisposable>())
                    {
                        document.Dispose();
                    }
                    project.LoadedDocuments.Clear();
                }

                // Process each document
                foreach (var docxDocument in docxDocuments)
                {
                    try
                    {
                        // For full refresh or if document not already loaded, process it
                        if (full || !project.LoadedDocuments.ContainsKey(docxDocument.RoboClerkID))
                        {
                            logger.Info($"Processing document: {docxDocument.RoboClerkID}");
                            
                            // Remove old document if it exists (for partial refresh)
                            if (project.LoadedDocuments.TryRemove(docxDocument.RoboClerkID, out var oldDoc) && oldDoc is IDisposable disposableOldDoc)
                            {
                                disposableOldDoc.Dispose();
                            }
                            
                            // Create new service provider if we have new data sources
                            var serviceProviderToUse = project.ProjectServiceProvider;
                            if (newDataSources != null)
                            {
                                // We need to create a new service provider with the updated data sources
                                var sharePointFileProvider = project.ProjectServiceProvider.GetRequiredService<IFileProviderPlugin>();
                                serviceProviderToUse = CreateProjectServiceProvider(sharePointFileProvider, currentConfig, newDataSources);
                            }
                            
                            var processedDocument = ProcessTemplate(serviceProviderToUse, docxDocument);
                            project.LoadedDocuments[docxDocument.RoboClerkID] = processedDocument;
                        }
                        else
                        {
                            logger.Info($"Document already loaded, skipping: {docxDocument.RoboClerkID}");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex, $"Error processing document {docxDocument.RoboClerkID}: {ex.Message}");
                    }
                }

                // Update the project context with new service provider if we created one
                if (newDataSources != null)
                {
                    var sharePointFileProvider = project.ProjectServiceProvider.GetRequiredService<IFileProviderPlugin>();
                    var newServiceProvider = CreateProjectServiceProvider(sharePointFileProvider, currentConfig, newDataSources);
                    
                    // Create updated project context
                    var updatedProject = project with 
                    { 
                        ProjectServiceProvider = newServiceProvider,
                        LastUpdated = DateTime.UtcNow
                    };
                    loadedProjects[projectId] = updatedProject;
                }
                else
                {
                    // Update the LastUpdated timestamp even for partial refresh
                    var updatedProject = project with { LastUpdated = DateTime.UtcNow };
                    loadedProjects[projectId] = updatedProject;
                }
                
                logger.Info($"Successfully refreshed SharePoint project: {projectId}");
                return new RefreshResult { Success = true };
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to refresh SharePoint project: {projectId}");
                return new RefreshResult { Success = false, Error = $"Failed to refresh project: {ex.Message}" };
            }
        }

        /// <summary>
        /// Refreshes a specific document and clears its virtual content controls.
        /// This is useful when only one document needs to be updated without affecting others.
        /// </summary>
        /// <param name="projectId">The project ID</param>
        /// <param name="documentId">The document ID to refresh</param>
        /// <returns>RefreshResult indicating success or failure</returns>
        public async Task<RefreshResult> RefreshDocumentAsync(string projectId, string documentId)
        {
            if (!loadedProjects.TryGetValue(projectId, out var project))
                throw new ArgumentException("SharePoint project not loaded");

            try
            {
                logger.Info($"Refreshing document {documentId} in SharePoint project: {projectId}");
                
                var configuration = project.ProjectServiceProvider.GetRequiredService<IConfiguration>();
                
                // Find the document config
                var docxDocuments = configuration.Documents
                    .Where(d => d.DocumentTemplate.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                var docConfig = docxDocuments.FirstOrDefault(d => d.RoboClerkID == documentId);
                
                if (docConfig == null)
                {
                    return new RefreshResult { Success = false, Error = $"Document {documentId} not found in project configuration" };
                }

                // Clear virtual tags for this document before refreshing
                var clearedVirtualTags = project.ContentControlManager.ClearVirtualTagsForDocument(documentId);
                logger.Info($"Cleared {clearedVirtualTags} virtual tags for document {documentId}");

                // Remove the existing loaded document
                project.LoadedDocuments.TryRemove(documentId, out var oldDocument);
                if (oldDocument is IDisposable disposableDoc)
                {
                    disposableDoc.Dispose();
                }

                // Process the document fresh
                var processedDocument = ProcessTemplate(project.ProjectServiceProvider, docConfig);
                project.LoadedDocuments[documentId] = processedDocument;
                
                // Update the project's last updated timestamp
                var updatedProject = project with { LastUpdated = DateTime.UtcNow };
                loadedProjects[projectId] = updatedProject;
                
                logger.Info($"Successfully refreshed document {documentId} in SharePoint project: {projectId}");
                return new RefreshResult { Success = true };
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to refresh document {documentId} in SharePoint project: {projectId}");
                return new RefreshResult { Success = false, Error = $"Failed to refresh document: {ex.Message}" };
            }
        }

        public async Task UnloadProjectAsync(string projectId)
        {
            if (loadedProjects.TryRemove(projectId, out var project))
            {
                // Dispose of any disposable documents
                foreach (var document in project.LoadedDocuments.Values.OfType<IDisposable>())
                {
                    document.Dispose();
                }
                
                logger.Info($"Unloaded SharePoint project: {projectId}");
            }
        }



        /// <summary>
        /// Determines if the given path is a SharePoint path.
        /// Validates based on either full SharePoint URLs or drive-relative paths with SharePoint context.
        /// </summary>
        private bool IsSharePointPath(string path, LoadProjectRequest? request = null)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            // Check if we have SharePoint context (drive ID provided)
            if (request != null && !string.IsNullOrEmpty(request.SPDriveId))
            {
                // For SharePoint drive operations, paths are relative to the drive root
                return path.StartsWith("/");
            }

            // Fallback to checking for full SharePoint URLs
            return path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                   (path.Contains(".sharepoint.com", StringComparison.OrdinalIgnoreCase) ||
                    path.Contains("sharepoint", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Generates a unique, deterministic project identifier from driveId and config file path
        /// </summary>
        private static string GenerateProjectIdentifier(string driveId, string configFilePath)
        {
            // Normalize the path to ensure consistency
            var normalizedPath = configFilePath.Trim('/').Replace('\\', '/');
            var combinedString = $"{driveId}:{normalizedPath}";
            
            // Generate SHA256 hash for uniqueness and consistency
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combinedString));
            
            // Convert to hex string and take first 16 characters for readability
            var hashString = Convert.ToHexString(hashBytes)[..16].ToLowerInvariant();
            
            return $"sp-{hashString}"; // Prefix to indicate SharePoint project
        }

        private async Task<IFileProviderPlugin?> CreateSharePointFileProviderAsync(LoadProjectRequest request)
        {
            try
            {
                logger.Info($"Creating SharePoint file provider for drive: {request.SPDriveId}");

                // Get the plugin loader to load SharePoint plugin
                var pluginLoader = serviceProvider.GetRequiredService<IPluginLoader>();

                // Create a temporary configuration with the SharePoint parameters
                var tempConfig = serviceProvider.GetRequiredService<IConfiguration>() as Configuration.Configuration;
                if (tempConfig == null)
                {
                    logger.Error("Base configuration is not of expected type");
                    return null;
                }
                tempConfig = tempConfig.Clone();  //make sure we're not modifying the global config
                tempConfig.AddOrUpdateCommandLineOption("SPDriveId", request.SPDriveId);
                if (!string.IsNullOrEmpty(request.SPSiteUrl))
                {
                    tempConfig.AddOrUpdateCommandLineOption("SPSiteUrl", request.SPSiteUrl);
                }

                // Load the SharePoint file provider plugin
                IFileProviderPlugin? sharePointPlugin = null;

                // Try loading from each plugin directory
                foreach (var pluginDir in tempConfig.PluginDirs)
                {
                    try
                    {
                        sharePointPlugin = pluginLoader.LoadByName<IFileProviderPlugin>(
                            pluginDir: pluginDir,
                            typeName: "SharePointFileProviderPlugin",
                            configureGlobals: sc =>
                            {
                                sc.AddSingleton<IFileProviderPlugin>(new LocalFileSystemPlugin(fileSystem));
                                sc.AddSingleton(tempConfig);
                            });

                        if (sharePointPlugin != null)
                        {
                            logger.Info($"Successfully loaded SharePoint file provider from: {pluginDir}");
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Warn($"Failed to load SharePoint plugin from {pluginDir}: {ex.Message}");
                    }
                }

                if (sharePointPlugin == null)
                {
                    logger.Error("Could not load SharePoint file provider plugin from any plugin directory");
                    return null;
                }

                // Initialize the plugin with the project-specific configuration
                sharePointPlugin.InitializePlugin(tempConfig);

                return sharePointPlugin;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to create SharePoint file provider");
                return null;
            }
        }

        /// <summary>
        /// Creates a project-specific service provider that uses the SharePoint file provider
        /// </summary>
        private IServiceProvider CreateProjectServiceProvider(IFileProviderPlugin sharePointFileProvider, IConfiguration configuration, IDataSources? dataSources = null)
        {
            var services = new ServiceCollection();

            // Project-specific file provider
            services.AddSingleton(sharePointFileProvider);

            // Project-specific configuration
            services.AddSingleton(configuration);

            // Data sources factory (using project-specific file provider)
            services.AddSingleton<IDataSourcesFactory>(provider =>
            {
                return new DataSourcesFactory(provider);
            });

            // Get plugin loader from main service provider (doesn't depend on project config)
            services.AddSingleton(serviceProvider.GetRequiredService<IPluginLoader>());

            // Create project-specific ITraceabilityAnalysis with project-specific configuration
            services.AddSingleton<ITraceabilityAnalysis>(provider =>
            {
                var projectConfig = provider.GetRequiredService<IConfiguration>();
                return new RoboClerk.TraceabilityAnalysis(projectConfig);
            });

            // Register all content creators dynamically - same pattern as main application
            RegisterContentCreators(services);

            // Create project-specific IContentCreatorFactory with project-specific configuration
            services.AddSingleton<IContentCreatorFactory>(provider =>
                new ContentCreatorFactory(provider, provider.GetRequiredService<ITraceabilityAnalysis>()));

            // Add data sources - either provided ones or create new ones
            if (dataSources != null)
            {
                services.AddSingleton(dataSources);
            }
            else
            {
                services.AddSingleton<IDataSources>(provider =>
                {
                    var factory = provider.GetRequiredService<IDataSourcesFactory>();
                    var config = provider.GetRequiredService<IConfiguration>();
                    return factory.CreateDataSources(config);
                });

            }

            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Registers all content creators in the service collection - same pattern as main application
        /// </summary>
        private void RegisterContentCreators(IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            try
            {
                // Get the assembly containing the content creators using the fileSystem object
                var currentDir = fileSystem.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
                var coreAssemblyPath = fileSystem.Path.Combine(currentDir, "RoboClerk.Core.dll");
                
                if (!fileSystem.File.Exists(coreAssemblyPath))
                {
                    // Try alternative paths for different deployment scenarios
                    coreAssemblyPath = fileSystem.Path.Combine(AppContext.BaseDirectory, "RoboClerk.Core.dll");
                }
                
                // Verify the assembly file exists before attempting to load
                if (!fileSystem.File.Exists(coreAssemblyPath))
                {
                    logger.Warn($"RoboClerk.Core.dll not found at expected locations. Content creators may not be available.");
                    return;
                }

                var assembly = Assembly.LoadFrom(coreAssemblyPath);

                // Find all types that implement IContentCreator
                var contentCreatorTypes = assembly.GetTypes()
                    .Where(t => typeof(IContentCreator).IsAssignableFrom(t) && 
                               !t.IsInterface && 
                               !t.IsAbstract &&
                               !t.IsGenericType)
                    .ToList();

                logger.Debug($"Found {contentCreatorTypes.Count} content creator types to register for project");

                foreach (var type in contentCreatorTypes)
                {
                    try
                    {
                        // Register each content creator as transient
                        services.AddTransient(type);
                        logger.Debug($"Registered content creator for project: {type.Name}");
                    }
                    catch (Exception ex)
                    {
                        logger.Warn($"Failed to register content creator {type.Name} for project: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error registering content creators for project");
                // Don't throw here - let the application continue with what it has
            }
        }

        /// <summary>
        /// Gets virtual tag statistics for debugging purposes.
        /// Returns the count of virtual tags per document in the project.
        /// </summary>
        /// <param name="projectId">The project ID</param>
        /// <returns>Dictionary with document IDs and their virtual tag counts</returns>
        public async Task<Dictionary<string, int>> GetVirtualTagStatisticsAsync(string projectId)
        {
            if (!loadedProjects.TryGetValue(projectId, out var project))
                throw new ArgumentException("SharePoint project not loaded");

            return project.ContentControlManager.GetVirtualTagStatistics();
        }
    }

}