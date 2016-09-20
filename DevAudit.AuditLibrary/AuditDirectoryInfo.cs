using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public class AuditDirectoryInfo : FileSystemInfo, IDirectoryInfo
    {
        public AuditEnvironment AuditEnvironment { get; protected set; }

        #region Overriden members

        public string DirectoryName
        {
            get
            {
                return _Name;
            }
        }

        public long Length
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsReadOnly
        {
            get;
            set;
        }

        public override string FullName
        {
            get
            {
                return _Name;
            }
        }
        
        public override string Name
        {
            get
            {
                return _Name;
            }
        }

        public override bool Exists
        {
            get
            {
                throw new NotImplementedException();
            }
        }


        public override void Delete()
        {
            throw new NotImplementedException();

        }
        #endregion

        public IEnumerable<AuditDirectoryInfo> GetDirectories(string path)
        {
            
            string o = this.EnvironmentExecute("find", string.Format("{0} -type d -name \"*\"", path));
            if (!string.IsNullOrEmpty(o))
            {
                return null;
            }
            else
            {
                EnvironmentCommandError("Could not get directories in {0} for path {1}.", this.FullName, path);
                return null;
            }

          
        }
        public AuditDirectoryInfo(AuditEnvironment env, string name)
        {
            this.AuditEnvironment = env;
            this._Name = name;
        }

        #region Private and protecte members
        protected string _Name;
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
