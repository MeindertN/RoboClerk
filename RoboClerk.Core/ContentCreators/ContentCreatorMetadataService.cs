using System.Collections.Generic;
using System.Linq;

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

        public List<ContentCreatorMetadata> GetAllContentCreatorMetadata()
        {
            logger.Debug("Gathering metadata from all registered content creators");

            var allMetadata = ContentCreatorMetadataRegistry.GetAllMetadata().ToList();

            logger.Info($"Collected metadata for {allMetadata.Count} content creators");
            return allMetadata;
        }

        public ContentCreatorMetadata? GetContentCreatorMetadata(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                logger.Warn("GetContentCreatorMetadata called with null or empty source");
                return null;
            }

            logger.Debug($"Getting metadata for content creator source: {source}");

            var metadata = ContentCreatorMetadataRegistry.GetMetadata(source);
            
            if (metadata == null)
            {
                logger.Warn($"Could not find metadata for source: {source}");
            }
            else
            {
                logger.Debug($"Found metadata for {source}: {metadata.Name}");
            }

            return metadata;
        }
    }
}
