using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Alpheus;

namespace DevAudit.AuditLibrary
{
    public class NetFx4Application : Application
    {
        #region Constructors
        public NetFx4Application(Dictionary<string, object> application_options, EventHandler<EnvironmentEventArgs> message_handler) : base(application_options, new Dictionary<string, string[]>(),           
            new Dictionary<string, string[]>(), message_handler)
        {
            if (application_options.ContainsKey("File"))
            {
                this.NugetPackageSource = new NuGetPackageSource(application_options, message_handler);
                this.PackageSourceInitialised = true;
            }
        }

        public NetFx4Application(Dictionary<string, object> application_options, Dictionary<string, string[]> required_files,
            Dictionary<string, string[]> required_directories, EventHandler<EnvironmentEventArgs> message_handler) : base(application_options,
                required_files, required_directories, message_handler)
        { }

        public NetFx4Application(Dictionary<string, object> application_options, EventHandler<EnvironmentEventArgs> message_handler, NuGetPackageSource package_source) : this(application_options, message_handler)
        {
            if (package_source != null)
            {
                this.NugetPackageSource = package_source;
                this.PackageSourceInitialised = true;
            }
        }
        #endregion

        #region Overriden properties
        public override string ApplicationId { get; } = "netfx4";

        public override string ApplicationLabel { get; } = ".NET Framework 4";

        public override string PackageManagerId => this.NugetPackageSource?.PackageManagerId;

        public override string PackageManagerLabel => this.NugetPackageSource?.PackageManagerLabel;
        #endregion

        #region Overriden methods
        public override IEnumerable<OSSIndexQueryObject> GetPackages(params string[] o)
        {
            if (this.PackageSourceInitialised)
            {
                return this.NugetPackageSource.GetPackages(o);
            }
            else
            {
                throw new InvalidOperationException("The package source is not intialized.");
            }
        }

        public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
        {
            if (this.PackageSourceInitialised)
            {
                return this.IsVulnerabilityVersionInPackageVersionRange(vulnerability_version, package_version);
            }
            else throw new InvalidOperationException("The package source is not intialized.");
        }

        public override bool IsConfigurationRuleVersionInServerVersionRange(string configuration_rule_version, string server_version)
        {
            return configuration_rule_version == server_version;
        }

        protected override string GetVersion()
        {
            throw new NotImplementedException();
        }

        protected override Dictionary<string, IEnumerable<OSSIndexQueryObject>> GetModules()
        {
            throw new NotImplementedException();
        }

        protected override IConfiguration GetConfiguration()
        {
            this.AuditEnvironment.Status("Scanning {0} configuration.", this.ApplicationLabel);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            XMLConfig xml = new XMLConfig(this.ConfigurationFile);
            if (xml.ParseSucceded)
            {
                this.Configuration = xml;
                sw.Stop();
                this.AuditEnvironment.Success("Read configuration from {0} in {1} ms.", this.Configuration.File.Name, sw.ElapsedMilliseconds);
            }
            return this.Configuration;
        }
        #endregion

        #region Properties
        public NuGetPackageSource NugetPackageSource { get; protected set; }
        public string ConfigurationFile { get; protected set; }
        #endregion
    }
}
