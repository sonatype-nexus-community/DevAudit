using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Alpheus.IO;

namespace DevAudit.AuditLibrary
{
    public class LocalAuditFileInfo : AuditFileInfo
    {
        #region Overriden properties
        public override IDirectoryInfo Directory
        {
            get
            {
                return new LocalDirectoryInfo(this.file.Directory);
            }
        }

        public override string DirectoryName
        {
            get
            {
                return this.file.DirectoryName;
            }
        }

        public override bool Exists
        {
            get
            {
                return this.file.Exists;
            }
        }

        public override long Length
        {
            get
            {
                return this.file.Length;
            }
        }

        public override bool IsReadOnly
        {
            get
            {
                return this.file.IsReadOnly;
            }
        }

        public override string FullName { get; protected set; }
      

        public override string Name { get; protected set; }
    

        public override DateTime LastWriteTimeUtc
        {
            get
            {
                return this.file.LastAccessTimeUtc;
            }
        }

        public override bool PathExists(string file_path)
        {
            return File.Exists(file_path);
        }
        #endregion

        #region Overriden methods
        public override string ReadAsText()
        {
            using (StreamReader s = new StreamReader(this.file.OpenRead()))
            {
                return s.ReadToEnd();
            }
        }

        public override byte[] ReadAsBinary()
        {
            using (FileStream s = this.file.Open(FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[this.file.Length];
                s.Read(buffer, 0, buffer.Length);
                return buffer;
            }
        }

        public override IFileInfo Create(string file_path)
        {
            return new LocalAuditFileInfo(this.LocalAuditEnvironment, file_path);
        }

        public override LocalAuditFileInfo GetAsLocalFile()
        {
            throw new NotSupportedException();
        }
        #endregion

        #region Public properties
        public FileInfo SysFile
        {
            get
            {
                return this.file;
            }
        }
        #endregion

        #region Constructors
        public LocalAuditFileInfo(LocalEnvironment env, string file_path) : base(env, file_path)
        {
            this.LocalAuditEnvironment = env;
            this.file = new FileInfo(file_path);
        }

        public LocalAuditFileInfo(LocalEnvironment env, FileInfo f) : base(env, f.FullName)
        {
            this.LocalAuditEnvironment = env;
        }
        #endregion

        #region Protected fields
        private FileInfo file;
        private LocalEnvironment LocalAuditEnvironment { get; set; }
        #endregion
    }
}
