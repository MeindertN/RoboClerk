using Microsoft.Graph;
using Microsoft.Graph.Models;
using Azure.Identity;
using RoboClerk.Server.Configuration;
using System.Text.RegularExpressions;

namespace RoboClerk.Server.Services
{
    /// <summary>
    /// Service for interacting with SharePoint via Microsoft Graph API
    /// </summary>
    public class SharePointService : ISharePointService
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly ServerConfiguration serverConfig;

        public SharePointService(ServerConfiguration serverConfig)
        {
            this.serverConfig = serverConfig;
        }

        /// <summary>
        /// Extracts SharePoint project information from a document URL
        /// </summary>
        public async Task<SharePointProjectInfo> ExtractProjectInfoFromDocumentUrlAsync(string documentUrl, string clientSecret)
        {
            try
            {
                logger.Info($"Extracting SharePoint project information from document URL");
                
                // Parse the SharePoint URL to extract site URL and document path
                var urlInfo = ParseSharePointUrl(documentUrl);
                if (!urlInfo.Success)
                {
                    return new SharePointProjectInfo
                    {
                        Success = false,
                        Error = urlInfo.Error
                    };
                }

                // Create Graph client with app-only authentication
                var graphClient = CreateGraphClient(clientSecret);

                // Get site information
                var site = await GetSiteFromUrlAsync(graphClient, urlInfo.SiteUrl!);
                if (site == null)
                {
                    return new SharePointProjectInfo
                    {
                        Success = false,
                        Error = $"Unable to access SharePoint site: {urlInfo.SiteUrl}"
                    };
                }

                // Get drive information from the document URL
                var driveInfo = await GetDriveInfoFromDocumentPathAsync(graphClient, site.Id!, urlInfo.DocumentPath!);
                if (driveInfo == null || string.IsNullOrEmpty(driveInfo.Value.DriveId))
                {
                    return new SharePointProjectInfo
                    {
                        Success = false,
                        Error = "Unable to determine Drive ID from document URL"
                    };
                }

                // Extract project root from document path (parent directory of the document)
                // This is just the folder name, e.g., "RoboClerk_input"
                var projectRoot = ExtractProjectRoot(urlInfo.DocumentPath!);
                
                // ProjectPath is the relative path starting with /
                // e.g., "/RoboClerk_input"
                var projectPath = $"/{projectRoot}";

                logger.Info($"Successfully extracted SharePoint project info - Site: {urlInfo.SiteUrl}, Drive: {driveInfo.Value.DriveId}, ProjectPath: {projectPath}, ProjectRoot: {projectRoot}");

                return new SharePointProjectInfo
                {
                    Success = true,
                    SiteUrl = urlInfo.SiteUrl,
                    DriveId = driveInfo.Value.DriveId,
                    ProjectRoot = projectRoot,
                    ProjectPath = projectPath,
                    DocumentPath = urlInfo.DocumentPath
                };
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error extracting SharePoint project information from document URL");
                return new SharePointProjectInfo
                {
                    Success = false,
                    Error = $"Failed to extract project information: {ex.Message}"
                };
            }
        }

        private GraphServiceClient CreateGraphClient(string clientSecret)
        {
            var clientSecretCredential = new ClientSecretCredential(
                serverConfig.SharePoint.TenantId,
                serverConfig.SharePoint.ClientId,
                clientSecret);

            var graphClient = new GraphServiceClient(clientSecretCredential);
            logger.Debug("Created Graph client for SharePoint access");
            return graphClient;
        }

