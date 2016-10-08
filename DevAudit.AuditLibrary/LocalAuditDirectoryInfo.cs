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
            DirectoryInfo[] dirs = this.directory.GetDirectories(searchPattern);
            return dirs != null ? dirs.Select(d => new LocalDirectoryInfo(d)).ToArray() : null;

        }

        public override IDirectoryInfo[] GetDirectories(string searchPattern, SearchOption searchOption)
        {
            DirectoryInfo[] dirs = this.directory.GetDirectories(searchPattern, searchOption);
            return dirs != null ? dirs.Select(d => new LocalDirectoryInfo(d)).ToArray() : null;
        }

        public override IFileInfo[] GetFiles()
        {
            FileInfo[] files = this.directory.GetFiles();
            return files != null ? files.Select(f => new LocalFileInfo(f)).ToArray() : null;
        }

        public override IFileInfo[] GetFiles(string searchPattern)
        {
            FileInfo[] files = this.directory.GetFiles(searchPattern);
            return files != null ? files.Select(f => new LocalFileInfo(f)).ToArray() : null;
        }

        public override IFileInfo[] GetFiles(string searchPattern, SearchOption searchOption)
        {
            FileInfo[] files = this.directory.GetFiles(searchPattern, searchOption);
            return files != null ? files.Select(f => new LocalFileInfo(f)).ToArray() : null;
        }

        public override Dictionary<AuditFileInfo, string> ReadFilesAsText(IEnumerable<AuditFileInfo> files)
        {
            return this.AuditEnvironment.ReadFilesAsText(files.ToList());
        }

        public override Dictionary<AuditFileInfo, string> ReadFilesAsText(string searchPattern)
        {
            return this.AuditEnvironment.ReadFilesAsText(
             this.GetFiles(searchPattern).Select(f => f as AuditFileInfo).ToList());
        }

        public override Dictionary<AuditFileInfo, LocalAuditFileInfo> GetAsLocalDirectory()
        {
            throw new NotSupportedException();
        }
        #endregion

        #region Constructors
        public LocalAuditDirectoryInfo(LocalEnvironment env, string dir_path) : base(env, dir_path)
        {
            this.directory = new DirectoryInfo(dir_path);
        }

        public LocalAuditDirectoryInfo(DirectoryInfo dir) : base(new LocalEnvironment(), dir?.FullName)
        {
            this.directory = dir;
        }
        #endregion

        #region Private fields
        private DirectoryInfo directory;
        #endregion
    }
}
