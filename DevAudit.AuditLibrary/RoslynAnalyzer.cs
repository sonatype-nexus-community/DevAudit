using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
namespace DevAudit.AuditLibrary
{
    public class RoslynAnalyzer : Analyzer
    {
        #region Constructors
        public RoslynAnalyzer(ScriptEnvironment script_env, object workspace, object project, object compilation) : base(script_env, workspace, project, compilation)
        {
            this.Workspace = workspace as Workspace;
            this.Project = project as Project;
            this.Compilation = compilation as Compilation;
            this.ScriptEnvironment.Info("Initialised roslyn analyzer with {0} syntax trees from compilation of {1} project.", this.Compilation.SyntaxTrees.Count(), this.Project.Name);
        }
        #endregion

        protected Workspace Workspace { get; set; }
        protected Project Project { get; set; }
        protected Compilation Compilation { get; set; }
    }
}
