using CliWrap;
using System;
using System.Collections.Generic;
using System.Text;
using Tomlyn.Model;

namespace RoboClerk
{
    internal class Commands
    {
        private string outputdir = string.Empty;
        private string filename = string.Empty;
        private List<string> executables = new List<string>();
        private List<string> workingDirectories = new List<string>();
        private List<string> arguments = new List<string>();
        private List<bool> ignoreErrors = new List<bool>();

        public Commands(TomlTableArray commands, string outputdir, string filename)
        {
            this.outputdir = outputdir;
            this.filename = filename;
            ProcessCommands(commands);
        }

        private void ProcessCommands(TomlTableArray commands)
        {
            if (commands == null)
                return;

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
                    temp = outputdir;
                }
                workingDirectories.Add(ReplaceVariables(temp));
                temp = (string)command["arguments"];
                arguments.Add(ReplaceVariables(temp));
                ignoreErrors.Add((string)command["ignoreErrors"] == "True");
            }
        }

        private string ReplaceVariables(string temp)
        {
            temp = temp.Replace("%OUTPUTFILE%", filename);
            temp = temp.Replace("%OUTPUTDIR%", outputdir);
            return temp;
        }

        public void RunCommands()
        {
            for (int i = 0; i < executables.Count; i++)
            {
                var stdOutBuffer = new StringBuilder();
                var stdErrBuffer = new StringBuilder();
                CommandResultValidation validation = (ignoreErrors[i] ? CommandResultValidation.None : CommandResultValidation.ZeroExitCode);
                var result = Cli.Wrap(executables[i])
                    .WithArguments(arguments[i])
                    .WithWorkingDirectory(workingDirectories[i])
                    .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                    .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                    .WithValidation(validation);
                result.ExecuteAsync().Task.Wait();
            }
        }
    }
}
