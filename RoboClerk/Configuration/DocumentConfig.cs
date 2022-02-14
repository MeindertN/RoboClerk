using System.Collections.Generic;
using Tomlyn.Model;

namespace RoboClerk.Configuration
{
    internal class DocumentConfig
    {
        private string documentID = string.Empty;
        private string documentTitle = string.Empty;
        private string documentAbbreviation = string.Empty;
        private string documentTemplate = string.Empty;
        private Commands commands = null;

        internal DocumentConfig(string documentID, string documentTitle, string documentAbbreviation, string documentTemplate)
        {
            this.documentID = documentID;
            this.documentTitle = documentTitle;
            this.documentAbbreviation = documentAbbreviation;
            this.documentTemplate = documentTemplate;
        }

        internal void AddCommands(Commands commands)
        {
            this.commands = commands;
        }

        internal string DocumentID => documentID;
        internal string DocumentTitle => documentTitle;
        internal string DocumentAbbreviation => documentAbbreviation;
        internal string DocumentTemplate => documentTemplate;
        internal Commands Commands => commands; 
    }
}
