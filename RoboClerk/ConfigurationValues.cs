using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Tomlyn;
using Tomlyn.Syntax;
using Tomlyn.Model;

namespace RoboClerk
{
    public class ConfigurationValues
    {
        private Dictionary<string, string> keyValues = new Dictionary<string, string>();
        private Dictionary<string, string> docToTitle = new Dictionary<string, string>();
        public ConfigurationValues()
        {

        }

        public void FromToml(string config)
        {
            var toml = Toml.Parse(config).ToModel();
            foreach(var val in (TomlTable)toml["ConfigValues"])
            {
                keyValues[val.Key] = (string)val.Value;
            }

            foreach (var docloc in (TomlTable)toml["DocumentLocations"])
            {
                TomlArray arr = (TomlArray)docloc.Value;
                if ((string)arr[0] != "") //if empty the assumption is that there is no such document
                {
                    docToTitle[docloc.Key] = (string)arr[0];
                }
            }

        }

        public bool HasKey(string key)
        {
            return keyValues.ContainsKey(key);
        }

        public string GetValue(string key)
        {
            if( !keyValues.ContainsKey(key) )
            {
                return "NOT FOUND";
            }
            return keyValues[key];
        }

        public string GetDocumentTitle(string docID)
        {
            if (docToTitle.ContainsKey(docID))
            {
                return docToTitle[docID];
            }
            return string.Empty;
        }
    }
}
