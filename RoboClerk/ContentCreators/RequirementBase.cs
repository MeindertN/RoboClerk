using RoboClerk.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RoboClerk.ContentCreators
{
    abstract public class RequirementBase : ContentCreatorBase
    {
        protected List<RequirementItem> requirements = null;
        protected string requirementName = string.Empty;
        protected TraceEntity sourceType = null;

        public RequirementBase()
        {

        }

        private string GenerateADOC(RequirementItem item, IDataSources sources)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("|====");
            sb.Append($"| {sourceType.Name} ID: ");
            sb.AppendLine(item.HasLink ? $"| {item.Link}[{item.ItemID}]" : $"| {item.ItemID}");
            sb.AppendLine();
            
            sb.Append($"| {sourceType.Name} Revision: ");
            sb.AppendLine($"| {item.ItemRevision}");
            sb.AppendLine();
            
            sb.Append($"| {sourceType.Name} Category: ");
            sb.AppendLine($"| {item.ItemCategory}");
            sb.AppendLine();
            
            sb.Append("| Parent ID: ");
            sb.AppendLine($"| {GetLinkedField(item,sources,ItemLinkType.Parent)}");
            sb.AppendLine();
            
            sb.Append("| Title: ");
            sb.AppendLine($"| {item.ItemTitle}");
            sb.AppendLine();
            
            sb.AppendLine("| Description: ");
            sb.AppendLine($"a| {item.RequirementDescription}");
            sb.AppendLine("|====");
            return sb.ToString();
        }

        public override string GetContent(RoboClerkTag tag, IDataSources sources, ITraceabilityAnalysis analysis, DocumentConfig doc)
        {
            bool foundRequirement = false;
            //No selection needed, we return everything
            StringBuilder output = new StringBuilder();
            var properties = typeof(RequirementItem).GetProperties();
            foreach (var requirement in requirements)
            {
                if (ShouldBeIncluded(tag, requirement, properties) && CheckUpdateDateTime(tag, requirement))
                {
                    foundRequirement = true;
                    try
                    {
                        output.AppendLine(GenerateADOC(requirement,sources));
                    }
                    catch
                    {
                        logger.Error($"An error occurred while rendering requirement {requirement.ItemID} in {doc.DocumentTitle}.");
                        throw;
                    }
                    analysis.AddTrace(sourceType, requirement.ItemID, analysis.GetTraceEntityForTitle(doc.DocumentTitle), requirement.ItemID);
                }
            }
            if (!foundRequirement)
            {
                return $"Unable to find {requirementName}(s). Check if {requirementName}s of the correct type are provided or if a valid {requirementName} identifier is specified.";
            }
            return output.ToString();
        }
    }
}
