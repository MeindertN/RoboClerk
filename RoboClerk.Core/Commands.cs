using CliWrap;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Tomlyn.Model;

namespace RoboClerk
{
    public class Commands
    {
        private string outputDir = string.Empty;
        private string filename = string.Empty;
        private string filenameNoExt = string.Empty;
        private string inputDir = string.Empty;
        private List<string> executables = new List<string>();
        private List<string> workingDirectories = new List<string>();
        private List<string> arguments = new List<string>();
        private List<bool> ignoreErrors = new List<bool>();
        protected static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public Commands(TomlTableArray commands, string outputDir, string filename, string inputDir)
        {
            this.outputDir = outputDir;
            this.filename = filename;
            this.filenameNoExt = Path.GetFileNameWithoutExtension(filename);
            this.inputDir = inputDir;
            ProcessCommands(commands);
        }

        private void ProcessCommands(TomlTableArray commands)
        {
            if (commands == null)
                return;
            DateTime currentDateTime = DateTime.Now;

            foreach (var command in commands)
            {
                if (!command.ContainsKey("executable") || !command.ContainsKey("arguments") ||
                    !command.ContainsKey("workingDirectory") || !command.ContainsKey("ignoreErrors"))
                {
                    throw new ArgumentException($"The command processor cannot process the requested command due to a missing configuration element.");
                }
                if ((string)command["executable"] == "") //if executable is unknown we cannot execute
                    continue;
                executables.Add((string)command["executable"]);
                string temp = (string)command["workingDirectory"];
                if (temp == String.Empty)
                {
                    temp = outputDir;
                }
                workingDirectories.Add(ReplaceVariables(temp, currentDateTime));
                temp = (string)command["arguments"];
                arguments.Add(ReplaceVariables(temp, currentDateTime));
                ignoreErrors.Add((string)command["ignoreErrors"] == "True");
            }
        }

        private string ReplaceVariables(string temp, DateTime now)
        {
            temp = temp.Replace("%OUTPUTFILE%", filename);
            temp = temp.Replace("%OUTPUTDIR%", outputDir);
            temp = temp.Replace("%OUTPUTFILENOEXT%", filenameNoExt);
            temp = temp.Replace("%INPUTDIR%", inputDir);
            temp = temp.Replace("%DATE%", now.ToString("yyyyMMdd"));
            temp = temp.Replace("%DATETIME%", now.ToString("yyyyMMddHHmm"));
            return temp;
        }

        public void RunCommands()
        {
            for (int i = 0; i < executables.Count; i++)
            {
                var stdOutBuffer = new StringBuilder();
                var stdErrBuffer = new StringBuilder();
                CommandResultValidation validation = (ignoreErrors[i] ? CommandResultValidation.None : CommandResultValidation.ZeroExitCode);
                try
                {
                    var result = Cli.Wrap(executables[i])
                        .WithArguments(arguments[i])
                        .WithWorkingDirectory(workingDirectories[i])
                        .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                        .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                        .WithValidation(validation);
                    result.ExecuteAsync().Task.Wait();
                }
                catch(AggregateException ex) 
                {
                    logger.Error($"{ex.Message}\n");
                    if (stdOutBuffer.Length > 0)
                    {
                        logger.Error("Standard command output:");
                        logger.Error(stdOutBuffer.ToString());
                    }
                    if (stdErrBuffer.Length > 0)
                    {
                        logger.Error("Standard error command output:");
                        logger.Error(stdErrBuffer.ToString());
                    }
                    throw new Exception("Command execution failed. Aborting...");
                }
            }
        }
    }
}
