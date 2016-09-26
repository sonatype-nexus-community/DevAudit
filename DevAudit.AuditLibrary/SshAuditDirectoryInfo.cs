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
        #region Overriden properties
        public override string FullName { get; protected set; }

        public override string Name { get; protected set; }

        public override IDirectoryInfo Parent
        {
            get
            {
                string[] components = this.GetPathComponents();
                return new SshAuditDirectoryInfo(this.SshAuditEnvironment, components[components.Length - 1]);
            }
        }

        public override IDirectoryInfo Root
        {
            get
            {
                string[] components = this.GetPathComponents();
                return new SshAuditDirectoryInfo(this.SshAuditEnvironment, components[0]);

            }
        }

        public override bool Exists
        {
            get
            {
                string o = this.EnvironmentExecute(string.Format("[ -f {0}] && echo \"Yes\" || echo \"No\"", this.FullName), "");
                if (!string.IsNullOrEmpty(o) && o == "Yes")
                {
                    return true;
                }
                else if (!string.IsNullOrEmpty(o) && o == "No")
                {
                    return false;
                }
                else
                {
                    EnvironmentCommandError(this.AuditEnvironment.Here(), "Could not test for existence of {0}. Command returned: {1}.", this.FullName, o);
                    return false;
                }
            }
        }
        #endregion

        #region Overriden methods
        public override IDirectoryInfo[] GetDirectories()
        {
            string o = this.EnvironmentExecute("find", string.Format("{0} -type d -name \"*\"", this.FullName));
            if (!string.IsNullOrEmpty(o))
            {
                return null;
            }
            else
            {
                EnvironmentCommandError(this.AuditEnvironment.Here(), "Could not get directories in {0} for path {1}.", this.FullName, this.FullName);
                return null;
            }
        }

        public override IDirectoryInfo[] GetDirectories(string path)
        {
            string o = this.EnvironmentExecute("find", string.Format("{0} -type d -name \"*\"", this.CombinePaths(this.FullName, path)));
            if (!string.IsNullOrEmpty(o))
            {
                return null;
            }
            else
            {
                EnvironmentCommandError(this.AuditEnvironment.Here(), "Could not get directories in {0} for path {1}.", this.CombinePaths(this.FullName, path));
                return null;
            }
        }

        public override IDirectoryInfo[] GetDirectories(string path, SearchOption search_option)
        {
            throw new NotImplementedException();
        }

        public override IFileInfo[] GetFiles()
        {
            string o = this.EnvironmentExecute("find", string.Format("{0} -type f -name \"*\"", this.FullName));
            if (!string.IsNullOrEmpty(o))
            {
                return null;
            }
            else
            {
                EnvironmentCommandError(this.AuditEnvironment.Here(), "Could not get files in {0}.", this.FullName);
                return null;
            }

        }

        public override IFileInfo[] GetFiles(string path)
        {
            string o = this.EnvironmentExecute("find", string.Format("{0} -type f -name \"*\"", this.CombinePaths(this.FullName, path)));
            if (!string.IsNullOrEmpty(o))
            {
                return null;
            }
            else
            {
                EnvironmentCommandError(this.AuditEnvironment.Here(), "Could not get files in {0} for path {1}.", this.CombinePaths(this.FullName, path));
                return null;
            }
        }

        public override IFileInfo[] GetFiles(string path, SearchOption search_option)
        {
            throw new NotImplementedException();
        }    
        
        #endregion

        #region Constructors
        public SshAuditDirectoryInfo(SshAuditEnvironment env, string path) : base(env, path)
        {
            this.SshAuditEnvironment = env;
        }
        #endregion

        #region Protected properties
        protected SshAuditEnvironment SshAuditEnvironment { get; set; }
        #endregion

        
    }
}
