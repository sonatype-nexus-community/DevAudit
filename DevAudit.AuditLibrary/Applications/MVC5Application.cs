using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary.Applications
{
    public class MVC5Application : Application
    {
        #region Constructors
        public MVC5Application(Dictionary<string, object> application_options, EventHandler<EnvironmentEventArgs> message_handler = null) : base(application_options, new Dictionary<string, string[]>()
            {
                { "WebConfig", new string[] { "@", "Web.config" } },
                { "CorePackagesFile", new string[] { "@", "core", "composer.json" } }
            }, new Dictionary<string, string[]>()
            {
                { "CoreModulesDirectory", new string[] { "@", "core", "modules" } },
                { "ContribModulesDirectory", new string[] { "@", "modules" } },
                { "DefaultSiteDirectory", new string[] { "@", "sites", "default" } }
            }, message_handler)
        { }
        #endregion

        #region Overriden properties

        public override string ApplicationId { get { return "mvc5"; } }

        public override string ApplicationLabel { get { return "ASP.NET MVC 5"; } }

        public override string PackageManagerId { get { return "nuget"; } }

        public override string PackageManagerLabel { get { return "Nugetv2"; } }

        public override OSSIndexHttpClient HttpClient { get; } = new OSSIndexHttpClient("1.1");

        #endregion
    }
}
