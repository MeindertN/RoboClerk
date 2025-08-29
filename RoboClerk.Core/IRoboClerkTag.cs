using System.Collections.Generic;

namespace RoboClerk.Core
{
    /// <summary>
    /// General interface for RoboClerk tags that supports both text and docx based tags
    /// </summary>
    public interface IRoboClerkTag
    {
        /// <summary>
        /// The data source for this tag (e.g., Source, Document, Trace, etc.)
        /// </summary>
        DataSource Source { get; }
        
        /// <summary>
        /// The content creator ID for dynamic resolution
        /// </summary>
        string ContentCreatorID { get; }
        
        /// <summary>
        /// Parameters associated with this tag
        /// </summary>
        IEnumerable<string> Parameters { get; }
        
        /// <summary>
        /// The contents of this tag (can contain nested RoboClerk tags)
        /// </summary>
        string Contents { get; set; }
        
        /// <summary>
        /// Gets a parameter value by name, or returns the default value if not found
        /// </summary>
        string GetParameterOrDefault(string parameterName, string defaultValue = "");
        
        /// <summary>
        /// Updates the tag content in the source document
        /// </summary>
        void UpdateContent(string newContent);
        
        /// <summary>
        /// Processes any nested RoboClerk tags within this tag's content
        /// </summary>
        IEnumerable<IRoboClerkTag> ProcessNestedTags();
        
        /// <summary>
        /// Checks if this tag has a specific parameter
        /// </summary>
        bool HasParameter(string parameterName);
        
        /// <summary>
        /// Indicates if this is an inline tag (for text-based tags)
        /// </summary>
        bool Inline { get; }
    }
}
