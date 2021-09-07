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
        public ConfigurationValues()
        {

        }

        public void FromToml(string configFile)
        {
            string config = File.ReadAllText(configFile);
            var toml = Toml.Parse(config).ToModel();
            foreach(var val in (TomlTable)toml["ConfigValues"])
            {
                keyValues[val.Key] = (string)val.Value;
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
    }
}
