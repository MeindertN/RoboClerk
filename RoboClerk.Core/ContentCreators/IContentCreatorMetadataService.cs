using System.Collections.Generic;
using RoboClerk.Core.Configuration;
using RoboClerk.Core.FileProviders;

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
        /// <param name="fileProvider">Optional file provider to access file system</param>
        /// <returns>Collection of content creator metadata</returns>
        List<ContentCreatorMetadata> GetAllContentCreatorMetadata(IConfiguration? configuration = null, IFileProviderPlugin? fileProvider = null);

    }
}
