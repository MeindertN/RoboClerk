using System;
using System.Collections.Generic;
using System.Text;
using Tomlyn;
using Tomlyn.Model;
using System.Linq;

namespace RoboClerk
{
 
    public class TraceabilityAnalysis
    {
        private List<TraceEntity> traceEntities = new List<TraceEntity>();
        private Dictionary<string, List<TraceLink>> documentTraceLinks = new Dictionary<string, List<TraceLink>>();
        //private Dictionary<TraceEntity, List<TraceLink>> truthTraceLinks = new Dictionary<TraceEntity, List<TraceLink>>();
        private Dictionary<string, List<TraceIssue>> documentTraceIssues = new Dictionary<string, List<TraceIssue>>();
        private Dictionary<TraceEntity, List<TraceIssue>> truthTraceIssues = new Dictionary<TraceEntity, List<TraceIssue>>();
        private readonly List<TraceSpecification> systemRequirementTraces = new List<TraceSpecification>();
        private readonly List<TraceSpecification> softwareRequirementTraces = new List<TraceSpecification>();

        public TraceabilityAnalysis(string config)
        {
            ParseConfigFile(config);
        }

        private void ParseConfigFile(string config)
        {
            var toml = Toml.Parse(config).ToModel();
            //Truth entities
            var truth = (TomlTable)toml["Truth"];
            if (!truth.ContainsKey("SystemRequirement") || !truth.ContainsKey("SoftwareRequirement") ||
                !truth.ContainsKey("SoftwareSystemTest") || !truth.ContainsKey("SoftwareUnitTest") ||
                !truth.ContainsKey("Anomaly"))
            {
                throw new Exception("Not all types of Truth entities were found in the project config file. Make sure the following are present: SystemRequirement, SoftwareRequirement, SoftwareSystemTest, SoftwareUnitTest, Anomaly");
            }
            foreach (var entityTable in truth)
            {
                TomlTable elements = (TomlTable)entityTable.Value;
                if (!elements.ContainsKey("name") || !elements.ContainsKey("abbreviation"))
                {
                    throw new Exception($"Error while reading {entityTable.Key} truth entity from project config file. Check if all required elements (\"name\" and \"abbreviation\") are present.");
                }
                TraceEntity entity = new TraceEntity(entityTable.Key, (string)elements["name"], (string)elements["abbreviation"]);
                foreach(var el in traceEntities)
                {
                    if(entity.Abbreviation == el.Abbreviation || 
                        entity.Name == el.Name)
                    {
                        throw new Exception($"Detected a duplicate abbreviation or name in Truth.{entityTable.Key}. All IDs, names and abbreviations must be unique.");
                    }
                }
                traceEntities.Add(entity);
            }
            //document trace entity extraction
            foreach (var docloc in (TomlTable)toml["Document"])
            {
                TomlTable elements = (TomlTable)docloc.Value;
                if(!elements.ContainsKey("title") || !elements.ContainsKey("abbreviation") ||
                    !elements.ContainsKey("template") || !elements.ContainsKey("commands"))
                {
                    throw new Exception($"Error while reading {docloc.Key} document from project config file. Check if all required elements (\"title\",\"abbreviation\",\"template\" and \"commands\") are present.");
                }
                TraceEntity entity = new TraceEntity(docloc.Key, (string)elements["title"], (string)elements["abbreviation"]);
                foreach (var el in traceEntities)
                {
                    if (entity.Abbreviation == el.Abbreviation ||
                        entity.Name == el.Name)
                    {
                        throw new Exception($"Detected a duplicate abbreviation or name in Document.{docloc.Key}. All IDs, names and abbreviations must be unique.");
                    }
                }
                traceEntities.Add(entity);
            }
            //trace configuration extraction
            foreach (var table in (TomlTable)toml["TraceConfig"])
            {
                TraceEntity source, target;
                foreach( var doc in (TomlTable)table.Value)
                {
                    source = traceEntities.Find(f => (f.ID == table.Key));
                    target = traceEntities.Find(f => (f.ID == doc.Key));
                    if(source == null || target == null)
                    {
                        throw new Exception($"Error setting up requested trace from {table.Key} to {doc.Key}. Check if both these entities are correctly defined in the project config file.");
                    }
                    var arr = (TomlArray)doc.Value;
                    bool completeTrace = false; 
                    if(arr.Count == 0 || ((string)arr[0]).ToUpper() == "ALL")
                    {
                        completeTrace = true;
                    }
                    TraceSpecification dt = new TraceSpecification(source, target, completeTrace);
                    if (!completeTrace && ((string)arr[0]).ToUpper() != "OPTIONAL")
                    {
                        dt.SelectedCategories = arr.Cast<string>().ToList();
                    }
                    if ((string)table.Key == "SystemRequirement")
                    {
                        systemRequirementTraces.Add(dt);
                    }
                    else if ((string)table.Key == "SoftwareRequirement")
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

        private void CheckDocumentTrace(List<RequirementItem> truthItems, List<List<Item>> traceData, TraceEntity tet, TraceSpecification ts)
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
                var foundLinks = from t in tls where (t.TraceID == req.RequirementID && t.Source.Equals(tet)) select t;
                if (foundLinks.Count() > 0)
                {
                    traceData.Add(new List<Item>() { req }); //TODO: need to add support for exclusive traces where only traces of a certain type are allowed to trace (e.g. only Risk Controls can trace to RAR)
                }
                else
                {
                    if (ts.CompleteTrace)
                    {
                        traceData.Add(new List<Item> { null });
                        var ti = new TraceIssue(tet, ts.Target, req.RequirementID, TraceIssueType.Missing);
                        if (!documentTraceIssues[documentTitle].Contains(ti))
                        {
                            documentTraceIssues[documentTitle].Add(ti);
                        }
                    }
                    else if(ts.SelectedCategories.Contains(req.RequirementCategory))
                    {
                        traceData.Add(new List<Item> { null });
                        var ti = new TraceIssue(tet, ts.Target, req.RequirementID, TraceIssueType.Missing);
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
                    var foundLinks = from t in truthItems where (t.RequirementID == tl.TraceID) select t;
                    if (foundLinks.Count() == 0)
                    {
                        var ti = new TraceIssue(ts.Target, tet, tl.TraceID, TraceIssueType.Extra);
                        if (!documentTraceIssues[documentTitle].Contains(ti))
                        {
                            documentTraceIssues[documentTitle].Add(ti);
                        }
                    }
                }
            }
        }

        public RoboClerkOrderedDictionary<TraceEntity,List<List<Item>>> PerformAnalysis(DataSources data, TraceEntity truth)
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

            foreach(var ts in requirementTraces)
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
                if(ts.Target.ID == "SystemRequirement")
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
                CheckDocumentTrace(truthItems, result[ts.Target], truth, ts);
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
            if(!truthTraceIssues.ContainsKey(source))
            {
                truthTraceIssues[source] = new List<TraceIssue>();
            }
            if (family.Count == 0)
            {
                truthTraceIssues[source].Add(new TraceIssue(source,
                    target, pri.RequirementID, TraceIssueType.PossiblyMissing));
            }
        }      

        public void AddTraceTag(string docTitle, RoboClerkTag tag)
        {
            if(!tag.Parameters.ContainsKey("ID"))
            {
                var ex = new TagInvalidException(tag.Contents, "Trace tag is missing \"ID\" parameter indicating the identity of the truth item.");
                ex.DocumentTitle = docTitle;
                throw ex;
            }
            if (!documentTraceLinks.ContainsKey(docTitle))
            {
                documentTraceLinks[docTitle] = new List<TraceLink>();
            }
            
            TraceEntity tlt = null;
            foreach(var entity in traceEntities)
            {
                if(tag.ContentCreatorID == entity.Name || tag.ContentCreatorID == entity.ID || tag.ContentCreatorID == entity.Abbreviation)
                {
                    tlt = entity;
                }
            }

            TraceLink link = new TraceLink(tlt,GetTraceEntityForTitle(docTitle),tag.Parameters["ID"]);
            documentTraceLinks[docTitle].Add(link);
        }    

        public void AddTrace(string docTitle, TraceLink link)
        {
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
            foreach( var entry in traceEntities)
            {
                if(entry.Name == title)
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
            foreach(var et in traceEntities)
            {
                if( et.ID == prop || et.Name == prop || et.Abbreviation == prop)
                {
                    return et;
                }
            }
            return null;
        }

        public IEnumerable<TraceLink> GetTraceLinksForDocument(string docTitle)
        {
            if(documentTraceLinks.ContainsKey(docTitle))
            {
                return documentTraceLinks[docTitle];
            }
            return new List<TraceLink>();
        }

        public IEnumerable<TraceIssue> GetTraceIssuesForDocument(string docTitle)
        {
            if (documentTraceIssues.ContainsKey(docTitle))
            {
                return documentTraceIssues[docTitle];
            }
            return new List<TraceIssue>();
        }  

        public IEnumerable<TraceIssue> GetTraceIssuesForDocument(TraceEntity tet)
        {
            return GetTraceIssuesForDocument(tet.Name);
        }

        public IEnumerable<TraceIssue> GetTraceIssuesForTruth(TraceEntity tet)
        {
            if(!truthTraceIssues.ContainsKey(tet))
            {
                return new List<TraceIssue>();
            }
            return truthTraceIssues[tet];
        }
    }
}
