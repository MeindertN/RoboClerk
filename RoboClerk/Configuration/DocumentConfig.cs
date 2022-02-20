using System.Collections.Generic;
using Tomlyn.Model;

namespace RoboClerk.Configuration
{
    public class DocumentConfig
    {
        private string documentID = string.Empty;
        private string documentTitle = string.Empty;
        private string documentAbbreviation = string.Empty;
        private string documentTemplate = string.Empty;
        private Commands commands = null;

        public DocumentConfig(string documentID, string documentTitle, string documentAbbreviation, string documentTemplate)
        {
            this.documentID = documentID;
            this.documentTitle = documentTitle;
            this.documentAbbreviation = documentAbbreviation;
            this.documentTemplate = documentTemplate;
        }

        public void AddCommands(Commands commands)
        {
            this.commands = commands;
        }

        public string DocumentID => documentID;
        public string DocumentTitle => documentTitle;
        public string DocumentAbbreviation => documentAbbreviation;
        public string DocumentTemplate => documentTemplate;
        public Commands Commands => commands; 
    }
}
