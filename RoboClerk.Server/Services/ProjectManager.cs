using AngleSharp.Io;
using DocumentFormat.OpenXml;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoboClerk.ContentCreators;
using RoboClerk.Core;
using RoboClerk.Core.ASCIIDOCSupport;
using RoboClerk.Core.Configuration;
using RoboClerk.Core.DocxSupport;
using RoboClerk.Core.FileProviders;
using RoboClerk.Server.Models;
using RoboClerk.SharePointFileProvider;
using System.Collections;
using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Tomlyn;
using Tomlyn.Model;
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

        //BELOW ARE ALL THE MAIN PROJECT MANAGEMENT METHODS
        public async Task<ProjectLoadResult> LoadProjectAsync(LoadProjectRequest request)
        {
            try
            {
                // Generate deterministic project ID from driveId and config path
                var projectConfigPath = $"{request.ProjectPath}/RoboClerkConfig/projectConfig.toml";
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
        /// Updates the project configuration file with new values
        /// </summary>
        /// <param name="projectId">The project ID</param>
        /// <param name="configUpdates">Dictionary of configuration keys and their new values</param>
        /// <returns>Result indicating success or failure</returns>
        public async Task<ConfigurationUpdateResult> UpdateProjectConfigurationAsync(string projectId, Dictionary<string, object> configUpdates)
        {
            if (!loadedProjects.TryGetValue(projectId, out var project))
                throw new ArgumentException("SharePoint project not loaded");

            try
            {
                logger.Info($"Updating project configuration for: {projectId}");

                var fileProvider = project.ProjectServiceProvider.GetRequiredService<IFileProviderPlugin>();
                var projectConfigPath = fileProvider.Combine(project.ProjectPath, "RoboClerkConfig", "projectConfig.toml");

                // Read current configuration
                var currentContent = fileProvider.ReadAllText(projectConfigPath);
                var toml = Tomlyn.Toml.Parse(currentContent).ToModel();

                var updatedKeys = new List<string>();
                bool requiresReload = false;

                // Apply updates to the TOML structure
                foreach (var update in configUpdates)
                {
                    if (ApplyConfigurationUpdate(toml, update.Key, update.Value))
                    {
                        updatedKeys.Add(update.Key);
                        
                        // Check if this change requires a project reload
                        if (IsReloadRequiredForKey(update.Key))
                        {
                            requiresReload = true;
                        }
                    }
                }

                if (updatedKeys.Any())
                {
                    // Write updated configuration back to SharePoint
                    var updatedContent = Tomlyn.Toml.FromModel(toml);
                    fileProvider.WriteAllText(projectConfigPath, updatedContent);

                    logger.Info($"Updated {updatedKeys.Count} configuration keys for project: {projectId}");

                    // If reload is required, refresh the project
                    if (requiresReload)
                    {
                        logger.Info($"Configuration changes require project reload for: {projectId}");
                        await RefreshProjectDocumentsAsync(projectId, false);
                    }
                }

                return new ConfigurationUpdateResult
                {
                    Success = true,
                    UpdatedKeys = updatedKeys,
                    RequiresProjectReload = requiresReload
                };
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to update project configuration for: {projectId}");
                return new ConfigurationUpdateResult 
                { 
                    Success = false, 
                    Error = $"Failed to update configuration: {ex.Message}" 
                };
            }
        }

        /// <summary>
        /// Gets the raw project configuration content as TOML
        /// </summary>
        /// <param name="projectId">The project ID</param>
        /// <returns>The raw TOML configuration content</returns>
        public async Task<string> GetProjectConfigurationContentAsync(string projectId)
        {
            if (!loadedProjects.TryGetValue(projectId, out var project))
                throw new ArgumentException("SharePoint project not loaded");

            var fileProvider = project.ProjectServiceProvider.GetRequiredService<IFileProviderPlugin>();
            var projectConfigPath = fileProvider.Combine(project.ProjectPath, "RoboClerkConfig", "projectConfig.toml");

            return fileProvider.ReadAllText(projectConfigPath);
        }

        /// <summary>
        /// Validates proposed configuration changes without applying them
        /// </summary>
        /// <param name="projectId">The project ID</param>
        /// <param name="configUpdates">Dictionary of configuration keys and their new values</param>
        /// <returns>Validation result with any errors or warnings</returns>
        public async Task<ConfigurationValidationResult> ValidateConfigurationUpdatesAsync(string projectId, Dictionary<string, object> configUpdates)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            // Validate each configuration update
            foreach (var update in configUpdates)
            {
                var validation = ValidateConfigurationKey(update.Key, update.Value);
                errors.AddRange(validation.Errors);
                warnings.AddRange(validation.Warnings);
            }

            return new ConfigurationValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors,
                Warnings = warnings
            };
        }

        /// <summary>
        /// Gets all DOCX template files in the template directory that are not configured as documents
        /// </summary>
        /// <param name="projectId">The project ID</param>
        /// <param name="includeConfiguredTemplates">Whether to include templates that are already configured as documents</param>
        /// <returns>Result containing available template files information</returns>
        public async Task<AvailableTemplateFilesResult> GetAvailableTemplateFilesAsync(string projectId, bool includeConfiguredTemplates = false)
        {
            if (!loadedProjects.TryGetValue(projectId, out var project))
                throw new ArgumentException("SharePoint project not loaded");

            try
            {
                logger.Info($"Getting available template files for project: {projectId}");

                var configuration = project.ProjectServiceProvider.GetRequiredService<IConfiguration>();
                var fileProvider = project.ProjectServiceProvider.GetRequiredService<IFileProviderPlugin>();

                // Get all configured document templates (only DOCX)
                var configuredTemplates = configuration.Documents
                    .Where(d => d.DocumentTemplate.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                    .Select(d => d.DocumentTemplate.ToLowerInvariant())
                    .ToHashSet();

                // Get all template files from the template directory
                var templateFiles = new List<TemplateFileInfo>();
                var templateDir = configuration.TemplateDir;

                try
                {
                    var allTemplateFiles = GetTemplateFilesRecursively(fileProvider, templateDir, templateDir);
                    
                    foreach (var filePath in allTemplateFiles)
                    {
                        try
                        {
                            var fileName = fileProvider.GetFileName(filePath);
                            var relativePath = fileProvider.GetRelativePath(templateDir, filePath);
                            var isDocx = fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase);
                            var isConfigured = configuredTemplates.Contains(fileName.ToLowerInvariant());

                            // Skip if we only want unconfigured templates and this one is configured
                            if (!includeConfiguredTemplates && isConfigured)
                                continue;

                            // Only include DOCX files for template section insertion
                            if (!isDocx)
                                continue;

                            var templateFileInfo = new TemplateFileInfo
                            {
                                FileName = fileName,
                                RelativePath = relativePath,
                                FullPath = filePath,
                                FileSizeBytes = fileProvider.GetFileSize(filePath),
                                LastModified = fileProvider.GetLastWriteTime(filePath),
                                IsDocx = isDocx
                            };

                            templateFiles.Add(templateFileInfo);
                        }
                        catch (Exception ex)
                        {
                            logger.Warn(ex, $"Error processing template file {filePath}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"Error accessing template directory {templateDir}: {ex.Message}");
                    return new AvailableTemplateFilesResult
                    {
                        Success = false,
                        Error = $"Failed to access template directory: {ex.Message}"
                    };
                }

                var totalTemplateFiles = templateFiles.Count + configuredTemplates.Count;
                var unconfiguredCount = templateFiles.Count(t => !configuredTemplates.Contains(t.FileName.ToLowerInvariant()));

                logger.Info($"Found {templateFiles.Count} available template files for project: {projectId}");

                return new AvailableTemplateFilesResult
                {
                    Success = true,
                    AvailableTemplateFiles = templateFiles.OrderBy(t => t.FileName).ToList(),
                    TotalTemplateFiles = totalTemplateFiles,
                    ConfiguredDocuments = configuredTemplates.Count,
                    UnconfiguredTemplateFiles = unconfiguredCount
                };
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to get available template files for project: {projectId}");
                return new AvailableTemplateFilesResult
                {
                    Success = false,
                    Error = $"Failed to get available template files: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Refreshes the data sources for a project by getting the existing IDataSources instance 
        /// from the project's service provider and calling RefreshDataSources on it.
        /// </summary>
        /// <param name="projectId">The project ID</param>
        /// <returns>RefreshResult indicating success or failure</returns>
        public async Task<RefreshResult> RefreshProjectDataSourcesAsync(string projectId)
        {
            if (!loadedProjects.TryGetValue(projectId, out var project))
                throw new ArgumentException("SharePoint project not loaded");

            try
            {
                logger.Info($"Refreshing data sources for SharePoint project: {projectId}");

                // Get the data sources from the project's service provider
                var dataSources = project.ProjectServiceProvider.GetRequiredService<IDataSources>();
                
                // Refresh all available data sources
                dataSources.RefreshDataSources();
                
                logger.Info($"Successfully refreshed data sources for SharePoint project: {projectId}");
                return new RefreshResult { Success = true };
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to refresh data sources for SharePoint project: {projectId}");
                return new RefreshResult { Success = false, Error = $"Failed to refresh data sources: {ex.Message}" };
            }
        }

        /// <summary>
        /// Refreshes the documents for a project by reloading the project configuration file,
        /// updating the configuration object, and synchronizing the loaded documents with the
        /// updated document list from the configuration.
        /// </summary>
        /// <param name="projectId">The project ID</param>
        /// <param name="processTags">Whether to actually process the tags in the documents</param>"
        /// <returns>RefreshResult indicating success or failure</returns>
        public async Task<RefreshResult> RefreshProjectDocumentsAsync(string projectId, bool processTags)
        {
            if (!loadedProjects.TryGetValue(projectId, out var project))
                throw new ArgumentException("SharePoint project not loaded");

            try
            {
                logger.Info($"Refreshing documents for SharePoint project: {projectId}");

                // Get the current configuration and file provider
                var currentConfig = project.ProjectServiceProvider.GetRequiredService<IConfiguration>();
                var fileProvider = project.ProjectServiceProvider.GetRequiredService<IFileProviderPlugin>();

                // Determine the project config path
                var projectConfigPath = fileProvider.Combine(project.ProjectPath, "RoboClerkConfig", "projectConfig.toml");
                
                logger.Info($"Reloading project configuration from: {projectConfigPath}");

                // Create a new configuration by reloading the project config file
                var baseConfiguration = serviceProvider.GetRequiredService<IConfiguration>();
                if (baseConfiguration is not RoboClerk.Configuration.Configuration concreteConfig)
                {
                    return new RefreshResult { Success = false, Error = "Configuration service is not of expected type" };
                }

                // Clone the base configuration and reload the project config
                var updatedConfiguration = RoboClerk.Configuration.ConfigurationBuilder
                    .FromExisting(concreteConfig.Clone())
                    .WithProjectConfig(fileProvider, projectConfigPath)
                    .Build();

                // Preserve command line options and project ID from current config
                if (currentConfig.HasCommandLineOption("SPDriveId"))
                {
                    updatedConfiguration.AddOrUpdateCommandLineOption("SPDriveId", currentConfig.GetCommandLineOption("SPDriveId"));
                }
                if (currentConfig.HasCommandLineOption("SPSiteUrl"))
                {
                    updatedConfiguration.AddOrUpdateCommandLineOption("SPSiteUrl", currentConfig.GetCommandLineOption("SPSiteUrl"));
                }
                updatedConfiguration.ProjectID = currentConfig.ProjectID;

                // Create new service provider with updated configuration
                var dataSources = project.ProjectServiceProvider.GetRequiredService<IDataSources>();
                var newServiceProvider = CreateProjectServiceProvider(fileProvider, updatedConfiguration, dataSources);

                // Get current and updated DOCX document lists
                var currentDocxDocuments = currentConfig.Documents
                    .Where(d => d.DocumentTemplate.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(d => d.RoboClerkID, d => d);

                var updatedDocxDocuments = updatedConfiguration.Documents
                    .Where(d => d.DocumentTemplate.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(d => d.RoboClerkID, d => d);

                // Remove documents that are no longer in the configuration
                var documentsToRemove = currentDocxDocuments.Keys.Except(updatedDocxDocuments.Keys).ToList();
                foreach (var docId in documentsToRemove)
                {
                    logger.Info($"Removing document no longer in configuration: {docId}");
                    
                    // Clear virtual tags for the document being removed
                    var clearedVirtualTags = project.ContentControlManager.ClearVirtualTagsForDocument(docId);
                    if (clearedVirtualTags > 0)
                    {
                        logger.Info($"Cleared {clearedVirtualTags} virtual tags for removed document {docId}");
                    }
                    
                    // Remove and dispose the document
                    if (project.LoadedDocuments.TryRemove(docId, out var removedDoc) && removedDoc is IDisposable disposableDoc)
                    {
                        disposableDoc.Dispose();
                    }
                }

                // Add or update documents that are new or changed in the configuration
                var documentsToAddOrUpdate = updatedDocxDocuments.Keys.ToList();
                foreach (var docId in documentsToAddOrUpdate)
                {
                    try
                    {
                        var docConfig = updatedDocxDocuments[docId];
                        
                        // Clear virtual tags for documents being replaced/updated
                        var clearedVirtualTags = project.ContentControlManager.ClearVirtualTagsForDocument(docId);
                        if (clearedVirtualTags > 0)
                        {
                            logger.Info($"Cleared {clearedVirtualTags} virtual tags for document {docId} before refresh");
                        }
                        
                        // Check if document already exists and needs to be replaced
                        if (project.LoadedDocuments.TryRemove(docId, out var existingDoc) && existingDoc is IDisposable disposableExistingDoc)
                        {
                            logger.Info($"Replacing existing document with updated configuration: {docId}");
                            disposableExistingDoc.Dispose();
                        }
                        else
                        {
                            logger.Info($"Loading new document from configuration: {docId}");
                        }

                        // Load the document with the new service provider
                        var processedDocument = ProcessTemplate(newServiceProvider, docConfig, processTags);
                        project.LoadedDocuments[docId] = processedDocument;
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex, $"Error loading/updating document {docId}: {ex.Message}");
                    }
                }

                // Update the project context with the new service provider and configuration
                var updatedProject = project with
                {
                    ProjectServiceProvider = newServiceProvider,
                    LastUpdated = DateTime.UtcNow
                };
                loadedProjects[projectId] = updatedProject;

                logger.Info($"Successfully refreshed documents for SharePoint project: {projectId}");
                return new RefreshResult { Success = true };
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to refresh documents for SharePoint project: {projectId}");
                return new RefreshResult { Success = false, Error = $"Failed to refresh documents: {ex.Message}" };
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


        /////////////////////////////////////////////////////////////////////////
        // BELOW ARE ALL THE SUPPORTING PRIVATE METHODS FOR PROJECT MANAGEMENT //
        /////////////////////////////////////////////////////////////////////////
        

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

        /// <summary>
        /// Applies a configuration update to the TOML structure
        /// </summary>
        /// <param name="toml">The TOML table to update</param>
        /// <param name="key">The configuration key (supports nested keys with dot notation)</param>
        /// <param name="value">The new value to set</param>
        /// <returns>True if the update was applied successfully</returns>
        private bool ApplyConfigurationUpdate(TomlTable toml, string key, object value)
        {
            try
            {
                // Handle nested keys (e.g., "Truth.SystemRequirement.name")
                var keyParts = key.Split('.');
                TomlTable currentTable = toml;

                // Navigate to the parent table
                for (int i = 0; i < keyParts.Length - 1; i++)
                {
                    if (!currentTable.ContainsKey(keyParts[i]))
                    {
                        currentTable[keyParts[i]] = new TomlTable();
                    }
                    currentTable = (TomlTable)currentTable[keyParts[i]];
                }

                // Set the final value
                var finalKey = keyParts[keyParts.Length - 1];
                
                // Convert value to appropriate TOML type
                currentTable[finalKey] = ConvertToTomlValue(value);
                
                return true;
            }
            catch (Exception ex)
            {
                logger.Warn(ex, $"Failed to apply configuration update for key: {key}");
                return false;
            }
        }

        /// <summary>
        /// Converts a .NET object to an appropriate TOML value
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <returns>The converted TOML value</returns>
        private object ConvertToTomlValue(object? value)
        {
            if (value is null)
                return string.Empty;

            return value switch
            {
                string s => s,
                bool b => b,

                int i => i,
                long l => l,
                short sh => (int)sh,
                byte by => (int)by,
                float f => (double)f,
                double d => d,
                decimal m => (double)m,

                IDictionary<string, object?> dict => CreateTomlTable(dict),
                IEnumerable enumerable => CreateTomlArray(enumerable),

                _ => value.ToString() ?? string.Empty
            };
        }

        private TomlArray CreateTomlArray(IEnumerable enumerable)
        {
            // Create an empty TomlArray and populate it manually
            var arr = new TomlArray();
            foreach (var item in enumerable)
            {
                arr.Add(ConvertToTomlValue(item));
            }
            return arr;
        }

        private TomlTable CreateTomlTable(IDictionary<string, object?> dict)
        {
            var table = new TomlTable();
            foreach (var kvp in dict)
            {
                table[kvp.Key] = ConvertToTomlValue(kvp.Value);
            }
            return table;
        }

        /// <summary>
        /// Determines if a configuration key change requires a project reload
        /// </summary>
        /// <param name="key">The configuration key</param>
        /// <returns>True if the key change requires a project reload</returns>
        private bool IsReloadRequiredForKey(string key)
        {
            // Keys that require project reload when changed
            var reloadRequiredKeys = new[]
            {
                "TemplateDirectory",
                "ProjectRoot",
                "DataSourcePlugin",
                "AISystemPlugin",
                "MediaDirectory"
            };

            return reloadRequiredKeys.Any(k => key.StartsWith(k, StringComparison.OrdinalIgnoreCase)) ||
                   key.StartsWith("Document.", StringComparison.OrdinalIgnoreCase) ||
                   key.StartsWith("Truth.", StringComparison.OrdinalIgnoreCase) ||
                   key.StartsWith("TraceConfig.", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Validates a configuration key and value
        /// </summary>
        /// <param name="key">The configuration key</param>
        /// <param name="value">The value to validate</param>
        /// <returns>Validation result with errors and warnings</returns>
        private ConfigurationValidationResult ValidateConfigurationKey(string key, object value)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            // Add validation logic for specific keys
            switch (key.Split('.')[0].ToLowerInvariant())
            {
                case "templatedirectory":
                case "outputdirectory":
                case "projectroot":
                case "mediadirectory":
                    if (string.IsNullOrWhiteSpace(value?.ToString()))
                    {
                        errors.Add($"Directory path cannot be empty for {key}");
                    }
                    break;
                    
                case "datasourceplugin":
                    if (value is not (string[] or List<string>))
                    {
                        errors.Add($"DataSourcePlugin must be an array of strings");
                    }
                    break;
                    
                case "truth":
                    // Validate truth entity structure
                    // Could add more specific validation here based on the expected structure
                    break;
                    
                case "document":
                    // Validate document configuration structure
                    // Could add more specific validation here based on the expected structure
                    break;
            }

            return new ConfigurationValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors,
                Warnings = warnings
            };
        }

        private IDocument ProcessTemplate(IServiceProvider projectServiceProvider, DocumentConfig docxDocument, bool processTags=false)
        {
            var configuration = projectServiceProvider.GetRequiredService<IConfiguration>();
            var fileSystem = projectServiceProvider.GetRequiredService<IFileProviderPlugin>();
            IDocument document= new DocxDocument(docxDocument.DocumentTitle, docxDocument.DocumentTemplate, configuration);
            logger.Info($"Reading document template: {docxDocument.RoboClerkID}");
            byte[] bytes = fileSystem.ReadAllBytes(fileSystem.Combine(configuration.TemplateDir, docxDocument.DocumentTemplate));
            document.FromStream(new MemoryStream(bytes));

            if (!processTags)
            {
                return document;
            }

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

            foreach (var tag in tags)
            {
                if (ProcessSingleTag(tag, doc, factory))
                {
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

        /// <summary>
        /// Recursively gets all files from a directory using the file provider
        /// </summary>
        /// <param name="fileProvider">The file provider to use</param>
        /// <param name="directoryPath">The directory path to scan</param>
        /// <param name="rootPath">The root path for calculating relative paths</param>
        /// <returns>List of file paths</returns>
        private List<string> GetTemplateFilesRecursively(IFileProviderPlugin fileProvider, string directoryPath, string rootPath)
        {
            var files = new List<string>();
            
            try
            {
                if (!fileProvider.DirectoryExists(directoryPath))
                {
                    logger.Warn($"Template directory does not exist: {directoryPath}");
                    return files;
                }

                // Get all DOCX files in the template directory
                var docxFiles = fileProvider.GetFiles(directoryPath, "*.docx", SearchOption.AllDirectories);
                files.AddRange(docxFiles);
            }
            catch (Exception ex)
            {
                logger.Warn(ex, $"Error enumerating directory {directoryPath}: {ex.Message}");
            }

            return files;
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
                var tempConfig = serviceProvider.GetRequiredService<IConfiguration>() as RoboClerk.Configuration.Configuration;
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
        /// Creates a project-specific service provider that uses the SharePoint file provider for configuration/templates
        /// and a smart file provider that routes based on path prefixes
        /// </summary>
        private IServiceProvider CreateProjectServiceProvider(IFileProviderPlugin sharePointFileProvider, IConfiguration configuration, IDataSources? dataSources = null)
        {
            var services = new ServiceCollection();

            // Create the smart file provider with local as default
            var localFileProvider = new LocalFileSystemPlugin(fileSystem);
            var smartProvider = new RoboClerk.Core.FileProviders.SmartFileProviderPlugin(localFileProvider);
            
            // Register SharePoint provider for sp:// prefixed paths
            smartProvider.RegisterProvider(sharePointFileProvider);
            
            logger.Info($"Smart file provider initialized: local (default), SharePoint ({sharePointFileProvider.GetPathPrefix()})");

            // Register ONLY the smart provider - it handles all routing
            services.AddSingleton<IFileProviderPlugin>(smartProvider);

            // Project-specific configuration
            services.AddSingleton(configuration);

            // Data sources factory
            services.AddSingleton<IDataSourcesFactory>(provider =>
            {
                return new DataSourcesFactory(provider);
            });

            // Get plugin loader from main service provider
            services.AddSingleton(serviceProvider.GetRequiredService<IPluginLoader>());

            // Create project-specific ITraceabilityAnalysis
            services.AddSingleton<ITraceabilityAnalysis>(provider =>
            {
                var projectConfig = provider.GetRequiredService<IConfiguration>();
                return new RoboClerk.TraceabilityAnalysis(projectConfig);
            });

            // Register all content creators dynamically
            RegisterContentCreators(services);

            // Create project-specific IContentCreatorFactory
            services.AddSingleton<IContentCreatorFactory>(provider =>
                new ContentCreatorFactory(provider, provider.GetRequiredService<ITraceabilityAnalysis>()));

            // Add data sources - either provided ones or create new ones via factory
            if (dataSources != null)
            {
                services.AddSingleton(dataSources);
            }
            else
            {
                services.AddSingleton<IDataSources>(provider =>
                {
                    // Use factory to create data sources - it handles routing automatically via smart provider
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
    }
}