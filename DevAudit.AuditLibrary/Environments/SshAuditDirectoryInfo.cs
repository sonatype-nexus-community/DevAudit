using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Alpheus.IO;

namespace DevAudit.AuditLibrary
{
    public class SshAuditDirectoryInfo : AuditDirectoryInfo
    {
        #region Constructors
        public SshAuditDirectoryInfo(SshAuditEnvironment env, string path) : base(env, path)
        {
            this.SshAuditEnvironment = env;
            string[] components = this.GetPathComponents();
            Name = components.Last();
        }
        #endregion

        #region Overriden properties
        public override string FullName { get; protected set; }

        public override string Name { get; protected set; }

        public override IDirectoryInfo Parent
        {
            get
            {
                string[] components = this.GetPathComponents();
                return new SshAuditDirectoryInfo(this.SshAuditEnvironment, components[components.Length - 1]);
            }
        }

        public override IDirectoryInfo Root
        {
            get
            {
                string[] components = this.GetPathComponents();
                return new SshAuditDirectoryInfo(this.SshAuditEnvironment, components[0]);

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
                SshAuditDirectoryInfo[] dirs = o.Split("\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(dn => new SshAuditDirectoryInfo(this.SshAuditEnvironment, dn)).ToArray();
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
                SshAuditDirectoryInfo[] dirs = o.Split("\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(dn => new SshAuditDirectoryInfo(this.SshAuditEnvironment, dn)).ToArray();
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
                SshAuditFileInfo[] files = o.Split("\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(fn => new SshAuditFileInfo(this.SshAuditEnvironment, fn)).ToArray();
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
                SshAuditFileInfo[] files = o.Split("\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(fn => new SshAuditFileInfo(this.SshAuditEnvironment, fn)).ToArray();
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
            return this.SshAuditEnvironment.ReadFilesAsText(files.ToList());
        }

        public override async Task<Dictionary<AuditFileInfo, string>> ReadFilesAsTextAsync(IEnumerable<AuditFileInfo> files)
        {
            return await Task.Run(() => this.SshAuditEnvironment.ReadFilesAsText(files.ToList()));
        }

        public override Dictionary<AuditFileInfo, string> ReadFilesAsText(string searchPattern)
        {
            return this.SshAuditEnvironment.ReadFilesAsText(
                this.GetFiles(searchPattern).Select(f => f as AuditFileInfo).ToList());
        }

        public override async Task<Dictionary<AuditFileInfo, string>> ReadFilesAsTextAsync(string searchPattern)
        {
            return await Task.Run(() => this.SshAuditEnvironment.ReadFilesAsText(this.GetFiles(searchPattern).Select(f => f as AuditFileInfo).ToList()));
        }

        public override LocalAuditDirectoryInfo GetAsLocalDirectory()
        {
            DirectoryInfo d = this.SshAuditEnvironment.GetDirectoryAsLocal(this.FullName, Path.Combine(this.AuditEnvironment.WorkDirectory.FullName, this.Name));
            if (d != null)
            {
                return new LocalAuditDirectoryInfo(d);
            }
            else return null;
        }

        public override async Task<LocalAuditDirectoryInfo> GetAsLocalDirectoryAsync()
        {
            DirectoryInfo d = await Task.Run(() => this.SshAuditEnvironment.GetDirectoryAsLocal(this.FullName, Path.Combine(this.AuditEnvironment.WorkDirectory.FullName, this.Name)));
            if (d != null)
            {
                return new LocalAuditDirectoryInfo(d);
            }
            else return null;
        }

        #endregion

        #region Properties
        protected SshAuditEnvironment SshAuditEnvironment { get; set; }
        #endregion

        #region Fields
        private bool? _Exists;
        #endregion
    }
}
