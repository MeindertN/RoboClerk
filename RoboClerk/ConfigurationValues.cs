﻿using System.Collections.Generic;
using Tomlyn;
using Tomlyn.Model;

namespace RoboClerk
{
    public class ConfigurationValues
    {
        private Dictionary<string, string> keyValues = new Dictionary<string, string>();
        public ConfigurationValues()
        {

        }

        public void FromToml(TomlTable toml)
        {
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
