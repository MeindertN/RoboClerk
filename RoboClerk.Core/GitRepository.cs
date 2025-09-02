using System;
using System.Text;
using CliWrap;
using RoboClerk.Core.Configuration;

namespace RoboClerk
{
    public class GitRepository
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private string projectRoot = string.Empty;

        public GitRepository(IConfiguration config) 
        {
            projectRoot = config.ProjectRoot;
            var result = RunGitCommand("--version");
            if(!result.Contains("git version"))
            {
                throw new Exception("Git support enabled in RoboClerk but Git executable not found in path. Please ensure Git command is in your path or disable Git support in RoboClerk.");
            }
            logger.Info($"Git support enabled and Git executable found in path: {result}");
            RunGitCommand($"config --global --add safe.directory {config.ProjectRoot}"); //needed to ensure we can apply git on files we don't own
        }

        public bool GetFileLocallyUpdated(string file)
        {
            var status = RunGitCommand($"status {file}");
            return status.Contains("modified:");
        }

        public string GetFileVersion(string file)
        {
            var commitSHA = RunGitCommand($"log -n 1 --pretty=format:%H -- {file}");
            if (!commitSHA.Contains("fatal:") && commitSHA.Length!=0)
            {
                return commitSHA.Substring(0, 7);
            }
            else
            {
                logger.Error($"GitRepoInformation: Requested file version not available, file {file} is not checked into git.");
                throw new Exception($"Error occurred when trying to retrieve the versioning information for {file}. File appears not checked into git.");
            }
        }

        public DateTime GetFileLastUpdated(string file)
        {
            var dateTime = RunGitCommand($"log -n 1 --format=\"%ai\" -- {file}");
            if (!dateTime.Contains("fatal:") && dateTime.Length!=0)
            {
                DateTime output;
                if(DateTime.TryParse(dateTime, out output))
                {
                    return output;
                }
                else
                {
                    throw new Exception($"Error trying to get DateTime for file \"{file}\", Git command output \"{dateTime}\".");
                }
            }
            throw new Exception($"Error trying to get DateTime for file \"{file}\", Git command output \"{dateTime}\".");
        }

        private string RunGitCommand(string arguments) 
        {
            var stdOutBuffer = new StringBuilder();
            var stdErrBuffer = new StringBuilder();
            CommandResultValidation validation = (CommandResultValidation.None);
            try
            {
                var cmd = Cli.Wrap("git")
                    .WithArguments(arguments)
                    .WithWorkingDirectory(projectRoot)
                    .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                    .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                    .WithValidation(validation);
                cmd.ExecuteAsync().Task.Wait();
                if (stdErrBuffer.Length > 0)
                    return stdErrBuffer.ToString();
                else
                    return stdOutBuffer.ToString();
            }
            catch (Exception ex)
            {
                logger.Error($"{ex.Message}\n");
                if (stdOutBuffer.Length > 0)
                {
                    logger.Error("Standard git command output:");
                    logger.Error(stdOutBuffer.ToString());
                }
                if (stdErrBuffer.Length > 0)
                {
                    logger.Error("Standard git error command output:");
                    logger.Error(stdErrBuffer.ToString());
                }
                throw new Exception("Git command execution failed. Ensure git is in your path. Aborting...");
            }
        }
    }
}
