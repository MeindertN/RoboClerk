using RoboClerk.Core.Configuration;
using RoboClerk.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RoboClerk.ContentCreators
{
    public class Document : IContentCreator
    {
        private readonly ITraceabilityAnalysis analysis;
        private readonly IConfiguration configuration;
        
        public Document(ITraceabilityAnalysis analysis, IConfiguration conf) 
        {
            this.analysis = analysis;
            this.configuration = conf;
        }

        /// <summary>
        /// Gets metadata for the Document content creator, optionally using configuration to populate allowed values
        /// </summary>
        public static ContentCreatorMetadata GetMetadata(IConfiguration? config = null)
        {
            var entityAllowedValues = new List<string>();
            string entityExample = "SystemRequirement";

            if (config != null)
            {
                foreach (var entity in config.TruthEntities)
                {
                    entityAllowedValues.Add(entity.Name);
                }
                
                if (entityAllowedValues.Count > 0)
                {
                    entityExample = entityAllowedValues[0];
                }
            }

            var metadata = new ContentCreatorMetadata("Document", "Document Properties", 
                "Provides access to document properties such as title, ID, abbreviation, and entity counts")
            {
                Category = "Document Information",
                Tags = new List<ContentCreatorTag>
                {
                    new ContentCreatorTag("Title", "Returns the document title", "Document")
                    {
                        ExampleUsage = "@@Document:Title()@@",
                        Category = "Basic Properties"
                    },
                    new ContentCreatorTag("Abbreviation", "Returns the document abbreviation", "Document")
                    {
                        ExampleUsage = "@@Document:Abbreviation()@@",
                        Category = "Basic Properties"
                    },
                    new ContentCreatorTag("Identifier", "Returns the document identifier", "Document")
                    {
                        ExampleUsage = "@@Document:Identifier()@@",
                        Category = "Basic Properties"
                    },
                    new ContentCreatorTag("Template", "Returns the document template path", "Document")
                    {
                        ExampleUsage = "@@Document:Template()@@",
                        Category = "Basic Properties"
                    },
                    new ContentCreatorTag("RoboClerkID", "Returns the RoboClerk document ID", "Document")
                    {
                        ExampleUsage = "@@Document:RoboClerkID()@@",
                        Category = "Basic Properties"
                    },
                    new ContentCreatorTag("GenDateTime", "Returns the current date and time of document generation", "Document")
                    {
                        ExampleUsage = "@@Document:GenDateTime()@@",
                        Category = "Basic Properties"
                    },
                    new ContentCreatorTag("CountEntities", 
                        "Returns the count of entities of a specific type in the document, or resets the counter", "Document")
                    {
                        Category = "Entity Counting",
                        Parameters = new List<ContentCreatorParameter>
                        {
                            new ContentCreatorParameter("entity", 
                                "The entity type to count (e.g., SystemRequirement, SoftwareRequirement, TestCase)", 
                                ParameterValueType.EntityType, required: true)
                            {
                                ExampleValue = entityExample,
                                AllowedValues = entityAllowedValues.Count > 0 ? entityAllowedValues : null
                            },
                            new ContentCreatorParameter("restart", 
                                "Set to 'true' to reset the counter for this entity type", 
                                ParameterValueType.Boolean, required: false, defaultValue: "false")
                            {
                                AllowedValues = new List<string> { "true", "false" },
                                ExampleValue = "false"
                            }
                        },
                        ExampleUsage = $"@@Document:CountEntities(entity={entityExample})@@"
                    }
                }
            };
            return metadata;
        }

        /// <summary>
        /// Static metadata for the Document content creator
        /// </summary>
        public static ContentCreatorMetadata StaticMetadata { get; } = GetMetadata();

        public ContentCreatorMetadata GetMetadata() => GetMetadata(configuration);

        public string GetContent(IRoboClerkTag tag, DocumentConfig doc)
        {
            if (tag.ContentCreatorID.ToUpper() == "TITLE")
            {
                return doc.DocumentTitle;
            }
            else if (tag.ContentCreatorID.ToUpper() == "ABBREVIATION")
            {
                return doc.DocumentAbbreviation;
            }
            else if (tag.ContentCreatorID.ToUpper() == "IDENTIFIER")
            {
                return doc.DocumentID;
            }
            else if (tag.ContentCreatorID.ToUpper() == "TEMPLATE")
            {
                return doc.DocumentTemplate;
            }
            else if (tag.ContentCreatorID.ToUpper() == "ROBOCLERKID")
            {
                return doc.RoboClerkID;
            }
            else if (tag.ContentCreatorID.ToUpper() == "GENDATETIME")
            {
                return DateTime.Now.ToString("yyyy/MM/dd HH:mm");
            }
            else if (tag.ContentCreatorID.ToUpper() == "COUNTENTITIES")
            {
                if (tag.HasParameter("entity"))
                {
                    string entityName = tag.GetParameterOrDefault("entity");
                    if (entityName != null) 
                    {
                        string restart = tag.GetParameterOrDefault("restart");
                        TraceEntity te = analysis.GetTraceEntityForAnyProperty(entityName);
                        if (te == null)
                        {
                            throw new Exception($"RoboClerk was unable to find the entity \"{entityName}\" as specified in the document tag: \"{tag.Source}:{tag.ContentCreatorID}\" in \"{doc.RoboClerkID}\".");
                        }
                        if (restart != null && restart.ToUpper() == "TRUE")
                        {
                            //reset the counter and return an empty string
                            doc.ResetEntityCount(te);
                            return string.Empty;
                        }

                        return doc.GetEntityCount(te).ToString();
                    }
                }
            }
            throw new Exception($"RoboClerk did not know how to handle the document tag: \"{tag.Source}:{tag.ContentCreatorID}\" in \"{doc.RoboClerkID}\".");
        }
    }
}
