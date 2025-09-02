using RoboClerk.Core;
using RoboClerk.Core.Configuration;
using System.Collections.Concurrent;
using IConfiguration = RoboClerk.Core.Configuration.IConfiguration;

namespace RoboClerk.Server.Models
{
    internal record ProjectContext
    {
        public string ProjectId { get; init; } = string.Empty;
        public string ProjectPath { get; init; } = string.Empty;
        public IConfiguration Configuration { get; init; } = null!;
        public IDataSources DataSources { get; init; } = null!;
        public List<DocumentConfig> DocxDocuments { get; init; } = new();
        public ConcurrentDictionary<string, IDocument> LoadedDocuments { get; init; } = new();
    }
}