using CommandLine;

namespace RoboClerk.Server
{
    internal class CommandlineOptions
    {
        [Option('c', "configurationFile", Required = false, HelpText = "Indicate the location of the RoboClerk configuration file.")]
        public string ConfigurationFile { get; set; }

        [Option('s', "serverConfigurationFile", Required = false, HelpText = "Indicate the location of the RoboClerk Server configuration file.")]
        public string ServerConfigurationFile { get; set; }

        [Option('o', "options", Required = false, HelpText = "Various space seperated options, can be used to substitute configuration file values to plugins and RoboClerk. E.g. -o APIKey=1234fdsa magicWords=\"one two three\" SPClientSecret=abc123")]
        public IEnumerable<string> ConfigurationOptions { get; set; }
        
        // Common configuration options:
        // - SPClientSecret: Azure AD client secret for SharePoint access (required for document URL-based project loading)
        // - SPClientId: Azure AD client ID (automatically injected from server config, can be overridden)
        // - SPTenantId: Azure AD tenant ID (automatically injected from server config, can be overridden)
        // - APIKey: API key for external services
        // - Any other key=value pair that can override configuration file values
        //
        // Note: SPClientID and SPTenantID are automatically loaded from RoboClerk.Server.toml [SharePoint] section
        // and added to the RoboClerk configuration as command-line options. You can override them by explicitly
        // providing them via -o if needed.
    }
}
