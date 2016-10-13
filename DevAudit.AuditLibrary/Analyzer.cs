using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public abstract class Analyzer
    {
        #region Public types
        public struct AuditResult
        {
            public bool Succeded;
            public bool IsVulnerable;
            public Exception Exception;
            public string DiagnosticMessage;
        }
        #endregion

        #region Constructors
        public Analyzer(ScriptEnvironment script_env, string name, object workspace, object project, object compilation)
        {
            this.ScriptEnvironment = script_env;
            this.Name = name;
            this._Workspace = workspace;
            this._Project = project;
            this._Compilation = compilation;
        }
        #endregion

        #region Public properties
        public string Name { get; protected set; }
        protected ScriptEnvironment ScriptEnvironment { get; set; }
        protected object _Workspace { get; set; }
        protected object _Project { get; set; }
        protected object _Compilation { get; set; }
        #endregion

        #region Public abstract methods
        public abstract AuditResult Audit();
        #endregion
    }
}
