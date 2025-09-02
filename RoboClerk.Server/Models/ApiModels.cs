using System.ComponentModel.DataAnnotations;

namespace RoboClerk.Server.Models
{
    public record ProjectInfo(string Name, string Path);
    
    public record LoadProjectRequest
    {
        [Required]
        public string ProjectPath { get; init; } = string.Empty;
    }
    
    public record ProjectLoadResult
    {
        public bool Success { get; init; }
        public string? Error { get; init; }
        public string? ProjectId { get; init; }
        public string? ProjectName { get; init; }
        public List<DocumentInfo>? Documents { get; init; }
    }
    
    public record DocumentInfo(string DocumentId, string Title, string Template);
    
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
        public Dictionary<string, string> Parameters { get; init; } = new();
        public string CurrentContent { get; init; } = string.Empty;
    }
    
    public record RoboClerkTagRequest
    {
        [Required]
        public string DocumentId { get; init; } = string.Empty;
        [Required]
        public string Source { get; init; } = string.Empty;
        public string? ContentCreatorId { get; init; }
        public Dictionary<string, string> Parameters { get; init; } = new();
    }
    
    public record RoboClerkContentControlTagRequest
    {
        [Required]
        public string DocumentId { get; init; } = string.Empty;
        [Required]
        public string ContentControlId { get; init; } = string.Empty;
    }
    
    public record TagContentResult
    {
        public bool Success { get; init; }
        public string? Error { get; init; }
        public string? Content { get; init; }
    }
    
    public record ConfigurationValue(string Key, string Value);
    
    public record RefreshResult
    {
        public bool Success { get; init; }
        public string? Error { get; init; }
    }

    public record DocumentAnalysisResult
    {
        public bool Success { get; init; }
        public string? Error { get; init; }
        public string? DocumentId { get; init; }
        public List<AvailableTagInfo>? AvailableTags { get; init; }
        public int TotalTagCount { get; init; }
        public int SupportedTagCount { get; init; }
    }

    public record AvailableTagInfo
    {
        public string TagId { get; init; } = string.Empty;
        public string Source { get; init; } = string.Empty;
        public string? ContentCreatorId { get; init; }
        public Dictionary<string, string> Parameters { get; init; } = new();
        public bool IsSupported { get; init; }
        public string? Error { get; init; }
        public string? ContentPreview { get; init; }
    }
}