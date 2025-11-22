using RoboClerk.Core.Configuration;
using RoboClerk.Core;
using System;
using System.Collections.Generic;

namespace RoboClerk.ContentCreators
{
    public class Document : IContentCreator
    {
        private readonly ITraceabilityAnalysis analysis;
        
        public Document(ITraceabilityAnalysis analysis) 
        {
            this.analysis = analysis;
        }

        /// <summary>
        /// Static metadata for the Document content creator
        /// </summary>
        public static ContentCreatorMetadata StaticMetadata { get; } = new ContentCreatorMetadata("Document", "Document Properties", 
            "Provides access to document properties such as title, ID, abbreviation, and entity counts")
        {
            Category = "Document Information",
            Tags = new List<ContentCreatorTag>
            {
                new ContentCreatorTag("Title", "Returns the document title")
                {
                    ExampleUsage = "@@Document:Title()@@",
                    Category = "Basic Properties"
                },
                new ContentCreatorTag("Abbreviation", "Returns the document abbreviation")
                {
                    ExampleUsage = "@@Document:Abbreviation()@@",
                    Category = "Basic Properties"
                },
                new ContentCreatorTag("Identifier", "Returns the document identifier")
                {
                    ExampleUsage = "@@Document:Identifier()@@",
                    Category = "Basic Properties"
                },
                new ContentCreatorTag("Template", "Returns the document template path")
                {
                    ExampleUsage = "@@Document:Template()@@",
                    Category = "Basic Properties"
                },
                new ContentCreatorTag("RoboClerkID", "Returns the RoboClerk document ID")
                {
                    ExampleUsage = "@@Document:RoboClerkID()@@",
                    Category = "Basic Properties"
                },
                new ContentCreatorTag("GenDateTime", "Returns the current date and time of document generation")
                {
                    ExampleUsage = "@@Document:GenDateTime()@@",
                    Category = "Basic Properties"
                },
                new ContentCreatorTag("CountEntities", 
                    "Returns the count of entities of a specific type in the document, or resets the counter")
                {
                    Category = "Entity Counting",
                    Parameters = new List<ContentCreatorParameter>
                    {
                        new ContentCreatorParameter("entity", 
                            "The entity type to count (e.g., SystemRequirement, SoftwareRequirement, TestCase)", 
                            ParameterValueType.EntityType, required: true)
                        {
                            ExampleValue = "SystemRequirement"
                        },
                        new ContentCreatorParameter("restart", 
                            "Set to 'true' to reset the counter for this entity type", 
                            ParameterValueType.Boolean, required: false, defaultValue: "false")
                        {
                            AllowedValues = new List<string> { "true", "false" },
                            ExampleValue = "false"
                        }
                    },
                    ExampleUsage = "@@Document:CountEntities(entity=SystemRequirement)@@"
                }
            }
        };

        public ContentCreatorMetadata GetMetadata() => StaticMetadata;

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
