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
    public class WeakCrypto2Analyzer : NetFxAnalyzer
    {
        #region Constructors
        public WeakCrypto2Analyzer(ScriptEnvironment script_env, object modules, IConfiguration configuration, Dictionary<string, object> application_options) : 
            base(script_env, "SA0001", modules, configuration, application_options)
        {
            this.Summary = "Weak Cryptography Used";
            this.Description = "Detects when calls are made to built-in .NET cryptography that use ciphers and algorithms considered cryptographically weak and insecure today.";
        }
        #endregion

        #region Overriden methods
        public override Task<ByteCodeAnalyzerResult> Analyze()
        {

            IEnumerable<TypeReference> type_references = this.Module.GetTypeReferences().Where(tr => tr.Name == "SHA1CryptoServiceProvider");

            foreach (TypeReference tr in type_references)
            {
                this.ScriptEnvironment.Info("Type reference to SHA1CryptoServiceProvider found in module {0}", this.Module.Name);
            }
            return Task.FromResult(this.AnalyzerResult);
        }
        #endregion
    }
}