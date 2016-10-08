using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Alpheus.IO;

namespace DevAudit.AuditLibrary
{
    public abstract class AuditFileInfo : AuditFileSystemInfo, IFileInfo
    {
        #region Abstract properties
        public abstract string DirectoryName { get; }
        public abstract IDirectoryInfo Directory { get; }
        public abstract bool IsReadOnly { get; }
        public abstract long Length { get; }
        public abstract DateTime LastWriteTimeUtc { get; }
        #endregion

        #region Abstract methods
        public abstract string ReadAsText();
        public abstract byte[] ReadAsBinary();
        public abstract bool PathExists(string file_path);
        public abstract IFileInfo Create(string file_path);
        public abstract LocalAuditFileInfo GetAsLocalFile();
        #endregion

        #region Constructors
        public AuditFileInfo(AuditEnvironment env, string file_path)
        {
            this.AuditEnvironment = env;
            this.FullName = file_path;
            this.PathSeparator = this.AuditEnvironment.OS.Platform == PlatformID.Win32NT ? "\\" : "/";
            this.Name = this.GetPathComponents().Last();
        }
        #endregion

        #region Protected methods
        #endregion
    }
}
