using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboClerk
{
    public interface ITraceabilityAnalysis
    {
        void AddTrace(string docTitle, TraceLink link);
        void AddTraceTag(string docTitle, RoboClerkTag tag);
        string GetAbreviationForTraceEntity(string ID);
        string GetTitleForTraceEntity(string ID);
        TraceEntity GetTraceEntityForAnyProperty(string prop);
        TraceEntity GetTraceEntityForID(string ID);
        TraceEntity GetTraceEntityForTitle(string title);
        IEnumerable<TraceIssue> GetTraceIssuesForDocument(string docTitle);
        IEnumerable<TraceIssue> GetTraceIssuesForDocument(TraceEntity tet);
        IEnumerable<TraceIssue> GetTraceIssuesForTruth(TraceEntity tet);
        IEnumerable<TraceLink> GetTraceLinksForDocument(string docTitle);
        RoboClerkOrderedDictionary<TraceEntity, List<List<Item>>> PerformAnalysis(IDataSources data, TraceEntity truth);
    }
}
