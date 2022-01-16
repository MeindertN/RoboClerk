

namespace RoboClerk
{
    public enum DataSource
    {
        SLMS, //the software lifecycle management system
        Source, //found in the source code
        Config, //found in the RoboClerk project configuration file
        OTS, //found in a binary control system
        Info, //informational tag, about the document contents
        Trace, //trace tage that is expected to be traced to this document
        Unknown //it is not known where to retrieve this information
    }

    public class RoboClerkTag
    {
        private int contentStart = -1; //stores the start location of the content in the *original* markdown string
        private int contentEnd = -1; //stores the end location of the content similar to the content start location
        private int tagStart = -1;
        private int tagEnd = -1;
        private string contents = string.Empty; //what is inside the tag in the document
        private string contentCreatorID = string.Empty; //the identifier of this tag 
        private string traceReference = string.Empty; //the trace reference for this tag
        private string target = "ALL"; //the target category for this tag, ALL returns all which is the default
        private bool inline; //true if this tag was found inline
        private DataSource source = DataSource.Unknown;
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public RoboClerkTag(int startIndex, int endIndex, string rawDocument, bool inline)
        {
            this.inline = inline;
            if(inline)
            {
                logger.Debug("Processing inline RoboClerk tag.");
                ProcessRoboClerkContainerInlineTag(startIndex, endIndex, rawDocument);
            }
            else
            {
                logger.Debug("Processing RoboClerk container tag.");
                ProcessRoboClerkContainerTag(startIndex, endIndex, rawDocument);
            }
        }

        public bool Inline
        {
            get => inline;
        }

        public string ContentCreatorID
        {
            get => contentCreatorID;
        }

        public string TraceReference
        {
            get => traceReference;
        }

        public string Target
        {
            get => target;
        }

        public DataSource Source
        {
            get => source;
        }

        public string Contents 
        {
            get => contents;
            set => contents = value;
        }

        public int ContentStart 
        {
            get => contentStart;
        }

        public int ContentEnd 
        {
            get => contentEnd;
        }

        public int TagStart
        {
            get => tagStart;
        }

        public int TagEnd
        {
            get => tagEnd;
        }


        private void ProcessRoboClerkContainerInlineTag(int startIndex, int endIndex, string rawDocument)
        {
            tagStart = startIndex;
            tagEnd = endIndex + 1;
            contentStart = startIndex + 2; //remove starting tag
            contentEnd = endIndex; //remove ending tag
            var tagContents = rawDocument.Substring(contentStart,contentEnd-contentStart+1);
            contentEnd = contentStart + tagContents.IndexOf('(') - 1; //do not include ( itself 
            string infostring = tagContents.Split('(')[1].Split(')')[0];
            var items = infostring.Split(':');
            if(items.Length != 2)
            {
                throw new System.Exception($"Error parsing RoboClerkInlineContainer tag: {infostring}. Two elements separated by : expected but not found.");
            }
            contentCreatorID = GetContentCreatorID(items[0]);
            source = GetSource(items[1]);
            contents = rawDocument.Substring(contentStart,contentEnd - contentStart + 1);
        }

        private void ProcessRoboClerkContainerTag(int startIndex, int endIndex, string rawDocument)
        {
            tagStart = startIndex;
            tagEnd = endIndex + 3;
            //parse the tagInfo, items are separated by :
            string info = rawDocument.Substring(startIndex + 3, endIndex - startIndex).Split('\n')[0];
            var items = info.Split(':');
            if(items.Length < 2 && items.Length > 4)
            {
                throw new System.Exception($"Error parsing RoboClerkContainer tag: {info}. Two to four elements separated by : expected but not found.");
            }
            if(items.Length == 4)
            {
                traceReference = items[0];
                target = items[1].Replace('_', ' ');
                contentCreatorID = GetContentCreatorID(items[2]);
                source = GetSource(items[3]);
            }
            if (items.Length == 3)
            {
                target = items[0].Replace('_',' ');
                contentCreatorID = GetContentCreatorID(items[1]);
                source = GetSource(items[2]);
            }
            else
            {
                contentCreatorID = GetContentCreatorID(items[0]);
                source = GetSource(items[1]);
            }            

            var prelimTagContents = rawDocument.Substring(startIndex, endIndex - startIndex + 1);
            contentStart = startIndex + prelimTagContents.IndexOf('\n') + 1; //ensure to skip linebreak
            if (prelimTagContents.IndexOf('\n') == prelimTagContents.LastIndexOf('\n'))
            {
                //this tag is empty
                contentEnd = contentStart-1;
                contents = "";
            }
            else
            {
                contentEnd = startIndex + prelimTagContents.LastIndexOf('\n');
                contents = rawDocument.Substring(contentStart, contentEnd - contentStart + 1);
            }            
        }

        private DataSource GetSource(string name)
        {
            switch(name) 
            {
                case "SLMS": return DataSource.SLMS;
                case "Source": return DataSource.Source;
                case "Config": return DataSource.Config;
                case "OTS": return DataSource.OTS;
                case "Info": return DataSource.Info;
                case "Trace": return DataSource.Trace;
            }
            return DataSource.Unknown;
        }

        private string GetContentCreatorID(string et)
        {
            if(et.ToUpper() == "PR")
            {
                return "ProductRequirements";
            }
            if(et.ToUpper() == "SR")
            {
                return "SoftwareRequirements";
            }
            if(et.ToUpper() == "TC")
            {
                return "TestCases";
            }
            if(et.ToUpper() == "BG")
            {
                return "Bugs";
            }
            return et;
        }
            
    }
}