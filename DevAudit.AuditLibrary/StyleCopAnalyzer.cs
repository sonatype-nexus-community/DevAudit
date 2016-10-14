using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace DevAudit.AuditLibrary.Analyzers
{
    public abstract class StyleCopAnalyzer : RoslynAnalyzer
    {
        #region Constructors
        public StyleCopAnalyzer(ScriptEnvironment script_env, string name, object workspace, object project, object compilation) : base(script_env, name, workspace, project, compilation)
        {
            this.Tags.Add("stylecop");
        }
        #endregion
    }
}
