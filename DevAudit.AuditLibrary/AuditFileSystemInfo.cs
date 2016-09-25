using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Alpheus.IO;
namespace DevAudit.AuditLibrary
{
    public abstract class AuditFileSystemInfo : IFileSystemInfo
    {
        #region Properties
        public virtual string FullName { get; protected set; }
        public virtual string Name { get; protected set; }
        public string PathSeparator { get; protected set; }
        public AuditEnvironment AuditEnvironment { get; protected set; }
        #endregion

        #region Abstract properties
        public abstract bool Exists { get; }
        #endregion

        #region Protected methods
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

        protected string CombinePaths(params string[] paths)
        {
            return paths.Aggregate((s1, s2) => s1 + this.PathSeparator + s2);
        }

        protected string[] GetPathComponents()
        {
            return this.FullName.Split(this.PathSeparator.ToArray()).ToArray();
        }
        #endregion

    }
}
