using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Alpheus.IO;
namespace DevAudit.AuditLibrary
{
    public class LocalAuditDirectoryInfo : AuditDirectoryInfo
    {
        #region Overriden properties
        public override string Name { get; protected set; }
        public override string FullName { get; protected set; }
        public override IDirectoryInfo Parent
        {
            get
            {
                return new LocalDirectoryInfo(this.directory.Parent);
            }
        }

        public override IDirectoryInfo Root
        {
            get
            {
                return new LocalDirectoryInfo(this.directory.Root);
            }
        }

        public override bool Exists
        {
            get
            {
                return this.directory.Exists;
            }
        }
        #endregion

        #region Overriden methods
        public override IDirectoryInfo[] GetDirectories()
        {
            DirectoryInfo[] dirs = this.directory.GetDirectories();
            return dirs != null ? dirs.Select(d => new LocalDirectoryInfo(d)).ToArray() : null;
        }

        public override IDirectoryInfo[] GetDirectories(string searchPattern)
        {
            try
            {
                return this.directory.GetDirectories(searchPattern)?.Select(d => new LocalDirectoryInfo(d)).ToArray();
            }
            catch (System.IO.DirectoryNotFoundException)
            {
                return null;
            }
        }

        public override IDirectoryInfo[] GetDirectories(string searchPattern, SearchOption searchOption)
        {
            DirectoryInfo[] dirs = this.directory.GetDirectories(searchPattern, searchOption);
            return dirs != null ? dirs.Select(d => new LocalDirectoryInfo(d)).ToArray() : null;
        }

        public override IFileInfo[] GetFiles()
        {
            FileInfo[] files = this.directory.GetFiles("*",SearchOption.AllDirectories);
            return files != null ? files.Select(f => this.AuditEnvironment.ConstructFile(f.FullName)).ToArray() : null;
        }

        public override IFileInfo[] GetFiles(string searchPattern)
        {
            FileInfo[] files = this.directory.GetFiles(searchPattern,SearchOption.AllDirectories);
            return files != null && files.Count() > 0 ? files.Select(f => this.AuditEnvironment.ConstructFile(f.FullName)).ToArray() : null;
        }

        public override IFileInfo[] GetFiles(string searchPattern, SearchOption searchOption)
        {
            FileInfo[] files = this.directory.GetFiles(searchPattern, searchOption);
            return files != null && files.Count() > 0 ? files.Select(f => this.AuditEnvironment.ConstructFile(f.FullName)).ToArray() : null;
        }

        public override Dictionary<AuditFileInfo, string> ReadFilesAsText(IEnumerable<AuditFileInfo> files)
        {
            return this.AuditEnvironment.ReadFilesAsText(files.ToList());
        }

        public override Task<Dictionary<AuditFileInfo, string>> ReadFilesAsTextAsync(IEnumerable<AuditFileInfo> files)
        {
            throw new NotImplementedException();
        }

        public override Dictionary<AuditFileInfo, string> ReadFilesAsText(string searchPattern)
        {
            return this.AuditEnvironment.ReadFilesAsText(
             this.GetFiles(searchPattern).Select(f => f as AuditFileInfo).ToList());
        }

        public override Task<Dictionary<AuditFileInfo, string>> ReadFilesAsTextAsync(string searchPattern)
        {
            throw new NotImplementedException();
        }

        public override LocalAuditDirectoryInfo GetAsLocalDirectory()
        {
            return this;
        }

        public override Task<LocalAuditDirectoryInfo> GetAsLocalDirectoryAsync()
        {
            return Task.FromResult(this);
        }

        public override AuditFileInfo GetFile(string file_path)
        {
            throw new NotSupportedException();
        }

        public DirectoryInfo GetAsSysDirectoryInfo()
        {
            return this.directory;
        }
        #endregion

        #region Constructors
        public LocalAuditDirectoryInfo(LocalEnvironment env, string dir_path) : base(env, dir_path)
        {
            if (string.IsNullOrEmpty(dir_path))
            {
                throw new ArgumentException("Value cannot be null.", "dir_path");
            }
            this.directory = new DirectoryInfo(dir_path);
            this.Name = this.directory.Name;
            this.FullName = this.directory.FullName;
        }

        public LocalAuditDirectoryInfo(DirectoryInfo dir) : this(new LocalEnvironment(), dir?.FullName)
        {
        }
        #endregion

        #region Private fields
        private DirectoryInfo directory;
        #endregion
    }
}
