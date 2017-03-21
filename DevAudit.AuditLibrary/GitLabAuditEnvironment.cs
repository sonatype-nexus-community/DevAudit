using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using NGitLab;
using NGitLab.Models;

namespace DevAudit.AuditLibrary
{
    public class GitLabAuditEnvironment : AuditEnvironment
    {
        public GitLabAuditEnvironment(EventHandler<EnvironmentEventArgs> message_handler, string api_token, string host_url, string project_name, string repository_branch, LocalEnvironment host_environment) 
            : base(message_handler, new OperatingSystem(PlatformID.Unix, new Version(0, 0)), host_environment)
        {
            try
            {
                GitLabClient = GitLabClient.Connect(host_url, api_token);
                Project = GitLabClient.Projects.All.Where(p => p.PathWithNamespace == project_name).FirstOrDefault();
                Repository = GitLabClient.GetRepository(Project.Id);
            }

            catch (AggregateException ae)
            {
                host_environment.Error(ae, "Error getting repository for project {0} from {1}.", project_name, host_url);
                RepositoryInitialised = false;
                return;
            }
            catch (Exception e)
            {
                host_environment.Error(e, "Error getting repository for project {0} from {1}.", project_name, host_url);
                RepositoryInitialised = false;
                return;
            }
            try
            {
                RepositoryBranch = Repository.Branches.All.Where(b => b.Name == repository_branch).FirstOrDefault();
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
            throw new NotImplementedException();
        }

        public override bool DirectoryExists(string dir_path)
        {
            throw new NotImplementedException();
        }

        public override AuditFileInfo ConstructFile(string file_path)
        {
            throw new NotImplementedException();
            //return new GitHubAuditFileInfo(this, file_path);
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
        public GitLabClient GitLabClient { get; set; }
        public Project Project { get; protected set; }
        public IRepositoryClient Repository { get; protected set; }
        public bool RepositoryInitialised { get; protected set; } = false;
        public string RepositoryOwner { get; protected set; }
        public string RepositoryName { get; protected set; }
        public Branch RepositoryBranch { get; protected set; }
        #endregion

        #region Methods
        public TreeOrBlob GetTree(string path)
        {
            string[] components = this.GetPathComponents(path);
            bool ancestors_exist = false;
            TreeOrBlob parent = null;
            try
            {
                for (int i = 0; i < components.Length - 1; i++)
                {
                    parent = Repository.Tree.Where(t => t.Name == components[i] && t.Type == ObjectType.tree).FirstOrDefault();
                    if (parent == null)
                    {
                        ancestors_exist = false;
                    }
                }
            }
            catch (Exception)
            {
                ancestors_exist = false;
            }
            if (!ancestors_exist)
            {
                return null;
            }
            else
            {
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

        protected string[] GetPathComponents(string path)
        {
            return path.Split(this.PathSeparator.ToArray()).ToArray();
        }
        #endregion
    }
}
