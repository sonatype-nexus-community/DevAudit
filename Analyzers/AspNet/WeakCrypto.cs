using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Mono.Cecil;

using DevAudit.AuditLibrary;
using Alpheus;

namespace DevAudit.AuditLibrary.Analyzers
{
    public class WeakCryptoAnalyzer : NetFxAnalyzer
    {
        #region Constructors
        public WeakCryptoAnalyzer(ScriptEnvironment script_env, object modules, IConfiguration configuration, Dictionary<string, object> application_options) : 
            base(script_env, "WeakCrypto", modules, configuration, application_options)
        {
            this.Summary = "The name of a C# element does not begin with an upper-case letter.";
            this.Description = "A violation of this rule occurs when the names of certain types of elements do not begin with an upper-case letter. The following types of elements should use an upper-case letter as the first letter of the element name: namespaces, classes, enums, structs, delegates, events, methods, and properties. In addition, any field which is public, internal, or marked with the const attribute should begin with an upper-case letter. Non-private readonly fields must also be named using an upper-case letter. If the field or variable name is intended to match the name of an item associated with Win32 or COM, and thus needs to begin with a lower-case letter, place the field or variable within a special NativeMethods class. A <c>NativeMethods</c> class is any class which contains a name ending in NativeMethods, and is intended as a placeholder for Win32 or COM wrappers. StyleCop will ignore this violation if the item is placed within a NativeMethods class.";
        }
        #endregion

        #region Overriden methods
        public override async Task<BinaryAnalyzerResult> Analyze()
        {
            List<AssemblyNameReference> references = this.Modules.AssemblyReferences.Where(ar => ar.Name != "mscorlib" && !ar.Name.StartsWith("System")).ToList();
            this.ScriptEnvironment.Info("Got {0} assembly references", references.Count);
            return this.AnalyzerResult;
        }
        #endregion
    }
}