using RoboClerk.Core;
using RoboClerk.Core.Configuration;
using System.Collections.Concurrent;
using IConfiguration = RoboClerk.Core.Configuration.IConfiguration;

namespace RoboClerk.Server.Models
{
    internal record ProjectContext
    {
        public string ProjectId { get; init; } = string.Empty;          // Hash-based ID
        public string ProjectPath { get; init; } = string.Empty;        // SharePoint folder path
        public string ProjectName { get; init; } = string.Empty;        // Display name
        public string SPDriveId { get; init; } = string.Empty;         // SharePoint drive ID
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public IServiceProvider ProjectServiceProvider { get; init; } = null!; // Project-specific service provider
        public ConcurrentDictionary<string, IDocument> LoadedDocuments { get; init; } = new();
    }
}