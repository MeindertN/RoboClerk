using System.Collections.Generic;
using System.Linq;
using RoboClerk.Core.Configuration;
using RoboClerk.Core.FileProviders;

namespace RoboClerk.ContentCreators
{
    /// <summary>
    /// Service implementation for retrieving content creator metadata.
    /// Uses the static metadata registry for efficient, dependency-free metadata access.
    /// </summary>
    public class ContentCreatorMetadataService : IContentCreatorMetadataService
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public ContentCreatorMetadataService()
        {
            // No dependencies needed! Metadata is accessed statically.
        }

        public List<ContentCreatorMetadata> GetAllContentCreatorMetadata(IConfiguration? configuration = null, IFileProviderPlugin? fileProvider = null)
        {
            logger.Debug("Gathering metadata from all registered content creators");

            var allMetadata = ContentCreatorMetadataRegistry.GetAllMetadata(configuration, fileProvider).ToList();

            logger.Info($"Collected metadata for {allMetadata.Count} content creators");
            return allMetadata;
        }
    }
}
