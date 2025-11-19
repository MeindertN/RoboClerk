using CommandLine;
using System.Collections.Generic;

namespace RoboClerk
{
    internal class CommandlineOptions
    {
        [Option('c', "configurationFile", Required = false, HelpText = "Indicate the location of the RoboClerk configuration file.")]
        public string ConfigurationFile { get; set; }

        [Option('p', "projectFile", Required = false, HelpText = "Indicate the location of the RoboClerk project configuration file.")]
        public string ProjectConfigurationFile { get; set; }

        [Option('o', "options", Required = false, HelpText = "Various space seperated options, can be used to substitute configuration file values to plugins and RoboClerk. E.g. -o APIKey=1234fdsa magicWords=\"one two three\"")]
        public IEnumerable<string> ConfigurationOptions { get; set; }
    }
}
