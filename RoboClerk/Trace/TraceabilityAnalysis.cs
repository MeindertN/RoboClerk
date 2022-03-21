using RoboClerk.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RoboClerk
{

    public class TraceabilityAnalysis : ITraceabilityAnalysis
    {
        private IConfiguration configuration = null;
        private List<TraceEntity> traceEntities = new List<TraceEntity>();
        private Dictionary<string, List<TraceLink>> documentTraceLinks = new Dictionary<string, List<TraceLink>>();
        private Dictionary<string, List<TraceIssue>> documentTraceIssues = new Dictionary<string, List<TraceIssue>>();
        private Dictionary<TraceEntity, List<TraceIssue>> truthTraceIssues = new Dictionary<TraceEntity, List<TraceIssue>>();
        private readonly List<TraceSpecification> systemRequirementTraces = new List<TraceSpecification>();
        private readonly List<TraceSpecification> softwareRequirementTraces = new List<TraceSpecification>();

        public TraceabilityAnalysis(IConfiguration config)
        {
            configuration = config;
            Configure();
        }

        private void Configure()
        {
            //Truth entities
            var truth = configuration.TruthEntities;
            CheckTruthEntities(truth);
            foreach (var entity in truth)
            {
                foreach (var el in traceEntities)
                {
                    if (entity.Abbreviation == el.Abbreviation ||
                        entity.Name == el.Name)
                    {
                        throw new Exception($"Detected a duplicate abbreviation or name in Truth.{entity.ID}. All IDs, names and abbreviations must be unique.");
                    }
                }
                traceEntities.Add(entity);
            }
            //document trace entity extraction
            foreach (var docloc in configuration.Documents)
            {
                TraceEntity entity = new TraceEntity(docloc.RoboClerkID, docloc.DocumentTitle, docloc.DocumentAbbreviation);
                foreach (var el in traceEntities)
                {
                    if (entity.Abbreviation == el.Abbreviation ||
                        entity.Name == el.Name)
                    {
                        throw new Exception($"Detected a duplicate abbreviation or name in Document.{docloc.RoboClerkID}. All IDs, names and abbreviations must be unique.");
                    }
                }
                traceEntities.Add(entity);
            }
            //trace configuration extraction
            foreach (var truthID in configuration.TraceConfig)
            {
                TraceEntity source, target;
                foreach (var doc in truthID.Traces)
                {
                    source = traceEntities.Find(f => (f.ID == truthID.ID));
                    target = traceEntities.Find(f => (f.ID == doc.Key));
                    if (source == null || target == null)
                    {
                        throw new Exception($"Error setting up requested trace from {truthID.ID} to {doc.Key}. Check if both these entities are correctly defined in the project config file.");
                    }
                    bool completeTrace = false;
                    if (doc.Value.Count == 0 || doc.Value[0].ToUpper() == "ALL")
                    {
                        completeTrace = true;
                    }
                    TraceSpecification dt = new TraceSpecification(source, target, completeTrace);
                    if (!completeTrace && doc.Value[0].ToUpper() != "OPTIONAL")
                    {
                        dt.SelectedCategories = doc.Value;
                    }
                    if (truthID.ID == "SystemRequirement")
                    {
                        systemRequirementTraces.Add(dt);
                    }
                    else if (truthID.ID == "SoftwareRequirement")
                    {
                        softwareRequirementTraces.Add(dt);
                    }
                    else
                    {
                        throw new Exception("Root trace must be from either SystemRequirement or SoftwareRequirement");
                    }
                }
            }
        }

        private static void CheckTruthEntities(List<TraceEntity> truth)
        {
            if( truth == null ||
                truth.Find(x => x.ID == "SystemRequirement") == null ||
                truth.Find(x => x.ID == "SoftwareRequirement") == null ||
                truth.Find(x => x.ID == "SoftwareSystemTest") == null ||
                truth.Find(x => x.ID == "SoftwareUnitTest") == null ||
                truth.Find(x => x.ID == "Risk") == null ||
                truth.Find(x => x.ID == "Anomaly") == null )
            {
                throw new Exception("Not all types of Truth entities were found in the project config file. Make sure the following are present: SystemRequirement, SoftwareRequirement, SoftwareSystemTest, SoftwareUnitTest, Risk, Anomaly");
            }
        }

        private void CheckDocumentTrace(IDataSources data, List<RequirementItem> truthItems, List<List<Item>> traceData, TraceEntity tet, TraceSpecification ts)
        {
            var documentTitle = ts.Target.Name;
            documentTraceIssues[documentTitle] = new List<TraceIssue>();

            List<TraceLink> tls = null;
            if (!documentTraceLinks.ContainsKey(documentTitle))
            {
                tls = new List<TraceLink>();
            }
            else
            {
                tls = documentTraceLinks[documentTitle];
            }

            foreach (var req in truthItems)
            {
                var foundLinks = from t in tls where (t.SourceID == req.RequirementID && t.Source.Equals(tet)) select t;
                if (foundLinks.Count() > 0)
                {
                    List<Item> items = new List<Item>();
                    foreach (var item in foundLinks)
                    {
                        items.Add(data.GetItem(item.TargetID));
                    }
                    //TODO: need to add support for exclusive traces where only traces of a certain type are allowed to trace (e.g. only Risk Controls can trace to RAR)
                    traceData.Add(items); 
                }
                else
                {
                    if (ts.CompleteTrace)
                    {
                        traceData.Add(new List<Item> { null });
                        var ti = new TraceIssue(tet, req.RequirementID, ts.Target, req.RequirementID, TraceIssueType.Missing);
                        if (!documentTraceIssues[documentTitle].Contains(ti))
                        {
                            documentTraceIssues[documentTitle].Add(ti);
                        }
                    }
                    else if (ts.SelectedCategories.Contains(req.RequirementCategory))
                    {
                        traceData.Add(new List<Item> { null });
                        var ti = new TraceIssue(tet, req.RequirementID, ts.Target, req.RequirementID, TraceIssueType.Missing);
                        if (!documentTraceIssues[documentTitle].Contains(ti))
                        {
                            documentTraceIssues[documentTitle].Add(ti);
                        }
                    }
                    else
                    {
                        traceData.Add(new List<Item>());
                    }
                }
            }
            foreach (var tl in tls)
            {
                if (tl.Source == tet)
                {
                    var foundLinks = from t in truthItems where (t.RequirementID == tl.SourceID) select t;
                    if (foundLinks.Count() == 0)
                    {
                        TraceIssue ti = null;
                        if (tl.TargetID == tl.SourceID)
                        {
                            ti = new TraceIssue(ts.Target, tl.TargetID, tet, tl.SourceID, TraceIssueType.Extra);
                        }
                        else
                        {
                            var item = data.GetItem(tl.SourceID);
                            ti = new TraceIssue(ts.Target, tl.TargetID, tet, tl.SourceID, TraceIssueType.Incorrect);
                        }
                        if (!documentTraceIssues[documentTitle].Contains(ti))
                        {
                            documentTraceIssues[documentTitle].Add(ti);
                        }
                    }
                }
            }
        }

        public RoboClerkOrderedDictionary<TraceEntity, List<List<Item>>> PerformAnalysis(IDataSources data, TraceEntity truth)
        {
            RoboClerkOrderedDictionary<TraceEntity, List<List<Item>>> result = new RoboClerkOrderedDictionary<TraceEntity, List<List<Item>>>();
            if (truth.ID != "SystemRequirement" &&
                truth.ID != "SoftwareRequirement")
            {
                throw new Exception($"Traceability analysis must start with {GetTitleForTraceEntity("SystemRequirement")}" +
                    $" or {GetTitleForTraceEntity("SoftwareRequirement")}");
            }

            List<TraceSpecification> requirementTraces = null;
            List<RequirementItem> truthItems = null;
            if (truth.ID == "SystemRequirement")
            {
                requirementTraces = systemRequirementTraces;
                truthItems = data.GetAllSystemRequirements();
            }
            else
            {
                requirementTraces = softwareRequirementTraces;
                truthItems = data.GetAllSoftwareRequirements();
            }

            result[truth] = new List<List<Item>>();
            foreach (var req in truthItems)
            {
                result[truth].Add(new List<Item> { req });
            }

            foreach (var ts in requirementTraces)
            {
                result[ts.Target] = new List<List<Item>>();
                if (ts.Target.ID == "SoftwareRequirement")
                {
                    //pull software requirements, match the software requirements with the product requirements
                    var srss = data.GetAllSoftwareRequirements();
                    foreach (var req in truthItems)
                    {
                        //find all software requirements children
                        var children = srss.FindAll((x => x.IsChildOf(req)));
                        result[ts.Target].Add(GetReqFamilyStrings(children));
                        AnalyzeTruthReqTrace(req, children, truth, ts.Target);
                    }
                    continue;
                }
                if (ts.Target.ID == "SystemRequirement")
                {
                    //pull product requirements, match the product requirements with the software requirements
                    var prss = data.GetAllSystemRequirements();
                    foreach (var req in truthItems)
                    {
                        //find all product requirement parents
                        var parent = prss.FindAll((x => x.IsParentOf(req)));
                        result[ts.Target].Add(GetReqFamilyStrings(parent));
                        AnalyzeTruthReqTrace(req, parent, truth, ts.Target);
                    }
                    continue;
                }
                CheckDocumentTrace(data, truthItems, result[ts.Target], truth, ts);
            }
            return result;
        }

        private List<Item> GetReqFamilyStrings(List<RequirementItem> family)
        {
            List<Item> result = new List<Item>();
            if (family.Count == 0)
            {
                result.Add(null);
                return result;
            }
            foreach (var item in family)
            {
                result.Add(item);
            }
            return result;
        }

        private void AnalyzeTruthReqTrace(RequirementItem pri, List<RequirementItem> family, TraceEntity source, TraceEntity target)
        {
            if (!truthTraceIssues.ContainsKey(source))
            {
                truthTraceIssues[source] = new List<TraceIssue>();
            }
            if (family.Count == 0)
            {
                truthTraceIssues[source].Add(new TraceIssue(source, pri.RequirementID, target, pri.RequirementID, TraceIssueType.PossiblyMissing));
            }
        }

        public void AddTraceTag(string docTitle, RoboClerkTag tag)
        {
            if (!tag.Parameters.ContainsKey("ID"))
            {
                var ex = new TagInvalidException(tag.Contents, "Trace tag is missing \"ID\" parameter indicating the identity of the truth item");
                ex.DocumentTitle = docTitle;
                throw ex;
            }
            if (!documentTraceLinks.ContainsKey(docTitle))
            {
                documentTraceLinks[docTitle] = new List<TraceLink>();
            }

            TraceEntity tlt = null;
            foreach (var entity in traceEntities)
            {
                if (tag.ContentCreatorID == entity.Name || tag.ContentCreatorID == entity.ID || tag.ContentCreatorID == entity.Abbreviation)
                {
                    tlt = entity;
                }
            }

            TraceLink link = new TraceLink(tlt, tag.Parameters["ID"], GetTraceEntityForTitle(docTitle), tag.Parameters["ID"]);
            documentTraceLinks[docTitle].Add(link);
        }

        public void AddTrace(TraceEntity source, string sourceID, TraceEntity target, string targetID)
        {
            string docTitle = GetTitleForTraceEntity(target.ID);
            TraceLink link = new TraceLink(source,sourceID,target,targetID);
            if (!documentTraceLinks.ContainsKey(docTitle))
            {
                documentTraceLinks[docTitle] = new List<TraceLink>();
            }
            documentTraceLinks[docTitle].Add(link);
        }

        public string GetTitleForTraceEntity(string ID)
        {
            foreach (var entry in traceEntities)
            {
                if (entry.ID == ID)
                {
                    return entry.Name;
                }
            }
            return $"{ID}: No title";
        }

        public string GetAbreviationForTraceEntity(string ID)
        {
            foreach (var entry in traceEntities)
            {
                if (entry.ID == ID)
                {
                    return entry.Abbreviation;
                }
            }
            return $"{ID}: No abbreviation";
        }

        public TraceEntity GetTraceEntityForTitle(string title)
        {
            foreach (var entry in traceEntities)
            {
                if (entry.Name == title)
                {
                    return entry;
                }
            }
            return default(TraceEntity);
        }

        public TraceEntity GetTraceEntityForID(string ID)
        {
            foreach (var entry in traceEntities)
            {
                if (entry.ID == ID)
                {
                    return entry;
                }
            }
            return default(TraceEntity);
        }

        public TraceEntity GetTraceEntityForAnyProperty(string prop)
        {
            foreach (var et in traceEntities)
            {
                if (et.ID == prop || et.Name == prop || et.Abbreviation == prop)
                {
                    return et;
                }
            }
            return default(TraceEntity);
        }

        public IEnumerable<TraceLink> GetTraceLinksForDocument(TraceEntity tet)
        {
            if (tet != null && documentTraceLinks.ContainsKey(tet.Name))
            {
                return documentTraceLinks[tet.Name];
            }
            return new List<TraceLink>();
        }

        public IEnumerable<TraceIssue> GetTraceIssuesForDocument(TraceEntity tet)
        {
            if (tet != null && documentTraceIssues.ContainsKey(tet.Name))
            {
                return documentTraceIssues[tet.Name];
            }
            return new List<TraceIssue>();
        }

        public IEnumerable<TraceIssue> GetTraceIssuesForTruth(TraceEntity tet)
        {
            if (tet != null && truthTraceIssues.ContainsKey(tet))
            {
                return truthTraceIssues[tet];
            }
            return new List<TraceIssue>();
        }
    }
}
