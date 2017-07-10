using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Alpheus.IO;

namespace DevAudit.AuditLibrary
{
    public class SshDockerAuditDirectoryInfo : AuditDirectoryInfo
    {
        #region Constructors
        public SshDockerAuditDirectoryInfo(SshDockerAuditEnvironment env, string path) : base(env, path)
        {
            this.DockerAuditEnvironment = env;
        }
        #endregion

        #region Overriden properties
        public override string FullName { get; protected set; }

        public override string Name { get; protected set; }

        public override IDirectoryInfo Parent
        {
            get
            {
                if (this._Parent == null)
                {
                    string[] components = this.GetPathComponents();
                    string fn = components.Length > 1 ? components.Take(components.Length - 1).Aggregate((c1, c2) => c1 + this.DockerAuditEnvironment.PathSeparator + c2) : 
                        this.DockerAuditEnvironment.PathSeparator;
                    AuditDirectoryInfo d = this.DockerAuditEnvironment.ConstructDirectory(fn);
                    if (d.Exists)
                    {
                        this._Parent = d;
                    }

                    else
                    {
                        this.AuditEnvironment.Error(this.AuditEnvironment.Here(), "Could not get parent directory for file {0}.", this.FullName);
                    }
                }
                return this._Parent;
            }
        }
       
        public override IDirectoryInfo Root
        {
            get
            {
                //string[] components = this.GetPathComponents();
                return new SshDockerAuditDirectoryInfo(this.DockerAuditEnvironment, this.DockerAuditEnvironment.PathSeparator);
            }
        }

        public override bool Exists
        {
            get
            {
                if (!this._Exists.HasValue)
                {
                    string o = this.EnvironmentExecute(string.Format("[ -d {0} ] && echo \"Yes\" || echo \"No\"", this.FullName), "");
                    if (!string.IsNullOrEmpty(o) && o == "Yes")
                    {
                        this._Exists =  true;
                    }
                    else if (!string.IsNullOrEmpty(o) && o == "No")
                    {
                        this._Exists =  false;
                    }
                    else
                    {
                        EnvironmentCommandError(this.AuditEnvironment.Here(), "Could not test for existence of {0}. Command returned: {1}.", this.FullName, o);
                        return false;
                    }
                }
                return this._Exists.Value;
            }
        }
        #endregion

