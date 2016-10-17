using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
namespace DevAudit.AuditLibrary
{
    public abstract class RoslynAnalyzer : Analyzer
    {
        #region Constructors
        public RoslynAnalyzer(ScriptEnvironment script_env, string name, object workspace, object project, object compilation) : base(script_env, name, workspace, project, compilation)
        {
            this.Workspace = workspace as Workspace;
            this.Project = project as Project;
            this.Compilation = compilation as Compilation;
            this.Tags.Add("roslyn");
        }
        #endregion

        #region Protected properties
        protected Workspace Workspace { get; set; }
        protected Project Project { get; set; }
        protected Compilation Compilation { get; set; }
        #endregion
    }
}
