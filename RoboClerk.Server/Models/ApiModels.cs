using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

namespace RoboClerk.Server.Models
{
    public record ProjectInfo(string Name, string Path);
    
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
        
    public record RoboClerkContentControlTagRequest
    {
        [Required]
        public string DocumentId { get; init; } = string.Empty;
        [Required]
        public string ContentControlId { get; init; } = string.Empty;
        [Required]
        public string RoboClerkTag { get; init; } = string.Empty;
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

    // Configuration management models
    public record ConfigurationUpdateResult
    {
        public bool Success { get; init; }
        public string? Error { get; init; }
        public List<string> UpdatedKeys { get; init; } = new();
        public bool RequiresProjectReload { get; init; }
    }

    public record ConfigurationValidationResult
    {
        public bool IsValid { get; init; }
        public List<string> Errors { get; init; } = new();
        public List<string> Warnings { get; init; } = new();
    }

    // Template file management models
    public record TemplateFileInfo
    {
        public string FileName { get; init; } = string.Empty;
        public string RelativePath { get; init; } = string.Empty;
        public string FullPath { get; init; } = string.Empty;
        public long FileSizeBytes { get; init; }
        public DateTime LastModified { get; init; }
        public bool IsDocx { get; init; }
    }

    public record AvailableTemplateFilesResult
    {
        public bool Success { get; init; }
        public string? Error { get; init; }
        public List<TemplateFileInfo> AvailableTemplateFiles { get; init; } = new();
        public int TotalTemplateFiles { get; init; }
        public int ConfiguredDocuments { get; init; }
        public int UnconfiguredTemplateFiles { get; init; }
    }
}