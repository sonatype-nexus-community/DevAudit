using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Mono.Cecil;

using Gendarme.Framework;
using Gendarme.Framework.Helpers;

namespace DevAudit.AuditLibrary
{
    public class DevAuditGendarmeRunner : Runner
    {
        public DevAuditGendarmeRunner(AssemblyDefinition assembly, string rules_library_name, ScriptEnvironment environment)
        {
            this.Environment = environment;
            // this will force cecil to load all the modules, which will throw (like old cecil) a FNFE
            // if a .netmodule is missing (otherwise this exception will occur later in several places)
            if (assembly.Modules.Count > 0)
                Assemblies.Add(assembly);
            IgnoreList = new BasicIgnoreList(this);
            LoadRulesFromAssembly(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Locati‌​on), rules_library_name + ".dll"));
        }

        private ScriptEnvironment Environment { get; set; }

        private void LoadRulesFromAssembly(string assemblyName)
        {
            AssemblyName aname = AssemblyName.GetAssemblyName(assemblyName);
            Assembly a = Assembly.Load(aname);
            foreach (Type t in a.GetTypes())
            {
                if (t.IsAbstract || t.IsInterface)
                    continue;

                if (t.FindInterfaces(RuleTypeFilter, "Gendarme.Framework.IRule").Length > 0)
                {
                    IRule r = (IRule)Activator.CreateInstance(t);
                    Rules.Add(r);
                    this.Environment.Info("Using Gendarme rule {0}.", r.Name);
                }
            }
        }

      
        public void Execute()
        {
            Initialize();
            Run();
            TearDown();         
        }

        protected override void OnAssembly(RunnerEventArgs e)
        {
            base.OnAssembly(e);
        }

        protected override void OnMethod(RunnerEventArgs e)
        {
            base.OnMethod(e);
        }

        protected override void OnType(RunnerEventArgs e)
        {
            base.OnType(e);
        }

        private static bool RuleFilter(Type type, object interfaceName)
        {
            return (type.ToString() == (interfaceName as string));
        }

        private static TypeFilter RuleTypeFilter = new TypeFilter(RuleFilter);
    }
}
