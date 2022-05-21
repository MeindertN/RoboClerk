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

        private string GenerateMarkdown(RequirementItem item, IDataSources sources)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("|====");
            sb.Append($"| {sourceType.Name} ID: ");
            sb.AppendLine(item.HasLink ? $"| {item.Link}[{item.ItemID}]" : $"| {item.ItemID}");
            sb.AppendLine();
            
            sb.Append($"| {sourceType.Name} Revision: ");
            sb.AppendLine($"| {item.RequirementRevision}");
            sb.AppendLine();
            
            sb.Append($"| {sourceType.Name} Category: ");
            sb.AppendLine($"| {item.ItemCategory}");
            sb.AppendLine();
            
            sb.Append("| Parent ID: ");
            sb.AppendLine($"| {GetParentField(item,sources)}");
            sb.AppendLine();
            
            sb.Append("| Title: ");
            sb.AppendLine($"| {item.RequirementTitle}");
            sb.AppendLine();
            
            sb.AppendLine("| Description: ");
            sb.AppendLine($"a| {item.RequirementDescription}");
            sb.AppendLine("|====");
            return sb.ToString();
        }

        private string GetParentField(RequirementItem item, IDataSources data)
        {
            StringBuilder parentField = new StringBuilder();
            var parents = item.LinkedItems.Where(x => x.LinkType == ItemLinkType.Parent);
            if (parents.Count() > 0)
            {
                foreach (var parent in parents)
                {
                    if (parentField.Length > 0)
                    {
                        parentField.Append(" / ");
                    }
                    var parentItem = data.GetItem(parent.TargetID) as RequirementItem;
                    if (parentItem != null)
                    {
                        parentField.Append(parentItem.HasLink ? $"{parentItem.Link}[{parentItem.ItemID}]" : parentItem.ItemID);
                        parentField.Append($": \"{parentItem.RequirementTitle}\"");
                    }
                    else
                    {
                        parentField.Append(parent.TargetID);
                    }
                }
                return parentField.ToString();
            }
            return "N/A";
        }

        public override string GetContent(RoboClerkTag tag, IDataSources sources, ITraceabilityAnalysis analysis, DocumentConfig doc)
        {
            bool foundRequirement = false;
            //No selection needed, we return everything
            StringBuilder output = new StringBuilder();
            var properties = typeof(RequirementItem).GetProperties();
            foreach (var requirement in requirements)
            {
                if (ShouldBeIncluded(tag, requirement, properties))
                {
                    foundRequirement = true;
                    try
                    {
                        output.AppendLine(GenerateMarkdown(requirement,sources));
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
