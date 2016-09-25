using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Alpheus.IO;

namespace DevAudit.AuditLibrary
{
    public class SshAuditDirectoryInfo : AuditDirectoryInfo
    {
        #region Overriden methods
        public override string FullName { get; protected set; }

        public override IDirectoryInfo Parent
        {
            get
            {
                string[] components = this.GetPathComponents();
                if (components.Length == 1)
                {
                    return null;
                }
                else
                {
                    return null;
                }
            }
        }

        public override IDirectoryInfo Root
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override IDirectoryInfo[] GetDirectories()
        {
            string o = this.EnvironmentExecute("find", string.Format("{0} -type d -name \"*\"", this.FullName));
            if (!string.IsNullOrEmpty(o))
            {
                return null;
            }
            else
            {
                EnvironmentCommandError("Could not get directories in {0} for path {1}.", this.FullName, this.FullName);
                return null;
            }
        }

        public override IDirectoryInfo[] GetDirectories(string path, SearchOption search_option)
        {
            throw new NotImplementedException();
        }

        public override IFileInfo[] GetFiles()
        {
            throw new NotImplementedException();
        }

        
        public override IFileInfo[] GetFiles(string path)
        {
            throw new NotImplementedException();
        }

        public override IFileInfo[] GetFiles(string path, SearchOption search_option)
        {
            throw new NotImplementedException();
        }

    
        public override bool Exists
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        #endregion

        #region Constructors
        public override IDirectoryInfo[] GetDirectories(string path)
        {
            string o = this.EnvironmentExecute("find", string.Format("{0} -type d -name \"*\"", this.CombinePaths(this.FullName, path)));
            if (!string.IsNullOrEmpty(o))
            {
                return null;
            }
            else
            {
                EnvironmentCommandError("Could not get directories in {0} for path {1}.", this.CombinePaths(this.FullName, path));
                return null;
            }
        }

        public SshAuditDirectoryInfo(SshAuditEnvironment env, string path) : base(env, path)
        {
            this.SshAuditEnvironment = env;
        }
        #endregion

        #region Protected properties
        protected SshAuditEnvironment SshAuditEnvironment { get; set; }
        #endregion

        #region Private fields
        #endregion
    }
}
