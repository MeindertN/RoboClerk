using System;
using System.Collections.Generic;
using System.Text;
using Tomlyn;
using Tomlyn.Model;
using System.Linq;

namespace RoboClerk
{
    public enum TraceEntityType
    {
        ProductRequirement,
        SoftwareRequirement,
        TestCase,
        Bug,
        ProductRequirementsSpecification,
        SoftwareRequirementsSpecification,
        SystemLevelTestPlan,
        SoftwareDevelopmentPlan,
        SoftwareDesignSpecification,
        RiskAssessmentRecord,
        IntegrationLevelTestPlan,
        UnitLevelTestPlan,
        DetailedSoftwareDesignSpecification,
        TransferToProductionPlan,
        CodeCoverageRecord,
        OutStandingBugsAndIssues,
        RunTimeErrorDetectionPlanAndReport,
        WorkOrder,
        RevisionLevelHistory,
        SystemBaselineRecord,
        TraceabilityAnalysisRecord,
        DocumentationReport,
        ProductValidationPlan,
        Unknown
    }
    public class TraceabilityAnalysis
    {
        private Dictionary<string, List<TraceLink>> documentTraceLinks = new Dictionary<string, List<TraceLink>>();
        private Dictionary<TraceEntityType, List<TraceLink>> truthTraceLinks = new Dictionary<TraceEntityType, List<TraceLink>>();
        private Dictionary<string, List<TraceIssue>> documentTraceIssues = new Dictionary<string, List<TraceIssue>>();
        private Dictionary<TraceEntityType, List<TraceIssue>> truthTraceIssues = new Dictionary<TraceEntityType, List<TraceIssue>>();
        private readonly Dictionary<TraceEntityType, string> entityToName = new Dictionary<TraceEntityType, string>();
        private readonly Dictionary<TraceEntityType, string> entityToAbbreviation = new Dictionary<TraceEntityType, string>();
        private readonly List<TraceSpecification> productRequirementTraces = new List<TraceSpecification>();
        private readonly List<TraceSpecification> softwareRequirementTraces = new List<TraceSpecification>();

        public TraceabilityAnalysis(string config)
        {
            ParseConfigFile(config);
        }

        private void ParseConfigFile(string config)
        {
            var toml = Toml.Parse(config).ToModel();
            foreach (var docloc in (TomlTable)toml["DocumentLocations"])
            {
                TomlArray arr = (TomlArray)docloc.Value;
                if ((string)arr[0] != "") //if empty the assumption is that there is no such document
                {
                    TraceEntityType tet;
                    try
                    {
                        tet = (TraceEntityType)Enum.Parse(typeof(TraceEntityType), docloc.Key, true);
                    }
                    catch (ArgumentException e)
                    {
                        throw new Exception($"Unknown document trace entity encountered in config [DocumentLocations]: {docloc.Key}");
                    }
                    entityToName[tet] = (string)arr[0];
                    entityToAbbreviation[tet] = (string)arr[1];
                }
            }
            foreach (var entityName in (TomlTable)toml["EntityNames"])
            {
                TraceEntityType tet;
                try
                {
                    tet = (TraceEntityType)Enum.Parse(typeof(TraceEntityType), entityName.Key, true);
                }
                catch (ArgumentException e)
                {
                    throw new Exception($"Unknown entity name encountered in config [EntityNames]: {entityName.Key}");
                }
                entityToName[tet] = (string)entityName.Value;
            }
            foreach (var table in (TomlTable)toml["TraceConfig"])
            {
                TraceEntityType source, target;
                foreach( var doc in (TomlTable)table.Value)
                {
                    source = (TraceEntityType)Enum.Parse(typeof(TraceEntityType), table.Key, true);
                    target = (TraceEntityType)Enum.Parse(typeof(TraceEntityType), doc.Key, true);
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
                    if ((string)table.Key == "ProductRequirement")
                    {
                        productRequirementTraces.Add(dt);
                    }
                    else if ((string)table.Key == "SoftwareRequirement")
                    {
                        softwareRequirementTraces.Add(dt);
                    }
                    else
                    {
                        throw new Exception("Root trace must be from either ProductRequirement or Software Requirement");
                    }
                }
            }
        }

