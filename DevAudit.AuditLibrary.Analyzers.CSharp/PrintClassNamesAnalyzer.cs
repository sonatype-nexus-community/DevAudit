using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;

using DevAudit.AuditLibrary;
using System;

namespace DevAudit.AuditLibrary.Analyzers
{
    public class PrintClassNamesAnalyzer : RoslynAnalyzer
    {
        #region Constructors
        public PrintClassNamesAnalyzer(ScriptEnvironment script_env, object workspace, object project, object compilation) : base(script_env, "Print Class Names", workspace, project, compilation) {}
        #endregion

        #region Public overriden methods
        public override AuditResult Audit()
        {
            throw new NotImplementedException();
        }
        #endregion

    }
}
