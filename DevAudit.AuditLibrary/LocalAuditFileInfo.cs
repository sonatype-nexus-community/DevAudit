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
        public override string ReadAsText()
        {
            using (StreamReader s = new StreamReader(this.file.OpenRead()))
            {
                return s.ReadToEnd();
            }
        }

        public override IFileInfo Create(string file_path)
        {
            throw new NotImplementedException();
        }

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

        public override string FullName
        {
            get
            {
                return this.file.FullName;
            }

            protected set
            {
                throw new NotSupportedException();
            }
        }

        public override string Name
        {
            get
            {
                return this.file.Name;
            }

            protected set
            {
                throw new NotSupportedException();
            }
        }

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

        public LocalAuditFileInfo(LocalEnvironment env, string file_path) : base(env, file_path)
        {
            this.file = new FileInfo(file_path);
        }

        public LocalAuditFileInfo(LocalEnvironment env, FileInfo f) : base(env, f.FullName) {}

        #region Protected fields
        private FileInfo file;
        #endregion
    }
}