        private void CheckDocumentTrace(List<RequirementItem> truthItems, List<List<Item>> traceData, TraceEntityType tet, TraceSpecification ts)
        {
            var documentTitle = entityToName[ts.Target];
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
                var foundLinks = from t in tls where (t.TraceID == req.RequirementID && t.Source == tet) select t;
                if (foundLinks.Count() > 0)
                {
                    traceData.Add(new List<Item>() { req }); //TODO: need to add support for exclusive traces where only traces of a certain type are allowed to trace (e.g. only Risk Controls can trace to RAR)
                }
                else
                {
                    if (ts.CompleteTrace)
                    {
                        traceData.Add(new List<Item> { null });
                        documentTraceIssues[documentTitle].Add(new TraceIssue(tet,
                                                                    ts.Target,
                                                                    req.RequirementID,
                                                                    TraceIssueType.Missing));
                    }
                    else if(ts.SelectedCategories.Contains(req.RequirementCategory))
                    {
                        traceData.Add(new List<Item> { null });
                        documentTraceIssues[documentTitle].Add(new TraceIssue(tet,
                                                                    ts.Target,
                                                                    req.RequirementID,
                                                                    TraceIssueType.Missing));
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
                        documentTraceIssues[documentTitle].Add(new TraceIssue(ts.Target,
                                                                        tet,
                                                                        tl.TraceID,
                                                                        TraceIssueType.Extra));
                    }
                }
            }
        }

        public Dictionary<TraceEntityType,List<List<Item>>> PerformAnalysis(DataSources data, TraceEntityType truth)
        {
            Dictionary<TraceEntityType, List<List<Item>>> result = new Dictionary<TraceEntityType, List<List<Item>>>();
            if (truth != TraceEntityType.ProductRequirement && 
                truth != TraceEntityType.SoftwareRequirement)
            {
                throw new Exception($"Traceability analysis must start with {GetTitleForTraceEntity(TraceEntityType.ProductRequirement)}" +
                    $" or {GetTitleForTraceEntity(TraceEntityType.SoftwareRequirement)}");
            }

            List<TraceSpecification> requirementTraces = null;
            List<RequirementItem> truthItems = null;
            if (truth == TraceEntityType.ProductRequirement)
            {
                requirementTraces = productRequirementTraces;
                truthItems = data.GetAllProductRequirements();
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
                if (ts.Target == TraceEntityType.SoftwareRequirement)
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
                if(ts.Target == TraceEntityType.ProductRequirement)
                {
                    //pull product requirements, match the product requirements with the software requirements
                    var prss = data.GetAllProductRequirements();
                    foreach (var req in truthItems)
                    {
                        //find all product requirement parents
                        var parent = prss.FindAll((x => x.IsParentOf(req)));
                        result[ts.Target].Add(GetReqFamilyStrings(parent));
                        AnalyzeTruthReqTrace(req, parent, truth, ts.Target);
                    }
                    continue;
                }
                if (!entityToName.ContainsKey(ts.Target))
                {
                    foreach (var req in truthItems)
                    {
                        result[ts.Target].Add(new List<Item> { null });
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
        
        private void AnalyzeTruthReqTrace(RequirementItem pri, List<RequirementItem> family, TraceEntityType source, TraceEntityType target)
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
            if (!documentTraceLinks.ContainsKey(docTitle))
            {
                documentTraceLinks[docTitle] = new List<TraceLink>();
            }

            TraceEntityType tlt = TraceEntityType.Unknown;
            if (tag.ContentCreatorID.ToUpper() == "SoftwareRequirements")
            {
                tlt = TraceEntityType.SoftwareRequirement;
            }
            else if (tag.ContentCreatorID.ToUpper() == "ProductRequirements")
            {
                tlt = TraceEntityType.ProductRequirement;
            }
            else if (tag.ContentCreatorID.ToUpper() == "TestCases")
            {
                tlt = TraceEntityType.TestCase;
            }

            TraceLink link = new TraceLink(tlt,GetTraceEntityForTitle(docTitle),tag.Contents);
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

        public string GetTitleForTraceEntity(TraceEntityType tet)
        {
            if(!entityToName.ContainsKey(tet))
            {
                return $"{tet.ToString()}: No doc title";
            }
            return entityToName[tet];
        }

        public string GetAbreviationForTraceEntity(TraceEntityType tet)
        {
            if (!entityToAbbreviation.ContainsKey(tet))
            {
                return $"{tet.ToString()}: No abreviation";
            }
            return entityToAbbreviation[tet];
        }

        public TraceEntityType GetTraceEntityForTitle(string title)
        {
            foreach(KeyValuePair<TraceEntityType, string> entry in entityToName)
            {
                if(entry.Value == title)
                {
                    return entry.Key;
                }
            }
            return TraceEntityType.Unknown;
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

        public IEnumerable<TraceIssue> GetTraceIssuesForDocument(TraceEntityType tet)
        {
            if(!entityToName.ContainsKey(tet))
            {
                return new List<TraceIssue>();
            }
            return GetTraceIssuesForDocument(entityToName[tet]);
        }

        public IEnumerable<TraceIssue> GetTraceIssuesForTruth(TraceEntityType tet)
        {
            if(!truthTraceIssues.ContainsKey(tet))
            {
                return new List<TraceIssue>();
            }
            return truthTraceIssues[tet];
        }
    }
}
