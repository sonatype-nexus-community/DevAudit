using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Mono.Cecil;

using Gendarme.Framework;
using Gendarme.Framework.Engines;

namespace DevAudit.AuditLibrary
{
    public class DevAuditGendarmeRunner : Runner
    {
        public DevAuditGendarmeRunner(string filename, AuditEnvironment environment)
        {
            string assembly_name = Path.GetFullPath(filename);
            AssemblyDefinition ad = AssemblyDefinition.ReadAssembly(
                assembly_name,
                new ReaderParameters { AssemblyResolver = AssemblyResolver.Resolver });
            // this will force cecil to load all the modules, which will throw (like old cecil) a FNFE
            // if a .netmodule is missing (otherwise this exception will occur later in several places)
            if (ad.Modules.Count > 0)
                Assemblies.Add(ad);         
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
    }
}
