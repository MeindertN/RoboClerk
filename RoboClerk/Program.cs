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

        private static void CleanOutputDirectory(string outputDir, ILogger logger)
        {
            logger.Info("Cleaning output directory.");
            string[] files = Directory.GetFiles(outputDir);
            foreach (string file in files)
            {
                if (!file.Contains("RoboClerkLog.txt") &&
                    !file.Contains(".gitignore"))
                {
                    File.Delete(file);
                }
            }
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
                           var serviceCollection = new ServiceCollection();
                           serviceCollection.AddTransient<IFileSystem, FileSystem>();
                           serviceCollection.AddSingleton<IConfiguration>(x => new RoboClerk.Configuration.Configuration(x.GetRequiredService<IFileSystem>(), roboClerkConfigFile, projectConfigFile, commandlineOptions));
                           serviceCollection.AddTransient<IPluginLoader, PluginLoader>();
                           serviceCollection.AddSingleton<ITraceabilityAnalysis, TraceabilityAnalysis>();
                           serviceCollection.AddSingleton<IRoboClerkCore, RoboClerkCore>();
                           
                           var serviceProvider = serviceCollection.BuildServiceProvider();

                           //clean the output directory before we start working
                           var config = serviceProvider.GetService<IConfiguration>();
                           if(config != null && config.ClearOutputDir)
                           {
                               CleanOutputDirectory(config.OutputDir,logger);
                           }
                           if (config.CheckpointConfig.CheckpointFile == string.Empty) //check if we are not using a checkpoint
                           {
                               serviceCollection.AddSingleton<IDataSources, PluginDataSources>();
                           }
                           else
                           {
                               serviceCollection.AddSingleton<IDataSources>(x => new CheckpointDataSources(x.GetRequiredService<IConfiguration>(), x.GetRequiredService<IPluginLoader>(), x.GetRequiredService<IFileSystem>(), config.CheckpointConfig.CheckpointFile));
                           }
                           serviceProvider = serviceCollection.BuildServiceProvider();

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
