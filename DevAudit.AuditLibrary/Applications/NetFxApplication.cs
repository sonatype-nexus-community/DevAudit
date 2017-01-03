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
            if (application_options.ContainsKey("PackageSource"))
            {
                this.NugetPackageSource = new NuGetPackageSource(new Dictionary<string, object>(1) { { "File", this.CombinePath((string) application_options["PackageSource"]) } }, 
                    message_handler);
                this.PackageSourceInitialized = true;
                this.AuditEnvironment.Debug("Using NuGet v2 package manager configuration file {0}", (string)application_options["PackageSource"]);
            }
            else
            {
                AuditFileInfo packages_config = this.AuditEnvironment.ConstructFile(this.CombinePath("@packages.config"));
                if (packages_config.Exists)
                {                    
                    this.NugetPackageSource = new NuGetPackageSource(new Dictionary<string, object>(1) { { "File", packages_config.FullName } }, message_handler);
                    this.PackageSourceInitialized = true;
                    this.AuditEnvironment.Debug("Using NuGet v2 package manager configuration file {0}", packages_config.FullName);
                }

                else
                {
                    this.AuditEnvironment.Warning("The default NuGet v2 package manager configuration file {0} does not exist and no PackageSource parameter was specified.", packages_config.FullName);
                    PackageSourceInitialized = false;
                }
            }
            AuditFileInfo cf;
            if (application_options.ContainsKey("AppConfig"))
            {
                cf = this.AuditEnvironment.ConstructFile(this.CombinePath((string)application_options["AppConfig"]));
                if (cf.Exists)
                {
                    this.AppConfigFilePath = cf.FullName;
                    this.AuditEnvironment.Info("Using {0} configuration file {1}.", this.ApplicationLabel, this.AppConfigFilePath);
                }
                else throw new ArgumentException("The configuration file {0} does not exist.", cf.FullName);
            }
            else if (application_options.ContainsKey("ConfigurationFile"))
            {
                cf = this.AuditEnvironment.ConstructFile(this.CombinePath((string)application_options["ConfigurationFile"]));
                if (cf.Exists)
                {
                    this.AppConfigFilePath = cf.FullName;
                    this.AuditEnvironment.Info("Using {0} configuration file {1}.", this.ApplicationLabel, this.AppConfigFilePath);
                }
                else throw new ArgumentException("The configuration file {0} does not exist.", cf.FullName);

            }
            else if (this.ApplicationBinary != null)
            {
                cf = this.AuditEnvironment.ConstructFile(this.ApplicationBinary.FullName + ".config");
                if (!cf.Exists)
                {
                    this.AuditEnvironment.Warning("The default .NET application configuration file {0} does not exist and no AppConfig parameter was specified.", cf.FullName);
                }
                else
                {
                    this.AppConfigFilePath = cf.FullName;
                    this.AuditEnvironment.Info("Using application configuration file {0}.", cf.FullName);
                }
            }
            else
            {
                this.AuditEnvironment.Warning("The default .NET application configuration file could not be determined and no AppConfig parameter was specified.");
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
                this.PackageSourceInitialized = true;
            }
            else throw new ArgumentException("Package source is null.", "package_source");
        }
        #endregion

        #region Overriden properties
        public override string ApplicationId { get; } = "netfx4";

        public override string ApplicationLabel { get; } = ".NET Framework 4";

        public override string PackageManagerId => this.NugetPackageSource?.PackageManagerId;

        public override string PackageManagerLabel => this.NugetPackageSource?.PackageManagerLabel;

        public override PackageSource PackageSource => this.NugetPackageSource;
        #endregion

        #region Overriden methods
        public override IEnumerable<OSSIndexQueryObject> GetPackages(params string[] o)
        {
            if (this.PackageSourceInitialized)
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
            if (this.PackageSourceInitialized)
            {
                return this.NugetPackageSource.IsVulnerabilityVersionInPackageVersionRange(vulnerability_version, package_version);
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
            if (this.AppConfigFilePath == null) throw new InvalidOperationException("The application configuration file was not specified.");
            Stopwatch sw = new Stopwatch();
            sw.Start();
            XMLConfig config = new XMLConfig(this.AppConfigFilePath);
            if (config.ParseSucceded)
            {
                this.Configuration = config;
                sw.Stop();                
                this.AuditEnvironment.Success("Read configuration from {0} in {1} ms.", this.Configuration.File.Name, sw.ElapsedMilliseconds);
            }
            else
            {
                sw.Stop();
                this.Configuration = null;                
                this.AuditEnvironment.Error("Failed to read configuration from {0}. Error: {1}. Time elapsed: {2} ms.",
                    this.AppConfigFilePath, config.LastException.Message, sw.ElapsedMilliseconds);
            }
            return this.Configuration;
        }

        #endregion

        #region Properties
        public NuGetPackageSource NugetPackageSource { get; protected set; }
        public string AppConfigFilePath { get; protected set; }
        #endregion
    }
}
