using System.Text.Json;
using System.ComponentModel.DataAnnotations;

namespace RoboClerk.Server.TestClient.Models
{
    // Request Models
    public record LoadProjectRequest
    {
        [Required]
        public string ProjectPath { get; init; } = string.Empty;
        
        [Required]
        public string SPDriveId { get; init; } = string.Empty;
        
        // Optional: Allow override for specific project identification
        public string? ProjectIdentifier { get; init; }
        
        // Optional SharePoint overrides
        public string? SPSiteUrl { get; init; }
        
        // Project root directory within the SharePoint drive
        [Required]
        public string ProjectRoot { get; init; } = string.Empty;
    }

    public record RoboClerkContentControlTagRequest
    {
        [Required]
        public string DocumentId { get; init; } = string.Empty;
        [Required]
        public string ContentControlId { get; init; } = string.Empty;
    }

    // Response Models
    public record ProjectLoadResult
    {
        public bool Success { get; init; }
        public string? Error { get; init; }
        public string? ProjectId { get; init; }
        public string? ProjectName { get; init; }
        public DateTime? LastUpdated { get; init; }
        public List<DocumentInfo>? Documents { get; init; }
    }

    public record DocumentInfo(string DocumentId, string Title, string Template);

    // Note: DocumentLoadResult and TagInfo are no longer used by the server API
    // The server now uses DocumentAnalysisResult instead
    public record DocumentLoadResult
    {
        public bool Success { get; init; }
        public string? Error { get; init; }
        public string? DocumentId { get; init; }
        public List<TagInfo>? Tags { get; init; }
    }

    public record TagInfo
    {
        public string TagId { get; init; } = string.Empty;
        public string Source { get; init; } = string.Empty;
        public string? ContentCreatorId { get; init; }
        public string? ContentControlId { get; init; }
        public Dictionary<string, string> Parameters { get; init; } = new();
        public string CurrentContent { get; init; } = string.Empty;
    }

    public record DocumentAnalysisResult
    {
        public bool Success { get; init; }
        public string? Error { get; init; }
        public string? DocumentId { get; init; }
    }

    public record AvailableTagInfo
    {
        public string TagId { get; init; } = string.Empty;
        public string Source { get; init; } = string.Empty;
        public string? ContentCreatorId { get; init; }
        public string? ContentControlId { get; init; }
        public Dictionary<string, string> Parameters { get; init; } = new();
        public bool IsSupported { get; init; }
        public string? Error { get; init; }
        public string? ContentPreview { get; init; }
    }

    public record TagContentResult
    {
        public bool Success { get; init; }
        public string? Error { get; init; }
        public string? Content { get; init; }
    }

    public record RefreshResult
    {
        public bool Success { get; init; }
        public string? Error { get; init; }
    }

    public record HealthCheckResult
    {
        public string Status { get; init; } = string.Empty;
        public string Service { get; init; } = string.Empty;
        public DateTime Timestamp { get; init; }
        public string? Version { get; init; }
    }

    public record ConfigurationValue(string Key, string Value);
}