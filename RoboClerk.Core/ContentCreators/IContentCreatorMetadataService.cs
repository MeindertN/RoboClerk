using System.Collections.Generic;

namespace RoboClerk.ContentCreators
{
    /// <summary>
    /// Service for retrieving metadata about all available content creators
    /// </summary>
    public interface IContentCreatorMetadataService
    {
        /// <summary>
        /// Gets metadata for all registered content creators
        /// </summary>
        /// <returns>Collection of content creator metadata</returns>
        List<ContentCreatorMetadata> GetAllContentCreatorMetadata();

        /// <summary>
        /// Gets metadata for a specific content creator by source
        /// </summary>
        /// <param name="source">The source identifier (e.g., "SLMS", "Document", "FILE")</param>
        /// <returns>Metadata for the content creator, or null if not found</returns>
        ContentCreatorMetadata? GetContentCreatorMetadata(string source);
    }
}