        private (bool Success, string? Error, string? SiteUrl, string? DocumentPath) ParseSharePointUrl(string documentUrl)
        {
            try
            {
                var uri = new Uri(documentUrl);
                
                // Extract site URL (everything up to the site collection)
                // Pattern: https://{tenant}.sharepoint.com/sites/{sitename} or https://{tenant}.sharepoint.com
                var match = Regex.Match(documentUrl, @"(https://[^/]+(?:/sites/[^/]+)?)", RegexOptions.IgnoreCase);
                if (!match.Success)
                {
                    return (false, "Invalid SharePoint URL format", null, null);
                }

                var siteUrl = match.Groups[1].Value;
                
                // Extract document path (everything after the site URL)
                // We keep the full path including "Shared Documents" for now
                var fullDocumentPath = documentUrl.Substring(siteUrl.Length).TrimStart('/');
                
                // For the Graph API, we need the path without "Shared Documents" or "Documents" prefix
                var documentPathForApi = Regex.Replace(fullDocumentPath, @"^(Shared Documents|Documents)/", "", RegexOptions.IgnoreCase);
                
                logger.Debug($"Parsed SharePoint URL - Site: {siteUrl}, Document Path for API: {documentPathForApi}, Full Path: {fullDocumentPath}");
                
                // Return the path without the common prefixes for API usage
                return (true, null, siteUrl, documentPathForApi);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error parsing SharePoint URL");
                return (false, $"Failed to parse SharePoint URL: {ex.Message}", null, null);
            }
        }

        private async Task<Site?> GetSiteFromUrlAsync(GraphServiceClient graphClient, string siteUrl)
        {
            try
            {
                var uri = new Uri(siteUrl);
                var hostname = uri.Host;
                var sitePath = uri.AbsolutePath.TrimStart('/');

                Site site;
                if (string.IsNullOrEmpty(sitePath))
                {
                    // Root site
                    site = await graphClient.Sites[hostname + ":/"].GetAsync();
                }
                else
                {
                    // Site collection
                    site = await graphClient.Sites[hostname + ":/" + sitePath].GetAsync();
                }

                logger.Debug($"Retrieved SharePoint site: {site?.DisplayName} (ID: {site?.Id})");
                return site;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error getting SharePoint site from URL: {siteUrl}");
                return null;
            }
        }

        private async Task<(string? DriveId, string? DriveName)?> GetDriveInfoFromDocumentPathAsync(
            GraphServiceClient graphClient, 
            string siteId, 
            string documentPath)
        {
            try
            {
                // Get all drives for the site
                var drivesResponse = await graphClient.Sites[siteId].Drives.GetAsync();
                var drives = drivesResponse?.Value;

                if (drives == null || !drives.Any())
                {
                    logger.Warn($"No drives found for site {siteId}");
                    return null;
                }

                // Try to find the document in each drive
                foreach (var drive in drives)
                {
                    try
                    {
                        // Try to get the item using the document path
                        var item = await graphClient.Drives[drive.Id]
                            .Root
                            .ItemWithPath(documentPath)
                            .GetAsync();

                        if (item != null)
                        {
                            logger.Info($"Found document in drive: {drive.Name} (ID: {drive.Id})");
                            return (drive.Id, drive.Name);
                        }
                    }
                    catch
                    {
                        // Document not in this drive, continue searching
                        continue;
                    }
                }

                logger.Warn($"Document not found in any drive for site {siteId}");
                
                // If we can't find the document, return the default document library drive
                var defaultDrive = drives.FirstOrDefault(d => d.Name == "Documents");
                if (defaultDrive != null)
                {
                    logger.Info($"Using default Documents drive (ID: {defaultDrive.Id})");
                    return (defaultDrive.Id, defaultDrive.Name);
                }

                // Return the first drive as fallback
                logger.Info($"Using first available drive: {drives[0].Name} (ID: {drives[0].Id})");
                return (drives[0].Id, drives[0].Name);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error getting drive information from document path");
                return null;
            }
        }

        private string ExtractProjectRoot(string documentPath)
        {
            // Get the parent directory of the document
            var lastSlashIndex = documentPath.LastIndexOf('/');
            if (lastSlashIndex > 0)
            {
                return documentPath.Substring(0, lastSlashIndex);
            }
            
            // Document is in root
            return string.Empty;
        }
    }
}
