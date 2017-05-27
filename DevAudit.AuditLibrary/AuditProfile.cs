using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
namespace DevAudit.AuditLibrary
{
    public class AuditProfile
    {
        #region Constructors
        public AuditProfile(AuditEnvironment env, AuditFileInfo pf)
        {
            this.AuditEnvironment = env;
            this.ProfileFile = pf;
            this.AuditEnvironment.Info("Using profile file {0}.", pf.FullName);
            Deserializer yaml_deserializer = new Deserializer(namingConvention: new CamelCaseNamingConvention(), ignoreUnmatched: true);
            try
            {
                this.Rules = yaml_deserializer.Deserialize<List<AuditProfileRule>>(new StringReader(this.ProfileFile.ReadAsText()));
                AuditEnvironment.Info("Loaded {0} rule(s) from audit profile.", this.Rules.Count);
            }
            catch (Exception e)
            {
                this.AuditEnvironment.Error(e, "Error occurred reading audit profile from {0}.", this.ProfileFile.FullName);
            }
            
        }
        #endregion

        #region Properties
        public AuditEnvironment AuditEnvironment { get; protected set; }
        public AuditFileInfo ProfileFile { get; protected set; }
        public List<AuditProfileRule> Rules { get; protected set; }
        #endregion
    }
}
