using RoboClerk.Core.Configuration;
using RoboClerk.Core;

namespace RoboClerk.ContentCreators
{
    // content creators use datasources to create document content. They are also responsible for
    // ensuring that the trace is kept up to date with any trace items they add to a document.
    public interface IContentCreator
    {
        /// <summary>
        /// Gets content for the specified tag
        /// </summary>
        /// <param name="tag">The RoboClerk tag (unified interface)</param>
        /// <param name="doc">Document configuration</param>
        /// <returns>Content to replace the tag with</returns>
        public string GetContent(IRoboClerkTag tag, DocumentConfig doc);

        /// <summary>
        /// Gets metadata describing this content creator's capabilities, tags, and parameters.
        /// Note: Implementations should delegate to a static property for efficiency.
        /// </summary>
        /// <returns>Metadata describing the content creator</returns>
        public ContentCreatorMetadata GetMetadata();
    }

    /// <summary>
    /// Interface for content creators that provide static metadata without requiring instantiation
    /// </summary>
    public interface IStaticMetadataProvider
    {
        /// <summary>
        /// Gets the static metadata for this content creator type
        /// </summary>
        static abstract ContentCreatorMetadata StaticMetadata { get; }
    }
}
