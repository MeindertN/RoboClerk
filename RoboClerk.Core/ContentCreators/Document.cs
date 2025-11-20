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

        public ContentCreatorMetadata GetMetadata()
        {
            var metadata = new ContentCreatorMetadata("Document", "Document Properties", 
                "Provides access to document properties such as title, ID, abbreviation, and entity counts");
            
            metadata.Category = "Document Information";

            // Title tag
            metadata.Tags.Add(new ContentCreatorTag("Title", "Returns the document title")
            {
                ExampleUsage = "@@Document:Title()@@",
                Category = "Basic Properties"
            });

            // Abbreviation tag
            metadata.Tags.Add(new ContentCreatorTag("Abbreviation", "Returns the document abbreviation")
            {
                ExampleUsage = "@@Document:Abbreviation()@@",
                Category = "Basic Properties"
            });

            // Identifier tag
            metadata.Tags.Add(new ContentCreatorTag("Identifier", "Returns the document identifier")
            {
                ExampleUsage = "@@Document:Identifier()@@",
                Category = "Basic Properties"
            });

            // Template tag
            metadata.Tags.Add(new ContentCreatorTag("Template", "Returns the document template path")
            {
                ExampleUsage = "@@Document:Template()@@",
                Category = "Basic Properties"
            });

            // RoboClerkID tag
            metadata.Tags.Add(new ContentCreatorTag("RoboClerkID", "Returns the RoboClerk document ID")
            {
                ExampleUsage = "@@Document:RoboClerkID()@@",
                Category = "Basic Properties"
            });

            // GenDateTime tag
            metadata.Tags.Add(new ContentCreatorTag("GenDateTime", "Returns the current date and time of document generation")
            {
                ExampleUsage = "@@Document:GenDateTime()@@",
                Category = "Basic Properties"
            });

            // CountEntities tag
            var countEntitiesTag = new ContentCreatorTag("CountEntities", 
                "Returns the count of entities of a specific type in the document, or resets the counter");
            countEntitiesTag.Category = "Entity Counting";
            countEntitiesTag.Parameters.Add(new ContentCreatorParameter("entity", 
                "The entity type to count (e.g., SystemRequirement, SoftwareRequirement, TestCase)", 
                ParameterValueType.EntityType, required: true)
            {
                ExampleValue = "SystemRequirement"
            });
            countEntitiesTag.Parameters.Add(new ContentCreatorParameter("restart", 
                "Set to 'true' to reset the counter for this entity type", 
                ParameterValueType.Boolean, required: false, defaultValue: "false")
            {
                AllowedValues = new List<string> { "true", "false" },
                ExampleValue = "false"
            });
            countEntitiesTag.ExampleUsage = "@@Document:CountEntities(entity=SystemRequirement)@@";
            metadata.Tags.Add(countEntitiesTag);

            return metadata;
        }

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
