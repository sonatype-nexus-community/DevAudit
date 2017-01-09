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
        public override Task<ByteCodeAnalyzerResult> Analyze()
        {
            IEnumerable<MethodDefinition> methods = 
            from type in this.Module.Types.Cast<TypeDefinition>()
            from method in type.Methods.Cast<MethodDefinition>()
            select method;

            IEnumerable<TypeReference> type_references = this.Module.GetTypeReferences().Where(tr => tr.Name == "SHA1CryptoServiceProvider");

            foreach (TypeReference tr in type_references)
            {
                this.ScriptEnvironment.Info("Type reference to SHA1CryptoServiceProvider found in module {0}", this.Module.Name);
                
            }
            
            foreach (MethodDefinition md in methods)
            {
                this.ScriptEnvironment.Info("Method {0} returns {1}.", md.Name, md.MethodReturnType.ReturnType.Name);
                foreach (Instruction i in md.Body.Instructions.Where(i => i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Callvirt))
                {
                    MethodReference mr = (MethodReference)i.Operand;
                    if (mr.FullName.Contains("System.Security.Cryptography.HashAlgorithm::ComputeHash"))
                    {
                        this.ScriptEnvironment.Info("Detected a call to System.Security.Cryptography.HashAlgorithm::ComputeHash in method {0}", md.FullName);
                        this.AnalyzerResult = new ByteCodeAnalyzerResult()
                        {
                            Analyzer = this,
                            Succeded = true,
                            IsVulnerable = true
                        };
                        return Task.FromResult(this.AnalyzerResult);
                    }
                }
            }

            return Task.FromResult(this.AnalyzerResult);
        }
        #endregion
    }
}