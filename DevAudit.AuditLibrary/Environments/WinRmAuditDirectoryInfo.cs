using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

using Alpheus.IO;

namespace DevAudit.AuditLibrary
{
    public class WinRmAuditDirectoryInfo : AuditDirectoryInfo
    {
        #region Constructors
        public WinRmAuditDirectoryInfo(WinRmAuditEnvironment env, string path) : base(env, path)
        {
            this.WinRmAuditEnvironment = env;
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
                return new WinRmAuditDirectoryInfo(this.WinRmAuditEnvironment, components[components.Length - 1]);
            }
        }

        public override IDirectoryInfo Root
        {
            get
            {
                string[] components = this.GetPathComponents();
                return new WinRmAuditDirectoryInfo(this.WinRmAuditEnvironment, components[0]);
            }
        }

        public override bool Exists
        {
            get
            {
                if (this.DirInfo != null)
                {
                    return (bool)this.DirInfo.Properties["Exists"].Value;
                }
                else
                {
                    this.AuditEnvironment.Error("Could not retrieve Exists property for {0}.", this.FullName);
                    return false;
                }
            }
        }
        #endregion

        #region Overriden methods
        public override IDirectoryInfo[] GetDirectories()
        {
            ICollection<dynamic> r = this.WinRmAuditEnvironment.RunPSScript("{$d = New-Object -TypeName System.IO.DirectoryInfo -ArgumentList " + "\"" + this.FullName + "\"\r\n" +
            "$d.GetDirectories() | Select-Object -Property FullName \r\n}", true);
            if (r != null)
            {
                return r.Select(f => this.AuditEnvironment.ConstructDirectory((string)((PSObject)f).Properties["FullName"].Value)).ToArray();
            }
            else
            {
                this.AuditEnvironment.Error("Could not get files for {0}.", this.FullName);
                return null;
            }
        }

        public override IDirectoryInfo[] GetDirectories(string path)
        {
            ICollection<dynamic> r = this.WinRmAuditEnvironment.RunPSScript("{(New-Object -TypeName System.IO.DirectoryInfo -ArgumentList " + "\"" + this.FullName + "\").GetDirectories(\"" + path + "\", 1) | Select-Object -Property FullName}", true);
            if (r != null)
            {
                return r.Select(f => this.AuditEnvironment.ConstructDirectory((string)((PSObject)f).Properties["FullName"].Value)).ToArray();
            }
            else
            {
                this.AuditEnvironment.Error("Could not get files for path {0} in {1}.", path, this.FullName);
                return null;
            }

        }

        public override IDirectoryInfo[] GetDirectories(string path, SearchOption search_option)
        {
            ICollection<dynamic> r = this.WinRmAuditEnvironment.RunPSScript("{$d = New-Object -TypeName System.IO.DirectoryInfo -ArgumentList " + "\"" + this.FullName + "\"\r\n" +
                "$d.GetDirectories(\"" + path + "\"," + (search_option == SearchOption.AllDirectories ? "1" : "0") + ") | Select-Object -Property FullName \r\n}", true);
            if (r != null)
            {
                return r.Select(f => this.AuditEnvironment.ConstructDirectory((string)((PSObject)f).Properties["FullName"].Value)).ToArray();
            }
            else
            {
                this.AuditEnvironment.Error("Could not get files for path {0} in {1}.", path, this.FullName);
                return null;
            }
        }

        public override IFileInfo[] GetFiles()
        {
            ICollection<dynamic> r = this.WinRmAuditEnvironment.RunPSScript("{$d = New-Object -TypeName System.IO.DirectoryInfo -ArgumentList " + "\"" + this.FullName + "\"\r\n" + 
                "$d.GetFiles() | Select-Object -Property FullName \r\n}", true);
            if (r != null)
            {
                return r.Select(f => this.AuditEnvironment.ConstructFile((string)((PSObject)f).Properties["FullName"].Value)).ToArray();
            }
            else
            {
                this.AuditEnvironment.Error("Could not get files for {0}.", this.FullName);
                return null;
            }
        }