        #region Overriden methods
        public override IDirectoryInfo[] GetDirectories()
        {
            string o = this.EnvironmentExecute("find", string.Format("{0} -type d -name \"*\"", this.FullName));
            if (!string.IsNullOrEmpty(o))
            {
                SshDockerAuditDirectoryInfo[] dirs = o.Split("\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(dn => new SshDockerAuditDirectoryInfo(this.DockerAuditEnvironment, dn)).ToArray();
                return dirs;
            }
            else
            {
                EnvironmentCommandError(this.AuditEnvironment.Here(), "Could not get directories in {0} for path {1}.", this.FullName, this.FullName);
                return null;
            }
        }

        public override IDirectoryInfo[] GetDirectories(string path)
        {
            string o = this.EnvironmentExecute("find", string.Format("{0} -type d -name \"*\"", this.CombinePaths(this.FullName, path)));
            if (!string.IsNullOrEmpty(o))
            {
                SshDockerAuditDirectoryInfo[] dirs = o.Split("\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(dn => new SshDockerAuditDirectoryInfo(this.DockerAuditEnvironment, dn)).ToArray();
                return dirs;
            }
            else
            {
                this.AuditEnvironment.Warning("Could not get directories for path {0}.", this.CombinePaths(this.FullName, path));
                return null;
            }
        }

        public override IDirectoryInfo[] GetDirectories(string path, SearchOption search_option)
        {
            throw new NotImplementedException();
        }

        public override IFileInfo[] GetFiles()
        {
            string o = this.EnvironmentExecute("find", string.Format("{0} -type f -name \"*\"", this.FullName));
            if (!string.IsNullOrEmpty(o))
            {
                SshDockerAuditFileInfo[] files = o.Split("\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(fn => new SshDockerAuditFileInfo(this.DockerAuditEnvironment, fn)).ToArray();
                return files;
            }
            else
            {
                EnvironmentCommandError(this.AuditEnvironment.Here(), "Could not get files in {0}.", this.FullName);
                return null;
            }

        }

        public override IFileInfo[] GetFiles(string path)
        {
            string[] pc = path.Split(this.AuditEnvironment.PathSeparator.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            string wildcard = "*";
            string search_path;
            if (pc.Last().Contains("*"))
            {
                wildcard = pc.Last();
                search_path = pc.Length > 1 ? pc.Take(pc.Length - 1).Aggregate((s1, s2) => s1 + this.AuditEnvironment.PathSeparator + s2) : string.Empty;    
            }
            else
            {
                search_path = path;
            }

            string o = this.EnvironmentExecute("find", string.Format("{0} -type f -name \"{1}\"", this.CombinePaths(this.FullName, search_path), wildcard));
            if (!string.IsNullOrEmpty(o))
            {
                SshDockerAuditFileInfo[] files = o.Split("\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(fn => new SshDockerAuditFileInfo(this.DockerAuditEnvironment, fn)).ToArray();
                return files;
            }
            else
            {
                this.AuditEnvironment.Warning("Could not get files for path {0}.", this.CombinePaths(this.FullName, path));
                return null;
            }
        }

        public override IFileInfo[] GetFiles(string path, SearchOption search_option)
        {
            throw new NotImplementedException();
        }

        public override AuditFileInfo GetFile(string file_path)
        {
            string o = this.EnvironmentExecute("find", string.Format("{0} -type f -name \"{1}\"", this.FullName, file_path));
            if (!string.IsNullOrEmpty(o))
            {
                return this.AuditEnvironment.ConstructFile(o);
            }
            else
            {
                this.AuditEnvironment.Warning("Could not get file for path {0}.", this.CombinePaths(this.FullName, file_path));
                return null;
            }
        }

        public override Dictionary<AuditFileInfo, string> ReadFilesAsText(IEnumerable<AuditFileInfo> files)
        {
            return this.DockerAuditEnvironment.ReadFilesAsText(files.ToList());
        }

        public override async Task<Dictionary<AuditFileInfo, string>> ReadFilesAsTextAsync(IEnumerable<AuditFileInfo> files)
        {
            return await Task.Run(() => this.DockerAuditEnvironment.ReadFilesAsText(files.ToList()));
        }

        public override Dictionary<AuditFileInfo, string> ReadFilesAsText(string searchPattern)
        {
            return this.DockerAuditEnvironment.ReadFilesAsText(
                this.GetFiles(searchPattern).Select(f => f as AuditFileInfo).ToList());
        }

        public override async Task<Dictionary<AuditFileInfo, string>> ReadFilesAsTextAsync(string searchPattern)
        {
            return await Task.Run(() => this.DockerAuditEnvironment.ReadFilesAsText(this.GetFiles(searchPattern).Select(f => f as AuditFileInfo).ToList()));
        }

        public override LocalAuditDirectoryInfo GetAsLocalDirectory()
        {
            DirectoryInfo d = this.DockerAuditEnvironment.GetDirectoryAsLocal(this.FullName, Path.Combine(this.AuditEnvironment.WorkDirectory.FullName, this.Name));
            if (d != null)
            {
                return new LocalAuditDirectoryInfo(d);
            }
            else return null;
        }

        public override async Task<LocalAuditDirectoryInfo> GetAsLocalDirectoryAsync()
        {
            DirectoryInfo d = await Task.Run(() => this.DockerAuditEnvironment.GetDirectoryAsLocal(this.FullName, Path.Combine(this.AuditEnvironment.WorkDirectory.FullName, this.Name)));
            if (d != null)
            {
                return new LocalAuditDirectoryInfo(d);
            }
            else return null;
        }

        #endregion

        #region Properties
        protected SshDockerAuditEnvironment DockerAuditEnvironment { get; set; }
        #endregion


        #region Private fields
        private bool? _Exists;
        private AuditDirectoryInfo _Parent;
        #endregion
    }
}

