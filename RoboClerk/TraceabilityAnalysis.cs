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
        private readonly List<DocTrace> productRequirementTraces = new List<DocTrace>();
        private readonly List<DocTrace> softwareRequirementTraces = new List<DocTrace>();

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
                TraceEntityType tet;
                if ( (string)table.Key == "ProductRequirement" )
                {
                    foreach( var doc in (TomlTable)table.Value)
                    {
                        tet = (TraceEntityType)Enum.Parse(typeof(TraceEntityType), doc.Key, true);
                        var arr = (TomlArray)doc.Value;
                        bool completeTrace = false; 
                        if(arr.Count == 0 || ((string)arr[0]).ToUpper() == "ALL")
                        {
                            completeTrace = true;
                        }
                        DocTrace dt = new DocTrace(tet, completeTrace);
                        if (!completeTrace && ((string)arr[0]).ToUpper() != "OPTIONAL")
                        {
                            dt.SelectedCatagories = arr.Cast<string>().ToList();
                        }
                        productRequirementTraces.Add(dt);
                    }
                }
                else if ((string)table.Key == "SoftwareRequirement")
                {

                }
                else
                {
                    throw new Exception("Root trace must be from either ProductRequirement or Software Requirement");
                }
                    //var test2 = (string)table.Value;
                //TraceEntityType
            }
        }

        private void CheckDocumentTrace(List<RequirementItem> truthItems, List<List<string>> traceData, string documentTitle,
            TraceLinkType linkType, TraceEntityType sourceEntity, TraceEntityType targetEntity, bool forwardComplete)
        {
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

            int index = 0;
            foreach (var req in truthItems)
            {
                var foundLinks = from t in tls where (t.TraceID == req.RequirementID && t.TraceType == linkType) select t;
                if (foundLinks.Count() > 0)
                {
                    traceData[index].Add("OK");
                }
                else
                {
                    
                    if (forwardComplete)
                    {
                        traceData[index].Add("NO TRACE");
                        documentTraceIssues[documentTitle].Add(new TraceIssue(sourceEntity,
                                                                    targetEntity,
                                                                    req.RequirementID,
                                                                    TraceIssueType.Missing));
                    }
                    else
                    {
                        traceData[index].Add(" ");
                    }
                }
                ++index;
            }
            foreach (var tl in tls)
            {
                if (tl.TraceType == linkType)
                {
                    var foundLinks = from t in truthItems where (t.RequirementID == tl.TraceID) select t;
                    if (foundLinks.Count() == 0)
                    {
                        documentTraceIssues[documentTitle].Add(new TraceIssue(targetEntity,
                                                                        sourceEntity,
                                                                        tl.TraceID,
                                                                        TraceIssueType.Extra));
                    }
                }
            }
        }

        public List<List<string>> PerformAnalysis(DataSources data, List<TraceEntityType> columns)
        {
            List<List<string>> traceData = new List<List<string>>();
            //first column has to be Product or Software requirements
            if(columns.Count == 0)
            {
                return traceData;
            }
            if(columns[0] != TraceEntityType.ProductRequirement && columns[0] != TraceEntityType.SoftwareRequirement)
            {
                throw new Exception("Traceability analysis must start with product or software requirements column");
            }

            //go over each item and, depending on whether it is a truth or document trace, take the appropriate steps
            if(columns[0] == TraceEntityType.ProductRequirement)
            {
                return AnalyzeProductRequirements(data, columns, traceData);
            }
            else if(columns[0] == TraceEntityType.SoftwareRequirement)
            {
                return AnalyzeSoftwareRequirements(data, columns, traceData);
            }
            else
            {
                throw new Exception($"A trace analysis must start from product requirements or software requirements. It cannot start with {columns[0].ToString()}.");
            }
        }

        private List<List<string>> AnalyzeSoftwareRequirements(DataSources data, List<TraceEntityType> columns, List<List<string>> traceData)
        {
            var srss = data.GetAllSoftwareRequirements();
            List<string> header = new List<string>() { entityToName[TraceEntityType.SoftwareRequirement] };
            
            foreach (var req in srss)
            {
                traceData.Add(new List<string>() { req.RequirementID });
            }

            for (int i = 1; i < columns.Count; ++i)
            {
                if (columns[i] == TraceEntityType.ProductRequirement)
                {
                    //pull product requirements, match the product requirements with the software requirements
                    header.Add($"{entityToName[TraceEntityType.ProductRequirement]}s");
                    var prss = data.GetAllProductRequirements();
                    int index = 0;
                    foreach (var req in srss)
                    {
                        //find all software requirements parents
                        var parents = prss.FindAll((x => x.IsParentOf(req)));
                        traceData[index].Add(GetReqFamilyString(parents, "MISSING"));
                        AnalyzeTruthReqTrace(req, parents, TraceEntityType.SoftwareRequirement, TraceEntityType.SoftwareRequirement);
                        ++index;
                    }
                    continue;
                }
                else if (!entityToName.ContainsKey(columns[i]))
                {
                    header.Add(columns[i].ToString());
                    int index = 0;
                    foreach (var req in srss)
                    {
                        traceData[index].Add("N/A");
                        ++index;
                    }
                    continue;
                }
                bool forwardComplete = true;
                if (columns[i] == TraceEntityType.SoftwareRequirementsSpecification)
                {
                    forwardComplete = true;
                }
                else if (columns[i] == TraceEntityType.SoftwareDesignSpecification)
                {
                    forwardComplete = true;
                }
                else if (columns[i] == TraceEntityType.SystemLevelTestPlan)
                {
                    forwardComplete = true;
                }
                else if (columns[i] == TraceEntityType.RiskAssessmentRecord)
                {
                    forwardComplete = false;
                }
                else if (columns[i] == TraceEntityType.TransferToProductionPlan)
                {
                    forwardComplete = false;
                }
                else
                {
                    throw new Exception($"Invalid trace entity type encountered: {columns[i].ToString()}");
                }
                var documentTitle = entityToName[columns[i]];
                header.Add(entityToAbbreviation[columns[i]]);
                CheckDocumentTrace(srss, traceData, documentTitle, TraceLinkType.SoftwareRequirementTrace,
                    TraceEntityType.SoftwareRequirement, columns[i], forwardComplete);
            }
            traceData.Insert(0, header);
            return traceData;
        }

        private List<List<string>> AnalyzeProductRequirements(DataSources data, List<TraceEntityType> columns, List<List<string>> traceData)
        {
            var prss = data.GetAllProductRequirements();
            List<string> header = new List<string>() { entityToName[TraceEntityType.ProductRequirement] };

            foreach (var req in prss)
            {
                traceData.Add(new List<string>() { req.RequirementID });
            }
            //go over the rest of the columns, look up the titles for the header, 
            for (int i = 1; i < columns.Count; ++i)
            {
                if (columns[i] == TraceEntityType.SoftwareRequirement)
                {
                    //pull software requirements, match the software requirements with the product requirements
                    header.Add($"{entityToName[TraceEntityType.SoftwareRequirement]}s");
                    var srss = data.GetAllSoftwareRequirements();
                    int index = 0;
                    foreach (var req in prss)
                    {
                        //find all software requirements children
                        var children = srss.FindAll((x => x.IsChildOf(req)));
                        traceData[index].Add(GetReqFamilyString(children,"MISSING"));
                        AnalyzeTruthReqTrace(req,children,TraceEntityType.ProductRequirement,TraceEntityType.SoftwareRequirement);
                        ++index;
                    }
                    continue;
                }
                //we are not expecting to find a truth column once we've checked for software requirement
                else if (!entityToName.ContainsKey(columns[i]))
                {
                    header.Add(columns[i].ToString());
                    int index = 0;
                    foreach (var req in prss)
                    {
                        traceData[index].Add("N/A");
                        ++index;
                    }
                    continue;
                }
                bool forwardComplete = true;
                if (columns[i] == TraceEntityType.ProductRequirementsSpecification)
                {
                    forwardComplete = true;
                } 
                else if (columns[i] == TraceEntityType.RiskAssessmentRecord)
                {
                    forwardComplete = false;
                } 
                else if (columns[i] == TraceEntityType.ProductValidationPlan)
                {
                    forwardComplete = false;
                }
                else
                {
                    throw new Exception($"Invalid trace entity type encountered: {columns[i]}");
                }
                var documentTitle = entityToName[columns[i]];
                header.Add(entityToAbbreviation[columns[i]]);
                CheckDocumentTrace(prss, traceData, documentTitle, TraceLinkType.ProductRequirementTrace,
                    TraceEntityType.ProductRequirement, columns[i], forwardComplete);
            }
            traceData.Insert(0, header);
            return traceData;
        }

        private string GetReqFamilyString(List<RequirementItem> family, string missing)
        {
            if (family.Count == 0)
            {
                return missing;
            }
            StringBuilder sb = new StringBuilder();
            foreach (var item in family)
            {
                if (sb.Length > 0)
                {
                    sb.Append($", {item.RequirementID}");
                }
                else
                {
                    sb.Append(item.RequirementID);
                }
            }
            return sb.ToString();
        }
        
        private void AnalyzeTruthReqTrace(RequirementItem pri, List<RequirementItem> family, TraceEntityType source, TraceEntityType target)
        {
            if(!truthTraceIssues.ContainsKey(target))
            {
                truthTraceIssues[target] = new List<TraceIssue>();
            }
            if (family.Count == 0)
            {
                truthTraceIssues[target].Add(new TraceIssue(source,
                    target, pri.RequirementID, TraceIssueType.PossiblyMissing));
            }
        }      

        public void AddTraceTag(string docTitle, RoboClerkTag tag)
        {
            if (!documentTraceLinks.ContainsKey(docTitle))
            {
                documentTraceLinks[docTitle] = new List<TraceLink>();
            }

            TraceLinkType tlt = TraceLinkType.Unknown;
            if (tag.ContentCreatorID.ToUpper() == "SoftwareRequirements")
            {
                tlt = TraceLinkType.SoftwareRequirementTrace;
            }
            else if (tag.ContentCreatorID.ToUpper() == "ProductRequirements")
            {
                tlt = TraceLinkType.ProductRequirementTrace;
            }
            else if (tag.ContentCreatorID.ToUpper() == "TestCases")
            {
                tlt = TraceLinkType.TestCaseTrace;
            }

            TraceLink link = new TraceLink(tag.Contents, tlt);
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
