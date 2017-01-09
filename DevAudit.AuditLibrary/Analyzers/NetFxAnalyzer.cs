using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Mono.Cecil;
using Alpheus;
namespace DevAudit.AuditLibrary.Analyzers
{
    public abstract class NetFxAnalyzer : BinaryAnalyzer
    {
        public NetFxAnalyzer(ScriptEnvironment script_env, string name, object modules, IConfiguration configuration, Dictionary<string, object> application_options) : 
            base(script_env, name, modules, configuration, application_options)
        {
            this.Modules = (ModuleDefinition) this._Modules;
        }

        #region Properties
        protected ModuleDefinition Modules { get; set; }
        #endregion
    }
}
