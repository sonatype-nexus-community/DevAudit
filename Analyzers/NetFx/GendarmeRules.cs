using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Mono.Cecil;
using Mono.Cecil.Cil;

using DevAudit.AuditLibrary;
using Alpheus;

namespace DevAudit.AuditLibrary.Analyzers
{
    public class GendarmeRulesAnalyzer : NetFxAnalyzer
    {
        #region Constructors
        public GendarmeRulesAnalyzer(ScriptEnvironment script_env, object modules, IConfiguration configuration, Dictionary<string, object> application_options) : 
            base(script_env, "SA-NETFX-0001-GendarmeRules", modules, configuration, application_options)
        {
            this.Summary = "Gendarme Rules Analyzer";
            this.Description = "Runs static analysis rules from the Mono Project Gendarme project. Use the -o GendarmeRules=Library to specify the rules library to use.";
        }
        #endregion

        #region Overriden methods
        public override Task<ByteCodeAnalyzerResult> Analyze()
        {
            if (!this.ApplicationOptions.ContainsKey("GendarmeRules"))
            {
                return Task.FromResult(this.AnalyzerResult);
            }
            try
            {
                DevAuditGendarmeRunner runner = new DevAuditGendarmeRunner(this.Module.Assembly, (string)this.ApplicationOptions["GendarmeRules"], this.ScriptEnvironment);
                runner.Execute();
                // casting to Severity int saves a ton of memory since IComparable<T> can be used instead of IComparable
                var query = from n in runner.Defects
                            orderby (int)n.Severity, n.Rule.Name
                            select n;
                if (query.Any())
                {
                    List<string> diagnostics = query.Select(d => string.Format("Name: {0} Problem: {1} Location: {2}", d.Rule.Name, d.Rule.Problem, d.Location)).ToList();
                    return Task.FromResult(new ByteCodeAnalyzerResult()
                    {
                        Analyzer = this,
                        Executed = true,
                        Succeded = true,
                        IsVulnerable = true,
                        DiagnosticMessages = diagnostics,
                        LocationDescription = "See diagnostics",
                        ModuleName = this.Module.Name
                    });
                }
                else
                {
                    return Task.FromResult(new ByteCodeAnalyzerResult()
                    {
                        Analyzer = this,
                        Executed = true,
                        Succeded = true,
                        IsVulnerable = false,
                        ModuleName = this.Module.Name
                    });
                }
            }
            catch (Exception e)
            {
                ScriptEnvironment.Error(e);
                return Task.FromResult(new ByteCodeAnalyzerResult()
                {
                    Analyzer = this,
                    Executed = true,
                    Succeded = false,
                    Exceptions = new List<Exception>() { e },
                    IsVulnerable = false,
                    ModuleName = this.Module.Name
                });
            }

        }
        #endregion
    }
}