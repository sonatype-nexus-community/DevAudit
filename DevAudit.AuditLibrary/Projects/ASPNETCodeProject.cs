using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;

namespace DevAudit.AuditLibrary
{
    public class AspNetCodeProject : NetFxCodeProject
    {
        public AspNetCodeProject(Dictionary<string, object> project_options, EventHandler<EnvironmentEventArgs> message_handler) : 
            base(project_options, new Dictionary<string, string[]> { { "AppConfig", new string[] {"@", "Web.config" } } }, "AspNet", message_handler)
        {}

        #region Overriden methods
        protected override Application GetApplication()
        {
            Dictionary<string, object> application_options = new Dictionary<string, object>()
            {
                { "RootDirectory", this.ProjectDirectory.FullName },
                { "AppConfig", this.AppConfigurationFile.FullName },
                { "AppDevMode", true }

            };

            foreach (KeyValuePair<string, object> kv in CodeProjectOptions.Where(o => o.Key == "SkipPackagesAudit" || o.Key == "ListPackages" || o.Key == "ListArtifacts" || o.Key == "ApplicationBinary"))
            {
                application_options.Add(kv.Key, kv.Value);
            }

            try
            {
                this.Application = new AspNetApplication(application_options, message_handler, this.PackageSource as NuGetPackageSource);
                this.ApplicationInitialised = true;
            }
            catch (Exception e)
            {
                this.AuditEnvironment.Error(e, "Error attempting to create ASP.NET MVC5 application audit target.");
                this.ApplicationInitialised = false;
                this.Application = null;
            }
            return this.Application;
        }
        #endregion

        #region Methods

        #endregion
    }
}
