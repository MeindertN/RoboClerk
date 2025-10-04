using NLog;
using System;
using System.Text;

namespace RoboClerk
{
    internal class Logging
    {
        private string logFileName = string.Empty;
        private string logLevel = "DEBUG";
        private bool configured = false;
        private bool fileDestinationSet = false;
        private IFileProviderPlugin fileProvider = null!;
        private string directory = string.Empty;
        internal Logging(string logFileName="RoboClerkLog.txt")
        {
            this.logFileName = logFileName;
        }

        public string LogFileName { get { return logFileName; } }
        public bool Configured { get { return configured; } }

        public void ConfigureLogging(string lLevel)
        {
            logLevel = lLevel;
            var config = new NLog.Config.LoggingConfiguration();
            // Targets where to log to
            var logFile = new NLog.Targets.MemoryTarget("memory");

            if (logLevel.ToUpper() == "DEBUG")
            {
                config.AddRule(LogLevel.Debug, LogLevel.Fatal, logFile);
            }
            else if (logLevel.ToUpper() == "WARN")
            {
                config.AddRule(LogLevel.Warn, LogLevel.Fatal, logFile);
            }
            else
            {
                config.AddRule(LogLevel.Info, LogLevel.Fatal, logFile);
            }
            LogManager.Configuration = config;
            configured = true;
        }

        public void SetLogDestination(IFileProviderPlugin fileProvider, string directory)
        {
            this.fileProvider = fileProvider ?? throw new ArgumentNullException(nameof(fileProvider));
            this.directory = directory;
            fileDestinationSet = true;
        }

        internal void WriteLogToFile()
        {
            if(!configured)
            {
                Console.WriteLine("Logging has not been configured. Cannot write log file to file.");
                throw new InvalidOperationException("Logging has not been configured. Call ConfigureLogging first.");
            }   
            var logger = LogManager.GetCurrentClassLogger();
            var memoryTarget = LogManager.Configuration.FindTargetByName<NLog.Targets.MemoryTarget>("memory");
            if (!fileDestinationSet)
            {
                Console.WriteLine("Log destination has not been set. Writing log to console.");
                if (memoryTarget != null)
                {
                    foreach (var logEvent in memoryTarget.Logs)
                    {
                        Console.WriteLine(logEvent);
                    }
                }
            }
                       
            if (memoryTarget != null)
            {
                StringBuilder logContent = new StringBuilder();
                foreach (var logEvent in memoryTarget.Logs)
                {
                    logContent.AppendLine(logEvent);
                }
                var logFileLocation = fileProvider.Combine(directory, logFileName);
                fileProvider.WriteAllText(logFileLocation, logContent.ToString());
            }
            LogManager.Shutdown();
        }
    }
}
