using Microsoft.CodeAnalysis.Scripting;
using RoboClerk.Core.Configuration;
using RoboClerk.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk.ContentCreators
{
    abstract public class RequirementBase : MultiItemContentCreator
    {
        protected List<RequirementItem> requirements = new List<RequirementItem>();
        protected string requirementName = string.Empty;
        protected TraceEntity sourceType = null;

        public RequirementBase(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration config)
            : base(data, analysis, config)
        {
        }

        protected override string GenerateContent(IRoboClerkTag tag, List<LinkedItem> items, TraceEntity te, TraceEntity docTE)
        {
            StringBuilder output = new StringBuilder();
            var dataShare = new ScriptingBridge(data, analysis, te,configuration);
            var extension = (configuration.OutputFormat == "ASCIIDOC" ? "adoc" : "html");
            //configuration.ProjectID will be empty when not running in server mode,
            //otherwise it will contain the unique project identifier
            var fileIdentifier = configuration.ProjectID+$"./ItemTemplates/{configuration.OutputFormat}/Requirement.{extension}";
            
            // Check if compiled template already exists in cache
            ItemTemplateRenderer renderer;
            if (ItemTemplateRenderer.ExistsInCache(fileIdentifier))
            {
                // Load existing compiled template from cache
                renderer = ItemTemplateRenderer.FromCachedTemplate(fileIdentifier);
            }
            else
            {
                // Create new renderer from string and compile template (will be cached automatically with fileIdentifier)
                var fileContent = data.GetTemplateFile($"./ItemTemplates/{configuration.OutputFormat}/Requirement.{extension}");
                renderer = ItemTemplateRenderer.FromString(fileContent, fileIdentifier);
            }
            
            foreach (var item in items)
            {
                RequirementItem? reqItem = item as RequirementItem;
                if (reqItem == null)
                {
                    throw new Exception("Item passed into requirement content creator is not a RequirementItem.");
                }
                string oldDescription = reqItem.RequirementDescription;
                //this will insert a tag in the description indicating where AI comments need to be included if an AI plugin is selected
                reqItem.RequirementDescription = TagFieldWithAIComment(reqItem.ItemID, reqItem.RequirementDescription);
                dataShare.Item = reqItem;
                
                string result = renderer.RenderItemTemplate(dataShare);
                AddAITagsToContent(output, result, te.ID, reqItem.ItemID);
                //remove the tag to restore the original description
                reqItem.RequirementDescription = oldDescription;
            }
            
            ProcessTraces(docTE, dataShare);
            return output.ToString();
        }
    }
}
