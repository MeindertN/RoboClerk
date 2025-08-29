using System.Collections.Generic;
using System.IO;

namespace RoboClerk.Core
{
    /// <summary>
    /// General interface for documents that supports both text and docx based documents
    /// </summary>
    public interface IDocument
    {
        /// <summary>
        /// Load document from stream
        /// </summary>
        void FromStream(Stream stream);
        
        /// <summary>
        /// Get all RoboClerk tags in the document
        /// </summary>
        IEnumerable<IRoboClerkTag> RoboClerkTags { get; }
        
        /// <summary>
        /// Document title
        /// </summary>
        string Title { get; set; }
        
        /// <summary>
        /// Template file path
        /// </summary>
        string TemplateFile { get; }
        
        /// <summary>
        /// Save document to file
        /// </summary>
        void SaveToFile(string filePath);
        
        /// <summary>
        /// Get document content as string (for text documents)
        /// </summary>
        string ToText();
        
        /// <summary>
        /// Update document from string content (for text documents)
        /// </summary>
        void FromString(string content);
        
        /// <summary>
        /// Indicates the document type (Text, Docx, etc.)
        /// </summary>
        DocumentType DocumentType { get; }
    }

    public enum DocumentType
    {
        Text,
        Docx
    }
}
