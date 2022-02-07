using NLog;
using System;
using System.IO;
using System.Reflection;
using Tomlyn;

namespace RoboClerk
{
    class Program
    {
        static void ConfigureLogging(string configFile)
        {
            var toml = Toml.Parse(File.ReadAllText(configFile)).ToModel();
            var logLevel = (string)toml["LogLevel"];
            var outputDir = (string)toml["OutputDirectory"];

            var config = new NLog.Config.LoggingConfiguration();

            // Targets where to log to
            var logFile = new NLog.Targets.FileTarget("logfile")
            {
                FileName =
                $"{outputDir}{Path.DirectorySeparatorChar}RoboClerkLog.txt",
                DeleteOldFileOnStartup = true
            };
            Console.WriteLine(logFile.FileName);
            if (logLevel.ToUpper() == "DEBUG")
            {
                config.AddRule(LogLevel.Debug, LogLevel.Fatal, logFile);
            }
            else
            {
                config.AddRule(LogLevel.Info, LogLevel.Fatal, logFile);
            }
            LogManager.Configuration = config;
        }

        static void Main(string[] args)
        {
            //set up logging first
            var assembly = Assembly.GetExecutingAssembly();
            var projectConfigFile = $"{Path.GetDirectoryName(assembly.Location)}/Configuration/Project/projectConfig.toml";
            var roboClerkConfigFile = $"{Path.GetDirectoryName(assembly.Location)}/Configuration/RoboClerk/RoboClerk.toml";

            try
            {
                ConfigureLogging(roboClerkConfigFile);
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred configuring Roboclerk logging: \n{e.Message}");
            }

            try
            {
                RoboClerkCore core = new RoboClerkCore(roboClerkConfigFile, projectConfigFile);
                core.GenerateDocs();
                core.SaveDocumentsToDisk();
            }
            catch (Exception e)
            {
                var logger = NLog.LogManager.GetCurrentClassLogger();
                logger.Error("An unhandled exception has occurred. RoboClerk failed to complete:\n\n");
                logger.Error(e);
            }
        }
    }
}
