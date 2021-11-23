using Markdig;
using Markdig.Syntax;
using Markdig.Extensions.RoboClerk;

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
        private int start = -1; //stores the start location in the *original* markdown string
        private int end = -1; //stores the end location similar to the start location
        private string contents = string.Empty; //what is inside the tag in the document
        private string contentCreatorID = string.Empty; //the identifier of this tag 
        private string traceReference = string.Empty; //the trace reference for this tag
        private string target = "ALL"; //the target category for this tag, ALL returns all which is the default
        private bool inline; //true if this tag was found inline
        private DataSource source = DataSource.Unknown;
        public RoboClerkTag(RoboClerkContainer tag, string rawDocument)
        {
            inline = false;
            ProcessRoboClerkContainerTag(tag, rawDocument);
        }

        public RoboClerkTag(RoboClerkContainerInline tag, string rawDocument)
        {
            inline = true;
            ProcessRoboClerkContainerInlineTag(tag, rawDocument);
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

        public int Start 
        {
            get => start;
        }

        public int End 
        {
            get => end;
        }

        private void ProcessRoboClerkContainerInlineTag(RoboClerkContainerInline tag, string rawDocument)
        {
            start = tag.Span.Start+2; //remove starting tag
            end = tag.Span.End-2; //remove ending tag
            var tagContents = rawDocument.Substring(start,end-start+1);
            end = start + tagContents.IndexOf('(') - 1; //do not include ( itself 
            string infostring = tagContents.Split('(')[1].Split(')')[0];
            var items = infostring.Split(':');
            if(items.Length != 2)
            {
                throw new System.Exception($"Error parsing RoboClerkInlineContainer tag: {infostring}. Two elements separated by : expected but not found.");
            }
            contentCreatorID = GetContentCreatorID(items[0]);
            source = GetSource(items[1]);
            contents = rawDocument.Substring(start,end - start + 1);
        }

        private void ProcessRoboClerkContainerTag(RoboClerkContainer container, string rawDocument)
        {
            //parse the tagInfo, items are separated by :
            var items = container.Info.Split(':');
            if(items.Length < 2 && items.Length > 4)
            {
                throw new System.Exception($"Error parsing RoboClerkContainer tag: {container.Info}. Two to four elements separated by : expected but not found.");
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

            var prelimTagContents = rawDocument.Substring(container.Span.Start, container.Span.End - container.Span.Start + 1);
            start = container.Span.Start + prelimTagContents.IndexOf('\n') + 1; //ensure to skip linebreak
            if (prelimTagContents.IndexOf('\n') == prelimTagContents.LastIndexOf('\n'))
            {
                //this tag is empty
                end = start-1;
                contents = "";
            }
            else
            {
                end = container.Span.Start + prelimTagContents.LastIndexOf('\n');
                contents = rawDocument.Substring(start, end - start + 1);
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
            return et;
        }
            
    }
}