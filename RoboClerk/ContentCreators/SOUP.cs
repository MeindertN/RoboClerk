﻿using RoboClerk.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace RoboClerk.ContentCreators
{
    public class SOUP : ContentCreatorBase
    {
        public SOUP()
        {

        }

        private string GenerateBriefADOC(List<SOUPItem> items, TraceEntity sourceType, RoboClerkTag tag, PropertyInfo[] properties, ITraceabilityAnalysis analysis, TraceEntity docType)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("|====");
            sb.AppendLine($"| {sourceType.Name} ID | {sourceType.Name} Name and Version");
            sb.AppendLine();

            foreach (var item in items)
            {
                if (ShouldBeIncluded<SOUPItem>(tag, item, properties))
                {
                    sb.Append(item.HasLink ? $"| {item.Link}[{item.ItemID}]" : $"| {item.ItemID} ");
                    sb.AppendLine($"| {item.SOUPTitle}");
                    sb.AppendLine();
                    analysis.AddTrace(sourceType, item.ItemID, docType, item.ItemID);
                }
            }
            sb.AppendLine("|====");
            return sb.ToString();
        }

        private string GenerateADOC(SOUPItem item, TraceEntity sourceType)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("|====");
            sb.Append($"| {sourceType.Name} ID: ");
            sb.AppendLine(item.HasLink ? $"| {item.Link}[{item.ItemID}]" : $"| {item.ItemID}");
            sb.AppendLine();

            sb.Append($"| {sourceType.Name} Revision: ");
            sb.AppendLine($"| {item.SOUPRevision}");
            sb.AppendLine();

            sb.Append($"| {sourceType.Name} Name and Version: ");
            sb.AppendLine($"| {item.SOUPTitle}");
            sb.AppendLine();

            sb.Append($"| Is {sourceType.Name} Critical for Performance: ");
            sb.AppendLine($"| {item.SOUPPerformanceCriticalText}");
            sb.AppendLine();

            sb.Append($"| Is {sourceType.Name} Critical for Cyber Security: ");
            sb.AppendLine($"| {item.SOUPCybersecurityCriticalText}");
            sb.AppendLine();

            if (item.SOUPPerformanceCritical)
            {
                sb.Append("| Result Anomaly List Examination: ");
                sb.AppendLine($"| {item.SOUPAnomalyListDescription}");
                sb.AppendLine();
            }

            sb.Append($"| Is {sourceType.Name} Installed by End-User: ");
            sb.AppendLine($"| {item.SOUPInstalledByUserText}");
            sb.AppendLine();

            if (item.SOUPInstalledByUser)
            {
                sb.Append("| Required End-User Training: ");
                sb.AppendLine($"| {item.SOUPEnduserTraining}");
                sb.AppendLine();
            }

            sb.AppendLine("| Detailed Description: ");
            sb.AppendLine($"a| {item.SOUPDetailedDescription}");
            sb.AppendLine();

            sb.Append($"| {sourceType.Name} License: ");
            sb.AppendLine($"| {item.SOUPLicense}");
            sb.AppendLine("|====");
            return sb.ToString();
        }

        public override string GetContent(RoboClerkTag tag, IDataSources sources, ITraceabilityAnalysis analysis, DocumentConfig doc)
        {
            bool foundSOUP = false;
            var soups = sources.GetAllSOUP();
            //No selection needed, we return everything
            StringBuilder output = new StringBuilder();
            var properties = typeof(SOUPItem).GetProperties();
            var sourceType = analysis.GetTraceEntityForID("SOUP");

            if (tag.Parameters.ContainsKey("BRIEF") && tag.Parameters["BRIEF"].ToUpper() == "TRUE")
            {
                if (soups.Count > 0)
                {
                    foundSOUP = true;
                    output.AppendLine(GenerateBriefADOC(soups, sourceType, tag, properties, analysis, analysis.GetTraceEntityForTitle(doc.DocumentTitle)));
                }
            }
            else foreach (var soup in soups)
            {
                if (ShouldBeIncluded<SOUPItem>(tag, soup, properties))
                {
                    foundSOUP = true;
                    try
                    {
                        output.AppendLine(GenerateADOC(soup, sourceType));
                    }
                    catch
                    {
                        logger.Error($"An error occurred while rendering SOUP {soup.ItemID} in {doc.DocumentTitle}.");
                        throw;
                    }
                    analysis.AddTrace(analysis.GetTraceEntityForID("SOUP"), soup.ItemID, analysis.GetTraceEntityForTitle(doc.DocumentTitle), soup.ItemID);
                }
            }
            if (!foundSOUP)
            {
                string soupName = sourceType.Name;
                return $"Unable to find {soupName}(s). Check if {soupName}s of the correct type are provided or if a valid {soupName} identifier is specified.";
            }
            return output.ToString();
        }
    }
}
