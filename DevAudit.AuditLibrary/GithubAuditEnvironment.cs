using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Octokit;

namespace DevAudit.AuditLibrary
{
    public class GitHubAuditEnvironment : AuditEnvironment
    {
        public GitHubAuditEnvironment(EventHandler<EnvironmentEventArgs> message_handler, string user_api_token, string repository_owner, string repository_name, string repository_branch, LocalEnvironment host_environment) 
            : base(message_handler, new OperatingSystem(PlatformID.Unix, new Version(0, 0)), host_environment)
        {
            GitHubClient = new GitHubClient(new ProductHeaderValue("DevAudit"));
            if (!string.IsNullOrEmpty(user_api_token))
            {
                GitHubClient.Credentials = new Credentials(user_api_token);
            }
            try
            {
                Repository = GitHubClient.Repository.Get(repository_owner, repository_name).Result;

            }
            catch (AggregateException ae)
            {
                host_environment.Error(ae, "Error getting repository {0}/{1}.", repository_owner, repository_name);
                RepositoryInitialised = false;
                return;
            }
            catch (Exception e)
            {
                host_environment.Error(e, "Error getting repository {0}/{1}.", repository_owner, repository_name);
                RepositoryInitialised = false;
                return;
            }
            try
            {
                RepositoryBranch = GitHubClient.Repository.Branch.Get(repository_owner, repository_name, repository_branch).Result;
            }
            catch (AggregateException ae)
            {
                host_environment.Error(ae, "Error getting branch {0}.", repository_branch);
                RepositoryInitialised = false;
                return;
            }
            catch (Exception e)
            {
                host_environment.Error(e, "Error getting branch {0}.", repository_branch);
                RepositoryInitialised = false;
                return;
            }
            RepositoryName = repository_name;
            RepositoryOwner = repository_owner;
            this.RepositoryInitialised = true;
        }

        #region Overriden properties
        protected override TraceSource TraceSource { get; set; } = new TraceSource("SshAuditEnvironment");
        #endregion

        #region Overriden methods
        public override bool Execute(string command, string arguments, out ProcessExecuteStatus process_status, out string process_output, out string process_error, Action<string> OutputDataReceived = null, Action<string> OutputErrorReceived = null, [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
        {
            throw new NotImplementedException();
        }

        public override bool FileExists(string file_path)
        {
            CallerInformation here = this.Here();
            if (file_path.StartsWith(this.PathSeparator)) file_path = file_path.Remove(0, 1);
            IReadOnlyList<RepositoryContent> f = this.GetContent(file_path);
            if (f == null || f.Count == 0)
            {
                return false;
            }
            else if (f != null && f.First().Path == file_path)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool DirectoryExists(string dir_path)
        {
            CallerInformation here = this.Here();
            IReadOnlyList<RepositoryContent> f = this.GetContent(dir_path);
            if (f == null || f.Count == 0)
            {
                return false;
            }
            else if (f != null && f.First().Path == dir_path)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override AuditFileInfo ConstructFile(string file_path)
        {
            return new GitHubAuditFileInfo(this, file_path);
        }

        public override AuditDirectoryInfo ConstructDirectory(string dir_path)
        {
            throw new NotImplementedException();
        }

        public override Dictionary<AuditFileInfo, string> ReadFilesAsText(List<AuditFileInfo> files)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Properties
        public GitHubClient GitHubClient { get; set; }
        public Repository Repository { get; protected set; }
        public bool RepositoryInitialised { get; protected set; } = false;
        public string RepositoryOwner { get; protected set; }
        public string RepositoryName { get; protected set; }
        public Branch RepositoryBranch { get; protected set; }
        #endregion

        #region Methods
        public IReadOnlyList<RepositoryContent> GetContent(string path)
        {
            CallerInformation here = this.Here();
            if (this.GitHubClient == null) throw new InvalidOperationException("The GitHub client is not initialized.");
            try
            {
                IReadOnlyList<RepositoryContent> c = GitHubClient.Repository.Content
                    .GetAllContentsByRef(this.RepositoryOwner, this.RepositoryName, path, RepositoryBranch.Name).Result;
                return c;
               
            }
            catch (AggregateException ae)
            {
                Error(here, ae, "Error getting path {0} from GitHub repository {1}/{2}.", path, RepositoryOwner, RepositoryName);
                return null;
            }
            catch (Exception e)
            {
                Error(here, e, "Error getting path {0} from GitHub repository {1}/{2}.", path, RepositoryOwner, RepositoryName);
                return null;
            }
        }

        public FileInfo GetFileAsLocal(string remote_path, string local_path)
        {
            throw new NotImplementedException();
        }

        public DirectoryInfo GetDirectoryAsLocal(string remote_path, string local_path)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
