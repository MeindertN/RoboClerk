using System;
using System.Collections.Generic;
using RoboClerk.Core.Configuration;
using RoboClerk.Core.FileProviders;

namespace RoboClerk.ContentCreators
{
    /// <summary>
    /// Central registry for content creator metadata.
    /// All content creators should register their metadata here for discovery.
    /// </summary>
    public static class ContentCreatorMetadataRegistry
    {
        private static readonly Dictionary<string, Func<IConfiguration?, IFileProviderPlugin?, ContentCreatorMetadata>> _metadataProviders = new(StringComparer.OrdinalIgnoreCase);
        private static bool _initialized = false;
        private static readonly object _lock = new object();

        /// <summary>
        /// Registers a metadata provider for a content creator
        /// </summary>
        public static void Register(string key, Func<IConfiguration?, IFileProviderPlugin?, ContentCreatorMetadata> metadataProvider)
        {
            _metadataProviders[key] = metadataProvider;
        }

        /// <summary>
        /// Gets all registered metadata
        /// </summary>
        public static IEnumerable<ContentCreatorMetadata> GetAllMetadata(IConfiguration? configuration = null, IFileProviderPlugin? fileProvider = null)
        {
            EnsureInitialized();
            
            foreach (var provider in _metadataProviders.Values)
            {
                yield return provider(configuration, fileProvider);
            }
        }

        /// <summary>
        /// Gets metadata by key (source or name)
        /// </summary>
        public static ContentCreatorMetadata? GetMetadata(string key, IConfiguration? configuration = null, IFileProviderPlugin? fileProvider = null)
        {
            EnsureInitialized();
            
            if (_metadataProviders.TryGetValue(key, out var provider))
            {
                return provider(configuration, fileProvider);
            }
            
            // Try to find by source in the metadata
            foreach (var metadataProvider in _metadataProviders.Values)
            {
                var metadata = metadataProvider(configuration, fileProvider);
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
            Register("Document", (config, _) => Document.GetMetadata(config));
            Register("Reference", (config, _) => Reference.GetMetadata(config));
            Register("ConfigurationValue", (config, _) => ConfigurationValue.GetMetadata(config));
            
            // Trace and layout
            Register("Trace", (_, _) => Trace.StaticMetadata);
                                  
            // Requirements
            Register("SystemRequirement", (_, _) => SystemRequirement.StaticMetadata);
            Register("SoftwareRequirement", (_, _) => SoftwareRequirement.StaticMetadata);
            Register("DocumentationRequirement", (_, _) => DocumentationRequirement.StaticMetadata);
            
            // Tests
            Register("SoftwareSystemTest", (_, _) => SoftwareSystemTest.StaticMetadata);
            Register("UnitTest", (_, _) => UnitTest.StaticMetadata);
            
            // Other SLMS items
            Register("Anomaly", (_, _) => Anomaly.StaticMetadata);
            Register("Risk", (_, _) => Risk.StaticMetadata);
            Register("SOUP", (_, _) => SOUP.StaticMetadata);
            Register("DocContent", (_, _) => DocContent.StaticMetadata);
            Register("Eliminated", (_, _) => Eliminated.StaticMetadata);
            
            // Traceability matrices
            Register("TraceMatrix", (config, _) => TraceMatrix.GetMetadata(config)); // Generic matrix
            Register("SystemLevelTraceabilityMatrix", (_, _) => SystemLevelTraceabilityMatrix.StaticMetadata);
            Register("SoftwareLevelTraceabilityMatrix", (_, _) => SoftwareLevelTraceabilityMatrix.StaticMetadata);
            Register("RiskTraceabilityMatrix", (_, _) => RiskTraceabilityMatrix.StaticMetadata);
            
            // File operations
            Register("ExcelTable", (config, fileProvider) => ExcelTable.GetMetadata(config, fileProvider));
            Register("TemplateSection", (config, fileProvider) => TemplateSection.GetMetadata(config, fileProvider));
            
            // Web services
            Register("KrokiDiagram", (_, _) => KrokiDiagram.StaticMetadata);
            
            // AI
            Register("AI", (config, _) => AIContentCreator.GetMetadata(config));
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
        /// Forces a refresh of the registry by marking it as uninitialized.
        /// The next call to get metadata will re-register all content creators.
        /// </summary>
        public static void Refresh()
        {
            lock (_lock)
            {
                _initialized = false;
            }
        }

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
