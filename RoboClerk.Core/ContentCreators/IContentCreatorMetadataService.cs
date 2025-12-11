using System.Collections.Generic;
using RoboClerk.Core.Configuration;

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
        /// <param name="configuration">Optional configuration to customize metadata</param>
        /// <returns>Collection of content creator metadata</returns>
        List<ContentCreatorMetadata> GetAllContentCreatorMetadata(IConfiguration? configuration = null);

        /// <summary>
        /// Gets metadata for a specific content creator by source
        /// </summary>
        /// <param name="source">The source identifier (e.g., "SLMS", "Document", "FILE")</param>
        /// <param name="configuration">Optional configuration to customize metadata</param>
        /// <returns>Metadata for the content creator, or null if not found</returns>
        ContentCreatorMetadata? GetContentCreatorMetadata(string source, IConfiguration? configuration = null);
    }
}
