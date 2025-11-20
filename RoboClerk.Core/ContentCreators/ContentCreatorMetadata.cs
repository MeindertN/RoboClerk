using System.Collections.Generic;

namespace RoboClerk.ContentCreators
{
    /// <summary>
    /// Describes the type of a parameter value
    /// </summary>
    public enum ParameterValueType
    {
        /// <summary>String value</summary>
        String,
        /// <summary>Boolean value (true/false)</summary>
        Boolean,
        /// <summary>Integer value</summary>
        Integer,
        /// <summary>Date/time value</summary>
        DateTime,
        /// <summary>Item ID reference</summary>
        ItemID,
        /// <summary>Entity type reference</summary>
        EntityType,
        /// <summary>Document reference</summary>
        DocumentReference,
        /// <summary>File path/name</summary>
        FilePath,
        /// <summary>Range specification</summary>
        Range
    }

    /// <summary>
    /// Describes a parameter accepted by a content creator
    /// </summary>
    public class ContentCreatorParameter
    {
        /// <summary>
        /// Parameter name (as it appears in tags)
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable description of the parameter
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Type of value this parameter accepts
        /// </summary>
        public ParameterValueType ValueType { get; set; }

        /// <summary>
        /// Whether this parameter is required
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        /// Default value if not specified (null if no default)
        /// </summary>
        public string? DefaultValue { get; set; }

        /// <summary>
        /// List of allowed values (null if any value is allowed)
        /// </summary>
        public List<string>? AllowedValues { get; set; }

        /// <summary>
        /// Example value for this parameter
        /// </summary>
        public string? ExampleValue { get; set; }

        public ContentCreatorParameter()
        {
        }

        public ContentCreatorParameter(string name, string description, ParameterValueType valueType, bool required = false, string? defaultValue = null)
        {
            Name = name;
            Description = description;
            ValueType = valueType;
            Required = required;
            DefaultValue = defaultValue;
        }
    }

    /// <summary>
    /// Describes a tag/function provided by a content creator
    /// </summary>
    public class ContentCreatorTag
    {
        /// <summary>
        /// Tag identifier (the part after the colon in @@Source:TagID@@)
        /// </summary>
        public string TagID { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable description of what this tag does
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Parameters accepted by this tag
        /// </summary>
        public List<ContentCreatorParameter> Parameters { get; set; } = new List<ContentCreatorParameter>();

        /// <summary>
        /// Example usage of this tag
        /// </summary>
        public string? ExampleUsage { get; set; }

        /// <summary>
        /// Category for grouping similar tags
        /// </summary>
        public string? Category { get; set; }

        public ContentCreatorTag()
        {
        }

        public ContentCreatorTag(string tagID, string description)
        {
            TagID = tagID;
            Description = description;
        }
    }

    /// <summary>
    /// Complete metadata describing a content creator's capabilities
    /// </summary>
    public class ContentCreatorMetadata
    {
        /// <summary>
        /// Source identifier for the content creator (e.g., "SLMS", "Document", "FILE")
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable name of the content creator
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the content creator's purpose
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// List of tags/functions provided by this content creator
        /// </summary>
        public List<ContentCreatorTag> Tags { get; set; } = new List<ContentCreatorTag>();

        /// <summary>
        /// Category for grouping content creators
        /// </summary>
        public string? Category { get; set; }

        public ContentCreatorMetadata()
        {
        }

        public ContentCreatorMetadata(string source, string name, string description)
        {
            Source = source;
            Name = name;
            Description = description;
        }
    }
}
