using System;
using System.Collections.Generic;
using Tomlyn.Model;

namespace RoboClerk.Configuration
{
    public class ConfigurationValues
    {
        private Dictionary<string, string> keyValues = new Dictionary<string, string>();
        public ConfigurationValues()
        {

        }

        public void FromToml(TomlTable toml)
        {
            if( !toml.ContainsKey("ConfigValues"))
            {
                throw new Exception("Required configuration element \"ConfigValues\" is missing from project configuration file. Cannot continue.");
            }
            foreach (var val in (TomlTable)toml["ConfigValues"])
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
            if (!keyValues.ContainsKey(key))
            {
                return "NOT FOUND";
            }
            return keyValues[key];
        }
    }
}
