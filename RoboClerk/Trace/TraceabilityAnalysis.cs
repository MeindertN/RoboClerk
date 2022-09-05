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
        private readonly Dictionary<TraceEntity, List<TraceSpecification>> traces = new Dictionary<TraceEntity,List<TraceSpecification>>();
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

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
                TraceEntity entity = new TraceEntity(docloc.RoboClerkID, docloc.DocumentTitle, docloc.DocumentAbbreviation, TraceEntityType.Document);
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
                    if (!traces.ContainsKey(source))
                    {
                        traces[source] = new List<TraceSpecification>();
                    }
                    TraceSpecification dt = new TraceSpecification(source, target);
                    dt.CompleteTraceForward = (doc.Value.ForwardFilters.Count == 0 || doc.Value.ForwardFilters[0].ToUpper() == "ALL");
                    if (!dt.CompleteTraceForward && doc.Value.ForwardFilters[0].ToUpper() != "OPTIONAL")
                    {
                        dt.SelectedCategoriesForward = doc.Value.ForwardFilters;
                    }
                    dt.ForwardLink = ItemLink.GetLinkTypeForString(doc.Value.ForwardLinkType);
                    dt.CompleteTraceBackward = (doc.Value.BackwardFilters.Count == 0 || doc.Value.BackwardFilters[0].ToUpper() == "ALL");
                    if (!dt.CompleteTraceBackward && doc.Value.BackwardFilters[0].ToUpper() != "OPTIONAL")
                    {
                        dt.SelectedCategoriesBackward = doc.Value.BackwardFilters;
                    }
                    dt.BackwardLink = ItemLink.GetLinkTypeForString(doc.Value.BackwardLinkType);
                    traces[source].Add(dt);
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

        private void CheckDocumentTrace(IDataSources data, List<LinkedItem> truthItems, List<List<Item>> traceData, TraceEntity tet, TraceSpecification ts)
        {
            var documentTitle = ts.Target.Name;
            documentTraceIssues[documentTitle] = new List<TraceIssue>();

            //Ensure there is at least an empty list of documentTraceLinks
            List<TraceLink> tls = null;
            if (!documentTraceLinks.ContainsKey(documentTitle))
            {
                tls = new List<TraceLink>();
            }
            else
            {
                tls = documentTraceLinks[documentTitle];
            }

            //go over all truthItems and find those linked to the document
            foreach (var req in truthItems)
            {
                //the source ID and the source type (e.g. SystemRequirement) need to match
                IEnumerable<TraceLink> foundLinks = new List<TraceLink>();
                if (ts.SelectedCategoriesForward.Count == 0 || ts.SelectedCategoriesForward.Contains(req.ItemCategory))
                {
                    foundLinks = from t in tls where (t.SourceID == req.ItemID && t.Source.Equals(tet)) select t;
                }
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
                    if (ts.CompleteTraceForward || ts.SelectedCategoriesForward.Contains(req.ItemCategory))
                    {
                        traceData.Add(new List<Item> { null });
                        var ti = new TraceIssue(tet, req.ItemID, ts.Target, req.ItemID, TraceIssueType.Missing);
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
            //now we need to go over the tracelinks to see if trace in the other direction is ok as well
            foreach (var tl in tls) 
            {
                if (tl.Source == tet)
                {
                    var foundLinks = from t in truthItems where (t.ItemID == tl.SourceID) select t;
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
            if(!traces.ContainsKey(truth))
            {
                throw new Exception($"No trace specification for traces starting with {GetTitleForTraceEntity(truth.ID)}");
            }
            RoboClerkOrderedDictionary<TraceEntity, List<List<Item>>> result = new RoboClerkOrderedDictionary<TraceEntity, List<List<Item>>>();
            List<TraceSpecification> specifiedTraces = traces[truth];
            
            if(truth.EntityType == TraceEntityType.Truth)
            {
                List<LinkedItem> truthItems = data.GetItems(truth);
                result[truth] = new List<List<Item>>();
                foreach (var req in truthItems)
                {
                    result[truth].Add(new List<Item> { req });
                }
                foreach (var ts in specifiedTraces)
                {
                    result[ts.Target] = new List<List<Item>>();
                    if (ts.Target.EntityType == TraceEntityType.Truth)
                    {
                        //tracing truth item to truth item, truth items are retrieved from the data source
                        var targetItems = data.GetItems(ts.Target);
                        foreach (var ti in truthItems)
                        {
                            if (ts.SelectedCategoriesForward.Count == 0 || (ts.SelectedCategoriesForward.FindIndex(x => x == ti.ItemCategory) >= 0))
                            {
                                var linked = targetItems.FindAll(x => x.GetItemLinkType(ti) == ts.BackwardLink);
                                if (ts.CompleteTraceForward && linked.Count == 0)
                                {
                                    // the trace should be complete but the link is not there, that means missing
                                    result[ts.Target].Add(new List<Item>() { null });
                                    // add a trace issue 
                                    AnalyzeTruthReqTrace(ti, linked, truth, ts.Target);
                                }
                                else
                                {
                                    // a complete trace is not needed or we found linked items
                                    result[ts.Target].Add(GetReqFamilyStrings(linked));
                                }
                            }
                            else
                            {
                                //this is a N/A, selected categories do not match so link is not important
                                result[ts.Target].Add(new List<Item>()); 
                            }
                        }
                        continue;
                    }
                    else
                    {
                        //tracing truth item to document
                        CheckDocumentTrace(data, truthItems, result[ts.Target], truth, ts);
                    }
                }
                return result;
            }
            else if(truth.EntityType == TraceEntityType.Document)
            {
                throw new NotImplementedException("Tracing from document not implemented");
            }
            else
            {
                throw new Exception($"Cannot do trace analysis for a source entity with an Unknown type.");
            }
        }

        private List<Item> GetReqFamilyStrings(List<LinkedItem> family)
        {
            List<Item> result = new List<Item>();
            foreach (var item in family)
            {
                result.Add(item);
            }
            return result;
        }

        private void AnalyzeTruthReqTrace(LinkedItem pri, List<LinkedItem> linked, TraceEntity source, TraceEntity target)
        {
            if (!truthTraceIssues.ContainsKey(source))
            {
                truthTraceIssues[source] = new List<TraceIssue>();
            }
            if (linked.Count == 0)
            {
                truthTraceIssues[source].Add(new TraceIssue(source, pri.ItemID, target, pri.ItemID, TraceIssueType.PossiblyMissing));
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
            logger.Warn($"TraceEntity with title: {title} not found!");
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
            logger.Warn($"TraceEntity with ID: {ID} not found!");
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
            logger.Warn($"TraceEntity with property: {prop} not found!");
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
