using RoboClerk.Core.Configuration;
using RoboClerk.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RoboClerk.ContentCreators
{
    internal class Reference : ContentCreatorBase
    {
        public Reference(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration conf) 
            : base(data,analysis, conf)
        {
        }

        /// <summary>
        /// Static metadata for the Reference content creator
        /// </summary>
        public static ContentCreatorMetadata StaticMetadata { get; } = new ContentCreatorMetadata("Reference", "Document Reference", 
            "Creates references to other RoboClerk documents")
        {
            Category = "Document Information",
            Tags = new List<ContentCreatorTag>
            {
                new ContentCreatorTag("[DocumentID]", "Creates a reference to another RoboClerk document that is included in the trace information")
                {
                    Category = "Cross-References",
                    Description = "Replace [DocumentID] with the actual RoboClerk document ID. " +
                        "Returns document properties based on specified parameters. " +
                        "If no parameters are specified, returns the document title.",
                    Parameters = new List<ContentCreatorParameter>
                    {
                        new ContentCreatorParameter("ID", 
                            "Include the document identifier in the reference", 
                            ParameterValueType.Boolean, required: false)
                        {
                            AllowedValues = new List<string> { "true", "false" },
                            ExampleValue = "true"
                        },
                        new ContentCreatorParameter("Title", 
                            "Include the document title in the reference", 
                            ParameterValueType.Boolean, required: false)
                        {
                            AllowedValues = new List<string> { "true", "false" },
                            ExampleValue = "true"
                        },
                        new ContentCreatorParameter("Abbr", 
                            "Include the document abbreviation in the reference", 
                            ParameterValueType.Boolean, required: false)
                        {
                            AllowedValues = new List<string> { "true", "false" },
                            ExampleValue = "true"
                        },
                        new ContentCreatorParameter("Template", 
                            "Include the document template path in the reference", 
                            ParameterValueType.Boolean, required: false)
                        {
                            AllowedValues = new List<string> { "true", "false" },
                            ExampleValue = "false"
                        }
                    },
                    ExampleUsage = "@@Reference:SystemRequirementsSpec(ID=true,Title=true,Abbr=true)@@"
                }
            }
        };

        public override ContentCreatorMetadata GetMetadata() => StaticMetadata;

        public override string GetContent(IRoboClerkTag tag, DocumentConfig doc)
        {
            StringBuilder result = new StringBuilder();
            DocumentConfig? reference = null;
            foreach(var docConfig in configuration.Documents)
            {
                if (docConfig.RoboClerkID == tag.ContentCreatorID)
                {
                    reference = docConfig;
                    break;
                }
            }

            if (reference == null)
            {
                var ex = new TagInvalidException(tag.Contents, $"Reference tag is referencing an unknown document: {tag.ContentCreatorID}");
                ex.DocumentTitle = doc.DocumentTitle;
                throw ex;
            }
            
            if (tag.HasParameter("ID") && tag.GetParameterOrDefault("ID", string.Empty).ToUpper() == "TRUE")
            {
                result.Append(reference.DocumentID);
            }
            if (tag.HasParameter("TITLE") && tag.GetParameterOrDefault("TITLE", string.Empty).ToUpper() == "TRUE")
            {
                if(result.Length > 0) 
                {
                    result.Append($" {reference.DocumentTitle}");
                }
                else
                {
                    result.Append(reference.DocumentTitle);
                }
            }
            if (tag.HasParameter("ABBR") && tag.GetParameterOrDefault("ABBR", string.Empty).ToUpper() == "TRUE")
            {
                if(result.Length > 0) 
                {
                    result.Append($" ({reference.DocumentAbbreviation})");
                }
                else
                {
                    result.Append(reference.DocumentAbbreviation);
                }
            }
            if (tag.HasParameter("TEMPLATE") && tag.GetParameterOrDefault("TEMPLATE", string.Empty).ToUpper() == "TRUE")
            {
                if (result.Length > 0)
                {
                    result.Append($" {reference.DocumentTemplate}");
                }
                else
                {
                    result.Append(reference.DocumentTemplate);
                }
            }
            if (tag.Parameters.Count() == 0)
            {
                result.Append(reference.DocumentTitle);
            }
            else
            {
                List<string> validParameters = new List<string>() { "TEMPLATE", "ABBR", "TITLE", "ID" };
                foreach (var parameter in tag.Parameters)
                {
                    if (!validParameters.Contains(parameter))
                    {
                        var ex = new TagInvalidException(tag.Contents, $"Reference tag has an unknown parameter: {parameter}");
                        ex.DocumentTitle = doc.DocumentTitle;
                        throw ex;
                    }
                }
            }
            
            analysis.AddTrace(analysis.GetTraceEntityForTitle(doc.DocumentTitle), tag.ContentCreatorID, analysis.GetTraceEntityForID(reference.RoboClerkID), tag.ContentCreatorID);
            return result.ToString();
        }
    }
}
