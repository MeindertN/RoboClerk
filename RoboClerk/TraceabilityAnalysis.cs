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
        private Dictionary<TraceEntityType, string> docToTitle = new Dictionary<TraceEntityType, string>();

        public TraceabilityAnalysis(string config)
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
                    catch(ArgumentException e)
                    {
                        throw new Exception($"Unknown trace entity encountered in configuraiton file: {docloc.Key}");
                    }
                    docToTitle[tet] = (string)arr[0];
                }
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
            List<string> header = new List<string>() { "Software Requirement" };
            foreach (var req in srss)
            {
                if (header.IndexOf(req.RequirementCategory) < 0)
                {
                    header.Add(req.RequirementCategory);
                }
            }
            List<string> emptyRow = new List<string>();
            foreach (var val in header) emptyRow.Add(" ");

            foreach (var req in srss)
            {
                List<string> row = new List<string>(emptyRow);

                int index = header.IndexOf(req.RequirementCategory);
                row[index] = req.RequirementID;
                traceData.Add(row);
            }

            for (int i = 1; i < columns.Count; ++i)
            {
                if (columns[i] == TraceEntityType.ProductRequirement)
                {
                    //pull product requirements, match the product requirements with the software requirements
                    header.Add("Product Requirements");
                    var prss = data.GetAllProductRequirements();
                    int index = 0;
                    foreach (var req in srss)
                    {
                        //find all software requirements parents
                        var parents = prss.FindAll((x => x.IsParentOf(req)));
                        traceData[index].Add(GetReqFamilyString(parents, "NO PR"));
                        AnalyzeTruthReqTrace(req, parents, TraceEntityType.SoftwareRequirement, TraceEntityType.SoftwareRequirement);
                        ++index;
                    }
                    continue;
                }
                else if (!docToTitle.ContainsKey(columns[i]))
                {
                    header.Add(columns[i].ToString());
                    int index = 0;
                    foreach (var req in srss)
                    {
                        traceData[index].Add("document not found");
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
                var documentTitle = docToTitle[columns[i]];
                header.Add(documentTitle);
                CheckDocumentTrace(srss, traceData, documentTitle, TraceLinkType.SoftwareRequirementTrace,
                    TraceEntityType.SoftwareRequirement, columns[i], forwardComplete);
            }
            traceData.Insert(0, header);
            return traceData;
        }

        private List<List<string>> AnalyzeProductRequirements(DataSources data, List<TraceEntityType> columns, List<List<string>> traceData)
        {
            var prss = data.GetAllProductRequirements();
            List<string> header = new List<string>() { "Product Requirement" };
            foreach (var req in prss)
            {
                if (header.IndexOf(req.RequirementCategory) < 0)
                {
                    header.Add(req.RequirementCategory);
                }
            }
            List<string> emptyRow = new List<string>();
            foreach (var val in header) emptyRow.Add(" ");

            foreach (var req in prss)
            {
                List<string> row = new List<string>(emptyRow);

                int index = header.IndexOf(req.RequirementCategory);
                row[index] = req.RequirementID;
                traceData.Add(row);
            }
            //go over the rest of the columns, look up the titles for the header, 
            for (int i = 1; i < columns.Count; ++i)
            {
                if (columns[i] == TraceEntityType.SoftwareRequirement)
                {
                    //pull software requirements, match the software requirements with the product requirements
                    header.Add("Software Requirements");
                    var srss = data.GetAllSoftwareRequirements();
                    int index = 0;
                    foreach (var req in prss)
                    {
                        //find all software requirements children
                        var children = srss.FindAll((x => x.IsChildOf(req)));
                        traceData[index].Add(GetReqFamilyString(children,"NO SR"));
                        AnalyzeTruthReqTrace(req,children,TraceEntityType.ProductRequirement,TraceEntityType.SoftwareRequirement);
                        ++index;
                    }
                    continue;
                }
                //we are not expecting to find a truth column once we've checked for software requirement
                else if (!docToTitle.ContainsKey(columns[i]))
                {
                    header.Add(columns[i].ToString());
                    int index = 0;
                    foreach (var req in prss)
                    {
                        traceData[index].Add("document not found");
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
                var documentTitle = docToTitle[columns[i]];
                header.Add(documentTitle);
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
            if(tet == TraceEntityType.ProductRequirement)
            {
                return "Product Requirement";
            }
            if(tet == TraceEntityType.SoftwareRequirement)
            {
                return "Software Requirement";
            }
            if(tet == TraceEntityType.TestCase)
            {
                return "Test Case";
            }
            if(!docToTitle.ContainsKey(tet))
            {
                return $"{tet.ToString()}: No doc title";
            }
            return docToTitle[tet];
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
            if(!docToTitle.ContainsKey(tet))
            {
                return new List<TraceIssue>();
            }
            return GetTraceIssuesForDocument(docToTitle[tet]);
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
