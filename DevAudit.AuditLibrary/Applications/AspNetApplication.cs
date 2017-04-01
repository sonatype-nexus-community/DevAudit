using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public class AspNetApplication : NetFx4Application
    {
        #region Constructors
        public AspNetApplication(Dictionary<string, object> application_options, EventHandler<EnvironmentEventArgs> message_handler) 
            : base(application_options, new Dictionary<string, string[]> (), new Dictionary<string, string[]>(), "AspNet", message_handler)
        {}

        public AspNetApplication(Dictionary<string, object> application_options, EventHandler<EnvironmentEventArgs> message_handler, NuGetPackageSource package_source) : 
            base(application_options, new Dictionary<string, string[]> { { "AppConfig", new string[] { "@", "Web.config" } } }, new Dictionary<string, string[]>(), "AspNet", message_handler, package_source)
        {}
        #endregion

        #region Overriden properties
        public override string ApplicationId { get; } = "aspnet";

        public override string ApplicationLabel { get; } = "ASP.NET";

        public override PackageSource PackageSource => this as PackageSource;
        #endregion
    }
}
