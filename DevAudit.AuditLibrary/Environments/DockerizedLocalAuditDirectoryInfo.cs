using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Alpheus.IO;

namespace DevAudit.AuditLibrary
{
    public class DockerizedLocalAuditDirectoryInfo : AuditDirectoryInfo
    {
        #region Constructors
        public DockerizedLocalAuditDirectoryInfo(DockerizedLocalEnvironment env, string path) : base(env, path)
        {
            this.DockerizedLocalEnvironment = env;
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
                return new DockerizedLocalAuditDirectoryInfo(this.DockerizedLocalEnvironment, components[components.Length - 1]);
            }
        }

        public override IDirectoryInfo Root
        {
            get
            {
                string[] components = this.GetPathComponents();
                return new DockerizedLocalAuditDirectoryInfo(this.DockerizedLocalEnvironment, components[0]);

            }
        }

        public override bool Exists
        {
            get
            {
                if (!this._Exists.HasValue)
                {
                    string o = this.EnvironmentExecute("stat", this.FullName);
                    if (!o.Contains("no such file or directory") && (o.Contains("directory")))
                    {
                        this._Exists = true;
                    }
                    else
                    {
                        this._Exists = false;
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
                DockerizedLocalAuditDirectoryInfo[] dirs = o.Split("\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(dn => new DockerizedLocalAuditDirectoryInfo(this.DockerizedLocalEnvironment, dn)).ToArray();
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
                DockerizedLocalAuditDirectoryInfo[] dirs = o.Split("\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(dn => new DockerizedLocalAuditDirectoryInfo(this.DockerizedLocalEnvironment, dn)).ToArray();
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
                DockerizedLocalAuditFileInfo[] files = o.Split("\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(fn => new DockerizedLocalAuditFileInfo(this.DockerizedLocalEnvironment, fn)).ToArray();
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
                DockerizedLocalAuditFileInfo[] files = o.Split("\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(fn => new DockerizedLocalAuditFileInfo(this.DockerizedLocalEnvironment, fn)).ToArray();
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
            return this.DockerizedLocalEnvironment.ReadFilesAsText(files.ToList());
        }

        public override async Task<Dictionary<AuditFileInfo, string>> ReadFilesAsTextAsync(IEnumerable<AuditFileInfo> files)
        {
            return await Task.Run(() => this.DockerizedLocalEnvironment.ReadFilesAsText(files.ToList()));
        }

        public override Dictionary<AuditFileInfo, string> ReadFilesAsText(string searchPattern)
        {
            return this.DockerizedLocalEnvironment.ReadFilesAsText(
                this.GetFiles(searchPattern).Select(f => f as AuditFileInfo).ToList());
        }

        public override async Task<Dictionary<AuditFileInfo, string>> ReadFilesAsTextAsync(string searchPattern)
        {
            return await Task.Run(() => this.DockerizedLocalEnvironment.ReadFilesAsText(this.GetFiles(searchPattern).Select(f => f as AuditFileInfo).ToList()));
        }

        public override LocalAuditDirectoryInfo GetAsLocalDirectory()
        {
            if (this.DockerizedLocalEnvironment.HostRootIsMounted)
            {
                return new LocalAuditDirectoryInfo(new DirectoryInfo(Path.Combine("/hostroot", this.FullName)));
            }
            else
            {
                return new LocalAuditDirectoryInfo(new DirectoryInfo(this.FullName));
            }
        }

        public override Task<LocalAuditDirectoryInfo> GetAsLocalDirectoryAsync()
        {
            LocalAuditDirectoryInfo d = null;
            if (this.DockerizedLocalEnvironment.HostRootIsMounted)
            {
                d =  new LocalAuditDirectoryInfo(new DirectoryInfo(Path.Combine("/hostroot", this.FullName)));
            }
            else
            {
                d = new LocalAuditDirectoryInfo(new DirectoryInfo(this.FullName));
            }
            return Task.FromResult(d);
        }
        #endregion

        #region Properties
        protected DockerizedLocalEnvironment DockerizedLocalEnvironment { get; set; }
        #endregion

        #region Fields
        private bool? _Exists;
        #endregion

    }
}