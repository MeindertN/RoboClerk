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
    }
}
