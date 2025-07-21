using RoboClerk.Configuration;
using RoboClerk.AISystem;

namespace RoboClerk.ContentCreators
{
    /// <summary>
    /// Factory interface for creating content creators based on their source and contentcreator ID. 
    /// This allows for dependency injection to be used for content creator instantiation.
    /// </summary>
    public interface IContentCreatorFactory
    {
        /// <summary>
        /// Creates a content creator for the specified source type.
        /// </summary>
        /// <param name="source">The data source type</param>
        /// <param name="contentCreatorId">The content creator ID (used for dynamic resolution)</param>
        /// <param name="aiPlugin">Optional AI plugin for AI content creators</param>
        /// <returns>The appropriate content creator instance</returns>
        IContentCreator CreateContentCreator(DataSource source, string contentCreatorId = null);
    }
} 