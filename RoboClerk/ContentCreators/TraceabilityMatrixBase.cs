﻿using RoboClerk.Configuration;
using System;
using System.Text;

namespace RoboClerk.ContentCreators
{
    public abstract class TraceabilityMatrixBase : IContentCreator
    {
        protected TraceEntity truthSource = null;
        protected IDataSources data = null;
        protected ITraceabilityAnalysis analysis = null;
        protected IConfiguration configuration = null;

        public TraceabilityMatrixBase(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration configuration)
        {
            this.data = data;
            this.analysis = analysis;
            this.configuration = configuration;
        }

        public virtual string GetContent(RoboClerkTag tag, DocumentConfig doc)
        {
            if (truthSource == null)
            {
                throw new Exception("Truth source is null, unclear where to start tracing.");
            }

            var traceMatrix = analysis.PerformAnalysis(data, truthSource);

            if (traceMatrix.Count == 0)
            {
                throw new Exception($"{truthSource.Name} level trace matrix is empty.");
            }

            StringBuilder matrix = new StringBuilder();
            matrix.AppendLine("|====");
            foreach (var entry in traceMatrix)
            {
                if (entry.Key.ID == "SystemRequirement" || entry.Key.ID == "SoftwareRequirement" || entry.Key.ID == "Risk")
                {
                    matrix.Append($"| {entry.Key.Name}s ");
                }
                else
                {
                    matrix.Append($"| {entry.Key.Abbreviation} ");
                }
            }
            matrix.AppendLine();

            for (int index = 0; index < traceMatrix[truthSource].Count; ++index)
            {
                foreach (var entry in traceMatrix)
                {
                    if (entry.Value[index].Count == 0)
                    {
                        matrix.Append("| N/A ");
                    }
                    else
                    {
                        StringBuilder combinedString = new StringBuilder();
                        foreach (Item item in entry.Value[index])
                        {
                            if (item == null)
                            {
                                combinedString.Append("MISSING");
                            }
                            else
                            {
                                if(entry.Key.EntityType == TraceEntityType.Document)
                                {
                                    combinedString.Append("Trace Present");
                                }
                                else
                                {
                                    combinedString.Append(item.HasLink ? $"{item.Link}[{item.ItemID}]" : item.ItemID);
                                }                                
                            }
                            combinedString.Append(", ");
                        }
                        combinedString.Remove(combinedString.Length - 2, 2); //remove extra comma and space
                        matrix.Append($"| {combinedString.ToString()} ");
                    }
                }
                matrix.AppendLine();
            }
            matrix.AppendLine("|====");
            matrix.AppendLine();

            matrix.AppendLine("\nTrace issues:\n");
            bool traceIssuesFound = false;
            //now visualize the trace issues, first the truth
            var truthTraceIssues = analysis.GetTraceIssuesForTruth(truthSource);
            foreach (var issue in truthTraceIssues)
            {
                traceIssuesFound = true;
                Item item = data.GetItem(issue.SourceID);
                matrix.AppendLine($". {truthSource.Name} {(item.HasLink ? $"{item.Link}[{item.ItemID}]" : item.ItemID)} is potentially missing a corresponding {issue.Target.Name}.");
            }

            foreach (var tet in traceMatrix)
            {
                if (tet.Key.ID == "SystemRequirement" || tet.Key.ID == "SoftwareRequirement" ||
                    tet.Key.ID == "SoftwareSystemTest" || tet.Key.ID == "UnitTest" ||
                    tet.Key.ID == "Anomaly" || tet.Key.ID == "Risk" ||
                    tet.Key.ID == "DocumentationRequirement" || tet.Key.ID == "DocContent" ||
                    tet.Key.ID == "SOUP") //skip the truth entity types
                {
                    continue;
                }

                var traceIssues = analysis.GetTraceIssuesForDocument(tet.Key);
                foreach (var issue in traceIssues)
                {
                    traceIssuesFound = true;
                    string sourceTitle = issue.Source.Name;
                    string targetTitle = issue.Target.Name;
                    Item item = data.GetItem(issue.SourceID);
                    string sourceID = issue.SourceID;
                    string targetID = issue.TargetID;
                    if (item != null)
                    {
                        sourceID = (item.HasLink ? $"{item.Link}[{item.ItemID}]" : item.ItemID);
                    }
                    if (issue.IssueType == TraceIssueType.Extra)
                    {
                        matrix.AppendLine($". An item with identifier {sourceID} appeared in {sourceTitle} without tracing to {targetTitle}.");
                    }
                    else if (issue.IssueType == TraceIssueType.Missing)
                    {
                        matrix.AppendLine($". An expected trace from {sourceID} in {sourceTitle} to {targetTitle} is missing.");
                    }
                    else if (issue.IssueType == TraceIssueType.PossiblyExtra)
                    {
                        matrix.AppendLine($". A possibly extra item with identifier {sourceID} appeared in {sourceTitle} without appearing in {targetTitle}.");
                    }
                    else if (issue.IssueType == TraceIssueType.PossiblyMissing)
                    {
                        matrix.AppendLine($". A possibly expected trace from {sourceID} in {sourceTitle} to {targetTitle} is missing.");
                    }
                    else if (issue.IssueType == TraceIssueType.Incorrect)
                    {
                        var targetItem = data.GetItem(targetID);
                        if (targetItem != null)
                        {
                            targetID = (targetItem.HasLink ? $"{targetItem.Link}[{targetItem.ItemID}]" : targetItem.ItemID);
                            matrix.AppendLine($". An incorrect trace was found in {sourceTitle} from {sourceID} to {targetID} where {targetID} was expected in {targetTitle} but was not found.");
                        }
                        else if (targetID != null)
                        {
                            matrix.AppendLine($". An incorrect trace was found in {sourceTitle} from {sourceID} to {targetID} where {targetID} was expected in {targetTitle} but was not a valid identifier.");
                        }
                        else
                        {
                            matrix.AppendLine($". A missing trace was detected in {sourceTitle}. The item with ID {sourceID} does not have a parent while it was expected to trace to {targetTitle}.");
                        }
                    }
                }
            }
            if (!traceIssuesFound)
            {
                matrix.AppendLine($"* No {truthSource.Name} level trace problems detected!");
            }
            return matrix.ToString();
        }
    }
}
