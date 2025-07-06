using Microsoft.CodeAnalysis.Scripting;
using RoboClerk.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk.ContentCreators
{
    abstract public class RequirementBase : MultiItemContentCreator
    {
        protected List<RequirementItem> requirements = null;
        protected string requirementName = string.Empty;
        protected TraceEntity sourceType = null;

        public RequirementBase(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration config)
            : base(data, analysis, config)
        {
        }

        protected override string GenerateContent(RoboClerkTag tag, List<LinkedItem> items, TraceEntity te, TraceEntity docTE)
        {
            StringBuilder output = new StringBuilder();
            var dataShare = new ScriptingBridge(data, analysis, te,configuration);
            var file = data.GetTemplateFile($"./ItemTemplates/{configuration.OutputFormat}/Requirement.{(configuration.OutputFormat == "HTML" ? "html" : "adoc")}");
            var renderer = new ItemTemplateRenderer(file);
            foreach (var item in items)
            {
                RequirementItem reqItem = item as RequirementItem;
                if (reqItem == null) 
                {
                    throw new Exception("Item passed into requirement content creator is not a RequirementItem.");
                }
                string oldDescription = reqItem.RequirementDescription;
                //this will insert a tag in the description indicating where AI comments need to be included if an AI plugin is selected
                reqItem.RequirementDescription = TagFieldWithAIComment(reqItem.ItemID, reqItem.RequirementDescription);  
                dataShare.Item = reqItem;
                string result = string.Empty;
                try
                {
                    result = renderer.RenderItemTemplate(dataShare);
                }
                catch (CompilationErrorException e)
                {
                    logger.Error($"A compilation error occurred while compiling Requirement.adoc script: {e.Message}");
                    throw;
                }
                AddAITagsToContent(output, result, te.ID, reqItem.ItemID);
                //remove the tag to restore the original description
                reqItem.RequirementDescription = oldDescription;
            }
            ProcessTraces(docTE, dataShare);
            return output.ToString();
        }
    }
}
