using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public class AuditFileInfo : IFileInfo
    {
        public AuditEnvironment AuditEnvironment { get; protected set; }

        public string FullName
        {
            get
            {
                return _FullName;
            }
        }
        
        public string Name
        {
            get
            {
                return _FullName;
            }
        }

        public bool IsReadOnly { get; set; }

        public long Length
        {
            get
            {
                if (_Length == -1)
                {
                    string c = this.ReadAsText();
                    if (!string.IsNullOrEmpty(c))
                    {
                        this._Length = c.Length;
                    }
                    else
                    {
                        EnvironmentCommandError("Could not get parent directory for {0}.", this.FullName);
                    }
                }
                return this._Length;
            }
        }
        public IDirectoryInfo Directory
        {
            get
            {
                if (this._Directory == null)
                {
                    string o = this.EnvironmentExecute("dirname", this.FullName);
                    if (!string.IsNullOrEmpty(o))
                    {
                        this._Directory = new AuditDirectoryInfo(this.AuditEnvironment, o);
                    }
                    else
                    {
                        EnvironmentCommandError("Could not get parent directory for {0}.", this.FullName);
                    }
                }
                return this._Directory;
            }
        }

        public string DirectoryName
        {
            get
            {
                if (this.Directory != null)
                {
                    return this.Directory.Name;
                }
                else
                {
                    EnvironmentCommandError("Could not get directory name for {0}.", this.FullName);
                    return string.Empty;
                }
            }
        }

        public string ReadAsText()
        {
            string o = this.EnvironmentExecute("cat", this.FullName);
            if (!string.IsNullOrEmpty(o))
            {
                this.AuditEnvironment.Debug("Read {0} characters from file {1}.", o.Length, this.FullName);
                return o;
            }
            else
            {
                EnvironmentCommandError("Could not read as text {0}.", this.FullName);
                return null;
            }
        }       

        public AuditFileInfo(AuditEnvironment env, string full_name)
        {
            this.AuditEnvironment = env;
            this._FullName = full_name;
        }

        public bool Exists
        {
            get
            {
                return this.AuditEnvironment.FileExists(this.Name);
            }
        }

        #region Private and protected members
        protected string _FullName;
        protected AuditDirectoryInfo _Directory = null;
        public long _Length = -1;

        protected string EnvironmentExecute(string command, string args)
        {
            AuditEnvironment.ProcessExecuteStatus process_status;
            string process_output = "";
            string process_error = "";
            if (this.AuditEnvironment.Execute(command, args, out process_status, out process_output, out process_error))
            {
                this.AuditEnvironment.Debug("Execute {0} returned {1}.", command + " " + args, process_output);
                return process_output;
            }

            else
            {
                this.AuditEnvironment.Debug("Execute returned false for {0}", command + " " + args);
                return string.Empty;
            }

        }

        protected void EnvironmentCommandError(string message_format, params object[] m)
        {
            this.AuditEnvironment.Error(message_format, m);
        }
        #endregion
    }
}
