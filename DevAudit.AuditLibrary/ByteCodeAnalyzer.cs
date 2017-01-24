using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Alpheus;

namespace DevAudit.AuditLibrary
{
    #region Public types
    public struct ByteCodeAnalyzerResult
    {
        public ByteCodeAnalyzer Analyzer;
        public bool Succeded;
        public bool IsVulnerable;
        public string ModuleName;
        public string LocationDescription;
        public List<Exception> Exceptions;
        public List<string> DiagnosticMessages;
    }
    #endregion

    public abstract class ByteCodeAnalyzer
    {
        #region Constructors
        public ByteCodeAnalyzer(ScriptEnvironment script_env, string name, object modules, IConfiguration configuration, Dictionary<string, object> application_options)
        {
            this.ScriptEnvironment = script_env;
            this.Name = name;
            this.AnalyzerResult = new ByteCodeAnalyzerResult() { Analyzer = this, Succeded = false };
            this._Modules = modules;
            this.Configuration = configuration;
            this.ApplicationOptions = application_options;
        }
        #endregion

        #region Public properties
        public string Name { get; protected set; }
        public string Summary { get; protected set; }
        public string Description { get; protected set; }
        public List<string> Tags { get; protected set; } = new List<string>(3);
        public ByteCodeAnalyzerResult AnalyzerResult { get; protected set; }
        protected ScriptEnvironment ScriptEnvironment { get; set; }
        protected object _Modules { get; set; }
        protected IConfiguration Configuration { get; set; }
        protected Dictionary<string, object> ApplicationOptions { get; set; }
        #endregion

        #region Public abstract methods
        public abstract Task<ByteCodeAnalyzerResult> Analyze();
        #endregion
    }
}
