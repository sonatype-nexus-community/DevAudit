using System;
using DevAudit.AuditLibrary;

namespace DevAudit.AuditLibrary
{
    public class TestAnalyzer : RoslynAnalyzer
    {
        #region Constructors
        public TestAnalyzer(ScriptEnvironment script_env, object workspace, object project, object compilation) : base(script_env, "Print out names", workspace, project, compilation) { }
        #endregion

        #region Public overriden methods
        public override AuditResult Audit()
        {
            throw new NotImplementedException();
        }
        #endregion


    }
}

