using RoboClerk.Core;
using RoboClerk.Core.DocxSupport;
using RoboClerk.ContentCreators;
using RoboClerk.Server.Models;
using System.Collections.Concurrent;
using System.IO.Abstractions;
using DocumentFormat.OpenXml;
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

        public async Task<ProjectLoadResult> LoadProjectAsync(string projectPath)
        {
            try
            {
                var projectId = Guid.NewGuid().ToString();
                
                // Validate SharePoint path
                if (!IsSharePointPath(projectPath))
                {
                    logger.Warn($"Non-SharePoint path provided: {projectPath}");
                    return new ProjectLoadResult { Success = false, Error = "Only SharePoint project paths are supported for Word add-in use" };
                }
                
                // Support SharePoint paths
                var roboClerkConfigPath = fileSystem.Path.Combine(projectPath, "RoboClerkConfig", "RoboClerk.toml");
                var projectConfigPath = fileSystem.Path.Combine(projectPath, "RoboClerkConfig", "projectConfig.toml");

                if (!fileSystem.File.Exists(roboClerkConfigPath) || !fileSystem.File.Exists(projectConfigPath))
                {
                    return new ProjectLoadResult { Success = false, Error = "Required configuration files not found in SharePoint project" };
                }

                var fileProvider = serviceProvider.GetRequiredService<IFileProviderPlugin>();
                
                // Configure for SharePoint
                var commandLineOptions = new Dictionary<string, string>
                {
                    ["ProjectRoot"] = projectPath,
                    ["TemplateDirectory"] = fileSystem.Path.Combine(projectPath, "Templates"),
                    ["MediaDirectory"] = fileSystem.Path.Combine(projectPath, "Media")
                };
                
                logger.Info($"Loading SharePoint project from: {projectPath}");

                // Use the new Configuration builder pattern
                var configuration = RoboClerk.Configuration.Configuration.CreateBuilder()
                    .WithRoboClerkConfig(fileProvider, roboClerkConfigPath, commandLineOptions)
                    .WithProjectConfig(fileProvider, projectConfigPath)
                    .Build();

                // Filter for DOCX documents only
                var docxDocuments = configuration.Documents
                    .Where(d => d.DocumentTemplate.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (!docxDocuments.Any())
                {
                    return new ProjectLoadResult { Success = false, Error = "No DOCX documents found in SharePoint project" };
                }

                // Initialize data sources with enhanced error handling for SharePoint
                IDataSources dataSources;
                try
                {
                    dataSources = await dataSourcesFactory.CreateDataSourcesAsync(configuration);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to initialize data sources for SharePoint project");
                    return new ProjectLoadResult { Success = false, Error = $"Failed to initialize data sources: {ex.Message}" };
                }
                
                var projectContext = new ProjectContext
                {
                    ProjectId = projectId,
                    ProjectPath = projectPath,
                    Configuration = configuration,
                    DataSources = dataSources,
                    DocxDocuments = docxDocuments,
                    LoadedDocuments = new ConcurrentDictionary<string, IDocument>()
                };

                loadedProjects[projectId] = projectContext;

                logger.Info($"Successfully loaded SharePoint project: {projectId} with {docxDocuments.Count} DOCX documents");
                return new ProjectLoadResult
                {
                    Success = true,
                    ProjectId = projectId,
                    ProjectName = fileSystem.Path.GetFileName(projectPath),
                    Documents = docxDocuments.Select(d => new DocumentInfo(
                        d.RoboClerkID,
                        d.DocumentTitle,
                        d.DocumentTemplate
                    )).ToList()
                };
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to load SharePoint project: {projectPath}");
                return new ProjectLoadResult { Success = false, Error = $"Failed to load SharePoint project: {ex.Message}" };
            }
        }

        public async Task<DocumentLoadResult> LoadDocumentAsync(string projectId, string documentId)
        {
            if (!loadedProjects.TryGetValue(projectId, out var project))
                throw new ArgumentException("SharePoint project not loaded");

            try
            {
                var docConfig = project.DocxDocuments.FirstOrDefault(d => d.RoboClerkID == documentId);
                if (docConfig == null)
                    return new DocumentLoadResult { Success = false, Error = "Document not found in SharePoint project" };

                var templatePath = fileSystem.Path.Combine(project.Configuration.TemplateDir, docConfig.DocumentTemplate);
                if (!fileSystem.File.Exists(templatePath))
                    return new DocumentLoadResult { Success = false, Error = "Template file not found in SharePoint" };

                var document = new DocxDocument(docConfig.DocumentTitle, docConfig.DocumentTemplate, project.Configuration);
                using var stream = fileSystem.FileStream.New(templatePath, FileMode.Open);
                document.FromStream(stream);

                project.LoadedDocuments[documentId] = document;

                // Only return RoboClerkDocxTag instances (content control-based tags)
                var tags = document.RoboClerkTags
                    .OfType<RoboClerkDocxTag>()
                    .Select(tag => new TagInfo
                    {
                        TagId = Guid.NewGuid().ToString(),
                        Source = tag.Source.ToString(),
                        ContentCreatorId = tag.ContentCreatorID,
                        ContentControlId = tag.ContentControlId,
                        Parameters = tag.Parameters.ToDictionary(p => p, p => tag.GetParameterOrDefault(p, "")),
                        CurrentContent = tag.Contents
                    }).ToList();

                logger.Info($"Loaded document {documentId} with {tags.Count} content control tags");
                return new DocumentLoadResult
                {
                    Success = true,
                    DocumentId = documentId,
                    Tags = tags
                };
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to load document from SharePoint: {documentId}");
                return new DocumentLoadResult { Success = false, Error = $"Failed to load document from SharePoint: {ex.Message}" };
            }
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

                // Check if SharePoint file provider is available and working
                var fileProvider = serviceProvider.GetRequiredService<IFileProviderPlugin>();
                if (!fileProvider.GetType().Name.Contains("SharePoint"))
                {
                    logger.Warn($"SharePoint file provider not available for project: {projectId}");
                    return false;
                }

                // Verify basic project structure
                if (project.Configuration == null || project.DataSources == null)
                {
                    logger.Warn($"Project configuration or data sources missing for project: {projectId}");
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
        /// Pre-processes a document for Word add-in scenarios by loading and analyzing all content control tags
        /// </summary>
        public async Task<DocumentAnalysisResult> AnalyzeDocumentForWordAddInAsync(string projectId, string documentId)
        {
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
    }
}