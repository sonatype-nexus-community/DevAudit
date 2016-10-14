using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    #region Public types
    public struct AnalyzerResult
    {
        public Analyzer Analyzer;
        public bool Succeded;
        public bool IsVulnerable;
        public List<Exception> Exceptions;
        public List<string> DiagnosticMessages;
    }
    #endregion

    public abstract class Analyzer
    {
        #region Constructors
        public Analyzer(ScriptEnvironment script_env, string name, object workspace, object project, object compilation)
        {
            this.ScriptEnvironment = script_env;
            this.Name = name;
            this.AnalyzerResult = new AnalyzerResult() { Analyzer = this };
            this._Workspace = workspace;
            this._Project = project;
            this._Compilation = compilation;
        }
        #endregion

        #region Public properties
        public string Name { get; protected set; }
        public string Summary { get; protected set; }
        public string Description { get; protected set; }
        public List<string> Tags { get; protected set; } = new List<string>(3);
        public AnalyzerResult AnalyzerResult { get; protected set; }
        protected ScriptEnvironment ScriptEnvironment { get; set; }
        protected object _Workspace { get; set; }
        protected object _Project { get; set; }
        protected object _Compilation { get; set; }
        #endregion

        #region Public abstract methods
        public abstract Task<AnalyzerResult> Analyze();
        
        #endregion
    }
}
