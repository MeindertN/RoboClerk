using RoboClerk.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Tomlyn;
using CliWrap;
using System.Text;

namespace RoboClerk.Gradle
{
    public class GradleDependenciesPlugin : IDependencyManagementPlugin
    {
        private string name = string.Empty;
        private string description = string.Empty;
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private string gradleConfig = string.Empty;
        private string projectRoot = string.Empty;
        private string commandlineToGradle = string.Empty;

        private List<ExternalDependency> externalDependencies = new List<ExternalDependency>();

        public GradleDependenciesPlugin()
        {
            name = "Gradle Dependencies Plugin";
            description = "A plugin that retrieves project dependencies via Gradle.";
        }

        public string Name => name;

        public string Description => description;

        public List<ExternalDependency> GetDependencies()
        {
            throw new System.NotImplementedException();
        }

        public void Initialize(IConfiguration configuration)
        {
            logger.Info("Initializing the Redmine SLMS Plugin");
            var assembly = Assembly.GetAssembly(this.GetType());
            try
            {
                var configFileLocation = $"{Path.GetDirectoryName(assembly?.Location)}/Configuration/GradleDependencyPlugin.toml";
                if (configuration.PluginConfigDir != string.Empty)
                {
                    configFileLocation = Path.Combine(configuration.PluginConfigDir, "GradleDependencyPlugin.toml");
                }
                var config = Toml.Parse(File.ReadAllText(configFileLocation)).ToModel();
                gradleConfig = configuration.CommandLineOptionOrDefault("GradleConfiguration", (string)config["GradleConfiguration"]);
                commandlineToGradle = configuration.CommandLineOptionOrDefault("CommandlineToGradle", (string)config["CommandlineToGradle"]);
                projectRoot = configuration.ProjectRoot;
            }
            catch (Exception e)
            {
                logger.Error("Error reading configuration file for Gradle Dependencies plugin.");
                logger.Error(e);
                throw new Exception("The Gradle Dependencies plugin could not read its configuration. Aborting...");
            }
        }

        public void RefreshItems()
        {
            var result = RunGradle(SubstituteStrings(commandlineToGradle));
            ParseGradleOutput(result);
        }

        private void ParseGradleOutput(string gradleOutput)
        {
            var lines = gradleOutput.Split('\n');
            foreach(var line in lines)
            {
                //if(line.StartsWith("+---"))
            }
        }

        private string SubstituteStrings(string command)
        {
            command.Replace("%PROJECTROOT%", projectRoot);
            return command;
        }

        private string RunGradle(string command)
        {
            var stdOutBuffer = new StringBuilder();
            var stdErrBuffer = new StringBuilder();
            CommandResultValidation validation = CommandResultValidation.ZeroExitCode;
            var result = Cli.Wrap(command)
                .WithArguments($"dependencies --configuration {gradleConfig}")
                .WithWorkingDirectory(projectRoot)
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .WithValidation(validation);
            result.ExecuteAsync().Task.Wait();
            return stdOutBuffer.ToString();
        }
    }
}