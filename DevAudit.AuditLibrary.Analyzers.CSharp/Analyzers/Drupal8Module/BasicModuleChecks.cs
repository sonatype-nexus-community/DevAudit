using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Devsense.PHP.Syntax;
using Devsense.PHP.Syntax.Ast;

using DevAudit.AuditLibrary;

namespace DevAudit.AuditLibrary.Analyzers.CSharp.Analyzers.Drupal8Module
{
    public class BasicModuleChecksAnalyzer : PHPAnalyzer
    {
        #region Constructors
        public BasicModuleChecksAnalyzer(ScriptEnvironment script_env, object workspace, object project, object compilation) : base(script_env, "BasicModuleChecks", workspace, project, compilation)
        {
            this.Summary = "Basic sanity checks for a Drupal 8 module based on " + @"https://www.drupal.org/docs/8/creating-custom-modules";
            this.Description = "";
        }
        #endregion

        #region Overriden methods
        public override Task<AnalyzerResult> Analyze()
        {
            AnalyzerResult ar = new AnalyzerResult() { Analyzer = this };
            if (this.JsonFiles == null || !this.JsonFiles.Any(jf => jf.Name == "composer.json"))
            {
                this.ScriptEnvironment.Info("The module diretory does not contain a composer.json file");
                ar.IsVulnerable = true;
            }
            return Task.FromResult(ar);
        }
        #endregion

        #region Private types
        /*
        public class CheckClassesDeclaredTreeVisitor : TreeVisitor
        {
            public Dictionary<string, bool> ClassesDeclared { get; private set; }

            public CheckClassesDeclaredTreeVisitor(List<string> class_names) : base()
            {
                this.ClassesDeclared = new Dictionary<string, bool>();
                foreach (string cn in class_names)
                {
                    this.ClassesDeclared.Add(cn, false);
                }
            }

            public override void VisitNamedTypeDecl(NamedTypeDecl x)
            {
                base.VisitNamedTypeDecl(x);
               
            }
            
 
        }*/
        #endregion
    }
}
