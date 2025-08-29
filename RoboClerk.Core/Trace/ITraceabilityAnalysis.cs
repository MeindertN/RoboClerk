using RoboClerk.Core;
using System.Collections.Generic;

namespace RoboClerk
{
    public interface ITraceabilityAnalysis
    {
        void AddTrace(TraceEntity source, string sourceID, TraceEntity target, string targetID);
        void AddTraceTag(string docTitle, IRoboClerkTag tag);
        string GetAbreviationForTraceEntity(string ID);
        string GetTitleForTraceEntity(string ID);
        TraceEntity GetTraceEntityForAnyProperty(string prop);
        TraceEntity GetTraceEntityForID(string ID);
        TraceEntity GetTraceEntityForTitle(string title);
        IEnumerable<TraceIssue> GetTraceIssuesForDocument(TraceEntity tet);
        IEnumerable<TraceIssue> GetTraceIssuesForTruth(TraceEntity tet);
        IEnumerable<TraceLink> GetTraceLinksForDocument(TraceEntity tet);
        RoboClerkOrderedDictionary<TraceEntity, List<List<Item>>> PerformAnalysis(IDataSources data, TraceEntity truth);
    }
}
