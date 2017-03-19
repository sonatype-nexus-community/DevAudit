using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Alpheus.IO;
using Octokit;

namespace DevAudit.AuditLibrary
{
    public class GitHubAuditDirectoryInfo : AuditDirectoryInfo
    {
        #region Constructors
        public GitHubAuditDirectoryInfo(GitHubAuditEnvironment env, string path) : base(env, path)
        {
            this.GitHubAuditEnvironment = env;
            string[] components = this.GetPathComponents();
            Name = components.Last();
            if (path.Length > 1 && path.EndsWith(this.AuditEnvironment.PathSeparator)) path = path.Remove(path.Length - 1);
            this.FullName = path;
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
                return new GitHubAuditDirectoryInfo(this.GitHubAuditEnvironment, components[components.Length - 1]);
            }
        }

        public override IDirectoryInfo Root
        {
            get
            {
                string[] components = this.GetPathComponents();
                return new GitHubAuditDirectoryInfo(this.GitHubAuditEnvironment, components[0]);

            }
        }

        public override bool Exists
        {
            get
            {
                if (!this._Exists.HasValue)
                {
                    this._Exists = this.GitHubAuditEnvironment.DirectoryExists(this.FullName);
                }
                return this._Exists.Value;
            }
        }
        #endregion

        #region Overriden methods
        public override IDirectoryInfo[] GetDirectories()
        {
            CallerInformation here = this.AuditEnvironment.Here();
            IReadOnlyList<RepositoryContent> c = this.GitHubAuditEnvironment.GetContent(this.FullName);
            if (c == null)
            {
                this.AuditEnvironment.Warning("Could not get directories for path {0}.", this.FullName);
                return null;
            }
            else if (c.Count == 0 || c.Where(content => content.Type == ContentType.Dir).Count() == 0)
            {
                return null;
            }
            else
            {

                return c.Where(content => content.Type == ContentType.Dir).Select(content => this.GitHubAuditEnvironment.ConstructDirectory(content.Path)).ToArray();
            }
        }

        public override IDirectoryInfo[] GetDirectories(string path)
        {
            CallerInformation here = this.AuditEnvironment.Here();
            if (path.StartsWith(this.AuditEnvironment.PathSeparator)) path = path.Remove(0, 1);
            IReadOnlyList<RepositoryContent> c = this.GitHubAuditEnvironment.GetContent(this.FullName + this.AuditEnvironment.PathSeparator + path);
            if (c == null)
            {
                this.AuditEnvironment.Warning("Could not get directories for path {0}.", this.FullName);
                return null;
            }
            else if (c.Count == 0 || c.Where(content => content.Type == ContentType.Dir).Count() == 0)
            {
                return null;
            }
            else
            {

                return c.Where(content => content.Type == ContentType.Dir).Select(content => this.GitHubAuditEnvironment.ConstructDirectory(content.Path)).ToArray();
            }
        }

        public override IDirectoryInfo[] GetDirectories(string path, SearchOption search_option)
        {
            throw new NotImplementedException();
        }

        public override IFileInfo[] GetFiles()
        {
            CallerInformation here = this.AuditEnvironment.Here();
            IReadOnlyList<RepositoryContent> c = this.GitHubAuditEnvironment.GetContent(this.FullName);
            if (c == null)
            {
                this.AuditEnvironment.Warning("Could not get directories for path {0}.", this.FullName);
                return null;
            }
            else if (c.Count == 0 || c.Where(content => content.Type == ContentType.Dir).Count() == 0)
            {
                return null;
            }
            else
            {

                return c.Where(content => content.Type == ContentType.File).Select(content => this.GitHubAuditEnvironment.ConstructFile(content.Path)).ToArray();
            }

        }

