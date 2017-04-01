using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Alpheus.IO;
using Octokit;

namespace DevAudit.AuditLibrary
{
    public class GitHubAuditFileInfo : AuditFileInfo
    {
        #region Constructors
        public GitHubAuditFileInfo(GitHubAuditEnvironment env, string file_path) : base(env, file_path)
        {
            this.GitHubAuditEnvironment = env;
            string[] components = this.GetPathComponents();
            Name = components.Last();
            if (file_path.Length > 1 && file_path.StartsWith(this.AuditEnvironment.PathSeparator)) file_path = file_path.Remove(0, 1);
            if (file_path.Length > 1 && file_path.EndsWith(this.AuditEnvironment.PathSeparator)) file_path = file_path.Remove(file_path.Length - 1);
            this.FullName = file_path;
        }

        
        #endregion

        #region Overriden properties
        public override string FullName { get; protected set; }

        public override string Name { get; protected set; }

        public override bool IsReadOnly
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public override bool Exists
        {
            get
            {
                return this.AuditEnvironment.FileExists(this.FullName);
            }
        }

        public override long Length
        {
            get
            {
                if (!_Length.HasValue)
                {
                    if (this.RepositoryFile == null)
                    {
                        _Length = 0;
                    }
                    else
                    {
                        _Length = RepositoryFile.Size;
                    }
                }
                return this._Length.Value;
            }
        }

        public override DateTime LastWriteTimeUtc
        {
            get
            {

                throw new NotImplementedException();
            }
        }

        public override IDirectoryInfo Directory
        {
            get
            {
                if (this._Directory == null)
                {
                    string[] components = this.GetPathComponents();
                    string fn = components.Length > 1 ? components[Length - 2] : this.GitHubAuditEnvironment.PathSeparator;
                    AuditDirectoryInfo d = this.GitHubAuditEnvironment.ConstructDirectory(fn);
                    if (d.Exists)
                    {
                        this._Directory = d;
                    }

                    else
                    {
                        EnvironmentCommandError(this.AuditEnvironment.Here(), "Could not get parent directory for file {0}.", this.FullName);
                    }
                }
                return this._Directory;
            }
        }

        public override string DirectoryName
        {
            get
            {
                if (this.Directory != null)
                {
                    return this.Directory.Name;
                }
                else
                {
                    EnvironmentCommandError(this.AuditEnvironment.Here(), "Could not get directory name for {0}.", this.FullName);
                    return string.Empty;
                }
            }
        }
        #endregion

        #region Overriden methods
        public override bool PathExists(string file_path)
        {
            IReadOnlyList<RepositoryContent> c = this.GitHubAuditEnvironment.GetContent(file_path);
            if (c == null || c.Count == 0)
            {
                return false;
            }
            else
            {
                return c.Any(content => content.Type == ContentType.Dir || content.Type == ContentType.File);
            }
        }

        public override string ReadAsText()
        {
            return RepositoryFile.Content;
        }

        public override byte[] ReadAsBinary()
        {
            throw new NotImplementedException();
        }

        public override LocalAuditFileInfo GetAsLocalFile()
        {
            throw new NotImplementedException();
        }

        public override async Task<LocalAuditFileInfo> GetAsLocalFileAsync()
        {
            return await Task.Run(() => this.GetAsLocalFile());
        }
        #endregion

        #region Properties
        public RepositoryContent RepositoryFile
        {
            get
            {
                if (this._RepositoryFile == null && !this.repository_file_polled)
                {
                    IReadOnlyList<RepositoryContent> c = this.GitHubAuditEnvironment.GetContent(this.FullName);
                    this.repository_file_polled = true;
                    if (c == null || c.Count == 0)
                    {
                        this._RepositoryFile = null;
                    }
                    else if (c.First().Type != ContentType.File)
                    {
                        this.AuditEnvironment.Warning("Repository path {0} is not a file but a {1}.", c.First().Path, c.First().Type);
                        this._RepositoryFile = null;
                    }
                    else
                    {
                        this._RepositoryFile = c.First();
                    }
                }
                return this._RepositoryFile;
            }
        }

        protected GitHubAuditEnvironment GitHubAuditEnvironment { get; set; }
        #endregion

        #region Fields
        private RepositoryContent _RepositoryFile;
        private long? _Length;
        private IDirectoryInfo _Directory;
        bool repository_file_polled = false;
        #endregion
    }
}
