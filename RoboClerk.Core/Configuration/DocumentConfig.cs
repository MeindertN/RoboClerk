

namespace RoboClerk.Core.Configuration
{
    public class DocumentConfig
    {
        private string roboClerkID = string.Empty;
        private string documentID = string.Empty;
        private string documentTitle = string.Empty;
        private string documentAbbreviation = string.Empty;
        private string documentTemplate = string.Empty;
        private Dictionary<TraceEntity, uint> entityCounts = new Dictionary<TraceEntity, uint>();
        private Commands commands = null;

        public DocumentConfig(string roboClerkID, string documentID, string documentTitle, string documentAbbreviation, string documentTemplate)
        {
            this.roboClerkID = roboClerkID;
            this.documentID = documentID;
            this.documentTitle = documentTitle;
            this.documentAbbreviation = documentAbbreviation;
            this.documentTemplate = documentTemplate;
        }

        public void AddCommands(Commands commands)
        {
            this.commands = commands;
        }


        public string RoboClerkID => roboClerkID;
        public string DocumentID => documentID;
        public string DocumentTitle => documentTitle;
        public string DocumentAbbreviation => documentAbbreviation;
        public string DocumentTemplate => documentTemplate;
        public Commands Commands => commands;
        public void AddEntityCount(TraceEntity te, uint count)
        {
            if (te != null)
            if (entityCounts.ContainsKey(te))
            {
                entityCounts[te] += count;
            }
            else
            {
                entityCounts.Add(te, count);
            }
        }

        public void ResetEntityCount(TraceEntity te)
        {
            if (te != null && entityCounts.ContainsKey(te))
            {
                entityCounts[te]=0;
            }
        }

        public uint GetEntityCount(TraceEntity te)
        {
            if (te != null && entityCounts.ContainsKey(te))
            {
                return entityCounts[te];
            }
            else return 0;
        }
    }
}
