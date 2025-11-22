using System;
using System.Collections.Generic;

namespace RoboClerk.ContentCreators
{
    /// <summary>
    /// Central registry for content creator metadata.
    /// All content creators should register their metadata here for discovery.
    /// </summary>
    public static class ContentCreatorMetadataRegistry
    {
        private static readonly Dictionary<string, Func<ContentCreatorMetadata>> _metadataProviders = new(StringComparer.OrdinalIgnoreCase);
        private static bool _initialized = false;
        private static readonly object _lock = new object();

        /// <summary>
        /// Registers a metadata provider for a content creator
        /// </summary>
        public static void Register(string key, Func<ContentCreatorMetadata> metadataProvider)
        {
            _metadataProviders[key] = metadataProvider;
        }

        /// <summary>
        /// Gets all registered metadata
        /// </summary>
        public static IEnumerable<ContentCreatorMetadata> GetAllMetadata()
        {
            EnsureInitialized();
            
            foreach (var provider in _metadataProviders.Values)
            {
                yield return provider();
            }
        }

        /// <summary>
        /// Gets metadata by key (source or name)
        /// </summary>
        public static ContentCreatorMetadata? GetMetadata(string key)
        {
            EnsureInitialized();
            
            if (_metadataProviders.TryGetValue(key, out var provider))
            {
                return provider();
            }
            
            // Try to find by source in the metadata
            foreach (var metadataProvider in _metadataProviders.Values)
            {
                var metadata = metadataProvider();
                if (metadata.Source.Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    return metadata;
                }
            }
            
            return null;
        }

        /// <summary>
        /// Initializes the registry by registering all known content creators
        /// </summary>
        private static void EnsureInitialized()
        {
            if (_initialized) return;
            
            lock (_lock)
            {
                if (_initialized) return;
                
                RegisterAllContentCreators();
                _initialized = true;
            }
        }

        /// <summary>
        /// Registers all known content creators.
        /// This method is called once during initialization.
        /// </summary>
        private static void RegisterAllContentCreators()
        {
            // Document content creators
            Register("Document", () => Document.StaticMetadata);
            Register("Reference", () => Reference.StaticMetadata);
            Register("ConfigurationValue", () => ConfigurationValue.StaticMetadata);
            Register("Config", () => ConfigurationValue.StaticMetadata);
            
            // Trace and layout
            Register("Trace", () => Trace.StaticMetadata);
            Register("PostLayout", () => PostLayout.StaticMetadata);
            Register("Post", () => PostLayout.StaticMetadata);
            
            // Comment
            Register("Comment", () => CommentMetadata);
            
            // Requirements
            Register("SystemRequirement", () => SystemRequirement.StaticMetadata);
            Register("SoftwareRequirement", () => SoftwareRequirement.StaticMetadata);
            Register("DocumentationRequirement", () => DocumentationRequirement.StaticMetadata);
            
            // Tests
            Register("SoftwareSystemTest", () => SoftwareSystemTest.StaticMetadata);
            Register("UnitTest", () => UnitTest.StaticMetadata);
            
            // Other SLMS items
            Register("Anomaly", () => Anomaly.StaticMetadata);
            Register("Risk", () => Risk.StaticMetadata);
            Register("SOUP", () => SOUP.StaticMetadata);
            Register("DocContent", () => DocContent.StaticMetadata);
            Register("Eliminated", () => Eliminated.StaticMetadata);
            
            // Traceability matrices
            Register("TraceMatrix", () => TraceMatrix.StaticMetadata); // Generic matrix
            Register("SystemLevelTraceabilityMatrix", () => SystemLevelTraceabilityMatrix.StaticMetadata);
            Register("SoftwareLevelTraceabilityMatrix", () => SoftwareLevelTraceabilityMatrix.StaticMetadata);
            Register("RiskTraceabilityMatrix", () => RiskTraceabilityMatrix.StaticMetadata);
            
            // File operations
            Register("ExcelTable", () => ExcelTable.StaticMetadata);
            Register("TemplateSection", () => TemplateSection.StaticMetadata);
            Register("FILE", () => ExcelTable.StaticMetadata); // Also register by source
            
            // Web services
            Register("KrokiDiagram", () => KrokiDiagram.StaticMetadata);
            Register("Web", () => KrokiDiagram.StaticMetadata);
            
            // AI
            Register("AIContentCreator", () => AIContentCreator.StaticMetadata);
            Register("AI", () => AIContentCreator.StaticMetadata);
        }

        /// <summary>
        /// Metadata for the Comment content creator (simple case, defined inline)
        /// </summary>
        private static readonly ContentCreatorMetadata CommentMetadata = new ContentCreatorMetadata("Comment", "Comment", 
            "Allows adding comments to templates that will be removed during processing")
        {
            Category = "Utility",
            Tags = new List<ContentCreatorTag>
            {
                new ContentCreatorTag("[Any]", "Adds a comment that will be removed")
                {
                    Category = "Comments",
                    Description = "Content within comment tags is ignored and removed during document generation. " +
                        "Useful for adding notes or temporarily disabling content.",
                    ExampleUsage = "@@Comment:Note(This is a note that won't appear in output)@@"
                }
            }
        };

        /// <summary>
        /// Resets the registry (useful for testing)
        /// </summary>
        internal static void Reset()
        {
            lock (_lock)
            {
                _metadataProviders.Clear();
                _initialized = false;
            }
        }
    }
}
