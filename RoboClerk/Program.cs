using NLog;
using System;
using System.IO;
using System.Reflection;
using Tomlyn;
using Microsoft.Extensions.DependencyInjection;
using RoboClerk.Configuration;
using System.IO.Abstractions;
using CommandLine;
using System.Collections.Generic;

[assembly: AssemblyVersion("0.9.*")]

namespace RoboClerk
{
    class Program
    {
        private static void ConfigureLogging(string configFile)
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

        private static Dictionary<string,string> GetConfigOptions(IEnumerable<string> commandlineOptions, ILogger logger)
        {
            Dictionary<string,string> options = new Dictionary<string, string>();
            foreach(var commandlineOption in commandlineOptions)
            {
                if(commandlineOption != ",")
                {
                    var elements = commandlineOption.Split('=');
                    if(elements.Length != 2)
                    {
                        logger.Error($"Commandline option can not be parsed: {commandlineOption}. Please check commandline call, it should be in the form of <IDENTIFIER>=<VALUE>");
                        Console.WriteLine($"An error occurred parsing commandline option: {commandlineOption}. Expected syntax is <IDENTIFIER>=<VALUE>.");
                        throw new Exception("Error parsing commandline options.");
                    }
                    options[elements[0]] = elements[1];
                }
            }
            return options;
        }

        static int Main(string[] args)
        {
            try
            {
                Parser.Default.ParseArguments<CommandlineOptions>(args)
                    .WithParsed<CommandlineOptions>(options =>
                   {
                   //set up logging first
                   var assembly = Assembly.GetExecutingAssembly();
                       var projectConfigFile = $"{Path.GetDirectoryName(assembly.Location)}/Configuration/Project/projectConfig.toml";
                       var roboClerkConfigFile = $"{Path.GetDirectoryName(assembly.Location)}/Configuration/RoboClerk/RoboClerk.toml";
                       if (options.ConfigurationFile != null)
                       {
                           roboClerkConfigFile = options.ConfigurationFile;
                       }
                       if (options.ProjectConfigurationFile != null)
                       {
                           projectConfigFile = options.ProjectConfigurationFile;
                       }

                       try
                       {
                           ConfigureLogging(roboClerkConfigFile);
                       }
                       catch (Exception e)
                       {
                           Console.WriteLine($"An error occurred configuring Roboclerk logging: \n{e.Message}");
                           throw;
                       }
                       var logger = NLog.LogManager.GetCurrentClassLogger();
                       logger.Info($"RoboClerk Version: {Assembly.GetExecutingAssembly().GetName().Version}");
                       var commandlineOptions = GetConfigOptions(options.ConfigurationOptions, logger);
                       try
                       {
                           var serviceProvider = new ServiceCollection()
                               .AddTransient<IFileSystem, FileSystem>()
                               .AddSingleton<IConfiguration>(x => new RoboClerk.Configuration.Configuration(x.GetRequiredService<IFileSystem>(), roboClerkConfigFile, projectConfigFile, commandlineOptions))
                               .AddTransient<IPluginLoader, PluginLoader>()
                               .AddSingleton<IDataSources, DataSources>()
                               .AddSingleton<ITraceabilityAnalysis, TraceabilityAnalysis>()
                               .AddSingleton<IRoboClerkCore, RoboClerkCore>()
                               .BuildServiceProvider();

                           var core = serviceProvider.GetService<IRoboClerkCore>();
                           core.GenerateDocs();
                           core.SaveDocumentsToDisk();
                       }
                       catch (Exception e)
                       {
                           logger.Error("An unhandled exception has occurred. RoboClerk failed to complete:\n\n");
                           logger.Error(e);
                           throw;
                       }
                   });
            }
            catch
            {
                return 1;
            }
            return 0;
        }
    }
}
