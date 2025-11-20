using RoboClerk.AISystem;
using RoboClerk.Core.Configuration;
using RoboClerk.Core;
using System;
using System.IO.Abstractions;
using System.Text.Json;

namespace RoboClerk.ContentCreators
{
    internal class Comment
    {
        public string CommentContent { get; set; } = string.Empty;
        public string ID { get; set; } = string.Empty;
    }

    public class AIContentCreator : IContentCreator
    {
        private readonly IConfiguration? configuration = null;
        private readonly IAISystemPlugin? aiSystem = null;
        private readonly IDataSources? dataSources = null;
        private readonly ITraceabilityAnalysis? traceabilityAnalysis = null;
        private readonly IFileSystem? fileSystem = null;

        public AIContentCreator(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration configuration, IAISystemPlugin aiSystem, IFileSystem fileSystem) 
        {
            this.configuration = configuration;
            this.aiSystem = aiSystem;
            if (aiSystem == null) 
            {
                throw new ArgumentNullException("AI System Plugin is null while AI system feedback was requested. Ensure that the RoboClerk configuration file contains the correct AI system plugin name.");
            }
            this.dataSources = data;
            this.traceabilityAnalysis = analysis;
            this.fileSystem = fileSystem;
        }

        public ContentCreatorMetadata GetMetadata()
        {
            var metadata = new ContentCreatorMetadata("AI", "AI Feedback", 
                "Generates AI-based feedback on documentation items using configured AI plugin");
            
            metadata.Category = "AI Integration";

            var aiTag = new ContentCreatorTag("AIFeedback", "Generates AI feedback for a specific item");
            aiTag.Category = "AI Analysis";
            aiTag.Description = "Uses the configured AI plugin to analyze an item and provide feedback. " +
                "The feedback is saved to a JSON file and can be used for review and improvement of documentation.";
            
            aiTag.Parameters.Add(new ContentCreatorParameter("entity", 
                "The entity type of the item to analyze", 
                ParameterValueType.EntityType, required: true)
            {
                ExampleValue = "SystemRequirement"
            });
            
            aiTag.Parameters.Add(new ContentCreatorParameter("itemID", 
                "The ID of the specific item to analyze", 
                ParameterValueType.ItemID, required: true)
            {
                ExampleValue = "REQ-001"
            });
            
            aiTag.ExampleUsage = "@@AI:AIFeedback(entity=SystemRequirement,itemID=REQ-001)@@";
            metadata.Tags.Add(aiTag);

            return metadata;
        }

        public string GetContent(IRoboClerkTag tag, DocumentConfig doc)
        {
            //look up item
            TraceEntity te = null;
            Item item = null;
            if( tag.HasParameter("entity") && tag.HasParameter("itemID") )
            {
                te = traceabilityAnalysis.GetTraceEntityForID(tag.GetParameterOrDefault("entity",""));
                item = dataSources.GetItem(tag.GetParameterOrDefault("itemID", ""));
            }
            else
            {
                throw new Exception("One or both of the required AI parameters are not present in the AI tag.");
            }
            //get feedback and create comment object
            var comment = new Comment() { CommentContent = aiSystem.GetFeedback(te, item), ID = item.ItemID };
            //open json comment file
            var fn = fileSystem.Path.GetFileName(doc.DocumentTemplate);
            fn = fileSystem.Path.GetFileNameWithoutExtension(fn);
            fn = fn + "_AIComments.json";
            fn = fileSystem.Path.Join(configuration.OutputDir, fn);
            //write feedback including the identifier of the anchor put into the asciidoc
            var serializedComment = JsonSerializer.Serialize(comment);
            fileSystem.File.AppendAllText(fn, serializedComment + "\n");

            return tag.Contents;
        }
    }
}
