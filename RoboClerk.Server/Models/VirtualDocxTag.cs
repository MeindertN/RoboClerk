using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using RoboClerk.Core;
using RoboClerk.Core.Configuration;
using RoboClerk.Core.DocxSupport;
using IConfiguration = RoboClerk.Core.Configuration.IConfiguration;

namespace RoboClerk.Server.Models
{
    /// <summary>
    /// Virtual implementation of RoboClerkDocxTag that doesn't modify the original document.
    /// Used for dynamic content control creation in the Word add-in scenario.
    /// </summary>
    public class VirtualDocxTag : RoboClerkDocxTag
    {
        private readonly string virtualContentControlId;

        public VirtualDocxTag(string contentControlId, string roboclerkTag, IConfiguration? configuration = null)
            : base(CreateVirtualContentControl(contentControlId, roboclerkTag), configuration)
        {
            virtualContentControlId = contentControlId;
        }

        /// <summary>
        /// Creates a minimal SDT element that serves as a placeholder for OpenXML generation
        /// without affecting any actual document structure.
        /// </summary>
        private static SdtElement CreateVirtualContentControl(string contentControlId, string roboclerkTag)
        {
            // Parse the content control ID to int, defaulting to a hash if not numeric
            int numericId;
            if (!int.TryParse(contentControlId, out numericId))
            {
                // Generate a deterministic hash for non-numeric IDs
                numericId = Math.Abs(contentControlId.GetHashCode());
            }

            // Create a minimal SDT element structure
            var sdt = new SdtBlock();
            var properties = new SdtProperties();
            properties.Append(new SdtId() { Val = numericId });
            properties.Append(new Tag() { Val = roboclerkTag }); // Will be overridden by property setting
            sdt.Append(properties);
            sdt.Append(new SdtContentBlock());
            
            return sdt;
        }

        /// <summary>
        /// Override to return the virtual content control ID
        /// </summary>
        public new string ContentControlId => virtualContentControlId;
    }
}