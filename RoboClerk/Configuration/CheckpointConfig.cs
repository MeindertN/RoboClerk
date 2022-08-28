using System;
using System.Collections.Generic;
using Tomlyn.Model;

namespace RoboClerk.Configuration
{
    public class CheckpointConfig
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public string CheckpointFile { get; set; } = string.Empty;

        public List<string> UpdatedSystemRequirementIDs {  get; set; } = new List<string>();

        public List<string> UpdatedSoftwareRequirementIDs { get; set; } = new List<string>();

        public List<string> UpdatedSoftwareSystemTestIDs { get; set; } = new List<string>();

        public List<string> UpdatedSoftwareUnitTestIDs { get; set; } = new List<string>();

        public List<string> UpdatedRiskIDs { get; set; } = new List<string>();

        public List<string> UpdatedAnomalyIDs { get; set; } = new List<string>();

        public List<string> UpdatedSOUPIDs { get; set; } = new List<string>();

        public void FromToml(TomlTable toml)
        {
            if (!toml.ContainsKey("CheckpointConfiguration"))
            {
                logger.Warn("CheckpointConfiguration table missing from the project configuration file. RoboClerk will not be able to process any checkpoint configurations.");
                return;
            }
            foreach (var val in (TomlTable)toml["CheckpointConfiguration"])
            {
                switch(val.Key)
                {
                    case "CheckpointFile": 
                        CheckpointFile = (string)val.Value; 
                        break;
                    case "UpdatedSystemRequirementIDs":
                        foreach(var sysid in (TomlArray)val.Value)
                        {
                            UpdatedSystemRequirementIDs.Add((string)sysid);
                        }
                        break;
                    case "UpdatedSoftwareRequirementIDs":
                        foreach (var id in (TomlArray)val.Value)
                        {
                            UpdatedSoftwareRequirementIDs.Add((string)id);
                        }
                        break;
                    case "UpdatedSoftwareSystemTestIDs":
                        foreach (var id in (TomlArray)val.Value)
                        {
                            UpdatedSoftwareSystemTestIDs.Add((string)id);
                        }
                        break;
                    case "UpdatedSoftwareUnitTestIDs":
                        foreach (var id in (TomlArray)val.Value)
                        {
                            UpdatedSoftwareUnitTestIDs.Add((string)id);
                        }
                        break;
                    case "UpdatedRiskIDs":
                        foreach (var id in (TomlArray)val.Value)
                        {
                            UpdatedRiskIDs.Add((string)id);
                        }
                        break;
                    case "UpdatedAnomalyIDs":
                        foreach (var id in (TomlArray)val.Value)
                        {
                            UpdatedAnomalyIDs.Add((string)id);
                        }
                        break;
                    case "UpdatedSOUPIDs":
                        foreach (var id in (TomlArray)val.Value)
                        {
                            UpdatedSOUPIDs.Add((string)id);
                        }
                        break;
                    default:
                        throw new Exception($"Unknown CheckpointConfiguration item \"{val.Key}\" found, please check project ocnfiguration file.");
                }
            }
        }
    }
}
