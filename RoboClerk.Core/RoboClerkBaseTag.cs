using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RoboClerk.Core
{
    /// <summary>
    /// Base class providing common functionality for RoboClerk tags
    /// </summary>
    public abstract class RoboClerkBaseTag : IRoboClerkTag
    {
        protected string contents = string.Empty;
        protected string contentCreatorID = string.Empty;
        protected Dictionary<string, string> parameters = new Dictionary<string, string>();
        protected DataSource source = DataSource.Unknown;

        public virtual DataSource Source => source;
        public virtual string ContentCreatorID => contentCreatorID;
        public virtual IEnumerable<string> Parameters => parameters.Keys;
        public virtual string Contents 
        { 
            get => contents; 
            set => contents = value; 
        }
        
        public abstract bool Inline { get; }

        public virtual string GetParameterOrDefault(string parameterName, string defaultValue = "")
        {
            return parameters.TryGetValue(parameterName.ToUpper(), out string? value) ? value : defaultValue;
        }

        public virtual bool HasParameter(string parameterName)
        {
            return parameters.ContainsKey(parameterName.ToUpper());
        }

        public virtual void UpdateContent(string newContent)
        {
            Contents = newContent;
        }

        public abstract IEnumerable<IRoboClerkTag> ProcessNestedTags();

        protected static DataSource GetSource(string name)
        {
            switch (name.ToUpper())
            {
                case "SLMS": return DataSource.SLMS;
                case "SOURCE": return DataSource.Source;
                case "CONFIG": return DataSource.Config;
                case "OTS": return DataSource.OTS;
                case "POST": return DataSource.Post;
                case "COMMENT": return DataSource.Comment;
                case "TRACE": return DataSource.Trace;
                case "DOCUMENT": return DataSource.Document;
                case "REF": return DataSource.Reference;
                case "FILE": return DataSource.File;
                case "AI": return DataSource.AI;
                case "WEB": return DataSource.Web;
                default: return DataSource.Unknown;
            }
        }

        protected void ExtractParameters(string parameterString)
        {
            int paramStart = parameterString.IndexOf('(');
            int paramEnd = parameterString.IndexOf(')');
            if (paramEnd - paramStart == 1)
            {
                return; // there are no parameters
            }
            string param = parameterString.Substring(paramStart + 1, paramEnd - paramStart - 1);
            var items = param.Split(',');
            if (items.Length > 0)
            {
                foreach (var item in items)
                {
                    var paramParts = item.Split('=');
                    if (paramParts.Length == 2)
                    {
                        parameters[paramParts[0].Trim().ToUpper()] = paramParts[1].Trim();
                    }
                }
            }
        }

        protected static void ValidateTagContents(string tagContents)
        {
            //verify required elements are present and in the right order
            if (tagContents.Count(f => (f == '(')) != 1 ||
                tagContents.Count(f => (f == ')')) != 1 ||
                tagContents.Substring(0, tagContents.IndexOf('(')).Count(f => (f == ':')) != 1 ||
                tagContents.IndexOf(')') < tagContents.IndexOf('(') ||
                tagContents.IndexOf(':') > tagContents.IndexOf('('))
            {
                throw new TagInvalidException(tagContents, "RoboClerk tag is not formatted correctly");
            }
            //verify the preamble
            string temp = Regex.Replace(tagContents, @"\s+", ""); //remove whitespace
            string[] preamble = temp.Split('(')[0].Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (preamble.Length != 2)
            {
                throw new TagInvalidException(tagContents, "Preamble section in RoboClerk tag not formatted correctly");
            }
            //verify the parameter string
            if (temp.IndexOf(')') - temp.IndexOf('(') > 1)
            {
                if (temp.Count(f => (f == '=')) - temp.Count(f => (f == ',')) != 1)
                {
                    throw new TagInvalidException(tagContents, "Parameter section in RoboClerk tag not formatted correctly");
                }
                //isolate the parameter string and check each individual element
                string contents = temp.Split('(')[1].Split(')')[0];
                string[] elements = contents.Split(',');
                foreach (var element in elements)
                {
                    if (element.Count(f => (f == '=')) != 1)
                    {
                        throw new TagInvalidException(tagContents, "Malformed element in parameter section of RoboClerk tag");
                    }
                    string[] variables = element.Split('=', StringSplitOptions.RemoveEmptyEntries);
                    if (variables.Length != 2)
                    {
                        throw new TagInvalidException(tagContents, "Malformed element in parameter section of RoboClerk tag");
                    }
                }
            }
        }

        protected void ParseSourceAndContentCreator(string tagContents)
        {
            // Parse source and content creator ID from "Source:ContentCreatorID(params)" format
            var items = tagContents.Split('(')[0].Split(':');
            if (items.Length >= 2)
            {
                source = GetSource(items[0].Trim().ToUpper());
                contentCreatorID = items[1].Trim();
                
                if (source == DataSource.Unknown)
                {
                    throw new TagInvalidException(tagContents, $"Unknown datasource {items[0].Trim()}");
                }
            }
            else
            {
                throw new TagInvalidException(tagContents, "RoboClerk tag preamble is not formatted correctly - expected Source:ContentCreatorID format");
            }
        }

        protected void ParseCompleteTag(string tagContents)
        {
            ValidateTagContents(tagContents);
            ParseSourceAndContentCreator(tagContents);
            ExtractParameters(tagContents);
        }
    }
}