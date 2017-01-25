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
            base(script_env, "SA-NETFX-0002-WeakCrypto", modules, configuration, application_options)
        {
            this.Summary = "Weak Cryptography Used";
            this.Description = "Detects when calls are made to built-in .NET cryptography that use ciphers and algorithms considered cryptographically weak and insecure today.";
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
                this.ScriptEnvironment.Debug("Method {0} returns {1}.", md.Name, md.MethodReturnType.ReturnType.Name);
                foreach (Instruction i in md.Body.Instructions.Where(i => i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Callvirt))
                {
                    MethodReference mr = (MethodReference)i.Operand;
                    if (mr.FullName.Contains("System.Security.Cryptography.HashAlgorithm::ComputeHash"))
                    {
                        this.ScriptEnvironment.Info("Detected a call to System.Security.Cryptography.HashAlgorithm::ComputeHash in method {0}", md.FullName);
                        this.AnalyzerResult = new ByteCodeAnalyzerResult()
                        {
                            Analyzer = this,
                            Executed = true,
                            Succeded = true,
                            IsVulnerable = true,
                            ModuleName = this.Module.Name,
                            LocationDescription = md.FullName
                        };
                        return Task.FromResult(this.AnalyzerResult);
                    }
                }
            }
            return Task.FromResult(new ByteCodeAnalyzerResult()
            {
                Analyzer = this,
                ModuleName = this.Module.Name,
                Executed = true,
                Succeded = true,
                IsVulnerable = false,

            });
        }
        #endregion
    }
}