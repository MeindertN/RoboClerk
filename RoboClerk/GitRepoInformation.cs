using LibGit2Sharp;
using RoboClerk.Configuration;
using System;
using System.IO;
using System.Linq;

namespace RoboClerk
{
    public class GitRepoInformation : IDisposable
    {
        private IConfiguration configuration;
        private Repository repo = null;
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public GitRepoInformation(IConfiguration config)
        {
            configuration = config;
            repo = new Repository(configuration.ProjectRoot);
        }

        //note that filename here is fully qualified
        public string GetFileVersion(string filename)
        {
            FileUnderProjectRoot(filename);
            var lastCommit = GetLastCommit(filename);

            if(lastCommit != null)
            {
                return lastCommit.Sha.Substring(0, 7);
            }
            else
            {
                logger.Error($"GitRepoInformation: Requested file version not available, file {filename} is not checked into git.");
                throw new Exception($"Error occurred when trying to retrieve the versioning information for {filename}. File appears not checked into git.");
            }
        }

        public DateTime GetFileLastUpdated(string filename)
        {
            FileUnderProjectRoot(filename);
            var lastCommit = GetLastCommit(filename);

            if (lastCommit != null)
            {
                return lastCommit.Author.When.LocalDateTime;
            }
            else
            {
                logger.Error($"GitRepoInformation: Requested date and time of last modification not available, file {filename} is not checked into git.");
                throw new Exception($"Error occurred when trying to retrieve the date and time of last modification for {filename}. File appears not checked into git.");
            }
        }

        public bool GetFileLocallyUpdated(string filename)
        {
            FileUnderProjectRoot(filename);
            var mdFile = Path.GetRelativePath(configuration.ProjectRoot, filename);
            mdFile = mdFile.Replace("\\", "/");
            var filestatus = repo.RetrieveStatus(new StatusOptions { IncludeUnaltered = true });
            foreach(var status in filestatus)
            {
                if( mdFile == status.FilePath )
                {
                    if( status.State == FileStatus.Unaltered )
                    {
                        return false;
                    }
                    return true;
                }
            }
            return true;
        }

        private Commit GetLastCommit(string filename)
        {
            var mdFile = Path.GetRelativePath(configuration.ProjectRoot, filename);
            mdFile = mdFile.Replace("\\", "/");
            var filter = new Func<Commit, bool>(c =>
            {
                var cId = c.Tree[mdFile]?.Target?.Sha;
                var pId = c.Parents?.FirstOrDefault()?[mdFile]?.Target?.Sha;
                return (cId != pId);
            });

            var commits = repo.Commits.Where(filter).OrderByDescending(c => c.Author.When);
            //return the one that is most recent and only has a single parent
            foreach(var commit in commits)
            {
                if (commit.Parents?.Count() < 2)
                {
                    return commit;
                }
            }
            //could not find the commit
            return null;
        }

        private void FileUnderProjectRoot(string filename)
        {
            var file = Directory.GetFiles(configuration.ProjectRoot, Path.GetFileName(filename), SearchOption.AllDirectories)
                                .FirstOrDefault();
            if(file == null)
            {
                logger.Error($"GitRepoInformation: File {filename} is not located under the project root {configuration.ProjectRoot}");
                throw new Exception($"Unable to find file {filename} under project root {configuration.ProjectRoot}. Check if file exists and was not deleted.");
            }
        }

        public void Dispose()
        {
            if (repo != null)
            {
                repo.Dispose();
            }
        }
    }
}
