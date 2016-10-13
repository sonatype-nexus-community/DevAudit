using DevAudit.AuditLibrary;

namespace DevAudit.AuditLibrary
{
    public class TestAnalyzer : Analyzer
    {
        public TestAnalyzer(ScriptEnvironment script_env) : base(script_env)
        {
            this.Name = "Print out names";
            this.ScriptEnvironment.Info("Initialized test analyzer");
        }

        public void PrintStatistics()
        {

        }
    }
}

