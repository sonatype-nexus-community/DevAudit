using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Xml.Linq;

namespace DevAudit.AuditLibrary
{
    public class AspNetApplication : NetFx4Application, IVulnerableCredentialStore
    {
        #region Constructors
        public AspNetApplication(Dictionary<string, object> application_options, EventHandler<EnvironmentEventArgs> message_handler) 
            : base(application_options, new Dictionary<string, string[]> (), new Dictionary<string, string[]>(), "AspNet", message_handler)
        {}

        public AspNetApplication(Dictionary<string, object> application_options, EventHandler<EnvironmentEventArgs> message_handler, NuGetPackageSource package_source) : 
            base(application_options, new Dictionary<string, string[]> (), new Dictionary<string, string[]>(), "AspNet", message_handler, package_source)
        {}
        #endregion

        #region Overriden properties
        public override string ApplicationId { get; } = "aspnet";

        public override string ApplicationLabel { get; } = "ASP.NET";

        public override PackageSource PackageSource => this as PackageSource;
        #endregion

        #region Methods
        public List<VulnerableCredentialStorage> GetVulnerableCredentialStorage()
        {
            if (!this.ConfigurationInitialised) throw new InvalidOperationException("The ASP.NET application configuration is not initialised.");
            XElement results;
            string message;
            IEnumerable<VulnerableCredentialStorage> cs1;
            if (this.Configuration.XPathEvaluate("//connectionStrings/add", out results, out message))
            {
                cs1 =
                   from e in results.Elements("add")
                   where e.Attribute("connectionString") != null
                   from s in e.Attribute("connectionString").Value.Split(';')
                   where s.ToLower().Trim().StartsWith("password") || s.ToLower().Trim().StartsWith("pwd")
                   select new VulnerableCredentialStorage
                   {
                       File = this.AppConfig.FullName,
                       Location = e.Attribute("Line").Value,
                       Contents = this.Configuration,
                       Value = e.ToString()
                   };
                return cs1.ToList();
            }
            else return null;
            
        }
        #endregion
        }
}
