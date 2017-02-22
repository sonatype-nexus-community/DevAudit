using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Alpheus.IO;

namespace DevAudit.AuditLibrary
{
    public class DockerizedLocalAuditFileInfo : AuditFileInfo
    {
        #region Constructors
        public DockerizedLocalAuditFileInfo(DockerizedLocalEnvironment env, string file_path) : base(env, file_path)
        {
            this.DockerizedLocalEnvironment = env;
        }
        #endregion

        #region Overriden properties
        public override string FullName { get; protected set; }

        public override string Name { get; protected set; }

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
                    string c = this.ReadAsText();
                    if (!string.IsNullOrEmpty(c))
                    {
                        this._Length = c.Length;
                    }
                    else
                    {
                        EnvironmentCommandError(this.AuditEnvironment.Here(), "Could not read {0} as text.", this.FullName);
                    }
                }
                return this._Length.HasValue ? this._Length.Value : -1;
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

        public override DateTime LastWriteTimeUtc
        {
            get
            {
                DateTime result;
                string cmd = "stat";
                string args = string.Format("-c '%y' {0}", this.FullName);
                string d = EnvironmentExecute(cmd, args);
                if (DateTime.TryParse(d, out result))
                {
                    return result;
                }
                else
                {
                    EnvironmentCommandError(this.AuditEnvironment.Here(), "Could not parse result of {0} as DateTime: {1}.", cmd + " " + args, d);
                    return new DateTime(1000, 1, 1);
                }
            }
        }

        public override IDirectoryInfo Directory
        {
            get
            {
                if (this._Directory == null)
                {
                    string o = this.EnvironmentExecute("dirname", this.FullName);
                    if (!string.IsNullOrEmpty(o))
                    {
                        this._Directory = new DockerizedLocalAuditDirectoryInfo(this.AuditEnvironment as DockerizedLocalEnvironment, o);
                    }
                    else
                    {
                        EnvironmentCommandError(this.AuditEnvironment.Here(), "Could not get parent directory for {0}.", this.FullName);
                    }
                }
                return this._Directory;
            }
        }
        #endregion

        #region Overriden methods
        public override string ReadAsText()
        {
            string o = this.EnvironmentExecute("cat", this.FullName);

            if (!string.IsNullOrEmpty(o))
            {
                string _byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
                var lastIndexOfUtf8 = _byteOrderMarkUtf8.Length;
                if (o.StartsWith(_byteOrderMarkUtf8, StringComparison.Ordinal))
                {
                    o = o.Remove(0, lastIndexOfUtf8);
                }
                if (o == string.Format("cat: {0}: No such file or directory", this.FullName))
                {
                    EnvironmentCommandError(this.AuditEnvironment.Here(), "Access denied reading {0}.", this.FullName);
                    return string.Empty;
                }
                else
                {
                    this.AuditEnvironment.Debug(this.AuditEnvironment.Here(), "Read {0} characters from file {1}.", o.Length, this.FullName);
                    return o;
                }
            }
            else
            {
                EnvironmentCommandError(this.AuditEnvironment.Here(), "Could not read as text {0}.", this.FullName);
                return string.Empty;
            }
        }

        public override bool PathExists(string file_path)
        {
            string result = this.EnvironmentExecute("stat", this.CombinePaths(this.Directory.FullName, file_path));
            if (!result.Contains("no such file or directory"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override byte[] ReadAsBinary()
        {
            throw new NotImplementedException();
        }

        public override bool IsReadOnly
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override LocalAuditFileInfo GetAsLocalFile()
        {
            if (this.DockerizedLocalEnvironment.HostRootIsMounted)
            {
                return new LocalAuditFileInfo(new LocalEnvironment(), new FileInfo(Path.Combine("/hostroot", this.FullName)));
            }
            else
            {
                return new LocalAuditFileInfo(new LocalEnvironment(), new FileInfo(this.FullName));
            }
        }

        public override Task<LocalAuditFileInfo> GetAsLocalFileAsync()
        {
            return Task.FromResult(GetAsLocalFile());
        }
        #endregion

        #region Properties
        protected DockerizedLocalEnvironment DockerizedLocalEnvironment { get; set; }
        #endregion

        #region Fields
        private long? _Length;
        private IDirectoryInfo _Directory;
        #endregion
    }
}
