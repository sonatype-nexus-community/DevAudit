using DevAudit.AuditLibrary;

namespace DevAudit.AuditLibrary
{
    public class TestAnalyzer : RoslynAnalyzer
    {
        
        public TestAnalyzer(ScriptEnvironment script_env, object workspace, object project, object compilation) : base(script_env, workspace, project, compilation)
        {
            this.Name = "Print out names";
        }

        #region Public properties
        #endregion

        
    }
}

