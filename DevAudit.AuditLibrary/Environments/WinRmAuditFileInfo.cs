using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

using Alpheus.IO;

namespace DevAudit.AuditLibrary
{
    public class WinRmAuditFileInfo : AuditFileInfo
    {
        #region Constructors
        public WinRmAuditFileInfo(WinRmAuditEnvironment env, string file_path) : base(env, file_path)
        {
            this.WinRmAuditEnvironment = env;
        }
        #endregion

        #region Overriden properties
        public override string FullName { get; protected set; }

        public override string Name { get; protected set; }

        public override bool IsReadOnly
        {
            get
            {
                if (this.FileInfo != null)
                {
                    return (bool) this.FileInfo.Properties["IsReadOnly"].Value;
                }
                else
                {
                    this.AuditEnvironment.Error("Could not retrieve IsReadOnly property for {0}.", this.FullName);
                    return false;
                }
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
                if (this.FileInfo != null)
                {
                    return (long) this.FileInfo.Properties["Length"].Value;
                }
                else
                {
                    this.AuditEnvironment.Error("Could not retrieve length for {0}.", this.FullName);
                    return 0;
                }
            }
        }

        public override DateTime LastWriteTimeUtc
        {
            get
            {
                if (this.FileInfo != null)
                {
                    return (DateTime) this.FileInfo.Properties["LastWriteTimeUtc"].Value;
                }
                else
                {
                    this.AuditEnvironment.Error("Could not retrieve last write time for {0}.", this.FullName);
                    return DateTime.MinValue;
                }

            }
        }

        public override IDirectoryInfo Directory
        {
            get
            {
                if (this.FileInfo != null)
                {
                    return this.AuditEnvironment.ConstructDirectory(this.DirectoryName);
                }
                else
                {
                    this.AuditEnvironment.Error("Could not retrieve directory for {0}.", this.FullName);
                    return null;
                }
            }
        }

        public override string DirectoryName
        {
            get
            {
                if (this.FileInfo != null)
                {
                    return (string) this.FileInfo.Properties["DirectoryName"].Value;
                }
                else
                {
                    this.AuditEnvironment.Error("Could not retrieve directory name for {0}.", this.FullName);
                    return null;
                }
            }
        }
        #endregion

        #region Overriden methods
        public override bool PathExists(string file_path)
        {
            ICollection<dynamic> r = this.WinRmAuditEnvironment.RunPSScript("@{ param($path) Test-Path $path }", new string[] { file_path });
            if (r == null)
            {
                this.AuditEnvironment.Error("Could not test path {0} exists on {1}.", file_path, this.WinRmAuditEnvironment.Manager.IpAddress);
                return false;
            }
            else
            {
                PSObject o = r.First();
                return (bool)o.BaseObject;
            }
        }

        public override string ReadAsText()
        {
            
            ICollection<dynamic> r = this.WinRmAuditEnvironment.RunPSScript("{ param($file) Get-Content -Path $file | Out-String}", new string[] { this.FullName });
            if (r == null)
            {
                this.AuditEnvironment.Error("Could not read file {0} on {1} as text.", this.FullName, this.WinRmAuditEnvironment.Manager.IpAddress);
                return string.Empty;
            }
            else
            {
                PSObject o = r.First();
                return (string) o.BaseObject;
            }
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
        public PSObject FileInfo
        {
            get
            {
                if (_FileInfo == null)
                {
                    ICollection<dynamic> r = this.WinRmAuditEnvironment.RunPSScript("{New-Object -TypeName System.IO.FileInfo -ArgumentList " + "\"" + this.FullName + "\" }");
                    if (r == null)
                    {
                        this.AuditEnvironment.Error("Could not retrieve file object for path {0} on {1}.", this.FullName, this.WinRmAuditEnvironment.Manager.IpAddress);
                    }
                    else
                    {
                        _FileInfo = r.First();
                        
                    }
                }
                return _FileInfo;
            }
        }
        protected WinRmAuditEnvironment WinRmAuditEnvironment { get; set; }
        #endregion

        #region Fields
        private PSObject _FileInfo;
        #endregion
    }
}
