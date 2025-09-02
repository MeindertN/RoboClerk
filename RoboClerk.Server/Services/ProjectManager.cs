using Microsoft.Extensions.Logging;
using RoboClerk.Core;
using RoboClerk.Core.Configuration;
using RoboClerk.Core.DocxSupport;
using RoboClerk.ContentCreators;
using RoboClerk.Server.Models;
using System.Collections.Concurrent;
using System.IO.Abstractions;
using DocumentFormat.OpenXml.Wordprocessing;
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

        public async Task<List<ProjectInfo>> GetAvailableProjectsAsync()
        {
            var projects = new List<ProjectInfo>();
            
            // Scan common project locations for RoboClerk projects
            var searchPaths = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Projects"),
                @"C:\Projects",
                @"C:\RoboClerk"
            };

            foreach (var searchPath in searchPaths)
            {
                if (fileSystem.Directory.Exists(searchPath))
                {
                    var foundProjects = await ScanForProjectsAsync(searchPath);
                    projects.AddRange(foundProjects);
                }
            }

            return projects.DistinctBy(p => p.Path).ToList();
        }

        public async Task<ProjectLoadResult> LoadProjectAsync(string projectPath)
        {
            try
            {
                var projectId = Guid.NewGuid().ToString();
                
                // Support both local and SharePoint paths
                var roboClerkConfigPath = fileSystem.Path.Combine(projectPath, "RoboClerkConfig", "RoboClerk.toml");
                var projectConfigPath = fileSystem.Path.Combine(projectPath, "RoboClerkConfig", "projectConfig.toml");

                if (!fileSystem.File.Exists(roboClerkConfigPath) || !fileSystem.File.Exists(projectConfigPath))
                {
                    return new ProjectLoadResult { Success = false, Error = "Required configuration files not found" };
                }

                var fileProvider = serviceProvider.GetRequiredService<IFileProviderPlugin>();
                
                // Pass the project path as a command line option for SharePoint scenarios
                var commandLineOptions = new Dictionary<string, string>();
                if (IsSharePointPath(projectPath))
                {
                    commandLineOptions["ProjectRoot"] = projectPath;
                    commandLineOptions["TemplateDirectory"] = fileSystem.Path.Combine(projectPath, "Templates");
                    commandLineOptions["MediaDirectory"] = fileSystem.Path.Combine(projectPath, "Media");
                    logger.Info($"Loading SharePoint project from: {projectPath}");
                }

                var configuration = new RoboClerk.Configuration.Configuration(fileProvider, roboClerkConfigPath, projectConfigPath, commandLineOptions);

                // Filter for DOCX documents only
                var docxDocuments = configuration.Documents
                    .Where(d => d.DocumentTemplate.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (!docxDocuments.Any())
                {
                    return new ProjectLoadResult { Success = false, Error = "No DOCX documents found in project" };
                }

                // Initialize data sources with enhanced error handling for SharePoint
                IDataSources dataSources;
                try
                {
                    dataSources = await dataSourcesFactory.CreateDataSourcesAsync(configuration);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to initialize data sources");
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

                logger.Info($"Successfully loaded project: {projectId} from {projectPath}");
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
                logger.Error(ex, $"Failed to load project: {projectPath}");
                return new ProjectLoadResult { Success = false, Error = ex.Message };
            }
        }

        public async Task<List<DocumentInfo>> GetProjectDocumentsAsync(string projectId)
        {
            if (!loadedProjects.TryGetValue(projectId, out var project))
                throw new ArgumentException("Project not loaded");

            return project.DocxDocuments.Select(d => new DocumentInfo(
                d.RoboClerkID,
                d.DocumentTitle,
                d.DocumentTemplate
            )).ToList();
        }

        public async Task<DocumentLoadResult> LoadDocumentAsync(string projectId, string documentId)
        {
            if (!loadedProjects.TryGetValue(projectId, out var project))
                throw new ArgumentException("Project not loaded");

            try
            {
                var docConfig = project.DocxDocuments.FirstOrDefault(d => d.RoboClerkID == documentId);
                if (docConfig == null)
                    return new DocumentLoadResult { Success = false, Error = "Document not found" };

                var templatePath = fileSystem.Path.Combine(project.Configuration.TemplateDir, docConfig.DocumentTemplate);
                if (!fileSystem.File.Exists(templatePath))
                    return new DocumentLoadResult { Success = false, Error = "Template file not found" };

                var document = new DocxDocument(docConfig.DocumentTitle, docConfig.DocumentTemplate, project.Configuration);
                using var stream = fileSystem.FileStream.New(templatePath, FileMode.Open);
                document.FromStream(stream);

                project.LoadedDocuments[documentId] = document;

                var tags = document.RoboClerkTags.Select(tag => new TagInfo
                {
                    TagId = Guid.NewGuid().ToString(),
                    Source = tag.Source.ToString(),
                    ContentCreatorId = tag.ContentCreatorID,
                    Parameters = tag.Parameters.ToDictionary(p => p, p => tag.GetParameterOrDefault(p, "")),
                    CurrentContent = tag.Contents
                }).ToList();

                return new DocumentLoadResult
                {
                    Success = true,
                    DocumentId = documentId,
                    Tags = tags
                };
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to load document: {documentId}");
                return new DocumentLoadResult { Success = false, Error = ex.Message };
            }
        }

        public async Task<List<ConfigurationValue>> GetProjectConfigurationAsync(string projectId)
        {
            if (!loadedProjects.TryGetValue(projectId, out var project))
                throw new ArgumentException("Project not loaded");

            var configValues = new List<ConfigurationValue>
            {
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

        public async Task<TagContentResult> GetTagContentAsync(string projectId, RoboClerkTagRequest tagRequest)
        {
            if (!loadedProjects.TryGetValue(projectId, out var project))
                throw new ArgumentException("Project not loaded");

            try
            {
                var contentCreatorFactory = serviceProvider.GetRequiredService<IContentCreatorFactory>();
                
                // Create a tag from the request
                var tag = CreateTagFromRequest(tagRequest);
                
                // Find the appropriate document config
                var docConfig = project.DocxDocuments.FirstOrDefault(d => d.RoboClerkID == tagRequest.DocumentId);
                if (docConfig == null)
                    return new TagContentResult { Success = false, Error = "Document not found" };

                // Get content using the content creator
                var contentCreator = contentCreatorFactory.CreateContentCreator(tag.Source, tag.ContentCreatorID);
                var content = contentCreator.GetContent(tag, docConfig);

                // Convert content to OpenXML format for Word add-in consumption
                var openXmlContent = ConvertToOpenXml(content, project.Configuration);

                return new TagContentResult
                {
                    Success = true,
                    Content = openXmlContent
                };
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to get tag content: {tagRequest.Source}:{tagRequest.ContentCreatorId}");
                return new TagContentResult { Success = false, Error = ex.Message };
            }
        }

        /// <summary>
        /// Gets tag content using RoboClerkDocxTag approach for better OpenXML conversion
        /// This method expects the Word add-in to provide content control information
        /// </summary>
        public async Task<TagContentResult> GetTagContentWithContentControlAsync(string projectId, RoboClerkContentControlTagRequest tagRequest)
        {
            if (!loadedProjects.TryGetValue(projectId, out var project))
                throw new ArgumentException("Project not loaded");

            try
            {
                var contentCreatorFactory = serviceProvider.GetRequiredService<IContentCreatorFactory>();
                
                // Find the appropriate document config
                var docConfig = project.DocxDocuments.FirstOrDefault(d => d.RoboClerkID == tagRequest.DocumentId);
                if (docConfig == null)
                    return new TagContentResult { Success = false, Error = "Document not found" };

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
                    return new TagContentResult { Success = false, Error = "Content control not found" };

                // Get content using the content creator
                var contentCreator = contentCreatorFactory.CreateContentCreator(docxTag.Source, docxTag.ContentCreatorID);
                var content = contentCreator.GetContent(docxTag, docConfig);

                // Update the tag content and convert to OpenXML using the existing RoboClerkDocxTag logic
                docxTag.Contents = content;
                
                // Extract the OpenXML after conversion
                docxTag.ConvertContentToOpenXml();
                
                // Get the converted OpenXML content
                // Note: We need to extract this from the content control after conversion
                var openXmlContent = await ExtractOpenXmlFromContentControl(docxTag);

                return new TagContentResult
                {
                    Success = true,
                    Content = openXmlContent
                };
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to get tag content with content control: {tagRequest.ContentControlId}");
                return new TagContentResult { Success = false, Error = ex.Message };
            }
        }

        /// <summary>
        /// Extracts OpenXML content from a RoboClerkDocxTag after conversion
        /// </summary>
        private async Task<string> ExtractOpenXmlFromContentControl(RoboClerkDocxTag docxTag)
        {
            try
            {
                // This is a placeholder - we need to implement the extraction of OpenXML
                // from the content control after ConvertContentToOpenXml() has been called
                // The exact implementation depends on how we can access the converted content
                
                // For now, fall back to the utility converter with the tag content
                return OpenXmlConverter.ConvertToOpenXml(docxTag.Contents);
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "Failed to extract OpenXML from content control, using fallback");
                return OpenXmlConverter.ConvertToOpenXml(docxTag.Contents);
            }
        }

        public async Task<RefreshResult> RefreshProjectAsync(string projectId)
        {
            if (!loadedProjects.TryGetValue(projectId, out var project))
                throw new ArgumentException("Project not loaded");

            try
            {
                // Refresh data sources by recreating them
                var newDataSources = await dataSourcesFactory.CreateDataSourcesAsync(project.Configuration);
                
                // Update the project context with new data sources
                var updatedProject = project with { DataSources = newDataSources };
                loadedProjects[projectId] = updatedProject;
                
                logger.Info($"Successfully refreshed project: {projectId}");
                return new RefreshResult { Success = true };
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to refresh project: {projectId}");
                return new RefreshResult { Success = false, Error = ex.Message };
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
                
                logger.Info($"Unloaded project: {projectId}");
            }
        }

        private async Task<List<ProjectInfo>> ScanForProjectsAsync(string searchPath)
        {
            var projects = new List<ProjectInfo>();
            
            try
            {
                var directories = fileSystem.Directory.GetDirectories(searchPath);
                
                foreach (var directory in directories)
                {
                    var configPath = fileSystem.Path.Combine(directory, "RoboClerkConfig");
                    if (fileSystem.Directory.Exists(configPath))
                    {
                        var roboClerkConfig = fileSystem.Path.Combine(configPath, "RoboClerk.toml");
                        var projectConfig = fileSystem.Path.Combine(configPath, "projectConfig.toml");
                        
                        if (fileSystem.File.Exists(roboClerkConfig) && fileSystem.File.Exists(projectConfig))
                        {
                            projects.Add(new ProjectInfo(
                                fileSystem.Path.GetFileName(directory),
                                directory
                            ));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, $"Error scanning directory: {searchPath}");
            }
            
            return projects;
        }

        private IRoboClerkTag CreateTagFromRequest(RoboClerkTagRequest request)
        {
            // Create a SimpleRoboClerkTag from the request
            return new SimpleRoboClerkTag(request.Source, request.ContentCreatorId, request.Parameters);
        }

        /// <summary>
        /// Converts plain text/HTML content to OpenXML format for Word add-in consumption
        /// </summary>
        private string ConvertToOpenXml(string content, IConfiguration configuration)
        {
            return OpenXmlConverter.ConvertToOpenXml(content, configuration);
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
        /// Validates that a project is properly configured for Word add-in usage
        /// </summary>
        public async Task<bool> ValidateProjectForWordAddInAsync(string projectId)
        {
            if (!loadedProjects.TryGetValue(projectId, out var project))
                return false;

            try
            {
                // Check if SharePoint file provider is available and working
                if (IsSharePointPath(project.ProjectPath))
                {
                    var fileProvider = serviceProvider.GetRequiredService<IFileProviderPlugin>();
                    if (fileProvider.GetType().Name.Contains("SharePoint"))
                    {
                        logger.Info($"SharePoint file provider validated for project: {projectId}");
                        return true;
                    }
                    else
                    {
                        logger.Warn($"SharePoint path detected but SharePoint file provider not available for project: {projectId}");
                        return false;
                    }
                }

                // For local projects, just verify basic functionality
                return project.Configuration != null && project.DataSources != null;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to validate project for Word add-in: {projectId}");
                return false;
            }
        }

        /// <summary>
        /// Pre-processes a document for Word add-in scenarios by loading and analyzing all tags
        /// </summary>
        public async Task<DocumentAnalysisResult> AnalyzeDocumentForWordAddInAsync(string projectId, string documentId)
        {
            if (!loadedProjects.TryGetValue(projectId, out var project))
                throw new ArgumentException("Project not loaded");

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

                foreach (var tag in document.RoboClerkTags)
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
                            Parameters = tag.Parameters.ToDictionary(p => p, p => tag.GetParameterOrDefault(p, "")),
                            IsSupported = false,
                            Error = $"Content creator not available: {ex.Message}"
                        });
                    }
                }

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

        /// <summary>
        /// Gets a preview of tag content for analysis purposes
        /// </summary>
        private async Task<string> GetTagContentPreviewAsync(IRoboClerkTag tag, ProjectContext project)
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
    }
}