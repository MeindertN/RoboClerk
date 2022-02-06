

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RoboClerk
{
    public class TagInvalidException : Exception
    {
        private int line = -1;
        private int character = -1;
        private string tagContents = string.Empty;
        private string documentTitle = string.Empty;
        private string reason = string.Empty;

        public TagInvalidException(string tagContents, string reason)
        {
            this.tagContents = tagContents;
            this.reason = reason;
        }

        public string TagContents
        {
            get => tagContents;
        }

        public string Reason
        {
            get => reason;
        }

        public string DocumentTitle
        {
            set => documentTitle = value;
        }

        public override string Message
        {
            get
            {
                if(documentTitle == string.Empty )
                {
                    if(line == -1 || character == -1)
                    {
                        return $"{reason}. Tag contents: {tagContents}";
                    }
                    else
                    {
                        return $"{reason} at ({line}:{character}). Tag contents: {tagContents}";
                    }
                }
                else
                {
                    if (line == -1 || character == -1)
                    {
                        return $"{reason} in {documentTitle} template. Tag contents: {tagContents}";
                    }
                    else
                    {
                        return $"{reason} in {documentTitle} template at ({line}:{character}). Tag contents: {tagContents}";
                    }
                }
            }
        }

        public void SetLocation(int tagStart, string doc)
        {
            string relevantPart = doc.Substring(0, tagStart);
            line = relevantPart.Count(f => (f == '\n'))+1;
            character = tagStart - relevantPart.LastIndexOf('\n');
        }
    }

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
        private Dictionary<string, string> parameters = new Dictionary<string, string>();
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

        public Dictionary<string,string> Parameters
        {
            get => parameters;
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
            contents = rawDocument.Substring(contentStart,contentEnd - contentStart );
            //contentEnd = contentStart + tagContents.IndexOf('(') - 1; //do not include ( itself 
            try
            {
                ValidateTagContents(contents);
            }
            catch(TagInvalidException e)
            {
                e.SetLocation(tagStart, rawDocument);
                throw e;
            }
            ExtractParameters(contents);
            var items = contents.Split('(')[0].Split(':');
            contentCreatorID = items[1].Trim();
            source = GetSource(items[0].Trim());
            //contents = rawDocument.Substring(contentStart,contentEnd - contentStart + 1);
        }

        private void ValidateTagContents(string tagContents)
        {
            //verify required elements are present and in the right order
            if( tagContents.Count(f => (f == '(')) != 1 ||
                tagContents.Count(f => (f == ')')) != 1 ||
                tagContents.Count(f => (f == ':')) != 1 ||
                tagContents.IndexOf(')') < tagContents.IndexOf('(') ||
                tagContents.IndexOf(':') > tagContents.IndexOf('(') )
            {
                throw new TagInvalidException(tagContents, "RoboClerk tag is not formatted correctly");
            }
            //verify the preamble
            string temp = Regex.Replace(tagContents, @"\s+", ""); //remove whitespace
            string[] preamble = temp.Split('(')[0].Split(':');
            if(preamble.Length != 2)
            {
                throw new TagInvalidException(tagContents, "Preamble section in RoboClerk tag not formatted correctly");
            }
            //verify the parameter string
            if( temp.IndexOf(')') - temp.IndexOf('(') > 1 )
            {
                if( temp.Count(f => (f == '=')) - temp.Count(f => (f == ',')) != 1 )
                {
                    throw new TagInvalidException(tagContents, "Parameter section in RoboClerk tag not formatted correctly");
                }
                //isolate the parameter string and check each individual element
                string contents = temp.Split('(')[1].Split(')')[0];
                string[] elements = contents.Split(',');
                foreach(var element in elements)
                {
                    if( element.Count( f=> (f == '=')) != 1)
                    {
                        throw new TagInvalidException(tagContents, "Malformed element in parameter section of RoboClerk tag");
                    }
                    string[] variables = element.Split('=');
                    if( variables.Length != 2)
                    {
                        throw new TagInvalidException(tagContents, "Malformed element in parameter section of RoboClerk tag");
                    }
                }
            }
        }

        private void ProcessRoboClerkContainerTag(int startIndex, int endIndex, string rawDocument)
        {
            tagStart = startIndex;
            tagEnd = endIndex + 3;
            //parse the tagInfo, items are separated by :
            string tagContents = rawDocument.Substring(startIndex + 3, endIndex - startIndex).Split('\n')[0];
            try
            {
                ValidateTagContents(tagContents);
            }
            catch (TagInvalidException e)
            {
                e.SetLocation(tagStart, rawDocument);
                throw e;
            }
            var items = tagContents.Split(':');
            source = GetSource(items[0].Trim());
            contentCreatorID = items[1].Split('(')[0].Trim();
            ExtractParameters(tagContents);
            var prelimTagContents = rawDocument.Substring(startIndex, endIndex - startIndex + 1);
            contentStart = startIndex + prelimTagContents.IndexOf('\n') + 1; //ensure to skip linebreak
            if (prelimTagContents.IndexOf('\n') == prelimTagContents.LastIndexOf('\n'))
            {
                //this tag is empty
                contentEnd = contentStart - 1;
                contents = "";
            }
            else
            {
                contentEnd = startIndex + prelimTagContents.LastIndexOf('\n');
                contents = rawDocument.Substring(contentStart, contentEnd - contentStart + 1);
            }
        }

        private void ExtractParameters(string parameterString)
        {
            int paramStart = parameterString.IndexOf('(');
            int paramEnd = parameterString.IndexOf(')');
            if(paramEnd - paramStart == 1)
            {
                return; //there are no parameters
            }
            string param = parameterString.Substring(paramStart + 1, paramEnd - paramStart - 1);
            var items = param.Split(',');
            if (items.Length > 0)
            {
                foreach (var item in items)
                {
                    parameters[item.Split('=')[0].Trim().ToUpper()] = item.Split('=')[1].Trim();
                }
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

        public string GetParameterOrDefault(string key, string defaultVal)
        {
            if(parameters.ContainsKey(key))
            {
                return parameters[key];
            }
            return defaultVal;
        }
    }
}