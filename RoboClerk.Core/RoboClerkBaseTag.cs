using System;
using System.Collections.Generic;
using System.Linq;

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
    }
}