using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RoboClerk.AISystem
{
    public class PromptTemplate
    {
        private List<string> segments = new List<string>();
        private Dictionary<string,int> identifiers = new Dictionary<string, int>();
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public PromptTemplate(string template)
        {
            Init(template);
        }

        private void Init(string template)
        {
            string[] segs = template.Split("%{");
            segments.Add(segs[0]);

            for(int i=1; i<segs.Length; i++) 
            {
                string[] moreSegs = segs[i].Split("}%");
                identifiers.Add(moreSegs[0].ToUpper(),segments.Count);
                segments.Add(moreSegs[0]);
                segments.Add(moreSegs[1]);
            }   
        }

        public string GetPrompt(Dictionary<string, string> parameters) 
        {
            List<string> segmentsClone = new List<string>(segments);
            foreach( var parameter in parameters )
            {
                if( identifiers.ContainsKey(parameter.Key.ToUpper()) )
                {
                    segmentsClone[identifiers[parameter.Key.ToUpper()]] = parameter.Value; 
                }
            }
            foreach( var identifier in identifiers)
            {
                if(!parameters.Keys.Any(key => string.Equals(key, identifier.Key, StringComparison.OrdinalIgnoreCase)))
                {
                    logger.Warn("Prompt template identifier not found/supplied: \"{identifier.Key}\".");
                }
            }
            return string.Join( "", segmentsClone );
        }

        public string GetPrompt<T>(Dictionary<string, string> parameters, T item)
        {
            Dictionary<string, string> parametersClone = new Dictionary<string, string>(parameters);
            PropertyInfo[] properties = item.GetType().GetProperties();
            foreach( var property in properties ) 
            {
                if (!parametersClone.ContainsKey(property.Name))
                {
                    if (property.GetValue(item) != null)
                    {
                        parametersClone[property.Name] = property.GetValue(item).ToString();
                    }
                }
            }
            return GetPrompt(parametersClone);
        }
    }
}
