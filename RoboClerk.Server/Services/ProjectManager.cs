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
                if (!IsSharePointPath(request.ProjectPath))
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
                
                try
                {
                    foreach (var docxDocument in docxDocuments)
                    {
                        projectContext.LoadedDocuments[""] = ProcessTemplate(projectServiceProvider,docxDocument);
                    }
                }
                catch (Exception ex)
                {
                    logger.Warn(ex, $"Error processing documents in project: {ex}");
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

            var configValues = new List<ConfigurationValue>
            {
                new("ProjectType", "SharePoint"),
                new("OutputDirectory", project.Configuration.OutputDir),
                new("TemplateDirectory", project.Configuration.TemplateDir),
                new("ProjectRoot", project.Configuration.ProjectRoot),
                new("MediaDirectory", project.Configuration.MediaDir),
                new("LogLevel", project.Configuration.LogLevel),
                new("OutputFormat", project.Configuration.OutputFormat),
                new("PluginConfigDir", project.Configuration.PluginConfigDir)
            };

            // Add data source plugins
            for (int i = 0; i < project.Configuration.DataSourcePlugins.Count; i++)
            {
                configValues.Add(new($"DataSourcePlugin[{i}]", project.Configuration.DataSourcePlugins[i]));
            }

            // Add plugin directories
            for (int i = 0; i < project.Configuration.PluginDirs.Count; i++)
            {
                configValues.Add(new($"PluginDir[{i}]", project.Configuration.PluginDirs[i]));
            }

            return configValues;
        }

        /// <summary>
        /// Gets tag content using RoboClerkDocxTag for OpenXML conversion.
        /// This method expects the Word add-in to provide content control information.
        /// </summary>
        public async Task<TagContentResult> GetTagContentWithContentControlAsync(string projectId, RoboClerkContentControlTagRequest tagRequest)
        {
            if (!loadedProjects.TryGetValue(projectId, out var project))
                throw new ArgumentException("SharePoint project not loaded");

            try
            {
                var contentCreatorFactory = serviceProvider.GetRequiredService<IContentCreatorFactory>();
                
                // Find the appropriate document config
                var docConfig = project.DocxDocuments.FirstOrDefault(d => d.RoboClerkID == tagRequest.DocumentId);
                if (docConfig == null)
                    return new TagContentResult { Success = false, Error = "Document not found in SharePoint project" };

                // Load the document to get access to the actual content control
                if (!project.LoadedDocuments.TryGetValue(tagRequest.DocumentId, out var document) || 
                    document is not DocxDocument docxDocument)
                {
                    var loadResult = await LoadDocumentAsync(projectId, tagRequest.DocumentId);
                    if (!loadResult.Success)
                        return new TagContentResult { Success = false, Error = loadResult.Error };
                    
                    docxDocument = (DocxDocument)project.LoadedDocuments[tagRequest.DocumentId];
                }

                // Find the specific RoboClerkDocxTag by content control ID
                var docxTag = docxDocument.RoboClerkTags
                    .OfType<RoboClerkDocxTag>()
                    .FirstOrDefault(t => t.ContentControlId == tagRequest.ContentControlId);

                if (docxTag == null)
                    return new TagContentResult { Success = false, Error = $"Content control '{tagRequest.ContentControlId}' not found in document" };

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
        public async Task<bool> ValidateProjectForWordAddInAsync(string projectId)
        {
            if (!loadedProjects.TryGetValue(projectId, out var project))
                return false;

            try
            {
                // Verify SharePoint path
                if (!IsSharePointPath(project.ProjectPath))
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
        /// Refreshes a document for Word add-in scenarios by loading and analyzing all content control tags (full or partial).
        /// </summary>
        public async Task<DocumentAnalysisResult> RefreshDocumentForWordAddInAsync(string projectId, string documentId, bool full)
        {
            //if full is true, delete the document from loaded documents to force a full reload
            //if full is false, keep the document if already loaded to allow partial refresh and skip any known tags


            if (!loadedProjects.TryGetValue(projectId, out var project))
                throw new ArgumentException("SharePoint project not loaded");

            try
            {
                // Load the document if not already loaded
                if (!project.LoadedDocuments.ContainsKey(documentId))
                {
                    var loadResult = await LoadDocumentAsync(projectId, documentId);
                    if (!loadResult.Success)
                    {
                        return new DocumentAnalysisResult 
                        { 
                            Success = false, 
                            Error = loadResult.Error 
                        };
                    }
                }

                var document = project.LoadedDocuments[documentId];
                var availableTags = new List<AvailableTagInfo>();
                var contentCreatorFactory = serviceProvider.GetRequiredService<IContentCreatorFactory>();

                // Only analyze RoboClerkDocxTag instances (content control-based tags)
                foreach (var tag in document.RoboClerkTags.OfType<RoboClerkDocxTag>())
                {
                    try
                    {
                        // Test if content creator is available
                        var contentCreator = contentCreatorFactory.CreateContentCreator(tag.Source, tag.ContentCreatorID);
                        
                        availableTags.Add(new AvailableTagInfo
                        {
                            TagId = Guid.NewGuid().ToString(),
                            Source = tag.Source.ToString(),
                            ContentCreatorId = tag.ContentCreatorID,
                            ContentControlId = tag.ContentControlId,
                            Parameters = tag.Parameters.ToDictionary(p => p, p => tag.GetParameterOrDefault(p, "")),
                            IsSupported = true,
                            ContentPreview = await GetTagContentPreviewAsync(tag, project)
                        });
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex, $"Content creator not available for tag: {tag.Source}:{tag.ContentCreatorID}");
                        availableTags.Add(new AvailableTagInfo
                        {
                            TagId = Guid.NewGuid().ToString(),
                            Source = tag.Source.ToString(),
                            ContentCreatorId = tag.ContentCreatorID,
                            ContentControlId = tag.ContentControlId,
                            Parameters = tag.Parameters.ToDictionary(p => p, p => tag.GetParameterOrDefault(p, "")),
                            IsSupported = false,
                            Error = $"Content creator not available: {ex.Message}"
                        });
                    }
                }

                logger.Info($"Document analysis complete: {availableTags.Count(t => t.IsSupported)}/{availableTags.Count} content controls supported");
                return new DocumentAnalysisResult
                {
                    Success = true,
                    DocumentId = documentId,
                    AvailableTags = availableTags,
                    TotalTagCount = availableTags.Count,
                    SupportedTagCount = availableTags.Count(t => t.IsSupported)
                };
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to analyze document for Word add-in: {documentId}");
                return new DocumentAnalysisResult 
                { 
                    Success = false, 
                    Error = ex.Message 
                };
            }
        }

        public async Task<RefreshResult> RefreshProjectAsync(string projectId)
        {
            if (!loadedProjects.TryGetValue(projectId, out var project))
                throw new ArgumentException("SharePoint project not loaded");

            try
            {
                logger.Info($"Refreshing data sources for SharePoint project: {projectId}");
                
                // Refresh data sources by recreating them
                var newDataSources = await dataSourcesFactory.CreateDataSourcesAsync(project.Configuration);
                
                // Update the project context with new data sources
                var updatedProject = project with { DataSources = newDataSources };
                loadedProjects[projectId] = updatedProject;
                
                logger.Info($"Successfully refreshed SharePoint project data sources: {projectId}");
                return new RefreshResult { Success = true };
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to refresh SharePoint project: {projectId}");
                return new RefreshResult { Success = false, Error = $"Failed to refresh data sources: {ex.Message}" };
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
        /// Gets a preview of tag content for analysis purposes
        /// </summary>
        private async Task<string> GetTagContentPreviewAsync(RoboClerkDocxTag tag, ProjectContext project)
        {
            try
            {
                var contentCreatorFactory = serviceProvider.GetRequiredService<IContentCreatorFactory>();
                var docConfig = project.DocxDocuments.FirstOrDefault();
                
                if (docConfig != null)
                {
                    var contentCreator = contentCreatorFactory.CreateContentCreator(tag.Source, tag.ContentCreatorID);
                    var content = contentCreator.GetContent(tag, docConfig);
                    
                    // Return first 100 characters as preview
                    return content.Length > 100 ? content.Substring(0, 100) + "..." : content;
                }
                
                return "Preview not available";
            }
            catch
            {
                return "Preview not available";
            }
        }

        /// <summary>
        /// Determines if the given path is a SharePoint path
        /// </summary>
        private bool IsSharePointPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            // Check for common SharePoint URL patterns
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
                SharePointFileProviderPlugin? sharePointPlugin = null;

                // Try loading from each plugin directory
                foreach (var pluginDir in tempConfig.PluginDirs)
                {
                    try
                    {
                        sharePointPlugin = pluginLoader.LoadByName<SharePointFileProviderPlugin>(
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
        private IServiceProvider CreateProjectServiceProvider(IFileProviderPlugin sharePointFileProvider, IConfiguration configuration)
        {
            var services = new ServiceCollection();

            // Project-specific file provider
            services.AddSingleton(sharePointFileProvider);

            // Project-specific configuration
            services.AddSingleton(configuration);

            // Data sources factory (using project-specific file provider)
            services.AddSingleton(serviceProvider.GetRequiredService<IDataSourcesFactory>());

            // Other required services from the main service provider
            services.AddSingleton(serviceProvider.GetRequiredService<IPluginLoader>());
            services.AddSingleton(serviceProvider.GetRequiredService<ITraceabilityAnalysis>());
            services.AddSingleton(serviceProvider.GetRequiredService<IContentCreatorFactory>());

            // Add data sources as a factory that creates them on first access
            services.AddSingleton<IDataSources>(provider =>
            {
                var factory = provider.GetRequiredService<IDataSourcesFactory>();
                var config = provider.GetRequiredService<IConfiguration>();
                return factory.CreateDataSourcesAsync(config).Result;
            });

            return services.BuildServiceProvider();
        }
    }

}