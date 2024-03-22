using RoboClerk.AISystem;
using RoboClerk.Configuration;
using System;
using System.IO.Abstractions;
using System.Text.Json;

namespace RoboClerk.ContentCreators
{
    internal class Comment
    {
        public string CommentContent { get; set; }
        public string ID { get; set; } 
    }

    public class AIContentCreator : IContentCreator
    {
        private readonly IConfiguration configuration = null;
        private readonly IAISystemPlugin aiSystem = null;
        private readonly IDataSources dataSources = null;
        private readonly ITraceabilityAnalysis traceabilityAnalysis = null;
        private readonly IFileSystem fileSystem = null;

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

        public string GetContent(RoboClerkTag tag, DocumentConfig doc)
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