        public override IFileInfo[] GetFiles(string path)
        {
            CallerInformation here = this.AuditEnvironment.Here();
            if (path.StartsWith(this.AuditEnvironment.PathSeparator)) path = path.Remove(0, 1);
            IReadOnlyList<RepositoryContent> c = this.GitHubAuditEnvironment.GetContent(this.FullName + this.AuditEnvironment.PathSeparator + path);
            if (c == null)
            {
                this.AuditEnvironment.Warning("Could not get directories for path {0}.", this.FullName);
                return null;
            }
            else if (c.Count == 0 || c.Where(content => content.Type == ContentType.Dir).Count() == 0)
            {
                return null;
            }
            else
            {

                return c.Where(content => content.Type == ContentType.File).Select(content => this.GitHubAuditEnvironment.ConstructFile(content.Path)).ToArray();
            }
        }

        public override IFileInfo[] GetFiles(string path, SearchOption search_option)
        {
            throw new NotImplementedException();
        }

        public override AuditFileInfo GetFile(string file_path)
        {
            IReadOnlyList<RepositoryContent> c = this.GitHubAuditEnvironment.GetContent(file_path);
            if (c == null)
            {
                this.AuditEnvironment.Warning("Could not get directories for path {0}.", this.FullName);
                return null;
            }
            else if (c.Count == 0 || c.First().Type != ContentType.File)
            {
                return null;
            }
            else
            {

                return this.GitHubAuditEnvironment.ConstructFile(c.First().Path);
            }
        }

        public override Dictionary<AuditFileInfo, string> ReadFilesAsText(IEnumerable<AuditFileInfo> files)
        {
            return this.GitHubAuditEnvironment.ReadFilesAsText(files.ToList());
        }

        public override async Task<Dictionary<AuditFileInfo, string>> ReadFilesAsTextAsync(IEnumerable<AuditFileInfo> files)
        {
            return await Task.Run(() => this.GitHubAuditEnvironment.ReadFilesAsText(files.ToList()));
        }

        public override Dictionary<AuditFileInfo, string> ReadFilesAsText(string searchPattern)
        {
            return this.GitHubAuditEnvironment.ReadFilesAsText(
                this.GetFiles(searchPattern).Select(f => f as AuditFileInfo).ToList());
        }

        public override async Task<Dictionary<AuditFileInfo, string>> ReadFilesAsTextAsync(string searchPattern)
        {
            return await Task.Run(() => this.GitHubAuditEnvironment.ReadFilesAsText(this.GetFiles(searchPattern).Select(f => f as AuditFileInfo).ToList()));
        }

        public override LocalAuditDirectoryInfo GetAsLocalDirectory()
        {
            DirectoryInfo d = this.GitHubAuditEnvironment.GetDirectoryAsLocal(this.FullName, Path.Combine(this.AuditEnvironment.WorkDirectory.FullName, this.Name));
            if (d != null)
            {
                return new LocalAuditDirectoryInfo(d);
            }
            else return null;
        }

        public override async Task<LocalAuditDirectoryInfo> GetAsLocalDirectoryAsync()
        {
            DirectoryInfo d = await Task.Run(() => this.GitHubAuditEnvironment.GetDirectoryAsLocal(this.FullName, Path.Combine(this.AuditEnvironment.WorkDirectory.FullName, this.Name)));
            if (d != null)
            {
                return new LocalAuditDirectoryInfo(d);
            }
            else return null;
        }

        #endregion

        #region Properties
        public RepositoryContent RepositoryDirectory
        {
            get
            {
                if (this._RepositoryDirectory == null && !this.repository_directory_polled)
                {
                    IReadOnlyList<RepositoryContent> c = this.GitHubAuditEnvironment.GetContent(this.FullName);
                    this.repository_directory_polled = true;
                    if (c == null || c.Count == 0)
                    {
                        this._RepositoryDirectory = null;
                    }
                    else if (c.First().Type != ContentType.Dir)
                    {
                        this.AuditEnvironment.Warning("Repository path {0} is not a dir but a {1}.", c.First().Path, c.First().Type);
                        this._RepositoryDirectory = null;
                    }
                    else
                    {
                        this._RepositoryDirectory = c.First();
                    }
                }
                return this._RepositoryDirectory;
            }
        }

        protected GitHubAuditEnvironment GitHubAuditEnvironment { get; set; }
        #endregion

        #region Fields
        private bool? _Exists;
        public RepositoryContent _RepositoryDirectory;
        bool repository_directory_polled;
        #endregion
    }
}
