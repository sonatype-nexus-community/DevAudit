using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public abstract class Analyzer : IAnalyzer
    {
        public string Name { get; protected set; }
        public ScriptEnvironment ScriptEnvironment { get; protected set; }

        public Analyzer(ScriptEnvironment script_env)
        {
            this.ScriptEnvironment = script_env;
        }
    }
}
