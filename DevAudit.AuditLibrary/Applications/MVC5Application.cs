using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public class MVC5Application : NetFx4Application
    {
        #region Constructors
        public MVC5Application(Dictionary<string, object> application_options, EventHandler<EnvironmentEventArgs> message_handler) 
            : base(application_options, message_handler)
        {}

        public MVC5Application(Dictionary<string, object> application_options, EventHandler<EnvironmentEventArgs> message_handler, NuGetPackageSource package_source) : base(application_options, message_handler, package_source)
        {}
        #endregion

        #region Overriden properties
        public override string ApplicationId { get; } = "mvc5";

        public override string ApplicationLabel { get; } = "ASP.NET MVC5";

        public override PackageSource PackageSource => this.NugetPackageSource;
        #endregion
    }
}
