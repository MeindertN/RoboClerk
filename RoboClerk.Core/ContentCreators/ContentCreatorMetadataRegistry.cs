using System;
using System.Collections.Generic;
using RoboClerk.Core.Configuration;

namespace RoboClerk.ContentCreators
{
    /// <summary>
    /// Central registry for content creator metadata.
    /// All content creators should register their metadata here for discovery.
    /// </summary>
    public static class ContentCreatorMetadataRegistry
    {
        private static readonly Dictionary<string, Func<IConfiguration?, ContentCreatorMetadata>> _metadataProviders = new(StringComparer.OrdinalIgnoreCase);
        private static bool _initialized = false;
        private static readonly object _lock = new object();

        /// <summary>
        /// Registers a metadata provider for a content creator
        /// </summary>
        public static void Register(string key, Func<IConfiguration?, ContentCreatorMetadata> metadataProvider)
        {
            _metadataProviders[key] = metadataProvider;
        }

        /// <summary>
        /// Gets all registered metadata
        /// </summary>
        public static IEnumerable<ContentCreatorMetadata> GetAllMetadata(IConfiguration? configuration = null)
        {
            EnsureInitialized();
            
            foreach (var provider in _metadataProviders.Values)
            {
                yield return provider(configuration);
            }
        }

        /// <summary>
        /// Gets metadata by key (source or name)
        /// </summary>
        public static ContentCreatorMetadata? GetMetadata(string key, IConfiguration? configuration = null)
        {
            EnsureInitialized();
            
            if (_metadataProviders.TryGetValue(key, out var provider))
            {
                return provider(configuration);
            }
            
            // Try to find by source in the metadata
            foreach (var metadataProvider in _metadataProviders.Values)
            {
                var metadata = metadataProvider(configuration);
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
            Register("Document", _ => Document.StaticMetadata);
            Register("Reference", _ => Reference.StaticMetadata);
            Register("ConfigurationValue", config => ConfigurationValue.GetMetadata(config));
            
            // Trace and layout
            Register("Trace", _ => Trace.StaticMetadata);
                                  
            // Requirements
            Register("SystemRequirement", _ => SystemRequirement.StaticMetadata);
            Register("SoftwareRequirement", _ => SoftwareRequirement.StaticMetadata);
            Register("DocumentationRequirement", _ => DocumentationRequirement.StaticMetadata);
            
            // Tests
            Register("SoftwareSystemTest", _ => SoftwareSystemTest.StaticMetadata);
            Register("UnitTest", _ => UnitTest.StaticMetadata);
            
            // Other SLMS items
            Register("Anomaly", _ => Anomaly.StaticMetadata);
            Register("Risk", _ => Risk.StaticMetadata);
            Register("SOUP", _ => SOUP.StaticMetadata);
            Register("DocContent", _ => DocContent.StaticMetadata);
            Register("Eliminated", _ => Eliminated.StaticMetadata);
            
            // Traceability matrices
            Register("TraceMatrix", _ => TraceMatrix.StaticMetadata); // Generic matrix
            Register("SystemLevelTraceabilityMatrix", _ => SystemLevelTraceabilityMatrix.StaticMetadata);
            Register("SoftwareLevelTraceabilityMatrix", _ => SoftwareLevelTraceabilityMatrix.StaticMetadata);
            Register("RiskTraceabilityMatrix", _ => RiskTraceabilityMatrix.StaticMetadata);
            
            // File operations
            Register("ExcelTable", _ => ExcelTable.StaticMetadata);
            Register("TemplateSection", _ => TemplateSection.StaticMetadata);
            
            // Web services
            Register("KrokiDiagram", _ => KrokiDiagram.StaticMetadata);
            
            // AI
            Register("AI", _ => AIContentCreator.StaticMetadata);
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
