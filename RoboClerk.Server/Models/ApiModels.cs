using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

namespace RoboClerk.Server.Models
{
    public record ProjectInfo(string Name, string Path);
    
    public record LoadProjectRequest
    {
        // Either provide a document URL (new approach) or all the required fields (legacy approach)
        
        /// <summary>
        /// SharePoint document URL - when provided, server extracts all necessary information
        /// </summary>
        public string? DocumentUrl { get; init; }
        
        /// <summary>
        /// Optional: Explicit project identifier/path (can be derived from document URL)
        /// </summary>
        public string? ProjectPath { get; init; }
        
        /// <summary>
        /// Optional: SharePoint Drive ID (extracted from document URL if not provided)
        /// </summary>
        public string? SPDriveId { get; init; }
        
        /// <summary>
        /// Optional: Specific project identification override
        /// </summary>
        public string? ProjectIdentifier { get; init; }
        
        /// <summary>
        /// Optional: SharePoint Site URL override (extracted from document URL if not provided)
        /// </summary>
        public string? SPSiteUrl { get; init; }
        
        /// <summary>
        /// Optional: Project root directory within SharePoint (extracted from document URL if not provided)
        /// </summary>
        public string? ProjectRoot { get; init; }
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