using RoboClerk.Core;
using RoboClerk.Core.DocxSupport;
using System.Collections.Concurrent;
using IConfiguration = RoboClerk.Core.Configuration.IConfiguration;

namespace RoboClerk.Server.Models
{
    /// <summary>
    /// Manages virtual content controls for documents, allowing dynamic content control creation
    /// without modifying the original document structure.
    /// </summary>
    public class DocumentContentControlManager
    {
        private readonly ConcurrentDictionary<string, List<VirtualDocxTag>> virtualTagsByDocument = new();
        
        /// <summary>
        /// Gets or creates a virtual content control for the specified document and content control ID.
        /// </summary>
        /// <param name="documentId">The document ID</param>
        /// <param name="contentControlId">The content control ID</param>
        /// <param name="source">The data source for the tag</param>
        /// <param name="contentCreatorId">The content creator ID</param>
        /// <param name="configuration">The configuration to use</param>
        /// <returns>A virtual RoboClerkDocxTag instance</returns>
        public VirtualDocxTag GetOrCreateContentControl(
            string documentId, 
            string contentControlId, 
            string roboclerkTag, 
            IConfiguration configuration)
        {
            var documentTags = virtualTagsByDocument.GetOrAdd(documentId, _ => new List<VirtualDocxTag>());
            
            // Thread-safe search and creation
            lock (documentTags)
            {
                var existingTag = documentTags.FirstOrDefault(t => t.ContentControlId == contentControlId);
                if (existingTag != null)
                {
                    return existingTag;
                }
                
                // Create new virtual tag
                var newTag = new VirtualDocxTag(contentControlId, roboclerkTag, configuration);
                documentTags.Add(newTag);
                return newTag;
            }
        }

        /// <summary>
        /// Gets all virtual tags for a specific document.
        /// </summary>
        /// <param name="documentId">The document ID</param>
        /// <returns>A list of virtual tags for the document</returns>
        public List<VirtualDocxTag> GetVirtualTagsForDocument(string documentId)
        {
            if (virtualTagsByDocument.TryGetValue(documentId, out var tags))
            {
                lock (tags)
                {
                    return new List<VirtualDocxTag>(tags);
                }
            }
            return new List<VirtualDocxTag>();
        }

        /// <summary>
        /// Clears all virtual tags for a specific document.
        /// This should be called when a document is refreshed.
        /// </summary>
        /// <param name="documentId">The document ID to clear virtual tags for</param>
        /// <returns>The number of virtual tags that were cleared</returns>
        public int ClearVirtualTagsForDocument(string documentId)
        {
            if (virtualTagsByDocument.TryGetValue(documentId, out var tags))
            {
                lock (tags)
                {
                    int count = tags.Count;
                    tags.Clear();
                    return count;
                }
            }
            return 0;
        }

        /// <summary>
        /// Clears all virtual tags for all documents.
        /// This should be called when the entire project is refreshed.
        /// </summary>
        /// <returns>The total number of virtual tags that were cleared</returns>
        public int ClearAllVirtualTags()
        {
            int totalCleared = 0;
            
            foreach (var kvp in virtualTagsByDocument)
            {
                lock (kvp.Value)
                {
                    totalCleared += kvp.Value.Count;
                    kvp.Value.Clear();
                }
            }
            
            return totalCleared;
        }

        /// <summary>
        /// Gets the total count of virtual tags across all documents.
        /// </summary>
        /// <returns>The total number of virtual tags</returns>
        public int GetTotalVirtualTagCount()
        {
            int total = 0;
            
            foreach (var kvp in virtualTagsByDocument)
            {
                lock (kvp.Value)
                {
                    total += kvp.Value.Count;
                }
            }
            
            return total;
        }

        /// <summary>
        /// Gets statistics about virtual tags per document.
        /// </summary>
        /// <returns>A dictionary with document IDs and their virtual tag counts</returns>
        public Dictionary<string, int> GetVirtualTagStatistics()
        {
            var stats = new Dictionary<string, int>();
            
            foreach (var kvp in virtualTagsByDocument)
            {
                lock (kvp.Value)
                {
                    stats[kvp.Key] = kvp.Value.Count;
                }
            }
            
            return stats;
        }
    }
}