        public override IFileInfo[] GetFiles(string path)
        {
            ICollection<dynamic> r = this.WinRmAuditEnvironment.RunPSScript("{(New-Object -TypeName System.IO.DirectoryInfo -ArgumentList " + "\"" + this.FullName + "\").GetFiles(\"" + path + "\", 1) | Select-Object -Property FullName}", true);
            if (r != null)
            {
                return r.Select(f => this.AuditEnvironment.ConstructFile((string)((PSObject)f).Properties["FullName"].Value)).ToArray();
            }
            else
            {
                this.AuditEnvironment.Error("Could not get files for path {0} in {1}.", path, this.FullName);
                return null;
            }
        }

        public override IFileInfo[] GetFiles(string path, SearchOption search_option)
        {
            ICollection<dynamic> r = this.WinRmAuditEnvironment.RunPSScript("{$d = New-Object -TypeName System.IO.DirectoryInfo -ArgumentList " + "\"" + this.FullName + "\"\r\n" +
                    "$d.GetFiles(\"" + path + "\"," + (search_option == SearchOption.AllDirectories ? "1" : "0") + ") | Select-Object -Property FullName \r\n}", true);
            if (r != null)
            {
                return r.Select(f => this.AuditEnvironment.ConstructFile((string)((PSObject)f).BaseObject)).ToArray();
            }
            else
            {
                this.AuditEnvironment.Error("Could not get files for path {0} in {1}.", path, this.FullName);
                return null;
            }
        }

        public override AuditFileInfo GetFile(string file_path)
        {
            if (this.AuditEnvironment.FileExists(this.CombinePaths(this.FullName, file_path)))
            {
                return this.AuditEnvironment.ConstructFile(this.CombinePaths(this.FullName, file_path));
            }
            else
            {
                this.AuditEnvironment.Warning("Could not get file for path {0}.", this.CombinePaths(this.FullName, file_path));
                return null;
            }
 
        }

        public override Dictionary<AuditFileInfo, string> ReadFilesAsText(IEnumerable<AuditFileInfo> files)
        {
            return this.WinRmAuditEnvironment.ReadFilesAsText(files.ToList());
        }

        public override async Task<Dictionary<AuditFileInfo, string>> ReadFilesAsTextAsync(IEnumerable<AuditFileInfo> files)
        {
            return await Task.Run(() => this.WinRmAuditEnvironment.ReadFilesAsText(files.ToList()));
        }

        public override Dictionary<AuditFileInfo, string> ReadFilesAsText(string searchPattern)
        {
            return this.WinRmAuditEnvironment.ReadFilesAsText(
                this.GetFiles(searchPattern).Select(f => f as AuditFileInfo).ToList());
        }

        public override async Task<Dictionary<AuditFileInfo, string>> ReadFilesAsTextAsync(string searchPattern)
        {
            return await Task.Run(() => this.WinRmAuditEnvironment.ReadFilesAsText(this.GetFiles(searchPattern).Select(f => f as AuditFileInfo).ToList()));
        }

        public override LocalAuditDirectoryInfo GetAsLocalDirectory()
        {
            DirectoryInfo d = this.WinRmAuditEnvironment.GetDirectoryAsLocal(this.FullName, Path.Combine(this.AuditEnvironment.WorkDirectory.FullName, this.Name));
            if (d != null)
            {
                return new LocalAuditDirectoryInfo(d);
            }
            else return null;
        }

        public override async Task<LocalAuditDirectoryInfo> GetAsLocalDirectoryAsync()
        {
            DirectoryInfo d = await Task.Run(() => this.WinRmAuditEnvironment.GetDirectoryAsLocal(this.FullName, Path.Combine(this.AuditEnvironment.WorkDirectory.FullName, this.Name)));
            if (d != null)
            {
                return new LocalAuditDirectoryInfo(d);
            }
            else return null;
        }

        #endregion

        #region Properties
        public PSObject DirInfo
        {
            get
            {
                if (_DirInfo == null)
                {
                    ICollection<dynamic> r = this.WinRmAuditEnvironment.RunPSScript("@{ param($dir) New-Object System.IO.DirectoryInfo -ArgumentList $dir }", true, new string[] { this.FullName });
                    if (r == null)
                    {
                        this.AuditEnvironment.Error("Could not retrieve directory object for path {0} on {1}.", this.FullName, this.WinRmAuditEnvironment.Manager.IpAddress);
                    }
                    else
                    {
                        _DirInfo = r.First();
                    }
                }
                return _DirInfo;
            }
        }
        protected WinRmAuditEnvironment WinRmAuditEnvironment { get; set; }

        #endregion

        #region Fields
        private PSObject _DirInfo;
        #endregion
    }
}

