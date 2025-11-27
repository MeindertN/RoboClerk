using RoboClerk.Server.Models;

namespace RoboClerk.Server.Services
{
    /// <summary>
    /// Service for interacting with SharePoint via Microsoft Graph API
    /// </summary>
    public interface ISharePointService
    {
        /// <summary>
        /// Extracts SharePoint project information from a document URL
        /// </summary>
        /// <param name="documentUrl">The SharePoint document URL</param>
        /// <param name="clientSecret">The Azure AD client secret</param>
        /// <returns>Extracted project information including Site URL, Drive ID, and Project Root</returns>
        Task<SharePointProjectInfo> ExtractProjectInfoFromDocumentUrlAsync(string documentUrl, string clientSecret);
    }
    
    /// <summary>
    /// SharePoint project information extracted from document URL
    /// </summary>
    public record SharePointProjectInfo
    {
        public bool Success { get; init; }
        public string? Error { get; init; }
        public string? SiteUrl { get; init; }
        public string? DriveId { get; init; }
        public string? ProjectRoot { get; init; }
        public string? ProjectPath { get; init; }
        public string? DocumentPath { get; init; }
    }
}
