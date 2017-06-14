using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Alpheus;
using Alpheus.IO;

namespace DevAudit.AuditLibrary
{
    public class IIS6Server : ApplicationServer
    {
        #region Constructors
        public IIS6Server(Dictionary<string, object> server_options, EventHandler<EnvironmentEventArgs> message_handler) : base(server_options, 
            new Dictionary<PlatformID, string[]>()
            {
                { PlatformID.Win32NT, new string[] { "C:", "Windows", "system32", "inetsrv", "inetinfo.exe" } }
            },
            new Dictionary<PlatformID, string[]>()
            {
                { PlatformID.Win32NT, new string[] { "C:\\", "Windows", "system32", "inetsrv", "MetaBase.xml" } }
            }, new Dictionary<string, string[]>(), new Dictionary<string, string[]>(), message_handler)
        {
            if (this.RootDirectory.FullName == this.AuditEnvironment.PathSeparator)
            {
                this.ApplicationFileSystemMap["RootDirectory"] = this.AuditEnvironment.ConstructDirectory(@"C:\Windows\system32\inetsrv");
            }
            if (this.ApplicationBinary != null)
            {
                this.ApplicationFileSystemMap["iis6"] = this.ApplicationBinary;
            }
        }
        #endregion

        #region Overriden properties
        public override string ServerId { get { return "iis6"; } }

        public override string ServerLabel { get { return "IIS 6"; } }

        public override PackageSource PackageSource => this as PackageSource;
        #endregion

        #region Overriden methods
        protected override string GetVersion()
        {
            return "6.0";
        }

        protected override Dictionary<string, IEnumerable<Package>> GetModules()
        {
            Dictionary<string, IEnumerable<Package>> m = new Dictionary<string, IEnumerable<Package>>
            {
                {"iis6", new List<Package> {new Package(this.PackageManagerId, "iis", this.Version) }}
            };
            this.ModulePackages = m;
            this.PackageSourceInitialized = this.ModulesInitialised = true;
            return this.ModulePackages;
        }

        protected override IConfiguration GetConfiguration()
        {
            XMLConfig xml = new XMLConfig(this.ConfigurationFile, this.AlpheusEnvironment);
            if (xml.ParseSucceded)
            {
                this.Configuration = xml;
                this.ConfigurationInitialised = true;
                this.AuditEnvironment.Success(this.ConfigurationStatistics);
            }
            else
            {
                this.AuditEnvironment.Error("Could not parse configuration from {0}.", xml.FullFilePath);
                if (xml.LastParseException != null) this.AuditEnvironment.Error(xml.LastParseException);
                if (xml.LastIOException != null) this.AuditEnvironment.Error(xml.LastIOException);
                this.Configuration = null;
                this.ConfigurationInitialised = false;
            }
            return this.Configuration;
        }

        public override bool IsConfigurationRuleVersionInServerVersionRange(string configuration_rule_version, string server_version)
        {
            return (configuration_rule_version == server_version) || configuration_rule_version == ">0";
        }

        public override IEnumerable<Package> GetPackages(params string[] o)
        {
            if (!this.ModulesInitialised) throw new InvalidOperationException("Modules must be initialised before GetPackages is called.");
            return this.GetModules()["iis6"];
        }

        public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
        {
            return vulnerability_version == package_version;
        }
        #endregion

    }
}
